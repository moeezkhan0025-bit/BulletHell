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

        [Header("Pools")]
        [SerializeField, Min(1)] private int enemyPoolPrewarm = 32;
        [SerializeField, Min(1)] private int enemyPoolMax = 128;

        public float SpawnEdgeMargin => spawnEdgeMargin;
        public float MinSpawnDistanceFromPlayer => minSpawnDistanceFromPlayer;
        public float RingRadius => ringRadius;
        public float RowWidthFraction => rowWidthFraction;
        public float BannerSeconds => bannerSeconds;
        public int EnemyPoolPrewarm => enemyPoolPrewarm;
        public int EnemyPoolMax => enemyPoolMax;
    }
}
