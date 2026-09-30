using System.Collections.Generic;
using BulletHell.Shop;
using BulletHell.Weapons;

namespace BulletHell.Armory
{
    /// <summary>What equipping (or removing) an armament in a chosen slot of an arm would do.</summary>
    public struct ArmoryChange
    {
        /// <summary>The armament can go into that slot (the slot exists and the stack limit allows it).</summary>
        public bool CanEquip;
        /// <summary>Why it cannot (empty when it can).</summary>
        public string Reason;
        public ArmStats Before;
        public ArmStats After;
        /// <summary>Shot behaviour (pierce, bounces, homing, auto-fire) before and after, e.g. "pierce 1". Empty when none.</summary>
        public string ShotBefore;
        public string ShotAfter;
        /// <summary>The armament that is in the slot now and would return to the inventory (null when the slot is empty).</summary>
        public ArmamentData Replaced;

        /// <summary>"damage 5 -> 5.5, bullet speed 18 -> 22.5": only the stats that change.</summary>
        public string StatText => ShopDescriber.StatChange(Before, After, false);
    }

    /// <summary>
    /// The before -> after preview shown in the Armory before an armament is confirmed. Unlike the Shop's fit (first free
    /// slot only) this works for a chosen slot, including replacing an armament that is already there.
    /// </summary>
    public static class ArmoryPreview
    {
        public static ArmoryChange Equip(ArmInstance arm, int slot, ArmamentData armament)
        {
            var change = new ArmoryChange
            {
                Before = arm.Stats,
                After = arm.Stats,
                Reason = "",
                ShotBefore = ItemDescriber.ShotSummary(arm.Shot),
                Replaced = arm.GetArmament(slot),
            };
            change.ShotAfter = change.ShotBefore;

            if (!arm.CanEquipAt(slot, armament, out string reason))
            {
                change.Reason = reason;
                return change;
            }

            change.CanEquip = true;
            List<ArmamentData> after = Armaments(arm, slot, armament);
            change.After = StatCalculator.Calculate(arm.BaseStats, after);
            change.ShotAfter = ShotOf(arm, after);
            return change;
        }

        /// <summary>What taking the armament out of a slot would do.</summary>
        public static ArmoryChange Remove(ArmInstance arm, int slot)
        {
            var change = new ArmoryChange
            {
                CanEquip = arm.GetArmament(slot) != null,
                Reason = arm.GetArmament(slot) != null ? "" : "the slot is empty",
                Before = arm.Stats,
                After = arm.Stats,
                ShotBefore = ItemDescriber.ShotSummary(arm.Shot),
                Replaced = arm.GetArmament(slot),
            };
            change.ShotAfter = change.ShotBefore;
            if (!change.CanEquip)
                return change;

            List<ArmamentData> after = Armaments(arm, slot, null);
            change.After = StatCalculator.Calculate(arm.BaseStats, after);
            change.ShotAfter = ShotOf(arm, after);
            return change;
        }

        // The arm's armaments with one slot replaced (null = emptied).
        private static List<ArmamentData> Armaments(ArmInstance arm, int slot, ArmamentData replacement)
        {
            var list = new List<ArmamentData>();
            for (int i = 0; i < arm.SlotCount; i++)
            {
                ArmamentData a = i == slot ? replacement : arm.GetArmament(i);
                if (a != null)
                    list.Add(a);
            }
            return list;
        }

        private static string ShotOf(ArmInstance arm, List<ArmamentData> armaments)
        {
            var probe = new ArmInstance(arm.Data);
            foreach (ArmamentData a in armaments)
                probe.TryAdd(a);
            return ItemDescriber.ShotSummary(probe.Shot);
        }
    }
}
