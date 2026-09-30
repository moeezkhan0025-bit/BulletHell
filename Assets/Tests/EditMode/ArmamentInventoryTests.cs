using BulletHell.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    public class ArmamentInventoryTests
    {
        private static WeaponArmData NewArm() => ScriptableObject.CreateInstance<WeaponArmData>();

        private static ArmamentData NewArmament(params ArmEffect[] effects)
        {
            var armament = ScriptableObject.CreateInstance<ArmamentData>();
            armament.Set("a", new StatModifier(StatType.Damage, ModifierMode.Percent, 50f));
            armament.SetEffects(effects);
            return armament;
        }

        private static PierceEffect Pierce(int count)
        {
            var effect = ScriptableObject.CreateInstance<PierceEffect>();
            effect.Set(count);
            return effect;
        }

        private static RicochetEffect Ricochet(int bounces, int extraPerStack = 1)
        {
            var effect = ScriptableObject.CreateInstance<RicochetEffect>();
            effect.Set(bounces, extraPerStack);
            return effect;
        }

        [Test]
        public void EquipMovesArmamentFromInventoryToArm()
        {
            var inventory = new ArmamentInventory();
            ArmamentData armament = NewArmament();
            inventory.Add(armament);
            var arm = new ArmInstance(NewArm());

            Assert.IsTrue(arm.TryEquip(armament, inventory));

            Assert.AreEqual(0, inventory.Count);
            Assert.AreSame(armament, arm.GetArmament(0));
        }

        [Test]
        public void EquipFailsWhenInventoryDoesNotHoldIt()
        {
            var inventory = new ArmamentInventory();
            var arm = new ArmInstance(NewArm());

            Assert.IsFalse(arm.TryEquip(NewArmament(), inventory));
            Assert.AreEqual(0, arm.ArmamentCount);
        }

        [Test]
        public void EquipFailsWhenArmIsFullAndKeepsTheItemInInventory()
        {
            var inventory = new ArmamentInventory();
            ArmamentData armament = NewArmament();
            for (int i = 0; i < 4; i++)
                inventory.Add(armament);
            var arm = new ArmInstance(NewArm());

            Assert.IsTrue(arm.TryEquip(armament, inventory));
            Assert.IsTrue(arm.TryEquip(armament, inventory));
            Assert.IsTrue(arm.TryEquip(armament, inventory));
            Assert.IsFalse(arm.TryEquip(armament, inventory));
            Assert.AreEqual(1, inventory.Count);
        }

        [Test]
        public void UnequipReturnsArmamentToInventory()
        {
            var inventory = new ArmamentInventory();
            ArmamentData armament = NewArmament();
            inventory.Add(armament);
            var arm = new ArmInstance(NewArm());
            arm.TryEquip(armament, inventory);

            Assert.IsTrue(arm.Unequip(0, inventory));

            Assert.AreEqual(1, inventory.Count);
            Assert.IsNull(arm.GetArmament(0));
            Assert.AreEqual(arm.BaseStats.Damage, arm.Stats.Damage);
        }

        [Test]
        public void UnequipOfEmptySlotDoesNothing()
        {
            var inventory = new ArmamentInventory();
            var arm = new ArmInstance(NewArm());
            Assert.IsFalse(arm.Unequip(1, inventory));
            Assert.AreEqual(0, inventory.Count);
        }

        [Test]
        public void EquipAtOccupiedSlotSwapsTheOldOneBackToInventory()
        {
            var inventory = new ArmamentInventory();
            ArmamentData first = NewArmament();
            ArmamentData second = NewArmament();
            inventory.Add(first);
            inventory.Add(second);
            var arm = new ArmInstance(NewArm());
            arm.TryEquipAt(0, first, inventory);

            Assert.IsTrue(arm.TryEquipAt(0, second, inventory));

            Assert.AreSame(second, arm.GetArmament(0));
            Assert.AreEqual(1, inventory.Count);
            Assert.IsTrue(inventory.Contains(first));
        }

        [Test]
        public void EquippingTheSameArmamentTwiceUsesTwoCopies()
        {
            var inventory = new ArmamentInventory();
            ArmamentData armament = NewArmament();
            inventory.Add(armament);
            var arm = new ArmInstance(NewArm());

            Assert.IsTrue(arm.TryEquip(armament, inventory));
            Assert.IsFalse(arm.TryEquip(armament, inventory)); // only one copy was owned
            Assert.AreEqual(1, arm.ArmamentCount);
        }

        [Test]
        public void ArmInventoryKeepsArmamentsAttachedToAnArm()
        {
            var armInventory = new ArmInventory();
            var armaments = new ArmamentInventory();
            ArmamentData armament = NewArmament();
            armaments.Add(armament);
            var arm = new ArmInstance(NewArm());
            arm.TryEquip(armament, armaments);

            armInventory.Add(arm);
            Assert.IsTrue(armInventory.Remove(arm));

            Assert.AreSame(armament, arm.GetArmament(0));
            Assert.AreEqual(0, armInventory.Count);
        }

        [Test]
        public void InventoryVersionChangesOnEveryChange()
        {
            var inventory = new ArmamentInventory();
            ArmamentData armament = NewArmament();
            int v0 = inventory.Version;
            inventory.Add(armament);
            int v1 = inventory.Version;
            inventory.Remove(armament);
            Assert.Greater(v1, v0);
            Assert.Greater(inventory.Version, v1);
        }

        [Test]
        public void EffectsComeFromTheArmamentsAndDisappearWhenUnequipped()
        {
            var inventory = new ArmamentInventory();
            ArmamentData armament = NewArmament(Pierce(2));
            inventory.Add(armament);
            var arm = new ArmInstance(NewArm());
            Assert.AreEqual(0, arm.Effects.Count);

            arm.TryEquip(armament, inventory);
            Assert.AreEqual(1, arm.Effects.Count);
            Assert.AreEqual(2, arm.Shot.Pierce);

            arm.Unequip(0, inventory);
            Assert.AreEqual(0, arm.Effects.Count);
            Assert.AreEqual(0, arm.Shot.Pierce);
        }

        [Test]
        public void PierceAndBouncesFromDifferentEffectsAdd()
        {
            var inventory = new ArmamentInventory();
            ArmamentData a = NewArmament(Pierce(1), Ricochet(1));
            ArmamentData b = NewArmament(Pierce(2), Ricochet(2));
            inventory.Add(a);
            inventory.Add(b);
            var arm = new ArmInstance(NewArm());
            arm.TryEquip(a, inventory);
            arm.TryEquip(b, inventory);

            Assert.AreEqual(3, arm.Shot.Pierce);
            Assert.AreEqual(3, arm.Shot.Bounces);
            Assert.AreEqual(4, arm.Effects.Count);
        }

        [Test]
        public void NullEffectEntriesAreIgnored()
        {
            var inventory = new ArmamentInventory();
            ArmamentData armament = NewArmament(null, Pierce(1));
            inventory.Add(armament);
            var arm = new ArmInstance(NewArm());

            arm.TryEquip(armament, inventory);

            Assert.AreEqual(1, arm.Effects.Count);
            Assert.AreEqual(1, arm.Shot.Pierce);
        }
    }
}
