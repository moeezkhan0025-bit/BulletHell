using System;
using System.Collections.Generic;
using BulletHell.Weapons;

namespace BulletHell.Shop
{
    /// <summary>
    /// The stock of one Shop visit: arm offers, armament offers and the crate. Generated from a seed, so the same
    /// (seed, round, reroll) always gives the same stock. Items are picked by rarity weight (from the RarityTable, which
    /// depends on the round), without repeating an item inside one stock.
    /// </summary>
    public sealed class ShopStock
    {
        public ShopEntry[] Arms = new ShopEntry[0];
        public ShopEntry[] Armaments = new ShopEntry[0];
        public ShopEntry Crate;

        /// <summary>Number of offers: arm cards, armament cards, then the crate.</summary>
        public int Count => Arms.Length + Armaments.Length + 1;

        /// <summary>Offer by number: arms first, then armaments, then the crate.</summary>
        public ShopEntry Get(int offer)
        {
            if (offer < Arms.Length)
                return Arms[offer];
            offer -= Arms.Length;
            return offer < Armaments.Length ? Armaments[offer] : Crate;
        }

        public int CrateIndex => Arms.Length + Armaments.Length;

        public static ShopStock Generate(int seed, int round, int reroll, ShopPool pool, RarityTable table, ShopTuning tuning)
        {
            var random = new Random(Mix(seed, round, reroll, 1));
            var stock = new ShopStock();

            var arms = new List<WeaponArmData>();
            for (int i = 0; pool != null && i < pool.Arms.Count; i++)
                if (pool.Arms[i] != null)
                    arms.Add(pool.Arms[i]);
            var armaments = new List<ArmamentData>();
            for (int i = 0; pool != null && i < pool.Armaments.Count; i++)
                if (pool.Armaments[i] != null)
                    armaments.Add(pool.Armaments[i]);

            List<WeaponArmData> pickedArms = PickWeighted(arms, tuning.ArmCards, a => table.Weight(a.Rarity, round), random);
            stock.Arms = new ShopEntry[pickedArms.Count];
            for (int i = 0; i < pickedArms.Count; i++)
                stock.Arms[i] = new ShopEntry
                {
                    Kind = ShopItemKind.Arm,
                    Arm = pickedArms[i],
                    Price = table.Price(pickedArms[i].Rarity, pickedArms[i].PriceTier, round),
                };

            List<ArmamentData> pickedArmaments = PickWeighted(armaments, tuning.ArmamentCards, a => table.Weight(a.Rarity, round), random);
            stock.Armaments = new ShopEntry[pickedArmaments.Count];
            for (int i = 0; i < pickedArmaments.Count; i++)
                stock.Armaments[i] = new ShopEntry
                {
                    Kind = ShopItemKind.Armament,
                    Armament = pickedArmaments[i],
                    Price = table.Price(pickedArmaments[i].Rarity, pickedArmaments[i].PriceTier, round),
                };

            stock.Crate = new ShopEntry
            {
                Kind = ShopItemKind.Crate,
                Price = table.Price(tuning.CrateRarity, tuning.CratePriceTier, round),
            };
            return stock;
        }

        /// <summary>The armaments a crate offers (pick one). Same seed, round and reroll always give the same choices.</summary>
        public static List<ArmamentData> CrateChoices(int seed, int round, int reroll, ShopPool pool, RarityTable table, ShopTuning tuning)
        {
            var random = new Random(Mix(seed, round, reroll, 2));
            var armaments = new List<ArmamentData>();
            for (int i = 0; pool != null && i < pool.Armaments.Count; i++)
                if (pool.Armaments[i] != null)
                    armaments.Add(pool.Armaments[i]);
            return PickWeighted(armaments, tuning.CrateChoices, a => table.Weight(a.Rarity, round), random);
        }

        // Weighted pick without replacement. When every remaining weight is zero (nothing unlocked yet) the rest are equal.
        private static List<T> PickWeighted<T>(List<T> candidates, int count, Func<T, float> weight, Random random)
        {
            var pool = new List<T>(candidates);
            var picked = new List<T>();
            while (picked.Count < count && pool.Count > 0)
            {
                float total = 0f;
                foreach (T item in pool)
                    total += weight(item);

                int index;
                if (total <= 0f)
                {
                    index = random.Next(pool.Count);
                }
                else
                {
                    double roll = random.NextDouble() * total;
                    index = pool.Count - 1;
                    for (int i = 0; i < pool.Count; i++)
                    {
                        roll -= weight(pool[i]);
                        if (roll < 0.0)
                        {
                            index = i;
                            break;
                        }
                    }
                    // A zero-weight item can only be reached by rounding at the very end: skip back to a weighted one.
                    while (index > 0 && weight(pool[index]) <= 0f)
                        index--;
                }
                picked.Add(pool[index]);
                pool.RemoveAt(index);
            }
            return picked;
        }

        private static int Mix(int seed, int round, int reroll, int salt) =>
            unchecked(seed * 73856093 ^ round * 19349663 ^ reroll * 83492791 ^ salt * unchecked((int)2654435761u));
    }
}
