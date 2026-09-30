using BulletHell.Feedback;
using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>How an enemy moves and picks its moments to attack.</summary>
    public enum EnemyBehavior
    {
        /// <summary>Walks back and forth along an axis (bosses, static shooters). No navigation.</summary>
        Patrol,
        /// <summary>Pursues the player along the flow field; hurts on contact.</summary>
        Chaser,
        /// <summary>Ranged: holds a preferred distance, strafes, backs off, repositions without line of sight.</summary>
        Skirmisher,
        /// <summary>Moves to a firing spot with line of sight, plants, streams bullets until it overheats.</summary>
        Sentry,
        /// <summary>Telegraphs, then dashes in a straight line; jump over it.</summary>
        Charger,
        /// <summary>Keeps far away with line of sight, shows a warning line, fires one fast shot.</summary>
        Sniper,
    }

    /// <summary>Tunable data for one enemy type. Real enemies (M4) extend this; movement fields are optional.</summary>
    [CreateAssetMenu(fileName = "Enemy_", menuName = "BulletHell/Enemy Data")]
    public sealed class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Enemy";
        [SerializeField] private Color color = new Color(0.9f, 0.3f, 0.3f);
        [Tooltip("Diameter in world units.")]
        [SerializeField, Min(0.1f)] private float size = 1f;

        [Header("Painted art (optional; values are world units at the 1.5x character scale)")]
        [Tooltip("Painted sprite with its pivot at the feet. Empty = the placeholder shape. Drawn at its own size, Size does not scale it.")]
        [SerializeField] private Sprite paintedSprite;
        [Tooltip("Height of the painted body above the feet, in world units (1.185 = the tomato Chaser, 0.79 of the original P, x1.5). Places the health bar and the mid-body point.")]
        [SerializeField, Min(0.1f)] private float paintedHeight = 0.79f;
        [Tooltip("Movement footprint radius at the feet, in world units.")]
        [SerializeField, Min(0.05f)] private float paintedFootprintRadius = 0.3f;
        [Tooltip("Player bullets hit this box (width, height), standing on the feet.")]
        [SerializeField] private Vector2 paintedHurtboxSize = new Vector2(1f, 0.75f);
        [Tooltip("Hurtbox centre sideways from the feet, for art whose body is off-centre.")]
        [SerializeField] private float paintedHurtboxOffsetX;

        [Header("Health")]
        [SerializeField, Min(0.01f)] private float maxHealth = 10f;
        [SerializeField, Min(0f)] private float hitFlashDuration = 0.08f;

        [Header("Feedback (empty = the defaults in FeedbackTuning)")]
        [SerializeField] private MotionTuning motionOverride;
        [SerializeField] private HitFeedbackTuning hitOverride;
        [SerializeField] private LifeCycleTuning lifeCycleOverride;

        [Header("Reward")]
        [Tooltip("Currency in the coin this enemy drops when it dies.")]
        [SerializeField, Min(0)] private int coinValue = 5;

        [Header("Attacks")]
        [Tooltip("Patterns this enemy fires (each on its own timer). Empty = never shoots.")]
        [SerializeField] private AttackPattern[] attacks = new AttackPattern[0];

        [Header("Movement (speed 0 = static)")]
        [SerializeField] private EnemyBehavior behavior = EnemyBehavior.Patrol;
        [Tooltip("Top speed in world units per second (patrol speed for Patrol).")]
        [SerializeField, Min(0f)] private float moveSpeed;
        [Tooltip("Patrol only: distance from the start position it travels each way.")]
        [SerializeField, Min(0f)] private float moveRange = 3f;
        [Tooltip("Patrol only.")]
        [SerializeField] private Vector2 moveAxis = Vector2.right;

        [Header("Steering (all but Patrol)")]
        [Tooltip("Speed gained per second while getting up to speed.")]
        [SerializeField, Min(0.1f)] private float acceleration = 14f;
        [Tooltip("Speed lost per second when braking or slowing down.")]
        [SerializeField, Min(0.1f)] private float brake = 20f;
        [Tooltip("Degrees per second its heading can turn. Low = wide, heavy turns.")]
        [SerializeField, Min(10f)] private float turnRate = 360f;

        [Header("Contact (Chaser, Charger)")]
        [Tooltip("Hits taken by a grounded player it touches. An airborne player passes over.")]
        [SerializeField, Min(0f)] private float contactDamage = 1f;

        [Header("Ranged positioning (Skirmisher, Sniper)")]
        [SerializeField, Min(0.5f)] private float preferredDistance = 5f;
        [Tooltip("Slack around the preferred distance before it closes in or backs off.")]
        [SerializeField, Min(0.1f)] private float distanceTolerance = 0.8f;
        [Tooltip("Strafing speed as a fraction of top speed.")]
        [SerializeField, Range(0.1f, 1f)] private float strafeSpeedFraction = 0.6f;
        [Tooltip("Seconds between changes of strafing direction (min, max).")]
        [SerializeField] private Vector2 strafeSwitchSeconds = new Vector2(1.5f, 3.5f);
        [Tooltip("Skirmisher: visual windup pulse during the last N seconds before each shot. 0 = off. Does not delay shots.")]
        [SerializeField, Min(0f)] private float shotWarningSeconds;

        [Header("Sentry")]
        [Tooltip("Closest / furthest distance from the player it plants at.")]
        [SerializeField] private Vector2 sentryRange = new Vector2(3f, 7f);
        [Tooltip("Heat (fraction of full) added per volley. Full heat = overheated.")]
        [SerializeField, Range(0.01f, 1f)] private float heatPerShot = 0.06f;
        [SerializeField, Min(0.01f)] private float coolPerSecond = 0.25f;
        [Tooltip("Heat it must cool to before firing again.")]
        [SerializeField, Range(0f, 0.95f)] private float restartHeat = 0.35f;
        [Tooltip("Top speed while overheated, as a fraction of normal (it crawls while cooling).")]
        [SerializeField, Range(0f, 1f)] private float overheatedSpeedFraction = 0.35f;

        [Header("Charger")]
        [SerializeField, Min(1f)] private float chargeTriggerRange = 6f;
        [Tooltip("Closer than this it backs off to get a run-up instead of charging point-blank.")]
        [SerializeField, Min(0f)] private float chargeMinRange = 2.5f;
        [Tooltip("Warning time before the dash (the warning line shows).")]
        [SerializeField, Min(0.1f)] private float telegraphSeconds = 0.9f;
        [Tooltip("The dash direction stops following the player this long before the dash starts.")]
        [SerializeField, Min(0f)] private float telegraphLockSeconds = 0.3f;
        [SerializeField, Min(1f)] private float dashSpeed = 11f;
        [SerializeField, Min(1f)] private float dashDistance = 7f;
        [Tooltip("Seconds it stays put and vulnerable after a dash.")]
        [SerializeField, Min(0f)] private float recoverSeconds = 1f;
        [Tooltip("Seconds after recovering before it can charge again.")]
        [SerializeField, Min(0f)] private float chargeCooldown = 1.2f;

        [Header("Sniper")]
        [Tooltip("The single fast shot it fires (shape, speed, size, damage, colour).")]
        [SerializeField] private AttackPattern sniperShot;
        [SerializeField, Min(0.1f)] private float aimSeconds = 1.3f;
        [Tooltip("The warning line stops following the player this long before the shot.")]
        [SerializeField, Min(0f)] private float aimLockSeconds = 0.4f;
        [SerializeField, Min(0.1f)] private float sniperCooldown = 2.6f;

        public string DisplayName => displayName;
        public AttackPattern[] Attacks => attacks;
        public int CoinValue => coinValue;
        public Color Color => color;
        public float Size => size;
        public Sprite PaintedSprite => paintedSprite;
        public float PaintedHeight => paintedHeight;
        public float PaintedFootprintRadius => paintedFootprintRadius;
        public Vector2 PaintedHurtboxSize => paintedHurtboxSize;
        public float PaintedHurtboxOffsetX => paintedHurtboxOffsetX;
        public float MaxHealth => maxHealth;
        public float HitFlashDuration => hitFlashDuration;
        public MotionTuning MotionOverride => motionOverride;
        public HitFeedbackTuning HitOverride => hitOverride;
        public LifeCycleTuning LifeCycleOverride => lifeCycleOverride;
        public EnemyBehavior Behavior => behavior;
        public float MoveSpeed => moveSpeed;
        public float MoveRange => moveRange;
        public Vector2 MoveAxis => moveAxis;
        public float Acceleration => acceleration;
        public float Brake => brake;
        public float TurnRate => turnRate;
        public float ContactDamage => contactDamage;
        public float PreferredDistance => preferredDistance;
        public float DistanceTolerance => distanceTolerance;
        public float StrafeSpeedFraction => strafeSpeedFraction;
        public Vector2 StrafeSwitchSeconds => strafeSwitchSeconds;
        public Vector2 SentryRange => sentryRange;
        public float HeatPerShot => heatPerShot;
        public float CoolPerSecond => coolPerSecond;
        public float RestartHeat => restartHeat;
        public float OverheatedSpeedFraction => overheatedSpeedFraction;
        public float ChargeTriggerRange => chargeTriggerRange;
        public float ChargeMinRange => chargeMinRange;
        public float TelegraphSeconds => telegraphSeconds;
        public float ShotWarningSeconds => shotWarningSeconds;
        public float TelegraphLockSeconds => telegraphLockSeconds;
        public float DashSpeed => dashSpeed;
        public float DashDistance => dashDistance;
        public float RecoverSeconds => recoverSeconds;
        public float ChargeCooldown => chargeCooldown;
        public AttackPattern SniperShot => sniperShot;
        public float AimSeconds => aimSeconds;
        public float AimLockSeconds => aimLockSeconds;
        public float SniperCooldown => sniperCooldown;
    }
}
