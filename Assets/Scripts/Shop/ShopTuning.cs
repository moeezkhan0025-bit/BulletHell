using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Shop
{
    /// <summary>The Shop's layout counts, crate, reroll and debug numbers.</summary>
    [CreateAssetMenu(fileName = "ShopTuning", menuName = "BulletHell/Shop Tuning")]
    public sealed class ShopTuning : ScriptableObject
    {
        [Header("Stock")]
        [SerializeField, Range(1, 4)] private int armCards = 2;
        [SerializeField, Range(1, 5)] private int armamentCards = 3;

        [Header("Crate (pick 1 of N armaments)")]
        [SerializeField] private ArmamentRarity crateRarity = ArmamentRarity.Rare;
        [SerializeField, Range(1, 5)] private int cratePriceTier = 2;
        [SerializeField, Range(2, 5)] private int crateChoices = 3;

        [Header("Reroll")]
        [SerializeField, Min(0)] private int rerollBaseCost = 15;
        [Tooltip("Added to the reroll cost for every reroll already done this visit.")]
        [SerializeField, Min(0)] private int rerollStep = 10;

        [Header("Debug")]
        [Tooltip("Currency the debug key gives.")]
        [SerializeField, Min(1)] private int debugCurrencyGrant = 100;

        public int ArmCards => armCards;
        public int ArmamentCards => armamentCards;
        public ArmamentRarity CrateRarity => crateRarity;
        public int CratePriceTier => cratePriceTier;
        public int CrateChoices => crateChoices;
        public int RerollBaseCost => rerollBaseCost;
        public int RerollStep => rerollStep;
        public int DebugCurrencyGrant => debugCurrencyGrant;

        /// <summary>Cost of the next reroll after the given number of rerolls this visit.</summary>
        public int RerollCost(int rerollsDone) => rerollBaseCost + rerollStep * Mathf.Max(0, rerollsDone);
    }
}
