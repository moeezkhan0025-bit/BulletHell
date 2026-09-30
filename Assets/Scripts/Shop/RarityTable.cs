using System;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Shop
{
    /// <summary>How one rarity behaves in the Shop.</summary>
    [Serializable]
    public struct RarityEntry
    {
        public ArmamentRarity Rarity;
        [Tooltip("Card frame tint and rarity label colour.")]
        public Color Color;
        [Min(0f)] public float BaseWeight;
        [Tooltip("Weight gained for every round after First Round (rarer items get likelier later).")]
        public float WeightPerRound;
        [Min(1)] public int FirstRound;
        [Tooltip("Price multiplier for this rarity.")]
        [Min(0.1f)] public float PriceMultiplier;
    }

    /// <summary>
    /// Weights (by rarity and round), prices (by rarity, price tier and round) and frame colours for Shop stock. All the
    /// numbers live here; the code only reads them.
    /// </summary>
    [CreateAssetMenu(fileName = "RarityTable", menuName = "BulletHell/Rarity Table")]
    public sealed class RarityTable : ScriptableObject
    {
        [SerializeField]
        private RarityEntry[] entries =
        {
            new RarityEntry { Rarity = ArmamentRarity.Common, Color = new Color(0.78f, 0.78f, 0.8f), BaseWeight = 60f, WeightPerRound = 0f, FirstRound = 1, PriceMultiplier = 1f },
            new RarityEntry { Rarity = ArmamentRarity.Rare, Color = new Color(0.35f, 0.65f, 1f), BaseWeight = 25f, WeightPerRound = 5f, FirstRound = 1, PriceMultiplier = 1.6f },
            new RarityEntry { Rarity = ArmamentRarity.Epic, Color = new Color(0.72f, 0.4f, 0.95f), BaseWeight = 8f, WeightPerRound = 4f, FirstRound = 3, PriceMultiplier = 2.5f },
            new RarityEntry { Rarity = ArmamentRarity.Legendary, Color = new Color(1f, 0.72f, 0.2f), BaseWeight = 1f, WeightPerRound = 2f, FirstRound = 5, PriceMultiplier = 4f },
        };

        [Tooltip("Base price for price tier 1..5, before the rarity multiplier and round growth.")]
        [SerializeField] private int[] tierBasePrice = { 40, 70, 110, 160, 230 };
        [Tooltip("Price growth per round after round 1 (0.12 = +12% per round).")]
        [SerializeField, Min(0f)] private float priceGrowthPerRound = 0.12f;
        [Tooltip("Prices are rounded to a multiple of this.")]
        [SerializeField, Min(1)] private int priceRounding = 5;

        public RarityEntry Entry(ArmamentRarity rarity)
        {
            for (int i = 0; i < entries.Length; i++)
                if (entries[i].Rarity == rarity)
                    return entries[i];
            return new RarityEntry { Rarity = rarity, Color = Color.white, BaseWeight = 0f, FirstRound = 1, PriceMultiplier = 1f };
        }

        public Color ColorOf(ArmamentRarity rarity) => Entry(rarity).Color;

        /// <summary>How likely a rarity is to be picked in a given round (0 before its first round).</summary>
        public float Weight(ArmamentRarity rarity, int round)
        {
            RarityEntry entry = Entry(rarity);
            if (round < entry.FirstRound)
                return 0f;
            return Mathf.Max(0f, entry.BaseWeight + entry.WeightPerRound * (round - entry.FirstRound));
        }

        /// <summary>Price of an item: tier base x rarity multiplier x round growth, rounded.</summary>
        public int Price(ArmamentRarity rarity, int priceTier, int round)
        {
            int tier = Mathf.Clamp(priceTier, 1, tierBasePrice.Length) - 1;
            float price = tierBasePrice[tier] * Entry(rarity).PriceMultiplier * (1f + priceGrowthPerRound * Mathf.Max(0, round - 1));
            int step = Mathf.Max(1, priceRounding);
            return Mathf.Max(step, Mathf.RoundToInt(price / step) * step);
        }
    }
}
