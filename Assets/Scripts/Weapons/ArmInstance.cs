using System;

namespace BulletHell.Weapons
{
    /// <summary>
    /// Run-time state of one equipped arm: its WeaponArmData plus 3 upgrade slots. Upgrades belong to the instance, so
    /// two slots holding the same arm type can be upgraded differently. Assets are never modified; final stats are
    /// recalculated only when the upgrades change and cached.
    /// </summary>
    public sealed class ArmInstance
    {
        public const int UpgradeSlots = 3;

        private readonly UpgradeData[] upgrades = new UpgradeData[UpgradeSlots];

        public WeaponArmData Data { get; }
        public ArmStats BaseStats { get; }
        /// <summary>Final stats: base + upgrades. Cached.</summary>
        public ArmStats Stats { get; private set; }
        /// <summary>Increments on every upgrade change, so UI can tell when to refresh.</summary>
        public int Version { get; private set; }

        public event Action Changed;

        public ArmInstance(WeaponArmData data)
        {
            Data = data;
            BaseStats = ArmStats.FromData(data);
            Stats = BaseStats;
        }

        /// <summary>Upgrade in slot 0-2, or null if the slot is empty.</summary>
        public UpgradeData GetUpgrade(int slot) => upgrades[slot];

        public int UpgradeCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < UpgradeSlots; i++)
                    if (upgrades[i] != null)
                        count++;
                return count;
            }
        }

        /// <summary>Puts the upgrade in the first empty slot. Returns false when all slots are full.</summary>
        public bool TryAdd(UpgradeData upgrade)
        {
            if (upgrade == null)
                return false;
            for (int i = 0; i < UpgradeSlots; i++)
            {
                if (upgrades[i] != null)
                    continue;
                upgrades[i] = upgrade;
                Refresh();
                return true;
            }
            return false;
        }

        /// <summary>Empties a slot. Returns false if it was already empty.</summary>
        public bool RemoveAt(int slot)
        {
            if (upgrades[slot] == null)
                return false;
            upgrades[slot] = null;
            Refresh();
            return true;
        }

        /// <summary>Empties the highest filled slot. Returns false when there are no upgrades.</summary>
        public bool RemoveLast()
        {
            for (int i = UpgradeSlots - 1; i >= 0; i--)
                if (upgrades[i] != null)
                    return RemoveAt(i);
            return false;
        }

        private void Refresh()
        {
            Stats = StatCalculator.Calculate(BaseStats, upgrades);
            Version++;
            Changed?.Invoke();
        }
    }
}
