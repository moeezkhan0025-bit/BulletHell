using BulletHell.Core;
using BulletHell.Weapons;

namespace BulletHell.Armory
{
    public enum ArmoryResult { Done, SlotOccupied, SlotEmpty, NotInInventory, LastArm }

    /// <summary>
    /// The arm-slot half of the Armory (armament equipping lives on ArmInstance). The player must always keep at
    /// least one arm equipped: a run with no arms can't fight and can't be saved.
    /// </summary>
    public static class ArmoryActions
    {
        /// <summary>Moves a spare arm into an empty loadout slot.</summary>
        public static ArmoryResult PlaceArm(RunState state, int slot, ArmInstance arm)
        {
            if (state.Loadout[slot] != null)
                return ArmoryResult.SlotOccupied;
            if (!state.SpareArms.Remove(arm))
                return ArmoryResult.NotInInventory;

            state.Loadout[slot] = arm;
            return ArmoryResult.Done;
        }

        /// <summary>Moves the arm in a slot to the arm inventory, armaments still attached.</summary>
        public static ArmoryResult RemoveArm(RunState state, int slot)
        {
            ArmInstance arm = state.Loadout[slot];
            if (arm == null)
                return ArmoryResult.SlotEmpty;
            if (ArmCount(state) <= 1)
                return ArmoryResult.LastArm;

            state.Loadout[slot] = null;
            state.SpareArms.Add(arm);
            return ArmoryResult.Done;
        }

        public static int ArmCount(RunState state)
        {
            int count = 0;
            for (int i = 0; i < state.Loadout.Length; i++)
                if (state.Loadout[i] != null)
                    count++;
            return count;
        }
    }
}
