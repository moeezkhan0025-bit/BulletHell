using UnityEngine;

namespace BulletHell.Feedback
{
    /// <summary>Spawn pop-in and the squash / flash / dissolve death.</summary>
    [CreateAssetMenu(fileName = "LifeCycle_", menuName = "BulletHell/Feedback/Life Cycle Tuning")]
    public sealed class LifeCycleTuning : ScriptableObject
    {
        [Header("Spawn (pop out of the gate puff)")]
        [SerializeField, Min(0.01f)] private float spawnSeconds = 0.35f;
        [Tooltip("Scale curve over the spawn, 0..1 in time. Overshoot above 1 gives the pop.")]
        [SerializeField] private AnimationCurve spawnScale = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.6f, 1.2f), new Keyframe(0.8f, 0.94f), new Keyframe(1f, 1f));

        [Header("Death")]
        [Tooltip("Squash flat (scale X grows, Y shrinks) with a white flash, then dissolve.")]
        [SerializeField, Min(0.01f)] private float squashSeconds = 0.12f;
        [SerializeField, Min(0.01f)] private float dissolveSeconds = 0.35f;
        [SerializeField, Min(0.1f)] private float squashWidth = 1.35f;
        [SerializeField, Range(0.05f, 1f)] private float squashHeight = 0.45f;
        [Tooltip("Juice-splat debris particles when it dies.")]
        [SerializeField, Min(0)] private int splatParticles = 10;

        public float SpawnSeconds => spawnSeconds;
        public float SpawnScaleAt(float t01) => spawnScale.Evaluate(Mathf.Clamp01(t01));
        public float SquashSeconds => squashSeconds;
        public float DissolveSeconds => dissolveSeconds;
        public float SquashWidth => squashWidth;
        public float SquashHeight => squashHeight;
        public int SplatParticles => splatParticles;
    }
}
