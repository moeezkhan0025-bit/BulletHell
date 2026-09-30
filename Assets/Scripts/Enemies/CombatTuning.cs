using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>Tuning for where enemies spawn and how the wave messages behave.</summary>
    [CreateAssetMenu(fileName = "CombatTuning", menuName = "BulletHell/Combat Tuning")]
    public sealed class CombatTuning : ScriptableObject
    {
        [Header("Spawning")]
        [Tooltip("How far inside the screen edges enemies appear.")]
        [SerializeField, Min(0f)] private float spawnEdgeMargin = 1.5f;
        [Tooltip("Scattered enemies won't appear closer to the player than this.")]
        [SerializeField, Min(0f)] private float minSpawnDistanceFromPlayer = 3.5f;
        [Tooltip("Radius of a Ring spawn (kept inside the arena).")]
        [SerializeField, Min(0.5f)] private float ringRadius = 4.5f;
        [Tooltip("A Row spawn spans this fraction of the arena width.")]
        [SerializeField, Range(0.2f, 1f)] private float rowWidthFraction = 0.85f;

        [Header("Messages")]
        [Tooltip("How long the 'Wave X/Y' message stays on screen.")]
        [SerializeField, Min(0.2f)] private float bannerSeconds = 1.8f;

        [Tooltip("Enemies coming out of a spawn gate are nudged by up to this much, so they do not stack exactly.")]
        [SerializeField, Min(0f)] private float gateJitter = 0.3f;

        [Header("Round intro")]
        [Tooltip("How long the \"Round N\" / boss banner is shown before the countdown.")]
        [SerializeField, Min(0.2f)] private float introBannerSeconds = 1.6f;
        [Tooltip("Countdown numbers shown before combat (3, 2, 1).")]
        [SerializeField, Range(1, 9)] private int countdownSteps = 3;
        [SerializeField, Min(0.2f)] private float countdownStepSeconds = 0.8f;
        [Tooltip("How long \"Begin!\" stays on screen once combat has started.")]
        [SerializeField, Min(0.1f)] private float beginSeconds = 0.8f;

        [Header("Pools")]
        [SerializeField, Min(1)] private int enemyPoolPrewarm = 32;
        [SerializeField, Min(1)] private int enemyPoolMax = 128;
        [Tooltip("The Boss prefab's own pool: one boss at a time.")]
        [SerializeField, Min(1)] private int bossPoolPrewarm = 1;
        [SerializeField, Min(1)] private int bossPoolMax = 2;

        public float SpawnEdgeMargin => spawnEdgeMargin;
        public float MinSpawnDistanceFromPlayer => minSpawnDistanceFromPlayer;
        public float RingRadius => ringRadius;
        public float RowWidthFraction => rowWidthFraction;
        public float BannerSeconds => bannerSeconds;
        public float GateJitter => gateJitter;
        public float IntroBannerSeconds => introBannerSeconds;
        public int CountdownSteps => countdownSteps;
        public float CountdownStepSeconds => countdownStepSeconds;
        public float BeginSeconds => beginSeconds;
        public int EnemyPoolPrewarm => enemyPoolPrewarm;
        public int EnemyPoolMax => enemyPoolMax;
        public int BossPoolPrewarm => bossPoolPrewarm;
        public int BossPoolMax => bossPoolMax;
    }
}
