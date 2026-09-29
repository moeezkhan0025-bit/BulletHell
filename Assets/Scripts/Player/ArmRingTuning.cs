using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>
    /// The arm ring: the 8 arm slots sit on a flattened ellipse around the player's FEET (3/4 view), like a ring
    /// spinning around the base. Back-half arms draw behind the body, front-half arms in front.
    /// </summary>
    [CreateAssetMenu(fileName = "ArmRingTuning", menuName = "BulletHell/Arm Ring Tuning")]
    public sealed class ArmRingTuning : ScriptableObject
    {
        [Header("Ellipse (around the feet)")]
        [Tooltip("Horizontal radius of the ring.")]
        [SerializeField, Min(0f)] private float radiusX = 0.55f;
        [Tooltip("Vertical radius as a fraction of the horizontal one (flattened for the 3/4 look).")]
        [SerializeField, Range(0.1f, 1f)] private float radiusYRatio = 0.5f;
        [Tooltip("Small lift of the whole ring above the feet.")]
        [SerializeField, Min(0f)] private float verticalOffset = 0.1f;
        [Tooltip("Degrees per second the ring 'spins' when the selection changes or a locked arm is aimed. 0 = instant.")]
        [SerializeField, Min(0f)] private float spinDegreesPerSecond = 720f;

        [Header("Front / back")]
        [Tooltip("An arm is behind the body when it is this far up the ring (0..1 of the vertical radius). Side arms count as front.")]
        [SerializeField, Range(0f, 0.9f)] private float backDeadzone = 0.05f;
        [Tooltip("Sorting orders inside the player's sorting group (body 0, cape -1, headgear 2).")]
        [SerializeField] private int backArtOrder = -3;
        [SerializeField] private int backHaloOrder = -4;
        [SerializeField] private int frontArtOrder = 3;
        [SerializeField] private int frontHaloOrder = 2;

        [Header("Selection outline (shader)")]
        [Tooltip("Soft-selected arm: thin, dim outline in the arm's ID colour. Width is in texels of the arm sprite.")]
        [SerializeField, Min(0f)] private float softOutlineWidth = 1.5f;
        [SerializeField, Range(0f, 1f)] private float softOutlineAlpha = 0.45f;
        [Tooltip("Locked arm: thick, solid outline that breathes.")]
        [SerializeField, Min(0f)] private float lockedOutlineWidth = 3f;
        [SerializeField, Range(0f, 1f)] private float lockedOutlineAlpha = 1f;
        [SerializeField, Min(0f)] private float lockedPulseSpeed = 6f;
        [Tooltip("How far the locked outline width dips (0.3 = down to 70%).")]
        [SerializeField, Range(0f, 0.9f)] private float lockedPulseAmount = 0.3f;

        [Header("Depth cue (subtle)")]
        [SerializeField] private bool depthCue = true;
        [Tooltip("Scale of an arm at the very back / very front of the ring.")]
        [SerializeField, Min(0.1f)] private float backScale = 0.9f;
        [SerializeField, Min(0.1f)] private float frontScale = 1.05f;
        [Tooltip("Brightness multiplier of the arm art at the very back (1 = not darkened).")]
        [SerializeField, Range(0.2f, 1f)] private float backBrightness = 0.8f;

        public float RadiusX => radiusX;
        public float RadiusY => radiusX * radiusYRatio;
        public float VerticalOffset => verticalOffset;
        public float SpinDegreesPerSecond => spinDegreesPerSecond;
        public float BackDeadzone => backDeadzone;
        public int BackArtOrder => backArtOrder;
        public int BackHaloOrder => backHaloOrder;
        public int FrontArtOrder => frontArtOrder;
        public int FrontHaloOrder => frontHaloOrder;
        public bool DepthCue => depthCue;
        public float SoftOutlineWidth => softOutlineWidth;
        public float SoftOutlineAlpha => softOutlineAlpha;
        public float LockedOutlineWidth => lockedOutlineWidth;
        public float LockedOutlineAlpha => lockedOutlineAlpha;
        public float LockedPulseSpeed => lockedPulseSpeed;
        public float LockedPulseAmount => lockedPulseAmount;

        /// <summary>Scale multiplier for an arm at a depth (0 = back of the ring, 1 = front).</summary>
        public float ScaleAt(float depth01) => depthCue ? Mathf.Lerp(backScale, frontScale, depth01) : 1f;

        /// <summary>Brightness multiplier for an arm at a depth (0 = back of the ring, 1 = front).</summary>
        public float BrightnessAt(float depth01) => depthCue ? Mathf.Lerp(backBrightness, 1f, depth01) : 1f;

        /// <summary>Attach point of the arm at a compass angle, relative to the feet.</summary>
        public Vector2 PositionAt(float compassDegrees) =>
            ArmRingMath.Position(compassDegrees, radiusX, RadiusY, verticalOffset);

        private static ArmRingTuning fallback;

        /// <summary>The default numbers, for scenes and tests that have no asset assigned.</summary>
        public static ArmRingTuning Fallback => fallback != null ? fallback : fallback = CreateInstance<ArmRingTuning>();
    }
}
