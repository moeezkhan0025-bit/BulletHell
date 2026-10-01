using UnityEngine;

namespace BulletHell.Capture
{
    /// <summary>
    /// R1 portfolio capture kit - every number of the capture autopilot (<see cref="CaptureAutopilot"/>). One asset
    /// (Assets/Data/Capture/CaptureTuning.asset) referenced by the director. Tuned so the play looks human: smooth stick
    /// changes, a short reaction delay, bullets dodged by a margin rather than at the last pixel.
    /// Distances are world units, angles degrees, times seconds.
    /// </summary>
    [CreateAssetMenu(fileName = "CaptureTuning", menuName = "BulletHell/Capture/Tuning")]
    public sealed class CaptureTuning : ScriptableObject
    {
        [Header("Movement - feel")]
        [Tooltip("How often the autopilot re-plans its movement.")]
        [SerializeField, Min(0.01f)] private float decisionInterval = 0.07f;
        [Tooltip("Delay between seeing a danger and the stick reacting (a human is never instant).")]
        [SerializeField, Min(0f)] private float reactionSeconds = 0.16f;
        [Tooltip("How fast the move stick can change, in stick units per second (smooth, not jittery).")]
        [SerializeField, Min(0.1f)] private float stickAcceleration = 5.5f;
        [Tooltip("Largest move stick length while walking calmly (1 = full speed).")]
        [SerializeField, Range(0.2f, 1f)] private float cruiseSpeed = 0.85f;

        [Header("Movement - bullets")]
        [Tooltip("Only enemy bullets this close to the player are looked at.")]
        [SerializeField, Min(1f)] private float bulletScanRadius = 6.5f;
        [Tooltip("How far ahead a bullet's path is predicted.")]
        [SerializeField, Min(0.1f)] private float bulletHorizon = 0.9f;
        [Tooltip("Extra distance kept to a bullet's path beyond the hit radii.")]
        [SerializeField, Min(0f)] private float dodgeClearance = 0.5f;
        [SerializeField, Min(0f)] private float dodgeWeight = 2.4f;
        [Tooltip("At most this many bullets are considered per decision.")]
        [SerializeField, Min(1)] private int maxBulletsConsidered = 64;
        [Tooltip("A bullet that appears to move faster than this between two frames is a recycled one and ignored.")]
        [SerializeField, Min(1f)] private float bulletSpeedSanity = 40f;

        [Header("Movement - enemies")]
        [Tooltip("Preferred distance to the nearest melee enemy (Chaser, Charger).")]
        [SerializeField, Min(0.5f)] private float meleeKeepDistance = 4.6f;
        [Tooltip("Preferred distance to ranged enemies (Skirmisher, Sentry, Sniper) and the boss.")]
        [SerializeField, Min(0.5f)] private float rangedKeepDistance = 6.5f;
        [SerializeField, Min(0f)] private float keepDistanceWeight = 1.3f;
        [Tooltip("Circling around the nearest enemy (strafing) so the player is never standing still.")]
        [SerializeField, Min(0f)] private float strafeWeight = 0.6f;
        [Tooltip("Seconds between changes of strafe direction (random in this range).")]
        [SerializeField] private Vector2 strafeSwitchSeconds = new Vector2(3.5f, 7f);

        [Header("Movement - arena")]
        [Tooltip("Distance from the arena bounds where the walls start pushing back.")]
        [SerializeField, Min(0.1f)] private float wallMargin = 2.4f;
        [SerializeField, Min(0f)] private float wallWeight = 1.8f;
        [SerializeField, Min(0f)] private float centerPull = 0.12f;
        [Tooltip("Moved less than this far in the stuck window while trying to move: walk to the arena centre for a moment.")]
        [SerializeField, Min(0.01f)] private float stuckDistance = 0.3f;
        [SerializeField, Min(0.1f)] private float stuckSeconds = 1f;
        [SerializeField, Min(0.1f)] private float unstickSeconds = 0.9f;

