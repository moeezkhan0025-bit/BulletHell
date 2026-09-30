using System;
using BulletHell.Arena;
using BulletHell.Core;
using BulletHell.Feedback;
using BulletHell.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace BulletHell.Bosses
{
    /// <summary>
    /// A boss's jump with fake height, built from the player's jump pieces (JumpTimeline, JumpTuning, LandingResolver,
    /// DustPuffs). The root (feet, shadow, sort point) travels along the ground from takeoff to the landing spot; the Rig
    /// rises on the arc so the body appears to fly; the hurtbox rises with it (unless the boss is untouchable in the air),
    /// the shadow shrinks and the sorting group moves to the Airborne layer. The landing spot is resolved once at takeoff
    /// against walls and obstacles only (the player and other enemies ARE the target), so the telegraph can show it.
    /// </summary>
    public sealed class BossJump : MonoBehaviour
    {
        private const float TakeoffDust = 6f;

        [Tooltip("The enemy's Rig: lifted by the jump height.")]
        [SerializeField] private Transform rig;
        [SerializeField] private SpriteRenderer shadow;
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField] private CapsuleCollider2D hitbox;
        [SerializeField] private ProceduralMotion motion;

        private readonly JumpTimeline timeline = new JumpTimeline();
        private JumpTuning tuning;
        private ArenaController arena;
        private DustPuffs dust;
        private float footprint;
        private bool hittableInAir = true;
        private Vector2 start;
        private Vector2 landing;
        private Vector3 baseRig;
        private Vector3 baseShadowScale;
        private Color baseShadowColor;
        private Vector2 baseHitboxOffset;
        private bool hitboxWasEnabled;

        public bool IsAirborne => timeline.IsAirborne;
        public float Progress01 => timeline.Progress01;
        public float Height { get; private set; }
        public Vector2 LandingPoint => landing;
        /// <summary>Raised on landing with the ground position landed on.</summary>
        public event Action<Vector2> Landed;

        public void Configure(JumpTuning jumpTuning, ArenaController arenaController, float footprintRadius, bool hittableWhileAirborne)
        {
            Cancel();
            if (dust != null && tuning != jumpTuning)
            {
                dust.Destroy();
                dust = null;
            }
            tuning = jumpTuning;
            arena = arenaController;
            footprint = footprintRadius;
            hittableInAir = hittableWhileAirborne;
        }

        /// <summary>Takes off towards a ground target (clamped to a distance). False when already airborne or untuned.</summary>
        public bool TryStart(Vector2 target, float maxDistance)
        {
            if (tuning == null || !timeline.CanStart)
                return false;

            start = transform.position;
            Vector2 to = target - start;
            if (to.magnitude > maxDistance)
                target = start + to.normalized * maxDistance;
            landing = ResolveLanding(target);
            if (!timeline.TryStart())
                return false;

            baseRig = rig.localPosition;
            if (shadow != null)
            {
                baseShadowScale = shadow.transform.localScale;
                baseShadowColor = shadow.color;
            }
            if (hitbox != null)
            {
                baseHitboxOffset = hitbox.offset;
                hitboxWasEnabled = hitbox.enabled;
                if (!hittableInAir)
                    hitbox.enabled = false;
            }
            if (motion != null)
            {
                motion.HoldHop = true;
                motion.Squash(-(1f - tuning.TakeoffSquash.y));
            }
            FeedbackHub.Play(VfxKind.Dust, start, (int)TakeoffDust);
            Height = 0f;
            return true;
        }

        /// <summary>Back on the ground at once with everything restored (death, recycle). No landing event.</summary>
        public void Cancel()
        {
            if (!timeline.IsAirborne)
                return;
            timeline.Cancel();
            Restore();
        }

        private Vector2 ResolveLanding(Vector2 target)
        {
            if (arena == null || !arena.IsBuilt)
                return target;
            ArenaGrid grid = arena.Grid;
            return LandingResolver.Resolve(target, p => grid.CircleBlocked(p, footprint), grid.CellSize, 6f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            dust?.Update(dt);
            if (!timeline.IsAirborne)
                return;
            if (timeline.Tick(dt, tuning.Airtime, 0f))
                Land();
            else
                Apply();
        }

        private void Apply()
        {
            float progress = timeline.Progress01;
            float height01 = tuning.HeightAt(progress);
            Height = height01 * tuning.MaxHeight;

            Vector2 ground = Vector2.Lerp(start, landing, progress);
            transform.position = new Vector3(ground.x, ground.y, transform.position.z);
            rig.localPosition = baseRig + new Vector3(0f, Height, 0f);
            if (hitbox != null && hittableInAir)
                hitbox.offset = baseHitboxOffset + new Vector2(0f, Height);

            if (shadow != null)
            {
                shadow.transform.localScale = baseShadowScale * Mathf.Lerp(1f, tuning.ShadowScaleAtApex, height01);
                Color color = baseShadowColor;
                color.a *= Mathf.Lerp(1f, tuning.ShadowAlphaAtApex, height01);
                shadow.color = color;
            }

            // Sorting still uses the root on the ground; only the layer changes so the body clears things it flies over.
            if (sortingGroup != null)
                sortingGroup.sortingLayerID = SortingLayers.Id(height01 > tuning.AirborneSortHeight ? SortingLayers.Airborne : SortingLayers.Characters);
        }

        private void Land()
        {
            Restore();
            transform.position = new Vector3(landing.x, landing.y, transform.position.z);
            if (dust == null)
                dust = new DustPuffs(tuning);
            dust.Spawn(landing);
            if (motion != null)
                motion.Squash(1f - tuning.LandingSquash.y);
            Landed?.Invoke(landing);
        }

        private void Restore()
        {
            Height = 0f;
            rig.localPosition = baseRig;
            if (hitbox != null)
            {
                hitbox.offset = baseHitboxOffset;
                hitbox.enabled = hitboxWasEnabled;
            }
            if (shadow != null)
            {
                shadow.transform.localScale = baseShadowScale;
                shadow.color = baseShadowColor;
            }
            if (sortingGroup != null)
                sortingGroup.sortingLayerID = SortingLayers.Id(SortingLayers.Characters);
            if (motion != null)
                motion.HoldHop = false;
        }

        private void OnDisable() => Cancel();

        private void OnDestroy() => dust?.Destroy();
    }
}
