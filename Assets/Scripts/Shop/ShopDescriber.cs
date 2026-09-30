using System.Collections.Generic;
using System.Text;
using BulletHell.Core;
using BulletHell.Weapons;

namespace BulletHell.Shop
{
    /// <summary>
    /// The text of a Shop card tooltip, built from the item data (nothing here is written by hand): the M9a generated
    /// description, tags, stack limit, which of the player's arms it fits and the stats before and after. The detailed
    /// version (Square) adds every stat and the resulting shot behaviour.
    /// </summary>
    public static class ShopDescriber
    {
        public static List<string> Lines(in ShopEntry entry, RunState state, ShopTuning tuning, bool detailed)
        {
            switch (entry.Kind)
            {
                case ShopItemKind.Arm: return ArmLines(entry.Arm, detailed);
                case ShopItemKind.Armament: return ArmamentLines(entry.Armament, state, detailed);
                default: return CrateLines(tuning);
            }
        }

        public static List<string> ArmLines(WeaponArmData arm, bool detailed)
        {
            var lines = new List<string>
            {
                arm.DisplayName + " - " + arm.Rarity + " arm",
                "Goes to your spare arms; place it in the Armory.",
                arm.ArmamentSlots + (arm.ArmamentSlots == 1 ? " armament slot" : " armament slots"),
                "damage " + Number(arm.Damage) + "   fire rate " + Number(arm.FireRate),
            };
            if (detailed)
                lines.Add("bullet speed " + Number(arm.ProjectileSpeed) + "   projectiles " + arm.ProjectilesPerShot + "   spread " + Number(arm.Spread));
            for (int i = 0; i < arm.Effects.Count; i++)
                if (arm.Effects[i] != null)
                    arm.Effects[i].Describe(1, lines);
            return lines;
        }

        public static List<string> ArmamentLines(ArmamentData armament, RunState state, bool detailed)
        {
            List<string> lines = ItemDescriber.ArmamentLines(armament);
            lines.Add("");

            List<ArmFit> fits = ShopFit.ForArmament(state, armament);
            if (fits.Count == 0)
                lines.Add("Fits: you have no arms");
            bool shownChange = false;
            foreach (ArmFit fit in fits)
            {
                if (fit.CanEquip)
                {
                    lines.Add("Fits: " + fit.Label + " (" + fit.FreeSlots + " free " + (fit.FreeSlots == 1 ? "slot" : "slots") + ")");
                    if (detailed || !shownChange)
                    {
                        string change = StatChange(fit.Before, fit.After, detailed);
                        if (change.Length > 0)
                            lines.Add("   " + change);
                        if (detailed)
                        {
                            string shot = ShotChange(fit.Arm, armament);
                            if (shot.Length > 0)
                                lines.Add("   shot: " + shot);
                        }
                        shownChange = true;
                    }
                }
                else if (detailed)
                {
                    lines.Add("No fit: " + fit.Label + " (" + fit.Reason + ")");
                }
            }
            bool anyFit = false;
            foreach (ArmFit fit in fits)
                anyFit |= fit.CanEquip;
            if (fits.Count > 0 && !anyFit)
                lines.Add("No arm has room for it (" + fits[0].Reason + ")");
            return lines;
        }

        public static List<string> CrateLines(ShopTuning tuning) => new List<string>
        {
            "Harvest Crate",
            "Open it and pick 1 of " + (tuning != null ? tuning.CrateChoices : 3) + " armaments.",
        };

        /// <summary>"damage 5 -> 5.5, bullet speed 18 -> 22.5": only the stats that change (or all of them when detailed).</summary>
        public static string StatChange(in ArmStats before, in ArmStats after, bool all)
        {
            var builder = new StringBuilder();
            void Add(string name, float a, float b)
            {
                if (!all && System.Math.Abs(a - b) < 0.0001f)
                    return;
                builder.Append(builder.Length > 0 ? ",  " : "").Append(name).Append(' ').Append(Number(a)).Append(" -> ").Append(Number(b));
            }
            Add("damage", before.Damage, after.Damage);
            Add("fire rate", before.FireRate, after.FireRate);
            Add("bullet speed", before.ProjectileSpeed, after.ProjectileSpeed);
            Add("projectiles", before.ProjectilesPerShot, after.ProjectilesPerShot);
            Add("spread", before.Spread, after.Spread);
            return builder.ToString();
        }

        // The arm's resulting shot behaviour with the armament added, e.g. "pierce 1, homing 120 deg/s in 60 deg".
        private static string ShotChange(ArmInstance arm, ArmamentData armament)
        {
            var probe = new ArmInstance(arm.Data);
            for (int i = 0; i < arm.SlotCount; i++)
                if (arm.GetArmament(i) != null)
                    probe.TryAdd(arm.GetArmament(i));
            probe.TryAdd(armament);
            return ItemDescriber.ShotSummary(probe.Shot);
        }

        private static string Number(float value) => value.ToString("0.##");
    }
}
