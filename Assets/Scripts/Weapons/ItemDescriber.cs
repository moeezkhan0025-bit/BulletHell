using System.Collections.Generic;
using System.Text;

namespace BulletHell.Weapons
{
    /// <summary>
    /// Descriptions of arms, armaments and stats for the Shop/Armory UI and the debug overlay. Armament text is generated
    /// from the armament data (stat modifiers, effects, rarity, tags, stack limit), so it never goes stale.
    /// </summary>
    public static class ItemDescriber
    {
        /// <summary>One line: name, rarity and the effect names, e.g. "Pierce [Rare] &lt;Pierce&gt;".</summary>
        public static string Armament(ArmamentData armament)
        {
            if (armament == null)
                return "(empty)";
            return armament.DisplayName + " [" + armament.Rarity + "]" + EffectSuffix(armament.Effects);
        }

        /// <summary>Everything a tooltip shows, one line per entry: rarity and tags, stat changes, effects, stack limit.</summary>
        public static List<string> ArmamentLines(ArmamentData armament, int stacks = 1)
        {
            var lines = new List<string>();
            if (armament == null)
                return lines;

            lines.Add(armament.DisplayName + " - " + armament.Rarity + TagText(armament.Tags));
            for (int i = 0; i < armament.Modifiers.Count; i++)
                lines.Add(ModifierText(armament.Modifiers[i]));
            for (int i = 0; i < armament.Effects.Count; i++)
                if (armament.Effects[i] != null)
                    armament.Effects[i].Describe(stacks, lines);
            lines.Add("Max " + armament.MaxStacks + (armament.MaxStacks == 1 ? " per arm" : " per arm"));
            return lines;
        }

        /// <summary>The tooltip text: <see cref="ArmamentLines"/> joined with new lines.</summary>
        public static string ArmamentFull(ArmamentData armament, int stacks = 1) => string.Join("\n", ArmamentLines(armament, stacks));

        /// <summary>The generated text of one stat modifier: "+25% bullet speed", "+1 projectiles", "-20% spread".</summary>
        public static string ModifierText(in StatModifier modifier)
        {
            string sign = modifier.Value >= 0f ? "+" : "-";
            string amount = Number(System.Math.Abs(modifier.Value));
            return sign + amount + (modifier.Mode == ModifierMode.Percent ? "%" : "") + " " + StatName(modifier.Stat);
        }

        public static string Arm(WeaponArmData arm)
        {
            if (arm == null)
                return "(empty)";
            return arm.DisplayName + EffectSuffix(arm.Effects) + "  (dmg " + Number(arm.Damage) +
                   ", rate " + Number(arm.FireRate) + ", proj " + arm.ProjectilesPerShot + ", " + arm.ArmamentSlots + " slots)";
        }

        /// <summary>An arm instance with the armaments it carries: "Name [a | b | -]" (one entry per armament slot of this arm).</summary>
        public static string ArmWithArmaments(ArmInstance arm)
        {
            var builder = new StringBuilder(arm.Data.DisplayName);
            builder.Append(EffectSuffix(arm.Data.Effects)).Append("  [");
            for (int i = 0; i < arm.SlotCount; i++)
            {
                if (i > 0)
                    builder.Append(" | ");
                ArmamentData armament = arm.GetArmament(i);
                builder.Append(armament != null ? armament.DisplayName : "-");
            }
            return builder.Append(']').ToString();
        }

        /// <summary>What the arm's effects add up to, e.g. "pierce 2, bounces 3, homing 180 deg/s in 90 deg". Empty when none.</summary>
        public static string ShotSummary(in ShotProperties shot)
        {
            var builder = new StringBuilder();
            void Add(string text) => builder.Append(builder.Length > 0 ? ", " : "").Append(text);
            if (shot.Pierce > 0)
                Add("pierce " + shot.Pierce);
            if (shot.Bounces > 0)
                Add("bounces " + shot.Bounces);
            if (shot.HasHoming)
                Add("homing " + Number(shot.HomingTurnRate) + " deg/s in " + Number(shot.HomingCone) + " deg");
            if (shot.HasAutoFire)
                Add("auto-fire " + Number(shot.AutoFireRate * 100f) + "% / " + Number(shot.AutoFireArc * 2f) + " deg");
            return builder.ToString();
        }

        public static string Stats(in ArmStats stats) =>
            "dmg " + Number(stats.Damage) + "  rate " + Number(stats.FireRate) + "  speed " + Number(stats.ProjectileSpeed) +
            "  proj " + stats.ProjectilesPerShot + "  spread " + Number(stats.Spread);

        public static string StatName(StatType stat)
        {
            switch (stat)
            {
                case StatType.Damage: return "damage";
                case StatType.FireRate: return "fire rate";
                case StatType.ProjectileSpeed: return "bullet speed";
                case StatType.ProjectilesPerShot: return "projectiles";
                case StatType.Spread: return "spread";
                default: return stat.ToString();
            }
        }

        private static string TagText(ArmamentTags tags)
        {
            if (tags == ArmamentTags.None)
                return "";
            var builder = new StringBuilder("  (");
            bool first = true;
            foreach (ArmamentTags tag in System.Enum.GetValues(typeof(ArmamentTags)))
            {
                if (tag == ArmamentTags.None || (tags & tag) != tag)
                    continue;
                builder.Append(first ? "" : ", ").Append(tag);
                first = false;
            }
            return builder.Append(')').ToString();
        }

        private static string EffectSuffix(IReadOnlyList<ArmEffect> effects)
        {
            StringBuilder builder = null;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] == null)
                    continue;
                builder = builder == null ? new StringBuilder(" <").Append(effects[i].DisplayName)
                                          : builder.Append(", ").Append(effects[i].DisplayName);
            }
            return builder == null ? "" : builder.Append('>').ToString();
        }

        private static string Number(float value) => value.ToString("0.##");
    }
}
