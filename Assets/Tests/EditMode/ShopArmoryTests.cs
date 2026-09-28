using BulletHell.Armory;
using BulletHell.Core;
using BulletHell.Save;
using BulletHell.Shop;
using BulletHell.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    public class ShopArmoryTests
    {
        private static WeaponArmData Arm(string id)
        {
            var arm = ScriptableObject.CreateInstance<WeaponArmData>();
            arm.SetId(id);
            return arm;
        }

        private static ArmamentData Armament(string id)
        {
            var armament = ScriptableObject.CreateInstance<ArmamentData>();
            armament.SetId(id);
            return armament;
        }

        private static ShopEntry ArmEntry(WeaponArmData arm, int price) =>
            new ShopEntry { Kind = ShopItemKind.Arm, Arm = arm, Price = price };

        private static ShopEntry ArmamentEntry(ArmamentData armament, int price) =>
            new ShopEntry { Kind = ShopItemKind.Armament, Armament = armament, Price = price };

        private static RunState StateWithOneArm(int currency = 100)
        {
            var state = new RunState { Currency = currency };
            state.Loadout[0] = new ArmInstance(Arm("start"));
            return state;
        }

        // ---- shop

        [Test]
        public void BuyingAnArmamentSpendsCurrencyAndFillsTheInventory()
        {
            RunState state = StateWithOneArm(100);
            ArmamentData armament = Armament("a");

            Assert.AreEqual(PurchaseResult.Bought, ShopService.TryBuy(state, ArmamentEntry(armament, 40)));

            Assert.AreEqual(60, state.Currency);
            Assert.AreEqual(1, state.Armaments.Count);
            Assert.AreSame(armament, state.Armaments.Get(0));
        }

        [Test]
        public void BuyingAnArmAddsAFreshInstanceToTheArmInventory()
        {
            RunState state = StateWithOneArm(200);
            WeaponArmData arm = Arm("ember");

            Assert.AreEqual(PurchaseResult.Bought, ShopService.TryBuy(state, ArmEntry(arm, 150)));
            Assert.AreEqual(PurchaseResult.Bought, ShopService.TryBuy(state, ArmEntry(arm, 50)));

            Assert.AreEqual(0, state.Currency);
            Assert.AreEqual(2, state.SpareArms.Count);
            Assert.AreNotSame(state.SpareArms.Get(0), state.SpareArms.Get(1)); // two separate instances
        }

        [Test]
        public void CannotBuyWithoutEnoughCurrency_NothingChanges()
        {
            RunState state = StateWithOneArm(30);

            Assert.AreEqual(PurchaseResult.NotEnoughCurrency, ShopService.TryBuy(state, ArmamentEntry(Armament("a"), 31)));

            Assert.AreEqual(30, state.Currency);
            Assert.AreEqual(0, state.Armaments.Count);
        }

        [Test]
        public void ExactPriceCanBeBoughtAndInvalidEntriesAreRefused()
        {
            RunState state = StateWithOneArm(40);
            Assert.AreEqual(PurchaseResult.Bought, ShopService.TryBuy(state, ArmamentEntry(Armament("a"), 40)));
            Assert.AreEqual(0, state.Currency);

            var empty = new ShopEntry { Kind = ShopItemKind.Arm, Price = 0 };
            Assert.AreEqual(PurchaseResult.InvalidEntry, ShopService.TryBuy(state, empty));
        }

        // ---- armory: arms

        [Test]
        public void PlacingASpareArmMovesItIntoTheEmptySlot()
        {
            RunState state = StateWithOneArm();
            var spare = new ArmInstance(Arm("spare"));
            state.SpareArms.Add(spare);

            Assert.AreEqual(ArmoryResult.Done, ArmoryActions.PlaceArm(state, 3, spare));

            Assert.AreSame(spare, state.Loadout[3]);
            Assert.AreEqual(0, state.SpareArms.Count);
        }

        [Test]
        public void CannotPlaceIntoAnOccupiedSlotOrPlaceAnArmThatIsNotSpare()
        {
            RunState state = StateWithOneArm();
            var spare = new ArmInstance(Arm("spare"));
            state.SpareArms.Add(spare);

            Assert.AreEqual(ArmoryResult.SlotOccupied, ArmoryActions.PlaceArm(state, 0, spare));
            Assert.AreEqual(ArmoryResult.NotInInventory, ArmoryActions.PlaceArm(state, 2, new ArmInstance(Arm("stranger"))));
            Assert.AreEqual(1, state.SpareArms.Count);
            Assert.IsNull(state.Loadout[2]);
        }

        [Test]
        public void RemovingAnArmReturnsItToInventoryWithItsArmaments()
        {
            RunState state = StateWithOneArm();
            state.Loadout[4] = new ArmInstance(Arm("second"));
            ArmamentData armament = Armament("a");
            state.Armaments.Add(armament);
            ArmInstance second = state.Loadout[4];
            second.TryEquip(armament, state.Armaments);

            Assert.AreEqual(ArmoryResult.Done, ArmoryActions.RemoveArm(state, 4));

            Assert.IsNull(state.Loadout[4]);
            Assert.AreEqual(1, state.SpareArms.Count);
            Assert.AreSame(second, state.SpareArms.Get(0));
            Assert.AreSame(armament, second.GetArmament(0));  // armament still attached
            Assert.AreEqual(0, state.Armaments.Count);         // not dumped into the armament inventory
        }

        [Test]
        public void TheLastArmCannotBeRemoved()
        {
            RunState state = StateWithOneArm();
            Assert.AreEqual(ArmoryResult.LastArm, ArmoryActions.RemoveArm(state, 0));
            Assert.IsNotNull(state.Loadout[0]);
            Assert.AreEqual(ArmoryResult.SlotEmpty, ArmoryActions.RemoveArm(state, 5));
        }

        [Test]
        public void RemovedArmCanBePlacedBackInAnotherSlotKeepingItsArmaments()
        {
            RunState state = StateWithOneArm();
            state.Loadout[1] = new ArmInstance(Arm("second"));
            ArmamentData armament = Armament("a");
            state.Armaments.Add(armament);
            state.Loadout[1].TryEquip(armament, state.Armaments);
            ArmInstance second = state.Loadout[1];

            ArmoryActions.RemoveArm(state, 1);
            Assert.AreEqual(ArmoryResult.Done, ArmoryActions.PlaceArm(state, 6, second));

            Assert.AreSame(second, state.Loadout[6]);
            Assert.AreSame(armament, state.Loadout[6].GetArmament(0));
        }

        // ---- everything survives a save and load

        [Test]
        public void ShopAndArmoryResultsSurviveSaveAndContinue()
        {
            WeaponArmData start = Arm("start"), ember = Arm("ember");
            ArmamentData dmg = Armament("dmg"), pierce = Armament("pierce");
            var registry = ScriptableObject.CreateInstance<AssetRegistry>();
            registry.Set(new[] { start, ember }, new[] { dmg, pierce }, new AmmoTypeData[0]);

            var state = new RunState { Round = 2, Currency = 500 };
            state.Loadout[0] = new ArmInstance(start);

            // Shop: buy an arm and two armaments.
            ShopService.TryBuy(state, ArmEntry(ember, 150));
            ShopService.TryBuy(state, ArmamentEntry(dmg, 40));
            ShopService.TryBuy(state, ArmamentEntry(pierce, 90));

            // Armory: place the new arm, equip both armaments on it, then remove the starting arm's slot use.
            ArmInstance boughtArm = state.SpareArms.Get(0);
            ArmoryActions.PlaceArm(state, 2, boughtArm);
            boughtArm.TryEquipAt(0, dmg, state.Armaments);
            boughtArm.TryEquipAt(2, pierce, state.Armaments);
            state.Armaments.Add(dmg); // one spare copy left in the inventory

            SaveData data = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(RunSaveMapper.ToSave(state)));
            Assert.IsTrue(RunSaveMapper.TryFromSave(data, registry, out RunState loaded));

            Assert.AreEqual(220, loaded.Currency); // 500 - 150 - 40 - 90
            Assert.AreEqual(2, loaded.Round);
            Assert.AreSame(start, loaded.Loadout[0].Data);
            Assert.AreSame(ember, loaded.Loadout[2].Data);
            Assert.AreSame(dmg, loaded.Loadout[2].GetArmament(0));
            Assert.IsNull(loaded.Loadout[2].GetArmament(1));
            Assert.AreSame(pierce, loaded.Loadout[2].GetArmament(2));
            Assert.AreEqual(0, loaded.SpareArms.Count);
            Assert.AreEqual(1, loaded.Armaments.Count);
            Assert.AreSame(dmg, loaded.Armaments.Get(0));
        }

        [Test]
        public void ARemovedArmWithArmamentsSurvivesSaveAndContinue()
        {
            WeaponArmData start = Arm("start"), second = Arm("second");
            ArmamentData dmg = Armament("dmg");
            var registry = ScriptableObject.CreateInstance<AssetRegistry>();
            registry.Set(new[] { start, second }, new[] { dmg }, new AmmoTypeData[0]);

            RunState state = StateWithOneArm();
            state.Loadout[5] = new ArmInstance(second);
            state.Armaments.Add(dmg);
            state.Loadout[5].TryEquip(dmg, state.Armaments);
            ArmoryActions.RemoveArm(state, 5);

            SaveData data = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(RunSaveMapper.ToSave(state)));
            Assert.IsTrue(RunSaveMapper.TryFromSave(data, registry, out RunState loaded));

            Assert.IsNull(loaded.Loadout[5]);
            Assert.AreEqual(1, loaded.SpareArms.Count);
            Assert.AreSame(second, loaded.SpareArms.Get(0).Data);
            Assert.AreSame(dmg, loaded.SpareArms.Get(0).GetArmament(0));
        }
    }
}
