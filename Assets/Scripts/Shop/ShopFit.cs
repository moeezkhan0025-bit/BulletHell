using System.Collections.Generic;
using BulletHell.Core;
using BulletHell.Weapons;

namespace BulletHell.Shop
{
    /// <summary>How an armament would sit on one of the player's arms.</summary>
    public struct ArmFit
    {
        public ArmInstance Arm;
        /// <summary>"Blue Arm (E)" for a loadout arm (its slot), "spare Blue Arm" for one in the arm inventory.</summary>
        public string Label;
        public int FreeSlots;
        /// <summary>A free slot exists and the stack limit allows another copy.</summary>
        public bool CanEquip;
        /// <summary>Why it cannot be equipped (empty when it can).</summary>
        public string Reason;
        public ArmStats Before;
        /// <summary>Stats with the armament added (equal to Before when it cannot be equipped).</summary>
        public ArmStats After;
    }

    /// <summary>Which of the player's arms an armament fits, and the stats before and after. Used by the Shop tooltip.</summary>
    public static class ShopFit
    {
        private static readonly string[] SlotNames = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        public static List<ArmFit> ForArmament(RunState state, ArmamentData armament)
        {
            var fits = new List<ArmFit>();
            for (int i = 0; i < state.Loadout.Length; i++)
                if (state.Loadout[i] != null)
                    fits.Add(Evaluate(state.Loadout[i], armament, state.Loadout[i].Data.DisplayName + " (" + SlotNames[i] + ")"));
            for (int i = 0; i < state.SpareArms.Count; i++)
                fits.Add(Evaluate(state.SpareArms.Get(i), armament, "spare " + state.SpareArms.Get(i).Data.DisplayName));
            return fits;
        }

        public static ArmFit Evaluate(ArmInstance arm, ArmamentData armament, string label)
        {
            int slot = -1;
            for (int i = 0; i < arm.SlotCount; i++)
                if (arm.GetArmament(i) == null)
                {
                    slot = i;
                    break;
                }

            var fit = new ArmFit
            {
                Arm = arm,
                Label = label,
                FreeSlots = arm.SlotCount - arm.ArmamentCount,
                Before = arm.Stats,
                After = arm.Stats,
                Reason = "",
            };

            if (slot < 0)
            {
                fit.Reason = "no free slot";
                return fit;
            }
            if (!arm.CanEquipAt(slot, armament, out string reason))
            {
                fit.Reason = reason;
                return fit;
            }

            var list = new List<ArmamentData>();
            for (int i = 0; i < arm.SlotCount; i++)
                if (arm.GetArmament(i) != null)
                    list.Add(arm.GetArmament(i));
            list.Add(armament);
            fit.CanEquip = true;
            fit.After = StatCalculator.Calculate(arm.BaseStats, list);
            return fit;
        }
    }
}
