using System.Collections.Generic;
using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>
    /// Final arm stats. Order is fixed: start from the base value, add every Flat modifier, then multiply by every
    /// Percent modifier (each one is x(1 + value/100), so +50% and +50% give x2.25, not x2). Ammo multipliers are applied
    /// afterwards, at fire time. Projectiles per shot is rounded to a whole number, minimum 1.
    /// </summary>
    public static class StatCalculator
    {
        private const float MinFireRate = 0.01f;

        /// <summary>Empty (null) slots are skipped. Does not allocate.</summary>
        public static ArmStats Calculate(in ArmStats baseStats, IReadOnlyList<UpgradeData> upgrades)
        {
            float damage = Stat(baseStats.Damage, StatType.Damage, upgrades);
            float fireRate = Stat(baseStats.FireRate, StatType.FireRate, upgrades);
            float speed = Stat(baseStats.ProjectileSpeed, StatType.ProjectileSpeed, upgrades);
            float projectiles = Stat(baseStats.ProjectilesPerShot, StatType.ProjectilesPerShot, upgrades);
            float spread = Stat(baseStats.Spread, StatType.Spread, upgrades);

            return new ArmStats(
                Mathf.Max(0f, damage),
                Mathf.Max(MinFireRate, fireRate),
                Mathf.Max(0f, speed),
                Mathf.Max(1, Mathf.RoundToInt(projectiles)),
                Mathf.Max(0f, spread));
        }

        private static float Stat(float baseValue, StatType stat, IReadOnlyList<UpgradeData> upgrades)
        {
            float value = baseValue;
            for (int u = 0; u < upgrades.Count; u++)
            {
                if (upgrades[u] == null)
                    continue;
                IReadOnlyList<StatModifier> mods = upgrades[u].Modifiers;
                for (int m = 0; m < mods.Count; m++)
                    if (mods[m].Stat == stat && mods[m].Mode == ModifierMode.Flat)
                        value += mods[m].Value;
            }

            for (int u = 0; u < upgrades.Count; u++)
            {
                if (upgrades[u] == null)
                    continue;
                IReadOnlyList<StatModifier> mods = upgrades[u].Modifiers;
                for (int m = 0; m < mods.Count; m++)
                    if (mods[m].Stat == stat && mods[m].Mode == ModifierMode.Percent)
                        value *= 1f + mods[m].Value / 100f;
            }

            return value;
        }
    }
}
