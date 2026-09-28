using BulletHell.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    public class StatCalculatorTests
    {
        // damage 10, 5 shots/s, speed 20, 1 projectile, 0 spread.
        private static readonly ArmStats Base = new ArmStats(10f, 5f, 20f, 1, 0f);

        private static ArmamentData Armament(params StatModifier[] modifiers)
        {
            var armament = ScriptableObject.CreateInstance<ArmamentData>();
            armament.Set("test", modifiers);
            return armament;
        }

        private static ArmStats Calc(params ArmamentData[] armaments) => StatCalculator.Calculate(Base, armaments);

        [Test]
        public void NoArmamentsGivesBaseStats()
        {
            ArmStats stats = Calc(null, null, null);
            Assert.AreEqual(10f, stats.Damage, 0.0001f);
            Assert.AreEqual(5f, stats.FireRate, 0.0001f);
            Assert.AreEqual(20f, stats.ProjectileSpeed, 0.0001f);
            Assert.AreEqual(1, stats.ProjectilesPerShot);
        }

        [Test]
        public void FlatAddsBeforePercentMultiplies()
        {
            // (10 + 5) * 1.5 = 22.5; multiplying first would give 10 * 1.5 + 5 = 20.
            var percent = Armament(new StatModifier(StatType.Damage, ModifierMode.Percent, 50f));
            var flat = Armament(new StatModifier(StatType.Damage, ModifierMode.Flat, 5f));
            Assert.AreEqual(22.5f, Calc(percent, flat).Damage, 0.0001f);
        }

        [Test]
        public void PercentModifiersMultiplyTogether()
        {
            var a = Armament(new StatModifier(StatType.Damage, ModifierMode.Percent, 50f));
            var b = Armament(new StatModifier(StatType.Damage, ModifierMode.Percent, 50f));
            Assert.AreEqual(22.5f, Calc(a, b).Damage, 0.0001f);
        }

        [Test]
        public void ModifiersOnlyAffectTheirOwnStat()
        {
            ArmStats stats = Calc(Armament(new StatModifier(StatType.FireRate, ModifierMode.Flat, 2f)));
            Assert.AreEqual(7f, stats.FireRate, 0.0001f);
            Assert.AreEqual(10f, stats.Damage, 0.0001f);
        }

        [Test]
        public void ProjectileCountAddsAndRounds()
        {
            var extra = Armament(new StatModifier(StatType.ProjectilesPerShot, ModifierMode.Flat, 1f));
            Assert.AreEqual(3, Calc(extra, extra).ProjectilesPerShot);

            var half = Armament(new StatModifier(StatType.ProjectilesPerShot, ModifierMode.Percent, 100f));
            Assert.AreEqual(2, Calc(half).ProjectilesPerShot);
        }

        [Test]
        public void StatsAreClamped()
        {
            var bad = Armament(
                new StatModifier(StatType.Damage, ModifierMode.Flat, -100f),
                new StatModifier(StatType.FireRate, ModifierMode.Percent, -100f),
                new StatModifier(StatType.ProjectilesPerShot, ModifierMode.Flat, -5f));
            ArmStats stats = Calc(bad);
            Assert.AreEqual(0f, stats.Damage);
            Assert.Greater(stats.FireRate, 0f);
            Assert.AreEqual(1, stats.ProjectilesPerShot);
        }
    }
}
