using UnityEngine;

namespace BulletHell.AI
{
    /// <summary>Shared numbers for enemy navigation, separation and line of sight. Per-enemy numbers live on EnemyData.</summary>
    [CreateAssetMenu(fileName = "EnemyAiTuning", menuName = "BulletHell/Enemy AI Tuning")]
    public sealed class EnemyAiTuning : ScriptableObject
    {
        [Header("Flow field")]
        [Tooltip("Radius of the agent the flow field keeps clear of obstacles and walls. At least the largest walking enemy footprint, so paths never cut corners it can not fit.")]
        [SerializeField, Min(0.05f)] private float navRadius = 0.45f;
        [Tooltip("The flow field is rebuilt at most this often while the player moves (seconds).")]
        [SerializeField, Min(0.02f)] private float minRebuildInterval = 0.1f;

        [Header("Separation")]
        [Tooltip("Extra gap enemies try to keep between their footprints.")]
        [SerializeField, Min(0f)] private float separationPadding = 0.2f;
        [Tooltip("How strongly neighbours push an enemy away, relative to its wish to move (1 = as strong as the goal).")]
        [SerializeField, Min(0f)] private float separationWeight = 1.2f;

        [Header("Steering")]
        [Tooltip("Fraction of top speed kept while facing 90 degrees away from the wanted direction (turning corners slows an enemy).")]
        [SerializeField, Range(0f, 1f)] private float turnSlowdown = 0.35f;
        [Tooltip("Below this speed an enemy can pivot freely (it is practically standing still).")]
        [SerializeField, Min(0f)] private float pivotSpeed = 0.15f;

        [Header("Sentry / Sniper")]
        [Tooltip("A Sentry or Sniper counts as planted (free to fire or aim) below this speed.")]
        [SerializeField, Min(0.01f)] private float plantSpeed = 0.4f;
        [Tooltip("Width of a warning line (Charger dash, Sniper shot).")]
        [SerializeField, Min(0.01f)] private float warningLineWidth = 0.12f;
        [SerializeField] private Color warningColor = new Color(1f, 0.25f, 0.2f, 0.75f);

        [Header("Line of sight")]
        [Tooltip("Radius of the probe swept from an enemy to the player: about a bullet's radius.")]
        [SerializeField, Min(0.01f)] private float losRadius = 0.12f;

        public float NavRadius => navRadius;
        public float MinRebuildInterval => minRebuildInterval;
        public float SeparationPadding => separationPadding;
        public float SeparationWeight => separationWeight;
        public float TurnSlowdown => turnSlowdown;
        public float PivotSpeed => pivotSpeed;
        public float LosRadius => losRadius;
        public float PlantSpeed => plantSpeed;
        public float WarningLineWidth => warningLineWidth;
        public Color WarningColor => warningColor;

        private static EnemyAiTuning fallback;

        /// <summary>The default numbers, for scenes and tests that have no asset assigned.</summary>
        public static EnemyAiTuning Fallback => fallback != null ? fallback : fallback = CreateInstance<EnemyAiTuning>();
    }
}
