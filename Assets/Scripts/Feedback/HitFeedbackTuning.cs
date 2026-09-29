using UnityEngine;

namespace BulletHell.Feedback
{
    /// <summary>What happens when something takes a hit: white flash, knockback nudge, scale punch, hitstop, camera shake, sparks.</summary>
    [CreateAssetMenu(fileName = "Hit_", menuName = "BulletHell/Feedback/Hit Feedback Tuning")]
    public sealed class HitFeedbackTuning : ScriptableObject
    {
        [Tooltip("Seconds the sprite stays white.")]
        [SerializeField, Min(0f)] private float flashSeconds = 0.08f;
        [Tooltip("Visual-only nudge away from the hit, in world units. Never moves the collider or the AI.")]
        [SerializeField, Min(0f)] private float knockbackDistance = 0.12f;
        [Tooltip("Uniform scale kick (0.15 = swells 15% and settles).")]
        [SerializeField, Min(0f)] private float scalePunch = 0.15f;
        [Tooltip("Real-time freeze of gameplay in seconds. 0 = none. Only in Combat.")]
        [SerializeField, Min(0f)] private float hitstopSeconds = 0.04f;
        [Tooltip("Camera shake strength 0..1 added per hit.")]
        [SerializeField, Range(0f, 1f)] private float shake = 0.1f;
        [Tooltip("Sparks per hit.")]
        [SerializeField, Min(0)] private int sparks = 4;

        public float FlashSeconds => flashSeconds;
        public float KnockbackDistance => knockbackDistance;
        public float ScalePunch => scalePunch;
        public float HitstopSeconds => hitstopSeconds;
        public float Shake => shake;
        public int Sparks => sparks;
    }
}
