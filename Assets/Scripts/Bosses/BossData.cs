using System;
using BulletHell.Enemies;
using BulletHell.Player;
using UnityEngine;

namespace BulletHell.Bosses
{
    /// <summary>The three things a boss can do. Each attack entry names one and the pattern preset it fires.</summary>
    public enum BossAttackKind
    {
        /// <summary>A ring of bullets in all directions (a Ring pattern), one or more volleys, each rotated a little.</summary>
        CircleSpread,
        /// <summary>A fast bullet at the player along a warning line (an Aimed pattern); phase 2 makes it a burst.</summary>
        FastShot,
        /// <summary>Jumps onto the player's spot: the shadow telegraphs the landing, the impact hurts everything grounded around it, then rings follow.</summary>
        JumpSmash,
    }

    [Serializable]
    public sealed class BossAttack
    {
        public BossAttackKind Kind;
        [Tooltip("Circle Spread: the ring fired. Fast Shot: the aimed bullet. Jump & Smash: the ring fired after each landing.")]
        public AttackPattern Pattern;
        [Tooltip("Seconds of visible wind-up before the attack.")]
        [Min(0.1f)] public float WindupSeconds = 0.9f;
        [Tooltip("Rings per use, shots per burst, or jumps per use.")]
        [Min(1)] public int Volleys = 1;
        [Min(0f)] public float VolleyInterval = 0.4f;
        [Tooltip("Degrees added to the ring on every volley, so the gaps move.")]
        public float AngleOffsetStepDeg;
        [Tooltip("Seconds of repositioning after the attack (divided by the round's fire-rate multiplier).")]
        [Min(0f)] public float Cooldown = 1.5f;
        [Tooltip("Pick weight against the phase's other attacks. 0 = never.")]
        [Min(0f)] public float Weight = 1f;
    }

    [Serializable]
    public sealed class BossPhase
    {
        [Tooltip("The phase starts once health is at or below this fraction. Phase 1 uses 1.")]
        [Range(0f, 1f)] public float EnterBelowHp01 = 1f;
        [Min(0.1f)] public float MoveSpeedMultiplier = 1f;
        public BossAttack[] Attacks = new BossAttack[0];
        [Tooltip("Steady body glow through the character shader while this phase runs (0 = none).")]
        [ColorUsage(true, true)] public Color GlowColor = new Color(1f, 0.55f, 0.1f);
        [Range(0f, 1f)] public float GlowAmount;
        [Min(0f)] public float GlowPulseSpeed = 6f;
    }

    [Serializable]
    public sealed class SmashSettings
    {
        [Tooltip("Radius on the ground that the landing hurts.")]
        [Min(0.1f)] public float Radius = 1.9f;
        public float DamageToPlayer = 1f;
        public float DamageToEnemies = 6f;
        [Tooltip("The landing ring's colour: the reserved DANGER colour.")]
        public Color TelegraphColor = new Color(1f, 0.3f, 0.2f);
        public Sprite RingSprite;
        [Tooltip("Rings fired right after landing, and how they are spaced and rotated.")]
        [Min(0)] public int PostSpreads = 3;
        [Min(0.05f)] public float PostSpreadInterval = 0.25f;
        public float PostSpreadAngleStepDeg = 7.5f;
        [Tooltip("Phase 2 chains jumps: the short crouch between them.")]
        [Min(0.05f)] public float SecondJumpWindup = 0.35f;
        [Tooltip("Crouch squash while winding up a jump.")]
        [Range(0f, 0.8f)] public float CrouchSquash = 0.3f;
        [Range(0f, 1f)] public float Shake = 0.5f;
        [Min(0f)] public float HitstopSeconds = 0.06f;
        [Min(0)] public int DustCount = 14;
        [Min(0)] public int DebrisCount = 8;
        [Tooltip("The furthest a jump carries the boss.")]
        [Min(1f)] public float MaxJumpDistance = 7f;
        [Tooltip("On: player bullets still hit the boss in the air (the hurtbox rises with the body). Off: it is untouchable until it lands.")]
        public bool HittableInAir = true;
    }

    [Serializable]
    public sealed class TransitionSettings
    {
        [Min(0.1f)] public float Seconds = 1.6f;
        [Tooltip("Nothing hurts the boss during the transition.")]
        public bool Invulnerable = true;
        [Tooltip("The roar: a scale punch at the start.")]
        [Range(0f, 1f)] public float RoarInflate = 0.35f;
        [Min(0)] public int FlashCycles = 3;
        [Range(0f, 1f)] public float Shake = 0.7f;
        [Min(0f)] public float HitstopSeconds = 0.1f;
    }

