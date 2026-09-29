using BulletHell.Arena;
using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Input;
using UnityEngine;
using UnityEngine.Rendering;

namespace BulletHell.Player
{
    /// <summary>
    /// R2 / Space jump with FAKE height. The player root never leaves the ground plane; only the Visuals child rises
    /// on the height curve, the ground shadow shrinks and fades, and the body squashes on takeoff and landing.
    /// While airborne the player passes over ground things (traps, hazard zones, pickups; enemy bodies and dashes once
    /// enemies touch the player) by switching the root to the PlayerAirborne physics layer, which does not collide
    /// with enemies; obstacles and walls still block movement through the arena grid, and bullets still hit unless
    /// jumpDodgesBullets is on. The body draws on the Airborne sorting layer (above characters, below the foreground)
    /// while its position for sorting stays the ground position. Firing and aiming are untouched: the arms ride on Visuals.
    /// On landing the player is moved to the nearest spot not taken by an obstacle or an enemy.
    /// </summary>
    [DefaultExecutionOrder(5)]
    public sealed class JumpController : MonoBehaviour
    {
        private const string GroundedLayerName = "Player";
        private const string AirborneLayerName = "PlayerAirborne";
        private static readonly Collider2D[] EnemyBuffer = new Collider2D[24];

        [SerializeField] private GameplayInputReader input;
        [SerializeField] private PlayerVisualRig rig;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerData data;
        [SerializeField] private JumpTuning tuning;
        [SerializeField] private SortingGroup sortingGroup;
        [Tooltip("The ground shadow (stays on the root).")]
        [SerializeField] private SpriteRenderer shadow;
        [Tooltip("The arena to land in. Optional.")]
        [SerializeField] private ArenaController arena;

        private readonly JumpTimeline timeline = new JumpTimeline();
        private RunManager run;
        private DustPuffs dust;
        private Vector3 shadowScale;
        private Color shadowColor;
        private int groundedLayer = -1, airborneLayer = -1;
        private ContactFilter2D enemyFilter;
        private bool started;   // the shadow's grounded look is read in Start, after the rig has set it up

        private Vector2 squashFrom = Vector2.one;
        private float squashSeconds = 1f;
        private float squashElapsed = 1f;

        public bool IsAirborne => timeline.IsAirborne;
        /// <summary>0..1 of the peak height (0 on the ground).</summary>
        public float Height01 => timeline.IsAirborne ? tuning.HeightAt(timeline.Progress01) : 0f;
        /// <summary>Height above the ground in world units.</summary>
        public float Height => Height01 * tuning.MaxHeight;
        public float CooldownLeft => Mathf.Max(0f, timeline.CooldownLeft);
        public float Progress01 => timeline.Progress01;
        public bool JumpDodgesBullets => tuning.JumpDodgesBullets;
        /// <summary>Scales the walking speed: the air control while airborne, else 1.</summary>
        public float MoveMultiplier => timeline.IsAirborne ? tuning.AirControl : 1f;

        /// <summary>Raised when the player leaves the ground.</summary>
        public event System.Action Jumped;
        /// <summary>Raised when the player lands, with the landing position.</summary>
        public event System.Action<Vector2> Landed;

        private void Awake()
        {
            run = GameServices.Ensure().Run;
            dust = new DustPuffs(tuning);
            groundedLayer = LayerMask.NameToLayer(GroundedLayerName);
            airborneLayer = LayerMask.NameToLayer(AirborneLayerName);
            if (groundedLayer < 0 || airborneLayer < 0)
                Debug.LogWarning($"Physics layers '{GroundedLayerName}' / '{AirborneLayerName}' are missing: the jump will not switch layers. Run BulletHell/M7.6/Setup Everything.", this);
            enemyFilter = new ContactFilter2D { useLayerMask = true, layerMask = LayerMask.GetMask("Enemy"), useTriggers = true };
        }

        // The rig has positioned its objects and set the shadow in its own Awake by now.
        private void Start()
        {
            if (shadow != null)
            {
                shadowScale = shadow.transform.localScale;
                shadowColor = shadow.color;
            }
            started = true;
            ApplyGrounded();
        }

        private void OnEnable()
        {
            input.JumpPressed += OnJumpPressed;
            run.RoundIntroStarted += OnRoundIntroStarted;
        }

        private void OnDisable()
        {
            input.JumpPressed -= OnJumpPressed;
            run.RoundIntroStarted -= OnRoundIntroStarted;
        }

        private void OnDestroy() => dust?.Destroy();

