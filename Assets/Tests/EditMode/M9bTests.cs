using System.Collections.Generic;
using System.Linq;
using BulletHell.Core;
using BulletHell.Save;
using BulletHell.Shop;
using BulletHell.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    /// <summary>M9b: Shop stock, prices, rerolls, crate, fit/tooltip text and saving of the shop visit.</summary>
    public class M9bTests
    {
        private static T Make<T>() where T : ScriptableObject => ScriptableObject.CreateInstance<T>();

        private static WeaponArmData Arm(string id, ArmamentRarity rarity, int tier = 2, int slots = 2)
        {
            var arm = Make<WeaponArmData>();
            arm.SetId(id);
            arm.SetShopMeta(rarity, tier);
            arm.SetArmamentSlots(slots);
            return arm;
        }

        private static ArmamentData Armament(string id, ArmamentRarity rarity, int tier = 1, int maxStacks = 3, params StatModifier[] modifiers)
        {
            var armament = Make<ArmamentData>();
            armament.SetId(id);
            armament.Set(id, modifiers);
            armament.SetMeta(rarity, ArmamentTags.None, tier, maxStacks);
            return armament;
        }

        private static ShopPool Pool(WeaponArmData[] arms, ArmamentData[] armaments)
        {
            var pool = Make<ShopPool>();
            pool.Set(arms, armaments);
            return pool;
        }

        private static ShopPool BigPool()
        {
            var arms = new List<WeaponArmData>();
            var armaments = new List<ArmamentData>();
            foreach (ArmamentRarity rarity in new[] { ArmamentRarity.Common, ArmamentRarity.Rare, ArmamentRarity.Epic, ArmamentRarity.Legendary })
                for (int i = 0; i < 4; i++)
                {
                    arms.Add(Arm(rarity + "Arm" + i, rarity));
                    armaments.Add(Armament(rarity + "Armament" + i, rarity));
                }
            return Pool(arms.ToArray(), armaments.ToArray());
        }

        private static readonly RarityTable Table = Make<RarityTable>();
        private static readonly ShopTuning Tuning = Make<ShopTuning>();

        // ---- rarity table and prices

        [Test]
        public void RarerRaritiesUnlockLaterAndGrowLikelierWithTheRound()
        {
            Assert.Greater(Table.Weight(ArmamentRarity.Common, 1), 0f);
            Assert.AreEqual(0f, Table.Weight(ArmamentRarity.Epic, 1));
            Assert.AreEqual(0f, Table.Weight(ArmamentRarity.Epic, 2));
            Assert.Greater(Table.Weight(ArmamentRarity.Epic, 3), 0f);
            Assert.Greater(Table.Weight(ArmamentRarity.Epic, 7), Table.Weight(ArmamentRarity.Epic, 3));
            Assert.AreEqual(0f, Table.Weight(ArmamentRarity.Legendary, 4));
            Assert.Greater(Table.Weight(ArmamentRarity.Legendary, 5), 0f);
        }

        [Test]
        public void PricesScaleWithRarityTierAndRound()
        {
            int common = Table.Price(ArmamentRarity.Common, 1, 1);
            Assert.Greater(Table.Price(ArmamentRarity.Rare, 1, 1), common);
            Assert.Greater(Table.Price(ArmamentRarity.Epic, 1, 1), Table.Price(ArmamentRarity.Rare, 1, 1));
            Assert.Greater(Table.Price(ArmamentRarity.Common, 3, 1), common);
            Assert.Greater(Table.Price(ArmamentRarity.Common, 1, 6), common);
            Assert.AreEqual(0, Table.Price(ArmamentRarity.Rare, 2, 4) % 5);   // rounded to a multiple of 5
        }

        // ---- stock

        [Test]
        public void StockIsDeterministicForTheSameSeedRoundAndReroll()
        {
            ShopPool pool = BigPool();
            ShopStock a = ShopStock.Generate(1234, 4, 0, pool, Table, Tuning);
            ShopStock b = ShopStock.Generate(1234, 4, 0, pool, Table, Tuning);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a.Get(i).Name, b.Get(i).Name);
                Assert.AreEqual(a.Get(i).Price, b.Get(i).Price);
            }

            ShopStock rerolled = ShopStock.Generate(1234, 4, 1, pool, Table, Tuning);
            bool anyDifferent = false;
            for (int i = 0; i < a.Count; i++)
                anyDifferent |= a.Get(i).Name != rerolled.Get(i).Name;
            Assert.IsTrue(anyDifferent, "a reroll should change the stock");
        }

        [Test]
        public void StockHasTheConfiguredCardsWithNoRepeats()
        {
            ShopPool pool = BigPool();
            for (int seed = 1; seed < 40; seed++)
            {
                ShopStock stock = ShopStock.Generate(seed, 5, 0, pool, Table, Tuning);
                Assert.AreEqual(Tuning.ArmCards, stock.Arms.Length);
                Assert.AreEqual(Tuning.ArmamentCards, stock.Armaments.Length);
                Assert.AreEqual(ShopItemKind.Crate, stock.Crate.Kind);
                Assert.AreEqual(stock.Arms.Length, stock.Arms.Select(e => e.Arm).Distinct().Count());
                Assert.AreEqual(stock.Armaments.Length, stock.Armaments.Select(e => e.Armament).Distinct().Count());
            }
        }

        [Test]
        public void ARoundOnePoolWithFewCandidatesGivesFewerOffers()
        {
            ShopPool pool = Pool(new[] { Arm("only", ArmamentRarity.Common) }, new[] { Armament("a", ArmamentRarity.Common), Armament("b", ArmamentRarity.Common) });
            ShopStock stock = ShopStock.Generate(7, 1, 0, pool, Table, Tuning);
            Assert.AreEqual(1, stock.Arms.Length);
            Assert.AreEqual(2, stock.Armaments.Length);
        }

        [Test]
        public void RaritiesThatAreNotUnlockedYetNeverAppearWhileOthersExist()
        {
            ShopPool pool = BigPool();
            for (int seed = 1; seed < 60; seed++)
            {
                ShopStock stock = ShopStock.Generate(seed, 1, 0, pool, Table, Tuning);
                foreach (ShopEntry entry in stock.Armaments)
                    Assert.IsTrue(entry.Armament.Rarity == ArmamentRarity.Common || entry.Armament.Rarity == ArmamentRarity.Rare, entry.Name);
            }
        }

        [Test]
        public void BetterRaritiesAreLikelierInLaterRounds()
        {
            ShopPool pool = BigPool();
            int early = 0, late = 0;
            for (int seed = 1; seed <= 300; seed++)
            {
                foreach (ShopEntry e in ShopStock.Generate(seed, 3, 0, pool, Table, Tuning).Armaments)
                    if (e.Armament.Rarity >= ArmamentRarity.Epic)
                        early++;
                foreach (ShopEntry e in ShopStock.Generate(seed, 10, 0, pool, Table, Tuning).Armaments)
                    if (e.Armament.Rarity >= ArmamentRarity.Epic)
                        late++;
            }
            Assert.Greater(late, early);
        }

        [Test]
        public void StockPricesFollowTheTable()
        {
            ShopPool pool = BigPool();
            ShopStock stock = ShopStock.Generate(5, 6, 0, pool, Table, Tuning);
            foreach (ShopEntry e in stock.Armaments)
                Assert.AreEqual(Table.Price(e.Armament.Rarity, e.Armament.PriceTier, 6), e.Price);
            foreach (ShopEntry e in stock.Arms)
                Assert.AreEqual(Table.Price(e.Arm.Rarity, e.Arm.PriceTier, 6), e.Price);
            Assert.AreEqual(Table.Price(Tuning.CrateRarity, Tuning.CratePriceTier, 6), stock.Crate.Price);
        }

        // ---- buying, reroll, crate

        private static RunState State(int currency)
        {
            var state = new RunState { Currency = currency };
            state.Shop = ShopVisit.Create(99);
            return state;
        }

        [Test]
        public void BuyingSpendsCurrencyMarksSoldAndRefusesTwice()
        {
            ArmamentData a = Armament("a", ArmamentRarity.Common);
            RunState state = State(100);
            var entry = new ShopEntry { Kind = ShopItemKind.Armament, Armament = a, Price = 40 };

            Assert.AreEqual(PurchaseResult.Bought, ShopService.BuyOffer(state, state.Shop, 2, entry));
            Assert.AreEqual(60, state.Currency);
            Assert.IsTrue(state.Shop.IsSold(2));
            Assert.AreEqual(1, state.Armaments.Count);

            Assert.AreEqual(PurchaseResult.AlreadySold, ShopService.BuyOffer(state, state.Shop, 2, entry));
            Assert.AreEqual(60, state.Currency);
            Assert.IsFalse(state.Shop.IsSold(1));
        }

        [Test]
        public void ARefusedPurchaseChangesNothing()
        {
            RunState state = State(30);
            var entry = new ShopEntry { Kind = ShopItemKind.Armament, Armament = Armament("a", ArmamentRarity.Common), Price = 40 };
            Assert.AreEqual(PurchaseResult.NotEnoughCurrency, ShopService.BuyOffer(state, state.Shop, 0, entry));
            Assert.AreEqual(30, state.Currency);
            Assert.IsFalse(state.Shop.IsSold(0));
            Assert.AreEqual(0, state.Armaments.Count);
        }

        [Test]
        public void RerollCostRisesEveryRerollAndNewStockClearsSoldFlags()
        {
            RunState state = State(1000);
            ShopVisit visit = state.Shop;
            Assert.AreEqual(Tuning.RerollBaseCost, ShopService.RerollCost(visit, Tuning));

            visit.MarkSold(0);
            int before = state.Currency;
            Assert.IsTrue(ShopService.TryReroll(state, visit, Tuning));
            Assert.AreEqual(before - Tuning.RerollBaseCost, state.Currency);
            Assert.AreEqual(1, visit.Rerolls);
            Assert.AreEqual(0, visit.SoldMask);
            Assert.AreEqual(Tuning.RerollBaseCost + Tuning.RerollStep, ShopService.RerollCost(visit, Tuning));

            ShopService.TryReroll(state, visit, Tuning);
            Assert.AreEqual(Tuning.RerollBaseCost + 2 * Tuning.RerollStep, ShopService.RerollCost(visit, Tuning));
        }

        [Test]
        public void ACostOfMoreThanYouHaveRefusesTheReroll()
        {
            RunState state = State(Tuning.RerollBaseCost - 1);
            Assert.IsFalse(ShopService.TryReroll(state, state.Shop, Tuning));
            Assert.AreEqual(Tuning.RerollBaseCost - 1, state.Currency);
            Assert.AreEqual(0, state.Shop.Rerolls);
        }

        [Test]
        public void TheCrateOffersDistinctChoicesAndPaysOnlyOnceOnePicked()
        {
            ShopPool pool = BigPool();
            RunState state = State(500);
            ShopVisit visit = state.Shop;
            ShopStock stock = ShopStock.Generate(visit.Seed, 3, 0, pool, Table, Tuning);
            int crate = stock.CrateIndex;

            Assert.AreEqual(PurchaseResult.Bought, ShopService.BuyCrate(state, visit, crate, stock.Crate));
            Assert.AreEqual(500 - stock.Crate.Price, state.Currency);
            Assert.IsTrue(visit.CratePending);
            Assert.IsFalse(visit.IsSold(crate));                                   // not SOLD until a pick is made
            Assert.IsFalse(ShopService.TryReroll(state, visit, Tuning));           // no reroll while the choice is open

            List<ArmamentData> choices = ShopStock.CrateChoices(visit.Seed, 3, 0, pool, Table, Tuning);
            Assert.AreEqual(Tuning.CrateChoices, choices.Count);
            Assert.AreEqual(choices.Count, choices.Distinct().Count());
            CollectionAssert.AreEqual(choices, ShopStock.CrateChoices(visit.Seed, 3, 0, pool, Table, Tuning));

            Assert.IsTrue(ShopService.PickCrate(state, visit, crate, choices[1]));
            Assert.AreSame(choices[1], state.Armaments.Get(0));
            Assert.IsTrue(visit.IsSold(crate));
            Assert.IsFalse(visit.CratePending);
            Assert.IsFalse(ShopService.PickCrate(state, visit, crate, choices[0]));
        }

        [Test]
        public void ACrateYouCannotAffordChangesNothing()
        {
            RunState state = State(5);
            var crate = new ShopEntry { Kind = ShopItemKind.Crate, Price = 100 };
            Assert.AreEqual(PurchaseResult.NotEnoughCurrency, ShopService.BuyCrate(state, state.Shop, 5, crate));
            Assert.AreEqual(5, state.Currency);
            Assert.IsFalse(state.Shop.CratePending);
        }

        // ---- fit and tooltip

        [Test]
        public void FitListsEveryArmWithItsFreeSlotsAndTheStatsAfter()
        {
            WeaponArmData two = Arm("two", ArmamentRarity.Common, 2, 2);
            WeaponArmData one = Arm("one", ArmamentRarity.Common, 2, 1);
            ArmamentData damage = Armament("dmg", ArmamentRarity.Common, 1, 3, new StatModifier(StatType.Damage, ModifierMode.Percent, 50f));
            RunState state = State(0);
            state.Loadout[2] = new ArmInstance(two);
            state.Loadout[6] = new ArmInstance(one);
            var inventory = new ArmamentInventory();
            inventory.Add(damage);
            state.Loadout[6].TryEquip(damage, inventory);           // the 1-slot arm is now full
            state.SpareArms.Add(new ArmInstance(two));

            List<ArmFit> fits = ShopFit.ForArmament(state, damage);

            Assert.AreEqual(3, fits.Count);
            Assert.IsTrue(fits[0].CanEquip);
            Assert.AreEqual(2, fits[0].FreeSlots);
            Assert.AreEqual(fits[0].Before.Damage * 1.5f, fits[0].After.Damage, 0.001f);
            StringAssert.Contains("(E)", fits[0].Label);
            Assert.IsFalse(fits[1].CanEquip);
            Assert.AreEqual(0, fits[1].FreeSlots);
            StringAssert.Contains("no free slot", fits[1].Reason);
            StringAssert.Contains("spare", fits[2].Label);
        }

        [Test]
        public void ARespectedStackLimitMakesAnArmNotFit()
        {
            WeaponArmData arm = Arm("arm", ArmamentRarity.Common, 2, 3);
            ArmamentData single = Armament("single", ArmamentRarity.Rare, 2, 1);
            var inventory = new ArmamentInventory();
            inventory.Add(single);
            var instance = new ArmInstance(arm);
            instance.TryEquip(single, inventory);

            ArmFit fit = ShopFit.Evaluate(instance, single, "arm");
            Assert.IsFalse(fit.CanEquip);
            StringAssert.Contains("limited to 1", fit.Reason);
        }

        [Test]
        public void TooltipTextIsGeneratedFromTheDataWithFitAndBeforeAfter()
        {
            WeaponArmData arm = Arm("Blue", ArmamentRarity.Common, 2, 2);
            ArmamentData velocity = Armament("Velocity", ArmamentRarity.Common, 1, 4, new StatModifier(StatType.ProjectileSpeed, ModifierMode.Percent, 25f));
            RunState state = State(0);
            state.Loadout[2] = new ArmInstance(arm);
            var entry = new ShopEntry { Kind = ShopItemKind.Armament, Armament = velocity, Price = 40 };

            string brief = string.Join("\n", ShopDescriber.Lines(entry, state, Tuning, false));
            StringAssert.Contains("+25% bullet speed", brief);
            StringAssert.Contains("Fits:", brief);
            StringAssert.Contains("2 free slots", brief);
            StringAssert.Contains("bullet speed", brief);
            StringAssert.Contains("->", brief);

            string detailed = string.Join("\n", ShopDescriber.Lines(entry, state, Tuning, true));
            StringAssert.Contains("damage", detailed);   // the detailed view lists every stat
            Assert.Greater(detailed.Length, brief.Length);
        }

        [Test]
        public void ArmTooltipShowsSlotsAndStats()
        {
            var entry = new ShopEntry { Kind = ShopItemKind.Arm, Arm = Arm("Red", ArmamentRarity.Epic, 3, 3), Price = 200 };
            string text = string.Join("\n", ShopDescriber.Lines(entry, State(0), Tuning, false));
            StringAssert.Contains("Epic", text);
            StringAssert.Contains("3 armament slots", text);
        }

        // ---- saving the visit

        [Test]
        public void TheShopVisitSurvivesSaveAndContinue()
        {
            WeaponArmData arm = Arm("red", ArmamentRarity.Common, 2, 2);
            ArmamentData bought = Armament("Armament_A", ArmamentRarity.Common);
            var registry = Make<AssetRegistry>();
            registry.Set(new[] { arm }, new[] { bought }, new AmmoTypeData[0]);

            var state = new RunState { Round = 4, Currency = 120 };
            state.Loadout[2] = new ArmInstance(arm);
            state.Shop = new ShopVisit { Seed = 424242, Rerolls = 2, SoldMask = 0b101 };
            state.Armaments.Add(bought);

            SaveData data = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(RunSaveMapper.ToSave(state)));
            Assert.IsTrue(RunSaveMapper.TryFromSave(data, registry, out RunState loaded));

            Assert.AreEqual(120, loaded.Currency);
            Assert.AreEqual(1, loaded.Armaments.Count);
            Assert.AreEqual(424242, loaded.Shop.Seed);
            Assert.AreEqual(2, loaded.Shop.Rerolls);
            Assert.IsTrue(loaded.Shop.IsSold(0));
            Assert.IsFalse(loaded.Shop.IsSold(1));
            Assert.IsTrue(loaded.Shop.IsSold(2));

            // The same visit gives the same stock after Continue.
            ShopPool pool = BigPool();
            ShopStock before = ShopStock.Generate(state.Shop.Seed, state.Round, state.Shop.Rerolls, pool, Table, Tuning);
            ShopStock after = ShopStock.Generate(loaded.Shop.Seed, loaded.Round, loaded.Shop.Rerolls, pool, Table, Tuning);
            for (int i = 0; i < before.Count; i++)
                Assert.AreEqual(before.Get(i).Name, after.Get(i).Name);
        }

        [Test]
        public void AnOldSaveWithoutAShopVisitStillLoads()
        {
            WeaponArmData arm = Arm("red", ArmamentRarity.Common);
            var registry = Make<AssetRegistry>();
            registry.Set(new[] { arm }, new ArmamentData[0], new AmmoTypeData[0]);
            var data = new SaveData
            {
                round = 2,
                currency = 50,
                loadout = new[] { new ArmSave { armId = "red", armamentIds = new string[0] }, null, null, null, null, null, null, null },
            };

            Assert.IsTrue(RunSaveMapper.TryFromSave(data, registry, out RunState loaded));
            Assert.IsNull(loaded.Shop);            // RunManager.ContinueRun creates a fresh visit for it
        }
    }
}
