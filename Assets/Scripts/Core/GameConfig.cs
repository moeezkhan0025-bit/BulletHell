using BulletHell.Arena;
using BulletHell.Enemies;
using BulletHell.Save;
using BulletHell.Settings;
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
        [Tooltip("Settings and profile (chosen cosmetics) files. Separate from the run save; never deleted by Game Over or New Game.")]
        [SerializeField] private string settingsFileName = "settings.json";
        [SerializeField] private string profileFileName = "profile.json";
        [SerializeField] private SettingsDefaults settingsDefaults;

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

        [Header("Rounds")]
        [Tooltip("Round 1, 2, 3... in order. Rounds past the end loop over the rounds from Endless Loop Start Round on.")]
        [SerializeField] private RoundData[] rounds = new RoundData[0];
        [Tooltip("After the last authored round, play continues by repeating the authored rounds from this one (1-based) to the end.")]
        [SerializeField, Min(1)] private int endlessLoopStartRound = 4;
        [SerializeField] private DifficultyCurve difficulty;
        [Tooltip("The arena for rounds that don't name one.")]
        [SerializeField] private ArenaData defaultArena;
        [Tooltip("How the 3/4 art sits over the flat gameplay plane (footprints, bullet reach, hurtboxes).")]
        [SerializeField] private PerspectiveTuning perspective;

        public AssetRegistry Registry => registry;
        public PerspectiveTuning Perspective => perspective != null ? perspective : PerspectiveTuning.Fallback;
        public DifficultyCurve Difficulty => difficulty;
        public ArenaData DefaultArena => defaultArena;

        /// <summary>The arena a round is fought in: the round's own, else the default. Null only if neither is set.</summary>
        public ArenaData GetArena(int round)
        {
            RoundData data = GetRound(round);
            return data != null && data.Arena != null ? data.Arena : defaultArena;
        }
        public int StartingCurrency => startingCurrency;
        public ShopPool ShopPool => shopPool;
        public string SaveFileName => saveFileName;
        public string SettingsFileName => settingsFileName;
        public string ProfileFileName => profileFileName;
        public SettingsDefaults SettingsDefaults => settingsDefaults;
        public ArmLoadout NewRunLoadout => newRunLoadout;
        public AmmoTypeData[] StartingAmmo => startingAmmo;
        public ArmamentData[] StartingArmaments => startingArmaments;
        public WeaponArmData[] StartingSpareArms => startingSpareArms;

        /// <summary>
        /// The round to play for a round number (1-based). Past the authored rounds it loops over the ones from
        /// Endless Loop Start Round on, so with bosses on rounds 5 and 7 a boss comes every second round forever.
        /// </summary>
        public RoundData GetRound(int round)
        {
            if (rounds == null || rounds.Length == 0)
                return null;
            if (round <= rounds.Length)
                return rounds[Mathf.Max(1, round) - 1];

            int loopStart = Mathf.Clamp(endlessLoopStartRound, 1, rounds.Length);
            int loopLength = rounds.Length - loopStart + 1;
            return rounds[loopStart - 1 + (round - loopStart) % loopLength];
        }

#if UNITY_EDITOR
        /// <summary>Editor-only: used by tests.</summary>
        public void SetRounds(RoundData[] newRounds, int loopStartRound)
        {
            rounds = newRounds;
            endlessLoopStartRound = loopStartRound;
        }

        public void SetDefaultArena(ArenaData arena) => defaultArena = arena;
#endif
    }
}