    [Serializable]
    public sealed class DeathSettings
    {
        [Tooltip("Stage 1: shakes, flashes and bursts while it staggers.")]
        [Min(0f)] public float StaggerSeconds = 0.9f;
        [Tooltip("Stage 2: it squashes into the ground.")]
        [Min(0f)] public float SquashSeconds = 0.3f;
        [Tooltip("Stage 3: it dissolves.")]
        [Min(0.05f)] public float DissolveSeconds = 0.9f;
        [Min(0)] public int FlashCycles = 5;
        [Min(0)] public int DebrisBursts = 6;
        [Min(0)] public int DebrisPerBurst = 8;
        [Tooltip("Coin sparkle burst at the end (the real coin drops through the enemy's coin value).")]
        [Min(0)] public int CoinBurst = 24;
        [Range(0f, 1f)] public float ShakePerStage = 0.35f;
        [Min(0f)] public float HitstopSeconds = 0.12f;
    }

    /// <summary>
    /// Everything tunable about one boss: health, its phases (each with its own attack list, speed and glow), the
    /// jump, the smash, the phase transition and the death sequence. The boss's EnemyData points at this; the
    /// EnemyData keeps the art, footprint, hurtbox, speed and coin value like any other enemy.
    /// </summary>
    [CreateAssetMenu(fileName = "Boss_", menuName = "BulletHell/Boss Data")]
    public sealed class BossData : ScriptableObject
    {
        [SerializeField] private string displayName = "Boss";
        [Tooltip("Replaces the EnemyData's Max Health (still scaled by the round's difficulty).")]
        [SerializeField, Min(1f)] private float maxHealth = 420f;
        [Tooltip("Seconds it stands still after appearing before the first attack.")]
        [SerializeField, Min(0f)] private float entrySettleSeconds = 1f;
        [Tooltip("Distance band it keeps from the player between attacks (min, max).")]
        [SerializeField] private Vector2 preferredDistance = new Vector2(3f, 5f);
        [Tooltip("Fast Shot: the warning line stops following the player this long before the shot.")]
        [SerializeField, Min(0f)] private float fastShotAimLockSeconds = 0.3f;
        [Tooltip("Scale punch on every volley fired.")]
        [SerializeField, Range(0f, 0.5f)] private float recoilPunch = 0.1f;

        [Header("Phases (in order; phase 1 first)")]
        [SerializeField] private BossPhase[] phases = { new BossPhase() };

        [Header("Jump & Smash")]
        [SerializeField] private JumpTuning jumpTuning;
        [SerializeField] private SmashSettings smash = new SmashSettings();

        [Header("Phase transition")]
        [SerializeField] private TransitionSettings transition = new TransitionSettings();

        [Header("Death")]
        [SerializeField] private DeathSettings death = new DeathSettings();

        public string DisplayName => displayName;
        public float MaxHealth => maxHealth;
        public float EntrySettleSeconds => entrySettleSeconds;
        public Vector2 PreferredDistance => preferredDistance;
        public float FastShotAimLockSeconds => fastShotAimLockSeconds;
        public float RecoilPunch => recoilPunch;
        public BossPhase[] Phases => phases;
        public JumpTuning JumpTuning => jumpTuning;
        public SmashSettings Smash => smash;
        public TransitionSettings Transition => transition;
        public DeathSettings Death => death;

        /// <summary>The phase index for a health fraction: the last phase whose threshold the health is at or below (0 = phase 1).</summary>
        public int PhaseIndexFor(float hp01)
        {
            for (int i = phases.Length - 1; i >= 1; i--)
                if (hp01 <= phases[i].EnterBelowHp01)
                    return i;
            return 0;
        }

#if UNITY_EDITOR
        /// <summary>Editor-only: used by setup scripts and tests. Null settings keep the current ones.</summary>
        public void EditorConfigure(string name, float hp, float settleSeconds, Vector2 distanceBand, BossPhase[] newPhases,
                                    JumpTuning jump = null, SmashSettings newSmash = null, TransitionSettings newTransition = null,
                                    DeathSettings newDeath = null)
        {
            displayName = name;
            maxHealth = hp;
            entrySettleSeconds = settleSeconds;
            preferredDistance = distanceBand;
            phases = newPhases ?? phases;
            jumpTuning = jump != null ? jump : jumpTuning;
            smash = newSmash ?? smash;
            transition = newTransition ?? transition;
            death = newDeath ?? death;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