        [Header("Aiming")]
        [Tooltip("Enemies farther than this are not targeted.")]
        [SerializeField, Min(1f)] private float targetRange = 14f;
        [Tooltip("How often the target is re-chosen (nearest enemy).")]
        [SerializeField, Min(0.05f)] private float retargetInterval = 0.35f;
        [Tooltip("A new target has to be this much nearer (fraction) than the current one before the autopilot switches.")]
        [SerializeField, Range(0f, 0.9f)] private float retargetBias = 0.25f;
        [SerializeField, Min(10f)] private float aimTurnDegreesPerSecond = 520f;
        [Tooltip("How long the left stick points at the target before the arm is locked (shows the soft select).")]
        [SerializeField, Min(0f)] private float selectHoldSeconds = 0.16f;
        [Tooltip("An arm stays locked at least this long before the autopilot may switch to a better placed one.")]
        [SerializeField, Min(0f)] private float minLockedSeconds = 2.2f;
        [Tooltip("The locked arm is swapped (unlock, select, lock) when its home slot is farther than this from the target direction.")]
        [SerializeField, Range(30f, 180f)] private float rehomeAngle = 100f;

        [Header("Firing")]
        [Tooltip("Fire only when the aim is within this many degrees of the target.")]
        [SerializeField, Min(1f)] private float fireAngleTolerance = 14f;
        [Tooltip("Fire only at targets nearer than this.")]
        [SerializeField, Min(1f)] private float fireRange = 12f;
        [Tooltip("The trigger is held this long (random in range), then released briefly, like a thumb getting tired.")]
        [SerializeField] private Vector2 fireHoldSeconds = new Vector2(1.6f, 3.2f);
        [SerializeField] private Vector2 firePauseSeconds = new Vector2(0.12f, 0.3f);

        [Header("Ammo")]
        [Tooltip("Seconds between ammo swaps (random in range) when more than one ammo type is loaded. 0 = never.")]
        [SerializeField] private Vector2 ammoSwapSeconds = new Vector2(7f, 13f);

        [Header("Jumping")]
        [Tooltip("The player jumps this many seconds before the boss smash lands, so the landing happens mid-air.")]
        [SerializeField, Min(0f)] private float smashJumpLead = 0.22f;
        [Tooltip("The smash is avoided when the player is within its radius plus this margin.")]
        [SerializeField, Min(0f)] private float smashMargin = 0.6f;
        [Tooltip("Used when the boss data has no jump tuning.")]
        [SerializeField, Min(0.1f)] private float smashAirtimeFallback = 0.8f;
        [Tooltip("A melee enemy this close to the player triggers a hop over it. 0 = off.")]
        [SerializeField, Min(0f)] private float hopDistance = 1.5f;
        [SerializeField, Min(0f)] private float hopCooldown = 7f;

        public float DecisionInterval => decisionInterval;
        public float ReactionSeconds => reactionSeconds;
        public float StickAcceleration => stickAcceleration;
        public float CruiseSpeed => cruiseSpeed;
        public float BulletScanRadius => bulletScanRadius;
        public float BulletHorizon => bulletHorizon;
        public float DodgeClearance => dodgeClearance;
        public float DodgeWeight => dodgeWeight;
        public int MaxBulletsConsidered => maxBulletsConsidered;
        public float BulletSpeedSanity => bulletSpeedSanity;
        public float MeleeKeepDistance => meleeKeepDistance;
        public float RangedKeepDistance => rangedKeepDistance;
        public float KeepDistanceWeight => keepDistanceWeight;
        public float StrafeWeight => strafeWeight;
        public Vector2 StrafeSwitchSeconds => strafeSwitchSeconds;
        public float WallMargin => wallMargin;
        public float WallWeight => wallWeight;
        public float CenterPull => centerPull;
        public float StuckDistance => stuckDistance;
        public float StuckSeconds => stuckSeconds;
        public float UnstickSeconds => unstickSeconds;
        public float TargetRange => targetRange;
        public float RetargetInterval => retargetInterval;
        public float RetargetBias => retargetBias;
        public float AimTurnDegreesPerSecond => aimTurnDegreesPerSecond;
        public float SelectHoldSeconds => selectHoldSeconds;
        public float MinLockedSeconds => minLockedSeconds;
        public float RehomeAngle => rehomeAngle;
        public float FireAngleTolerance => fireAngleTolerance;
        public float FireRange => fireRange;
        public Vector2 FireHoldSeconds => fireHoldSeconds;
        public Vector2 FirePauseSeconds => firePauseSeconds;
        public Vector2 AmmoSwapSeconds => ammoSwapSeconds;
        public float SmashJumpLead => smashJumpLead;
        public float SmashMargin => smashMargin;
        public float SmashAirtimeFallback => smashAirtimeFallback;
        public float HopDistance => hopDistance;
        public float HopCooldown => hopCooldown;
    }
}
