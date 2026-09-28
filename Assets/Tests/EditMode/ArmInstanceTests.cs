using BulletHell.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    public class ArmInstanceTests
    {
        private static WeaponArmData NewArm() => ScriptableObject.CreateInstance<WeaponArmData>();

        private static ArmamentData Damage50()
        {
            var armament = ScriptableObject.CreateInstance<ArmamentData>();
            armament.Set("dmg", new StatModifier(StatType.Damage, ModifierMode.Percent, 50f));
            return armament;
        }

        [Test]
        public void StartsWithBaseStatsAndNoArmaments()
        {
            var instance = new ArmInstance(NewArm());
            Assert.AreEqual(0, instance.ArmamentCount);
            Assert.AreEqual(instance.BaseStats.Damage, instance.Stats.Damage);
        }

        [Test]
        public void AddFillsFirstEmptySlotUntilFull()
        {
            var instance = new ArmInstance(NewArm());
            ArmamentData armament = Damage50();
            Assert.IsTrue(instance.TryAdd(armament));
            Assert.IsTrue(instance.TryAdd(armament));
            Assert.IsTrue(instance.TryAdd(armament));
            Assert.IsFalse(instance.TryAdd(armament));
            Assert.AreEqual(3, instance.ArmamentCount);
        }

        [Test]
        public void AddingAnArmamentChangesFinalStatsButNotTheAsset()
        {
            WeaponArmData data = NewArm();
            float baseDamage = data.Damage;
            var instance = new ArmInstance(data);
            instance.TryAdd(Damage50());
            Assert.AreEqual(baseDamage * 1.5f, instance.Stats.Damage, 0.0001f);
            Assert.AreEqual(baseDamage, data.Damage);
        }

        [Test]
        public void TwoInstancesOfTheSameArmArmamentIndependently()
        {
            WeaponArmData data = NewArm();
            var a = new ArmInstance(data);
            var b = new ArmInstance(data);
            a.TryAdd(Damage50());
            Assert.Greater(a.Stats.Damage, b.Stats.Damage);
        }

        [Test]
        public void RemoveLastEmptiesTheHighestSlotAndRestoresStats()
        {
            var instance = new ArmInstance(NewArm());
            instance.TryAdd(Damage50());
            instance.TryAdd(Damage50());
            Assert.IsTrue(instance.RemoveLast());
            Assert.AreEqual(1, instance.ArmamentCount);
            Assert.IsNotNull(instance.GetArmament(0));
            Assert.IsNull(instance.GetArmament(1));
            Assert.IsTrue(instance.RemoveLast());
            Assert.IsFalse(instance.RemoveLast());
            Assert.AreEqual(instance.BaseStats.Damage, instance.Stats.Damage, 0.0001f);
        }

        [Test]
        public void ChangesRaiseEventAndBumpVersion()
        {
            var instance = new ArmInstance(NewArm());
            int raised = 0;
            instance.Changed += () => raised++;
            int before = instance.Version;
            instance.TryAdd(Damage50());
            instance.RemoveLast();
            Assert.AreEqual(2, raised);
            Assert.AreEqual(before + 2, instance.Version);
        }
    }
}
