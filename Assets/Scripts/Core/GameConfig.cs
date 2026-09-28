using BulletHell.Save;
using BulletHell.Shop;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Core
{
    /// <summary>
    /// Global game settings and the start-of-run setup. Loaded from Resources/GameConfig so any scene can bootstrap
    /// the services without going through the Boot scene.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "BulletHell/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        public const string ResourcePath = "GameConfig";

        [Header("Services")]
        [SerializeField] private AssetRegistry registry;
        [SerializeField] private string saveFileName = "run_save.json";

        [Header("New run")]
        [Tooltip("Arms equipped when a new run starts. StartingLoadout for real runs, DebugLoadout for testing.")]
        [SerializeField] private ArmLoadout newRunLoadout;
        [Tooltip("The 4 ammo slots (Cross/Circle/Square/Triangle) at run start. Empty = empty slot.")]
        [SerializeField] private AmmoTypeData[] startingAmmo = new AmmoTypeData[AmmoSlotSet.Count];
        [Tooltip("TEST STOCK: armaments in the inventory at run start (duplicates allowed).")]
        [SerializeField] private ArmamentData[] startingArmaments = new ArmamentData[0];
        [Tooltip("TEST STOCK: spare arms in the arm inventory at run start.")]
        [SerializeField] private WeaponArmData[] startingSpareArms = new WeaponArmData[0];

        [Tooltip("Currency at the start of a new run (test value while there are no enemy drops).")]
        [SerializeField, Min(0)] private int startingCurrency = 300;

        [Header("Shop")]
        [SerializeField] private ShopPool shopPool;

        [Header("Rewards (stub until M5 currency drops)")]
        [SerializeField, Min(0)] private int roundRewardBase = 50;
        [SerializeField, Min(0)] private int roundRewardPerRound = 25;

        public AssetRegistry Registry => registry;
        public int StartingCurrency => startingCurrency;
        public ShopPool ShopPool => shopPool;
        public string SaveFileName => saveFileName;
        public ArmLoadout NewRunLoadout => newRunLoadout;
        public AmmoTypeData[] StartingAmmo => startingAmmo;
        public ArmamentData[] StartingArmaments => startingArmaments;
        public WeaponArmData[] StartingSpareArms => startingSpareArms;

        /// <summary>Currency granted for clearing a round.</summary>
        public int RoundReward(int round) => roundRewardBase + roundRewardPerRound * Mathf.Max(0, round - 1);
    }
}
