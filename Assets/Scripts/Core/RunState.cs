using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Core
{
    /// <summary>
    /// Everything about the current run that must survive between rounds and be saved: round, currency, the 8-slot
    /// loadout of arm instances, both inventories and the 4 ammo slots. Live objects (player arms, ammo slots, debug
    /// controls) read and modify this object; nothing else owns run data.
    /// </summary>
    public sealed class RunState
    {
        /// <summary>The round being played, or whose Shop the player is in.</summary>
        public int Round = 1;
        public int Currency;

        /// <summary>N, NE, E, SE, S, SW, W, NW. Null = empty slot.</summary>
        public ArmInstance[] Loadout { get; } = new ArmInstance[ArmLoadout.SlotCount];
        public ArmamentInventory Armaments { get; } = new ArmamentInventory();
        public ArmInventory SpareArms { get; } = new ArmInventory();
        public AmmoSlotSet Ammo { get; } = new AmmoSlotSet();

        /// <summary>A fresh run from the game config: starting loadout, ammo and (test) inventory stock.</summary>
        public static RunState NewRun(GameConfig config)
        {
            var state = new RunState { Currency = config.StartingCurrency };

            ArmLoadout loadout = config.NewRunLoadout;
            if (loadout != null)
                for (int i = 0; i < ArmLoadout.SlotCount; i++)
                    if (loadout.IsFilled(i))
                        state.Loadout[i] = new ArmInstance(loadout.GetSlot(i));

            AmmoTypeData[] ammo = config.StartingAmmo;
            for (int i = 0; i < AmmoSlotSet.Count && ammo != null && i < ammo.Length; i++)
                if (ammo[i] != null)
                    state.Ammo.Set(i, ammo[i]);

            foreach (ArmamentData armament in config.StartingArmaments)
                state.Armaments.Add(armament);
            foreach (WeaponArmData arm in config.StartingSpareArms)
                if (arm != null)
                    state.SpareArms.Add(new ArmInstance(arm));

            if (loadout == null)
                Debug.LogWarning("GameConfig has no New Run Loadout: the player starts with no arms.");
            return state;
        }
    }
}