        private void OnRoundIntroStarted(int round) => CancelJump();

        private void OnJumpPressed()
        {
            GameState state = run.Machine.Current;
            if ((state != GameState.RoundIntro && state != GameState.Combat) || !health.IsAlive)
                return;
            if (!timeline.TryStart())
                return;

            SetAirborneRules(true);
            squashFrom = tuning.TakeoffSquash;
            squashSeconds = tuning.TakeoffSquashSeconds;
            squashElapsed = 0f;
            Jumped?.Invoke();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            dust.Update(dt);

            if (timeline.IsAirborne && !health.IsAlive)
                CancelJump();

            if (timeline.Tick(dt, tuning.Airtime, tuning.CooldownAfterLanding))
                Land();

            if (squashElapsed < squashSeconds)
                squashElapsed += dt;
            ApplyLook();
        }

        // ---- landing

        private void Land()
        {
            SetAirborneRules(false);
            Vector2 landed = ResolveLanding(transform.position);
            transform.position = landed;
            dust.Spawn(landed);
            squashFrom = tuning.LandingSquash;
            squashSeconds = tuning.LandingSquashSeconds;
            squashElapsed = 0f;
            Landed?.Invoke(landed);
        }

        /// <summary>The nearest spot to a landing position that is not inside an obstacle, wall or enemy footprint.</summary>
        private Vector2 ResolveLanding(Vector2 position)
        {
            float radius = data.BodyRadius;
            float step = arena != null && arena.IsBuilt ? arena.Grid.CellSize : 0.25f;
            return LandingResolver.Resolve(position, p => IsTaken(p, radius), step, 6f);
        }

        private bool IsTaken(Vector2 position, float radius)
        {
            if (arena != null && arena.IsBuilt && arena.Grid.CircleBlocked(position, radius))
                return true;

            int count = Physics2D.OverlapCircle(position, radius + 1.5f, enemyFilter, EnemyBuffer);
            for (int i = 0; i < count; i++)
            {
                if (!EnemyBuffer[i].TryGetComponent(out Enemy enemy) || !enemy.IsAlive)
                    continue;
                float reach = radius + enemy.FootprintRadius;
                if (((Vector2)enemy.Position - position).sqrMagnitude < reach * reach)
                    return true;
            }
            return false;
        }

        // ---- state

        private void CancelJump()
        {
            bool wasAirborne = timeline.IsAirborne;
            timeline.Cancel();
            squashElapsed = squashSeconds;
            if (wasAirborne)
                SetAirborneRules(false);
            ApplyLook();
        }

        /// <summary>The rules that differ in the air: the physics layer, bullets, ground hazards and the sorting layer.</summary>
        private void SetAirborneRules(bool airborne)
        {
            health.SetAirborne(airborne, tuning.JumpDodgesBullets);
            int layer = airborne ? airborneLayer : groundedLayer;
            if (layer >= 0)
                gameObject.layer = layer;
        }

        private void ApplyGrounded()
        {
            if (groundedLayer >= 0)
                gameObject.layer = groundedLayer;
            ApplyLook();
        }

        // ---- looks

        private void ApplyLook()
        {
            float height01 = Height01;
            float t = squashSeconds > 0f ? Mathf.Clamp01(squashElapsed / squashSeconds) : 1f;
            Vector2 squash = Vector2.Lerp(squashFrom, Vector2.one, Mathf.SmoothStep(0f, 1f, t));
            float apex = 1f + (tuning.ApexScale - 1f) * height01;
            var scale = new Vector3(squash.x * apex, squash.y * apex, 1f);

            // The body's centre stays over the feet: squashing shrinks it towards the ground, not towards its middle.
            Transform visuals = rig.Visuals;
            visuals.localScale = scale;
            visuals.localPosition = new Vector3(0f, Height + data.BodyCenterHeight * scale.y, 0f);

            if (started && shadow != null)
            {
                shadow.transform.localScale = shadowScale * Mathf.Lerp(1f, tuning.ShadowScaleAtApex, height01);
                Color color = shadowColor;
                color.a *= Mathf.Lerp(1f, tuning.ShadowAlphaAtApex, height01);
                shadow.color = color;
            }

            // Sorting uses the group's transform, which is on the ground, so "in front of" is decided by the shadow.
            if (sortingGroup != null)
            {
                bool high = height01 > tuning.AirborneSortHeight && timeline.IsAirborne;
                sortingGroup.sortingLayerID = SortingLayers.Id(high ? SortingLayers.Airborne : SortingLayers.Characters);
            }
        }
    }
}
