using UnityEngine;

namespace BulletHell.Feedback
{
    public enum VfxKind { Spark, Dust, Smoke, Steam, Debris, Coin, Confetti }

    /// <summary>
    /// Global feedback settings and the default tunings for the player and enemies. GameConfig points at this;
    /// EnemyData can override the per-enemy ones. Also holds the particle preset prefabs.
    /// </summary>
    [CreateAssetMenu(fileName = "FeedbackTuning", menuName = "BulletHell/Feedback/Feedback Tuning")]
    public sealed class FeedbackTuning : ScriptableObject
    {
        [Header("Colours")]
        [Tooltip("The reserved DANGER colour: enemy wind-ups and trap telegraphs only. (ART_SPEC leaves the exact colour TBD.)")]
        [SerializeField] private Color dangerColor = new Color(1f, 0.2f, 0.55f);
        [SerializeField] private Color flashColor = Color.white;

        [Header("Hitstop")]
        [Tooltip("Time scale during a hitstop (0 = frozen). Real-time duration comes from the hit tuning.")]
        [SerializeField, Range(0f, 0.5f)] private float hitstopScale = 0f;

        [Header("Camera shake")]
        [Tooltip("Max camera offset in world units at full trauma.")]
        [SerializeField, Min(0f)] private float shakeMaxOffset = 0.35f;
        [Tooltip("Trauma lost per second.")]
        [SerializeField, Min(0.1f)] private float shakeDecay = 2.2f;
        [SerializeField, Min(1f)] private float shakeFrequency = 28f;

        [Header("Defaults: player")]
        [SerializeField] private MotionTuning playerMotion;
        [SerializeField] private HitFeedbackTuning playerHit;

        [Header("Defaults: enemies (EnemyData can override)")]
        [SerializeField] private MotionTuning enemyMotion;
        [SerializeField] private HitFeedbackTuning enemyHit;
        [SerializeField] private LifeCycleTuning enemyLifeCycle;

        [Header("Armaments (ricochet, pierce, wall impact)")]
        [SerializeField, Min(0)] private int ricochetSparks = 6;
        [Tooltip("Camera shake (0..1) for each ricochet.")]
        [SerializeField, Range(0f, 1f)] private float ricochetShake = 0.04f;
        [SerializeField, Min(0)] private int pierceSparks = 4;
        [Tooltip("Camera shake (0..1) for each pierce.")]
        [SerializeField, Range(0f, 1f)] private float pierceShake = 0.02f;
        [Tooltip("Debris puffs when a bullet stops on a wall or obstacle.")]
        [SerializeField, Min(0)] private int wallImpactDebris = 3;

        [Header("Death ghosts")]
        [SerializeField, Min(1)] private int ghostPoolSize = 16;

        [Header("Particle presets (prefabs with a ParticleSystem)")]
        [SerializeField] private ParticleSystem spark;
        [SerializeField] private ParticleSystem dust;
        [SerializeField] private ParticleSystem smoke;
        [SerializeField] private ParticleSystem steam;
        [SerializeField] private ParticleSystem debris;
        [SerializeField] private ParticleSystem coin;
        [SerializeField] private ParticleSystem confetti;
        [SerializeField, Min(1)] private int particlePoolPerPreset = 6;

        public Color DangerColor => dangerColor;
        public Color FlashColor => flashColor;
        public float HitstopScale => hitstopScale;
        public float ShakeMaxOffset => shakeMaxOffset;
        public float ShakeDecay => shakeDecay;
        public float ShakeFrequency => shakeFrequency;
        public MotionTuning PlayerMotion => playerMotion;
        public HitFeedbackTuning PlayerHit => playerHit;
        public MotionTuning EnemyMotion => enemyMotion;
        public HitFeedbackTuning EnemyHit => enemyHit;
        public LifeCycleTuning EnemyLifeCycle => enemyLifeCycle;
        public int RicochetSparks => ricochetSparks;
        public float RicochetShake => ricochetShake;
        public int PierceSparks => pierceSparks;
        public float PierceShake => pierceShake;
        public int WallImpactDebris => wallImpactDebris;
        public int GhostPoolSize => ghostPoolSize;
        public int ParticlePoolPerPreset => particlePoolPerPreset;

        public ParticleSystem Prefab(VfxKind kind)
        {
            switch (kind)
            {
                case VfxKind.Spark: return spark;
                case VfxKind.Dust: return dust;
                case VfxKind.Smoke: return smoke;
                case VfxKind.Steam: return steam;
                case VfxKind.Debris: return debris;
                case VfxKind.Coin: return coin;
                case VfxKind.Confetti: return confetti;
                default: return null;
            }
        }
    }
}
