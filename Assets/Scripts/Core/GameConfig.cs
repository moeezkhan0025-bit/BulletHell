using BulletHell.Arena;
using BulletHell.Audio;
using BulletHell.Enemies;
using BulletHell.Feedback;
using BulletHell.Save;
using BulletHell.Settings;
using BulletHell.Shop;
using BulletHell.UI;
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
        [Tooltip("Local playtest log: one row per finished run (round reached, cause of death, time and damage per round, currency, purchases, boss phase). Never leaves the machine.")]
        [SerializeField] private string telemetryFileName = "playtest_runs.csv";
        [Tooltip("Sounds, music, mixer and audio tuning (Data/Audio). Empty = the game is silent.")]
        [SerializeField] private AudioLibrary audioLibrary;
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
        [Tooltip("Rarity weights by round, prices and frame colours for Shop stock.")]
        [SerializeField] private RarityTable rarityTable;
        [Tooltip("Shop card counts, crate, reroll cost and the debug currency grant.")]
        [SerializeField] private ShopTuning shopTuning;

        [Header("Rounds")]
        [Tooltip("Round 1, 2, 3... in order. Rounds past the end loop over the rounds from Endless Loop Start Round on.")]
        [SerializeField] private RoundData[] rounds = new RoundData[0];
        [Tooltip("After the last authored round, play continues by repeating the authored rounds from this one (1-based) to the end.")]
        [SerializeField, Min(1)] private int endlessLoopStartRound = 4;
        [SerializeField] private DifficultyCurve difficulty;
        [Tooltip("The layout for rounds that don't name one.")]
        [SerializeField] private ArenaLayoutData defaultLayout;
        [Tooltip("How the 3/4 art sits over the flat gameplay plane (footprints, bullet reach, hurtboxes).")]
        [SerializeField] private PerspectiveTuning perspective;
        [Tooltip("The painted arena backdrop. Empty = placeholder arena.")]
        [SerializeField] private ArenaArt arenaArt;
        [Tooltip("Sprites, colors, font and sounds for every UI screen (roles, not pack files).")]
        [SerializeField] private UITheme uiTheme;
        [Tooltip("Button prompt text and pictures per device family (PlayStation, Xbox, Nintendo, Touch, Keyboard).")]
        [SerializeField] private BulletHell.UI.ButtonGlyphLibrary buttonGlyphs;
        [Tooltip("Colours of every enemy bullet (Electric Violet / Hot Magenta family, outline, core, glow).")]
        [SerializeField] private BulletHell.Enemies.EnemyBulletPalette enemyBulletPalette;
        [Tooltip("Off: the paper-doll parts (armor, head, accessories, alternate bodies) show only on the Character Creation screen; in gameplay the player is the original sprite. Turn on when real part sprites exist.")]
        [SerializeField] private bool showCustomizationInGame;
        [Tooltip("Procedural motion, hit feedback, hitstop, camera shake and particle presets.")]
        [SerializeField] private FeedbackTuning feedback;

        [Header("Debug (editor and development builds only)")]
        [Tooltip("New runs start at this round (e.g. 3 to go straight to the first boss). 0 or 1 = off. Ignored in release builds.")]
        [SerializeField, Min(0)] private int debugStartRound;

        public AssetRegistry Registry => registry;
        public int DebugStartRound => debugStartRound;
        public FeedbackTuning Feedback => feedback;
        public PerspectiveTuning Perspective => perspective != null ? perspective : PerspectiveTuning.Fallback;
        public ArenaArt ArenaArt => arenaArt;
        public UITheme UITheme => uiTheme;
        public BulletHell.UI.ButtonGlyphLibrary ButtonGlyphs => buttonGlyphs;
        public BulletHell.Enemies.EnemyBulletPalette EnemyBulletPalette => enemyBulletPalette != null ? enemyBulletPalette : BulletHell.Enemies.EnemyBulletPalette.Fallback;
        public bool ShowCustomizationInGame => showCustomizationInGame;
        public DifficultyCurve Difficulty => difficulty;
        public ArenaLayoutData DefaultLayout => defaultLayout;
        public int RoundCount => rounds != null ? rounds.Length : 0;

        /// <summary>The layout a round is fought in: the round's own, else the default. Null only if neither is set.</summary>
        public ArenaLayoutData GetLayout(int round)
        {
            RoundData data = GetRound(round);
            return data != null && data.Layout != null ? data.Layout : defaultLayout;
        }
        public int StartingCurrency => startingCurrency;
        public ShopPool ShopPool => shopPool;
        public RarityTable RarityTable => rarityTable;
        public ShopTuning ShopTuning => shopTuning;
        public string SaveFileName => saveFileName;
        public string SettingsFileName => settingsFileName;
        public string ProfileFileName => profileFileName;
        public string TelemetryFileName => telemetryFileName;
        public AudioLibrary AudioLibrary => audioLibrary;
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

        public void SetDefaultLayout(ArenaLayoutData layout) => defaultLayout = layout;
#endif
    }
}
