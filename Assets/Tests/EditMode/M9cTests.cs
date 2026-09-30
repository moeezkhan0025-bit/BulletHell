using BulletHell.Armory;
using BulletHell.Core;
using BulletHell.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    public class M9cTests
    {
        private static WeaponArmData Arm(string id, int slots)
        {
            var arm = ScriptableObject.CreateInstance<WeaponArmData>();
            arm.SetId(id);
            arm.SetArmamentSlots(slots);
            return arm;
        }

        private static ArmamentData Armament(string id, ArmamentRarity rarity = ArmamentRarity.Common, int maxStacks = 3, float damagePercent = 25f)
        {
            var armament = ScriptableObject.CreateInstance<ArmamentData>();
            armament.SetId(id);
            armament.SetMeta(rarity, ArmamentTags.None, 1, maxStacks);
            armament.Set(id, new StatModifier(StatType.Damage, ModifierMode.Percent, damagePercent));
            return armament;
        }

        [Test]
        public void PreviewOfAnEmptySlotShowsTheStatChangeAndReplacesNothing()
        {
            var arm = new ArmInstance(Arm("a", 2));
            ArmamentData boost = Armament("boost");

            ArmoryChange change = ArmoryPreview.Equip(arm, 1, boost);

            Assert.IsTrue(change.CanEquip);
            Assert.IsNull(change.Replaced);
            Assert.Greater(change.After.Damage, change.Before.Damage);
            Assert.AreEqual(arm.Stats.Damage, change.Before.Damage); // a preview never changes the arm
            Assert.AreEqual(0, arm.ArmamentCount);
        }

        [Test]
        public void PreviewOfAnOccupiedSlotReportsWhatComesOut()
        {
            var arm = new ArmInstance(Arm("a", 1));
            ArmamentData small = Armament("small", damagePercent: 10f);
            ArmamentData big = Armament("big", damagePercent: 50f);
            arm.TryAdd(small);

            ArmoryChange change = ArmoryPreview.Equip(arm, 0, big);

            Assert.IsTrue(change.CanEquip);
            Assert.AreSame(small, change.Replaced);
            Assert.Greater(change.After.Damage, change.Before.Damage);
        }

        [Test]
        public void PreviewRefusesASlotThatDoesNotExist()
        {
            var arm = new ArmInstance(Arm("a", 1));

            ArmoryChange change = ArmoryPreview.Equip(arm, 2, Armament("x"));

            Assert.IsFalse(change.CanEquip);
            Assert.IsNotEmpty(change.Reason);
        }

        [Test]
        public void PreviewRespectsTheStackLimit()
        {
            var arm = new ArmInstance(Arm("a", 3));
            ArmamentData once = Armament("once", maxStacks: 1);
            arm.TryAdd(once);

            ArmoryChange change = ArmoryPreview.Equip(arm, 1, once);

            Assert.IsFalse(change.CanEquip);
            Assert.IsNotEmpty(change.Reason);
        }

        [Test]
        public void PreviewOfRemovingShowsTheDrop()
        {
            var arm = new ArmInstance(Arm("a", 1));
            arm.TryAdd(Armament("boost"));

            ArmoryChange change = ArmoryPreview.Remove(arm, 0);

            Assert.IsTrue(change.CanEquip);
            Assert.Less(change.After.Damage, change.Before.Damage);
            Assert.IsFalse(ArmoryPreview.Remove(arm, 1).CanEquip);
        }

        [Test]
        public void EquippingIntoAChosenSlotSwapsTheOldArmamentBackToTheInventory()
        {
            var state = new RunState();
            var arm = new ArmInstance(Arm("a", 2));
            state.Loadout[0] = arm;
            ArmamentData first = Armament("first");
            ArmamentData second = Armament("second");
            state.Armaments.Add(first);
            state.Armaments.Add(second);

            Assert.IsTrue(arm.TryEquipAt(1, first, state.Armaments));
            Assert.IsTrue(arm.TryEquipAt(1, second, state.Armaments));

            Assert.AreSame(second, arm.GetArmament(1));
            Assert.AreEqual(1, state.Armaments.Count);
            Assert.AreSame(first, state.Armaments.Get(0));
        }

        [Test]
        public void InventoryGroupsDuplicatesAndSortsBestRarityFirst()
        {
            var inventory = new ArmamentInventory();
            ArmamentData common = Armament("common");
            ArmamentData epic = Armament("epic", ArmamentRarity.Epic);
            inventory.Add(common);
            inventory.Add(epic);
            inventory.Add(common);

            var stacks = ArmoryInventoryView.Group(inventory);

            Assert.AreEqual(2, stacks.Count);
            Assert.AreSame(epic, stacks[0].Armament);
            Assert.AreEqual(1, stacks[0].Count);
            Assert.AreSame(common, stacks[1].Armament);
            Assert.AreEqual(2, stacks[1].Count);
        }

        [Test]
        public void RemovingAnArmKeepsItsArmamentsAndPlacingItBringsThemBack()
        {
            var state = new RunState();
            state.Loadout[0] = new ArmInstance(Arm("keep", 1));
            var carrier = new ArmInstance(Arm("carrier", 2));
            ArmamentData boost = Armament("boost");
            carrier.TryAdd(boost);
            state.Loadout[3] = carrier;

            Assert.AreEqual(ArmoryResult.Done, ArmoryActions.RemoveArm(state, 3));
            Assert.IsNull(state.Loadout[3]);
            Assert.AreEqual(1, ArmoryInventoryView.SpareArms(state.SpareArms).Count);

            ArmInstance spare = ArmoryInventoryView.SpareArms(state.SpareArms)[0];
            Assert.AreEqual(ArmoryResult.Done, ArmoryActions.PlaceArm(state, 5, spare));
            Assert.AreSame(boost, state.Loadout[5].GetArmament(0));
            Assert.AreEqual(0, state.SpareArms.Count);
        }

        [Test]
        public void TheLastArmCannotBeRemoved()
        {
            var state = new RunState();
            state.Loadout[2] = new ArmInstance(Arm("only", 1));

            Assert.AreEqual(ArmoryResult.LastArm, ArmoryActions.RemoveArm(state, 2));
            Assert.IsNotNull(state.Loadout[2]);
        }
    }
}
