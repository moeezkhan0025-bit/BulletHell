using System;
using System.Collections.Generic;

namespace BulletHell.Weapons
{
    /// <summary>
    /// Run-time state of one equipped arm: its WeaponArmData plus its armament slots (1-3, set on the WeaponArmData).
    /// Armaments belong to the instance, so two slots holding the same arm type can be kitted differently. Assets are
    /// never modified; final stats and the effect list are recalculated only when the armaments change and cached.
    /// Each armament has a max stack count: an arm can carry at most that many copies of it.
    /// </summary>
    public sealed class ArmInstance
    {
        /// <summary>The most armament slots any arm can have (array capacity and UI layout).</summary>
        public const int MaxArmamentSlots = 3;

        private static readonly ArmEffect[] NoEffects = new ArmEffect[0];
        private static readonly int[] NoStacks = new int[0];

        private readonly ArmamentData[] armaments = new ArmamentData[MaxArmamentSlots];
        private ArmEffect[] effects = NoEffects;
        private int[] effectStacks = NoStacks;

        public WeaponArmData Data { get; }
        public ArmStats BaseStats { get; }
        /// <summary>How many armaments this arm can carry (1-3).</summary>
        public int SlotCount { get; }
        /// <summary>Final stats: base + armaments. Cached.</summary>
        public ArmStats Stats { get; private set; }
        /// <summary>Each distinct effect from the arm itself plus every equipped armament, once. Cached; a new array on each change.</summary>
        public IReadOnlyList<ArmEffect> Effects => effects;
        /// <summary>How many copies of each effect in <see cref="Effects"/> this arm carries (same order).</summary>
        public IReadOnlyList<int> EffectStacks => effectStacks;
        /// <summary>Per-projectile behaviour (pierce, ricochet, homing, auto-fire) from the effects. Cached.</summary>
        public ShotProperties Shot { get; private set; }
        /// <summary>Increments on every armament change, so UI can tell when to refresh.</summary>
        public int Version { get; private set; }

        public event Action Changed;

        public ArmInstance(WeaponArmData data)
        {
            Data = data;
            SlotCount = Math.Clamp(data.ArmamentSlots, 1, MaxArmamentSlots);
            BaseStats = ArmStats.FromData(data);
            Stats = BaseStats;
            RebuildEffects();
        }

        /// <summary>Armament in slot 0..SlotCount-1, or null if the slot is empty (or does not exist on this arm).</summary>
        public ArmamentData GetArmament(int slot) => slot >= 0 && slot < SlotCount ? armaments[slot] : null;

        public int ArmamentCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < SlotCount; i++)
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
                for (int i = SlotCount - 1; i >= 0; i--)
                    if (armaments[i] != null)
                        return i;
                return -1;
            }
        }

        /// <summary>How many copies of an armament this arm carries.</summary>
        public int CountOf(ArmamentData armament)
        {
            int count = 0;
            for (int i = 0; i < SlotCount; i++)
                if (armaments[i] == armament && armament != null)
                    count++;
            return count;
        }

        /// <summary>Whether an armament could go into a slot (existing slot, and the stack limit allows another copy). Reason explains a no.</summary>
        public bool CanEquipAt(int slot, ArmamentData armament, out string reason)
        {
            reason = null;
            if (armament == null)
                reason = "nothing to equip";
            else if (slot < 0 || slot >= SlotCount)
                reason = "this arm only has " + SlotCount + (SlotCount == 1 ? " slot" : " slots");
            else if (CountOf(armament) - (armaments[slot] == armament ? 1 : 0) >= armament.MaxStacks)
                reason = armament.DisplayName + " is limited to " + armament.MaxStacks + " per arm";
            return reason == null;
        }

        /// <summary>
        /// Moves an armament from the inventory into the first empty slot. Returns false when all slots are full, the
        /// stack limit is reached, or the inventory doesn't hold it.
        /// </summary>
        public bool TryEquip(ArmamentData armament, ArmamentInventory inventory)
        {
            for (int i = 0; i < SlotCount; i++)
                if (armaments[i] == null)
                    return TryEquipAt(i, armament, inventory);
            return false;
        }

        /// <summary>
        /// Moves an armament from the inventory into a specific slot. An armament already in that slot goes back to the
        /// inventory. Returns false when the inventory doesn't hold the armament, the slot does not exist, or the stack
        /// limit is reached.
        /// </summary>
        public bool TryEquipAt(int slot, ArmamentData armament, ArmamentInventory inventory)
        {
            if (inventory == null || !CanEquipAt(slot, armament, out _) || !inventory.Remove(armament))
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
            ArmamentData armament = GetArmament(slot);
            if (armament == null || inventory == null)
                return false;
            armaments[slot] = null;
            inventory.Add(armament);
            Refresh();
            return true;
        }

        /// <summary>Puts the armament in the first empty slot, bypassing any inventory. Returns false when all slots are full or the stack limit is reached.</summary>
        public bool TryAdd(ArmamentData armament)
        {
            if (armament == null)
                return false;
            for (int i = 0; i < SlotCount; i++)
            {
                if (armaments[i] != null)
                    continue;
                if (!CanEquipAt(i, armament, out _))
                    return false;
                armaments[i] = armament;
                Refresh();
                return true;
            }
            return false;
        }

        /// <summary>Empties a slot and discards its armament (nothing returns to an inventory). Returns false if it was already empty.</summary>
        public bool RemoveAt(int slot)
        {
            if (GetArmament(slot) == null)
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

        // Groups the arm's own effects and every armament's effects by asset: each distinct effect once, with its stack count.
        private void RebuildEffects()
        {
            var distinct = new List<ArmEffect>();
            var counts = new List<int>();
            AddEffects(Data.Effects, distinct, counts);
            for (int i = 0; i < SlotCount; i++)
                if (armaments[i] != null)
                    AddEffects(armaments[i].Effects, distinct, counts);

            if (distinct.Count == 0)
            {
                effects = NoEffects;
                effectStacks = NoStacks;
                Shot = default;
                return;
            }

            ShotProperties shot = default;
            for (int i = 0; i < distinct.Count; i++)
                distinct[i].ModifyShot(ref shot, counts[i]);

            effects = distinct.ToArray();
            effectStacks = counts.ToArray();
            Shot = shot;
        }

        // Empty inspector slots (null entries) are skipped.
        private static void AddEffects(IReadOnlyList<ArmEffect> source, List<ArmEffect> distinct, List<int> counts)
        {
            for (int i = 0; i < source.Count; i++)
            {
                ArmEffect effect = source[i];
                if (effect == null)
                    continue;
                int index = distinct.IndexOf(effect);
                if (index >= 0)
                {
                    counts[index]++;
                }
                else
                {
                    distinct.Add(effect);
                    counts.Add(1);
                }
            }
        }
    }
}
