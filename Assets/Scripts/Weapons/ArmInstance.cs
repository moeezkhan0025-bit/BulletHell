using System;
using System.Collections.Generic;

namespace BulletHell.Weapons
{
    /// <summary>
    /// Run-time state of one equipped arm: its WeaponArmData plus 3 armament slots. Armaments belong to the instance, so
    /// two slots holding the same arm type can be kitted differently. Assets are never modified; final stats and the
    /// effect list are recalculated only when the armaments change and cached.
    /// </summary>
    public sealed class ArmInstance
    {
        public const int ArmamentSlots = 3;

        private static readonly ArmEffect[] NoEffects = new ArmEffect[0];

        private readonly ArmamentData[] armaments = new ArmamentData[ArmamentSlots];
        private ArmEffect[] effects = NoEffects;

        public WeaponArmData Data { get; }
        public ArmStats BaseStats { get; }
        /// <summary>Final stats: base + armaments. Cached.</summary>
        public ArmStats Stats { get; private set; }
        /// <summary>Effects from the arm itself plus every equipped armament. Cached; a new array on each change.</summary>
        public IReadOnlyList<ArmEffect> Effects => effects;
        /// <summary>Per-projectile behaviour (pierce, ricochet) from the effects. Cached.</summary>
        public ShotProperties Shot { get; private set; }
        /// <summary>Increments on every armament change, so UI can tell when to refresh.</summary>
        public int Version { get; private set; }

        public event Action Changed;

        public ArmInstance(WeaponArmData data)
        {
            Data = data;
            BaseStats = ArmStats.FromData(data);
            Stats = BaseStats;
            RebuildEffects();
        }

        /// <summary>Armament in slot 0-2, or null if the slot is empty.</summary>
        public ArmamentData GetArmament(int slot) => armaments[slot];

        public int ArmamentCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < ArmamentSlots; i++)
                    if (armaments[i] != null)
                        count++;
                return count;
            }
        }

        /// <summary>Highest filled slot, or -1 when there are no armaments.</summary>
        public int LastFilledSlot
        {
            get
            {
                for (int i = ArmamentSlots - 1; i >= 0; i--)
                    if (armaments[i] != null)
                        return i;
                return -1;
            }
        }

        /// <summary>
        /// Moves an armament from the inventory into the first empty slot. Returns false when all slots are full or
        /// the inventory doesn't hold it.
        /// </summary>
        public bool TryEquip(ArmamentData armament, ArmamentInventory inventory)
        {
            for (int i = 0; i < ArmamentSlots; i++)
                if (armaments[i] == null)
                    return TryEquipAt(i, armament, inventory);
            return false;
        }

        /// <summary>
        /// Moves an armament from the inventory into a specific slot. An armament already in that slot goes back to the
        /// inventory. Returns false when the inventory doesn't hold the armament.
        /// </summary>
        public bool TryEquipAt(int slot, ArmamentData armament, ArmamentInventory inventory)
        {
            if (inventory == null || armament == null || !inventory.Remove(armament))
                return false;
            ArmamentData previous = armaments[slot];
            armaments[slot] = armament;
            if (previous != null)
                inventory.Add(previous);
            Refresh();
            return true;
        }

        /// <summary>Empties a slot and returns its armament to the inventory. Returns false if the slot was empty.</summary>
        public bool Unequip(int slot, ArmamentInventory inventory)
        {
            ArmamentData armament = armaments[slot];
            if (armament == null || inventory == null)
                return false;
            armaments[slot] = null;
            inventory.Add(armament);
            Refresh();
            return true;
        }

        /// <summary>Puts the armament in the first empty slot, bypassing any inventory. Returns false when all slots are full.</summary>
        public bool TryAdd(ArmamentData armament)
        {
            if (armament == null)
                return false;
            for (int i = 0; i < ArmamentSlots; i++)
            {
                if (armaments[i] != null)
                    continue;
                armaments[i] = armament;
                Refresh();
                return true;
            }
            return false;
        }

        /// <summary>Empties a slot and discards its armament (nothing returns to an inventory). Returns false if it was already empty.</summary>
        public bool RemoveAt(int slot)
        {
            if (armaments[slot] == null)
                return false;
            armaments[slot] = null;
            Refresh();
            return true;
        }

        /// <summary>Empties the highest filled slot and discards its armament. Returns false when there are no armaments.</summary>
        public bool RemoveLast()
        {
            int slot = LastFilledSlot;
            return slot >= 0 && RemoveAt(slot);
        }

        private void Refresh()
        {
            Stats = StatCalculator.Calculate(BaseStats, armaments);
            RebuildEffects();
            Version++;
            Changed?.Invoke();
        }

        private void RebuildEffects()
        {
            int count = CountEffects(Data.Effects);
            for (int i = 0; i < ArmamentSlots; i++)
                if (armaments[i] != null)
                    count += CountEffects(armaments[i].Effects);

            if (count == 0)
            {
                effects = NoEffects;
                Shot = default;
                return;
            }

            var list = new ArmEffect[count];
            int next = CopyEffects(Data.Effects, list, 0);
            for (int a = 0; a < ArmamentSlots; a++)
                if (armaments[a] != null)
                    next = CopyEffects(armaments[a].Effects, list, next);

            ShotProperties shot = default;
            for (int i = 0; i < list.Length; i++)
                list[i].ModifyShot(ref shot);

            effects = list;
            Shot = shot;
        }

        // Empty inspector slots (null entries) are skipped.
        private static int CountEffects(IReadOnlyList<ArmEffect> source)
        {
            int count = 0;
            for (int i = 0; i < source.Count; i++)
                if (source[i] != null)
                    count++;
            return count;
        }

        private static int CopyEffects(IReadOnlyList<ArmEffect> source, ArmEffect[] target, int next)
        {
            for (int i = 0; i < source.Count; i++)
                if (source[i] != null)
                    target[next++] = source[i];
            return next;
        }
    }
}
