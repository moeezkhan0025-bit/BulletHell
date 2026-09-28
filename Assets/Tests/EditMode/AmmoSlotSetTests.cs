using BulletHell.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    public class AmmoSlotSetTests
    {
        private AmmoTypeData basic, shotgun, laser, gatling, extra;

        [SetUp]
        public void SetUp()
        {
            basic = ScriptableObject.CreateInstance<AmmoTypeData>();
            shotgun = ScriptableObject.CreateInstance<AmmoTypeData>();
            laser = ScriptableObject.CreateInstance<AmmoTypeData>();
            gatling = ScriptableObject.CreateInstance<AmmoTypeData>();
            extra = ScriptableObject.CreateInstance<AmmoTypeData>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var ammo in new[] { basic, shotgun, laser, gatling, extra })
                Object.DestroyImmediate(ammo);
        }

        [Test]
        public void SetActivatesFirstFilledSlot()
        {
            var slots = new AmmoSlotSet();
            slots.Set(0, basic);
            Assert.AreEqual(0, slots.ActiveIndex);
            Assert.AreSame(basic, slots.Active);
        }

        [Test]
        public void AutoFillUsesFirstEmptySlot()
        {
            var slots = new AmmoSlotSet();
            slots.Set(0, basic);
            slots.Set(2, laser);

            Assert.IsTrue(slots.TryAutoFill(shotgun, out int index));
            Assert.AreEqual(1, index);
            Assert.AreSame(shotgun, slots.Get(1));
        }

        [Test]
        public void AutoFillDoesNotChangeActiveSlot()
        {
            var slots = new AmmoSlotSet();
            slots.Set(0, basic);
            slots.TryAutoFill(shotgun, out _);
            Assert.AreEqual(0, slots.ActiveIndex);
        }

        [Test]
        public void AutoFillIgnoresCarriedType()
        {
            var slots = new AmmoSlotSet();
            slots.Set(0, basic);
            Assert.IsFalse(slots.TryAutoFill(basic, out _));
            Assert.IsNull(slots.Get(1));
        }

        [Test]
        public void AutoFillFailsWhenFull()
        {
            var slots = new AmmoSlotSet();
            slots.Set(0, basic);
            slots.Set(1, shotgun);
            slots.Set(2, laser);
            slots.Set(3, gatling);
            Assert.IsFalse(slots.TryAutoFill(extra, out _));
        }

        [Test]
        public void EmptySlotCannotBeSelected()
        {
            var slots = new AmmoSlotSet();
            slots.Set(0, basic);
            Assert.IsFalse(slots.TrySelect(2));
            Assert.AreEqual(0, slots.ActiveIndex);
        }

        [Test]
        public void FilledSlotCanBeSelected()
        {
            var slots = new AmmoSlotSet();
            slots.Set(0, basic);
            slots.Set(2, laser);
            Assert.IsTrue(slots.TrySelect(2));
            Assert.AreSame(laser, slots.Active);
        }

        [Test]
        public void ReplaceReturnsOldAmmoAndKeepsSlotActive()
        {
            var slots = new AmmoSlotSet();
            slots.Set(0, basic);
            slots.Set(1, shotgun);
            slots.TrySelect(1);

            AmmoTypeData old = slots.Replace(1, laser);

            Assert.AreSame(shotgun, old);
            Assert.AreSame(laser, slots.Active);
            Assert.IsFalse(slots.Contains(shotgun));
        }
    }
}
