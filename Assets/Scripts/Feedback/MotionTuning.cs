using UnityEngine;

namespace BulletHell.Feedback
{
    /// <summary>How a character moves procedurally: breathing, hop-walk bob and tilt, lean, squash/stretch, facing flip, windup.</summary>
    [CreateAssetMenu(fileName = "Motion_", menuName = "BulletHell/Feedback/Motion Tuning")]
    public sealed class MotionTuning : ScriptableObject
    {
        [Header("Idle breathing")]
        [Tooltip("Scale change at the top of a breath (0.03 = 3%).")]
        [SerializeField, Min(0f)] private float breathAmount = 0.03f;
        [SerializeField, Min(0f)] private float breathSpeed = 2.2f;

        [Header("Hop-walk")]
        [Tooltip("Speed (units/s) at which the bob/tilt/lean reach full strength.")]
        [SerializeField, Min(0.1f)] private float fullSpeed = 5f;
        [Tooltip("Below this speed the character counts as standing still.")]
        [SerializeField, Min(0f)] private float moveThreshold = 0.3f;
        [SerializeField, Min(0f)] private float hopHeight = 0.08f;
        [Tooltip("Hops per second at full speed.")]
        [SerializeField, Min(0f)] private float hopsPerSecond = 3.2f;
        [Tooltip("Side-to-side rock in degrees at full speed.")]
        [SerializeField, Min(0f)] private float tiltDegrees = 5f;
        [Tooltip("Puff of dust each time a hop lands.")]
        [SerializeField] private bool footDust;

        [Header("Lean into movement")]
        [Tooltip("Degrees the body leans toward the horizontal direction of travel at full speed.")]
        [SerializeField, Min(0f)] private float leanDegrees = 8f;
        [SerializeField, Min(0.1f)] private float leanFollow = 12f;

        [Header("Squash and stretch")]
        [Tooltip("Peak scale change when starting to move (stretches tall, 0.12 = 12%) and when stopping (squashes flat).")]
        [SerializeField, Min(0f)] private float startStretch = 0.12f;
        [SerializeField, Min(0f)] private float stopSquash = 0.18f;
        [SerializeField, Min(0.5f)] private float springFrequency = 3.2f;
        [SerializeField, Range(0.1f, 1.5f)] private float springDamping = 0.35f;

        [Header("Facing flip")]
        [Tooltip("Mirror the character to face left/right with a quick squash. Off for symmetric placeholders.")]
        [SerializeField] private bool flipToFace;
        [SerializeField, Min(0.01f)] private float flipSeconds = 0.12f;

        [Header("Windup (telegraph)")]
        [Tooltip("Scale growth at full windup progress (0.15 = +15%).")]
        [SerializeField, Min(0f)] private float windupInflate = 0.15f;
        [Tooltip("Sideways shake (world units) at full windup.")]
        [SerializeField, Min(0f)] private float windupTremble = 0.04f;
        [SerializeField, Min(0f)] private float trembleSpeed = 55f;
        [Tooltip("DANGER-colour pulses per second x 2pi (24 = about 4 per second).")]
        [SerializeField, Min(0f)] private float pulseSpeed = 24f;

        public float BreathAmount => breathAmount;
        public float BreathSpeed => breathSpeed;
        public float FullSpeed => fullSpeed;
        public float MoveThreshold => moveThreshold;
        public float HopHeight => hopHeight;
        public float HopsPerSecond => hopsPerSecond;
        public float TiltDegrees => tiltDegrees;
        public bool FootDust => footDust;
        public float LeanDegrees => leanDegrees;
        public float LeanFollow => leanFollow;
        public float StartStretch => startStretch;
        public float StopSquash => stopSquash;
        public float SpringFrequency => springFrequency;
        public float SpringDamping => springDamping;
        public bool FlipToFace => flipToFace;
        public float FlipSeconds => flipSeconds;
        public float WindupInflate => windupInflate;
        public float WindupTremble => windupTremble;
        public float TrembleSpeed => trembleSpeed;
        public float PulseSpeed => pulseSpeed;
    }
}
