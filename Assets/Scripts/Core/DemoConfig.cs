using BulletHell.Enemies;
using UnityEngine;

namespace BulletHell.Core
{
    /// <summary>
    /// The one switch behind the demo build (S1). With <see cref="IsDemo"/> on, the game hides what the demo must not show: Sentry enemies
    /// and traps / hazard zones before the first boss, and Character Creation's editing. Nothing is deleted: every rule only filters at
    /// run time, so turning the flag off gives the full game back. The D6 build menu sets the flag for demo builds and restores it afterwards.
    /// </summary>
    [CreateAssetMenu(fileName = "DemoConfig", menuName = "BulletHell/Demo Config")]
    public sealed class DemoConfig : ScriptableObject
    {
        [Tooltip("True in demo builds (the build menu sets it). Off = the full game.")]
        [SerializeField] private bool isDemo;

        [Header("Enemies")]
        [Tooltip("Sentry enemies (Ringer, Spiraler) only appear from this round on (the first boss is round 3).")]
        [SerializeField, Min(1)] private int sentryFromRound = 4;
        [Tooltip("What spawns in place of a cut Sentry so the wave keeps its size (normally the Grunt, a Chaser).")]
        [SerializeField] private EnemyData sentryStandIn;

        [Header("Arena")]
        [Tooltip("Traps and hazard zones (vents, skewers, zones) only appear from this round on. Obstacles are not affected.")]
        [SerializeField, Min(1)] private int trapsFromRound = 4;

        [Header("Character Creation")]
        [SerializeField] private bool lockCharacterCreation = true;
        [Tooltip("Draw the placeholder part overlays (head, armor, accessories) on the gladiator. Off in the demo: the preview, the HUD portrait and the Armory doll show only the base player sprite. The part system stays intact; turn this on to bring the overlays back.")]
        [SerializeField] private bool showPartOverlays;
        [SerializeField] private string characterCreationTitle = "Demo Version";
        [SerializeField, TextArea(2, 4)] private string characterCreationMessage = "Character Creation is a work in progress and unavailable in the demo version.";
        [SerializeField] private string okLabel = "OK";

        public bool IsDemo => isDemo;
        public int SentryFromRound => sentryFromRound;
        public EnemyData SentryStandIn => sentryStandIn;
        public int TrapsFromRound => trapsFromRound;
        public string CharacterCreationTitle => characterCreationTitle;
        public string CharacterCreationMessage => characterCreationMessage;
        public string OkLabel => okLabel;

        /// <summary>Character Creation shows the default gladiator and refuses edits (and saves none).</summary>
        public bool CharacterCreationLocked => isDemo && lockCharacterCreation;

        /// <summary>The paper-doll part overlays are drawn (always in the full game; in the demo only if the flag is on).</summary>
        public bool PartOverlaysShown => !isDemo || showPartOverlays;

        /// <summary>Sentries may appear in this round.</summary>
        public bool AllowsSentry(int round) => !isDemo || round >= sentryFromRound;

        /// <summary>Traps and hazard zones may appear in this round.</summary>
        public bool AllowsTraps(int round) => !isDemo || round >= trapsFromRound;

        /// <summary>
        /// The enemy that really spawns for an authored one: a Sentry before its round is swapped for the stand-in (same count, so the round
        /// stays full); everything else, and everything when the demo flag is off, is unchanged.
        /// </summary>
        public EnemyData Filter(int round, EnemyData authored)
        {
            if (authored == null || AllowsSentry(round) || authored.Behavior != EnemyBehavior.Sentry)
                return authored;
            return sentryStandIn != null ? sentryStandIn : authored;
        }

#if UNITY_EDITOR
        public void Configure(bool demo, EnemyData standIn)
        {
            isDemo = demo;
            sentryStandIn = standIn;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void SetDemo(bool demo)
        {
            isDemo = demo;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
