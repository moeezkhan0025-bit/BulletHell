using System.Collections.Generic;
using BulletHell.Arena;
using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Feedback;
using BulletHell.Player;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Projectiles
{
    /// <summary>
    /// A pooled bullet. Moves in a straight line and sweeps a circle along each step, so fast bullets can't tunnel
    /// through targets. Released to its pool on a final hit, on leaving the screen, or after its safety lifetime.
    /// The firing arm's effects can let it pierce, ricochet and apply on-hit effects (burn, stun, ...).
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        [Tooltip("The bullet's sprite, a child lifted above the ground point by the perspective's bullet lift.")]
        [SerializeField] private SpriteRenderer body;
        [Tooltip("Tiny flattened shadow on the ground under the bullet.")]
        [SerializeField] private SpriteRenderer shadow;

        private const int RecentCapacity = 8;

        private static readonly RaycastHit2D[] HitBuffer = new RaycastHit2D[8];


        // Targets already hit by this bullet: it never hits them twice, so piercing/ricocheting can't stick in one enemy.
        private readonly Collider2D[] recentHits = new Collider2D[RecentCapacity];
        private int recentCount;
        private int recentNext;

        private ProjectilePool pool;
        private PerspectiveTuning perspective;
        private float shadowFlatness;
        private IReadOnlyList<ArmEffect> hitEffects;
        private Vector2 velocity;
        private float damage;
        private float radius;
        private float lifeLeft;
        private ShotProperties shot;
        private ShotState state;
        private Enemy homingTarget;
        private bool steeredThisFrame;
        private FeedbackTuning feedback;
        private bool released;
        private bool hostile;
        private string source;   // what fired a hostile bullet (telemetry: cause of death)
        private SpriteRenderer fillLayer;
        private SpriteRenderer coreLayer;
        private SpriteRenderer glowLayer;

        /// <summary>Index in the pool's list of active projectiles (managed by ProjectilePool).</summary>
        public int PoolIndex { get; set; } = -1;

        public void Bind(ProjectilePool owner)
        {
            pool = owner;
            perspective = GameServices.Ensure().Config.Perspective;
            feedback = GameServices.Ensure().Config.Feedback;

            // Collision stays on the ground plane (this transform); only the drawing is lifted.
            body.transform.localPosition = new Vector3(0f, perspective.BulletVisualLift, 0f);
            Color shadowColor = Color.black;
            shadowColor.a = perspective.BulletShadowAlpha;
            shadow.color = shadowColor;
            shadowFlatness = perspective.ShadowFlatness;

            // Enemy bullets are drawn in layers (outline = body, then fill, core, glow). Built once here, with the pool, never during play.
            if (fillLayer == null)
            {
                fillLayer = MakeLayer("Fill", 1);
                coreLayer = MakeLayer("Core", 2);
                glowLayer = MakeLayer("Glow", -1);
            }
        }

        public void Launch(Vector2 position, Vector2 direction, float speed, float damageAmount,
                           Color color, float size, Sprite sprite, float maxLifetime,
                           ShotProperties shot, IReadOnlyList<ArmEffect> effects)
        {
            transform.position = position;
            body.transform.localScale = Vector3.one * size;
            body.sprite = sprite;
            body.color = color;
            float shadowSize = size * perspective.BulletShadowScale;
            shadow.transform.localScale = new Vector3(shadowSize, shadowSize * shadowFlatness, 1f);
            velocity = direction.normalized * speed;
            damage = damageAmount;
            radius = size * 0.5f;
            lifeLeft = maxLifetime;
            this.shot = shot;
            state = new ShotState { PierceLeft = shot.Pierce, BouncesLeft = shot.Bounces, NeedsRetarget = true };
            homingTarget = null;
            hitEffects = effects;
            hostile = false;
            fillLayer.enabled = coreLayer.enabled = glowLayer.enabled = false;
            recentCount = 0;
            recentNext = 0;
            released = false;
        }

        /// <summary>An enemy bullet: hurts only the player, ignores enemies, carries no arm effects.</summary>
        public void LaunchHostile(Vector2 position, Vector2 direction, float speed, float damageAmount,
                                  BulletStyle style, float size, Sprite sprite, float maxLifetime, string sourceName = null)
        {
            source = sourceName ?? "Enemy bullet";
            EnemyBulletPalette palette = GameServices.Ensure().Config.EnemyBulletPalette;
            EnemyBulletPalette.Look look = palette.LookOf(style);
            Launch(position, direction, speed, damageAmount, look.Outline, size, sprite, maxLifetime, default, null);
            hostile = true;

            // The body sprite is the dark outline; the fill sits inside it by the outline thickness (pixels at 1080p to world units).
            Camera view = Camera.main;
            float pixelsPerUnit = view != null ? 540f / view.orthographicSize : 108f;
            // Settings > Gameplay: outline thickness scales the palette value; high contrast adds a white halo and a minimum thickness.
            BulletHell.Settings.SettingsData options = GameServices.Ensure().Settings.Current;
            bool highContrast = options.highContrastBullets;
            float outlinePixels = palette.OutlinePixels * options.bulletOutlineScale;
            if (highContrast)
                outlinePixels = Mathf.Max(outlinePixels, palette.HighContrastOutlinePixels);
            float inner = Mathf.Clamp01(1f - 2f * (outlinePixels / pixelsPerUnit) / size);
            Layer(fillLayer, sprite, look.Body, inner);
            Layer(coreLayer, sprite, look.Core, inner * palette.CoreScale);
            if (highContrast)
            {
                Color halo = palette.HaloColor;
                halo.a = palette.HaloOpacity;
                Layer(glowLayer, sprite, halo, palette.HaloScale);
            }
            else if (palette.GlowOpacity > 0f)
            {
                Color glow = look.Body;
                glow.a = palette.GlowOpacity;
                Layer(glowLayer, sprite, glow, palette.GlowScale);
            }
        }

        private SpriteRenderer MakeLayer(string name, int orderOffset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(body.transform, false);
            var layer = go.AddComponent<SpriteRenderer>();
            layer.sortingLayerID = body.sortingLayerID;
            layer.sortingOrder = body.sortingOrder + orderOffset;
            layer.sharedMaterial = body.sharedMaterial;
            layer.enabled = false;
            return layer;
        }

        private static void Layer(SpriteRenderer layer, Sprite sprite, Color color, float scale)
        {
            layer.sprite = sprite;
            layer.color = color;
            layer.transform.localScale = Vector3.one * scale;
            layer.enabled = true;
        }

        private void Update()
        {
            using var _ = BulletHell.Perf.PerfMarkers.BulletUpdate.Auto();
            float dt = Time.deltaTime;
            if (hostile)
            {
                HostileStep(dt);
                return;
            }

            steeredThisFrame = false;
            if (shot.HasHoming)
                Home(dt);

            Vector2 start = transform.position;
            Vector2 step = velocity * dt;
            float distance = step.magnitude;

            if (distance > 0f)
            {
                int count = Physics2D.CircleCast(start, radius, step / distance, pool.HitFilter, HitBuffer, distance);
                int best = -1;
                float bestDistance = float.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    if (WasHit(HitBuffer[i].collider) || HitBuffer[i].distance >= bestDistance)
                        continue;
                    best = i;
                    bestDistance = HitBuffer[i].distance;
                }

                if (best >= 0 && ResolveHit(HitBuffer[best]))
                    return;
            }

            Vector2 end = start + step;
            transform.position = end;
            if (BulletPathDebug.Enabled)
                BulletPathDebug.Segment(start, end, steeredThisFrame);
            lifeLeft -= dt;
            if (lifeLeft <= 0f || !pool.ViewBounds.Contains(end))
                ReleaseToPool();
        }

        // Homing: steer towards the current target, picking a new one when there is none, when it died or was already hit,
        // and after every pierce or bounce (ShotState.NeedsRetarget).
        private void Home(float dt)
        {
            IReadOnlyList<Enemy> enemies = pool.Enemies;
            if (enemies == null)
                return;

            Vector2 position = transform.position;
            if (state.NeedsRetarget)
            {
                homingTarget = null;
                state.NeedsRetarget = false;
            }
            if (homingTarget != null && (!homingTarget.IsAlive || WasHit(homingTarget.HitCollider) ||
                                         (homingTarget.HitCenter - position).sqrMagnitude > shot.HomingRange * shot.HomingRange * 1.5f))
                homingTarget = null;

            if (homingTarget == null)
            {
                Vector2 forward = velocity.normalized;
                float bestSqr = float.MaxValue;
                for (int i = 0; i < enemies.Count; i++)
                {
                    Enemy candidate = enemies[i];
                    if (candidate == null || !candidate.IsAlive || WasHit(candidate.HitCollider))
                        continue;
                    if (ProjectileRules.InCone(position, forward, candidate.HitCenter, shot.HomingCone, shot.HomingRange, out float sqr) && sqr < bestSqr)
                    {
                        bestSqr = sqr;
                        homingTarget = candidate;
                    }
                }
            }

            if (homingTarget != null)
            {
                velocity = ProjectileRules.TurnToward(velocity, position, homingTarget.HitCenter, shot.HomingTurnRate * dt);
                steeredThisFrame = true;
            }
        }

        // Enemy bullets test one circle (the player's hitbox) against the segment they travel this frame: no physics query.
        // While the player is invulnerable (or dead) bullets fly straight through.
        private void HostileStep(float dt)
        {
            Vector2 start = transform.position;
            Vector2 end = start + velocity * dt;

            // Obstacles and walls stop every bullet; a breakable takes the hit.
            ArenaController arena = pool.Arena;
            if (arena != null && arena.SegmentBlocked(start, end, radius, out int owner))
            {
                arena.DamageObstacle(owner, damage);
                ReleaseToPool();
                return;
            }

            PlayerHealth target = pool.PlayerTarget;
            if (target != null && target.CanBeHit)
            {
                Vector2 segment = end - start;
                float lengthSqr = segment.sqrMagnitude;
                float t = lengthSqr > 0f ? Mathf.Clamp01(Vector2.Dot(target.Position - start, segment) / lengthSqr) : 0f;
                float reach = radius + target.HitRadius;
                if ((target.Position - (start + segment * t)).sqrMagnitude <= reach * reach)
                {
                    target.TryHit(damage, velocity, source);
                    ReleaseToPool();
                    return;
                }
            }

            transform.position = end;
            lifeLeft -= dt;
            if (lifeLeft <= 0f || !pool.ViewBounds.Contains(end))
                ReleaseToPool();
        }

        /// <summary>Handles a hit. Returns true when this frame's movement is done (released or bounced).</summary>
        private bool ResolveHit(in RaycastHit2D hit)
        {
            Collider2D target = hit.collider;
            bool damageableAlive = target.TryGetComponent(out IDamageable damageable) && damageable.IsAlive;
            bool isWall = target.gameObject.layer == pool.ObstacleLayer || !damageableAlive;

            if (isWall)
                return ResolveWallHit(hit, damageableAlive ? damageable : null);

            // A live enemy (or other damageable target).
            if (target.TryGetComponent(out IHitReceiver receiver))
                receiver.OnHitFrom(velocity);
            damageable.TakeDamage(damage);
            pool.RegisterHit();
            if (hitEffects != null)
                for (int i = 0; i < hitEffects.Count; i++)
                    if (hitEffects[i] != null)
                        hitEffects[i].OnHit(target, damage);
            RememberHit(target);

            Vector2 point = hit.point;
            if (ProjectileRules.OnEnemyHit(ref state) == HitOutcome.Continue)
            {
                PlayFeedback(VfxKind.Spark, point, feedback.PierceSparks, feedback.PierceShake);
                if (BulletPathDebug.Enabled)
                    BulletPathDebug.Marker(point, BulletPathDebug.MarkerKind.Pierce);
                return false; // keeps flying through the target
            }

            if (BulletPathDebug.Enabled)
                BulletPathDebug.Marker(point, BulletPathDebug.MarkerKind.Stop);
            ReleaseToPool();
            return true;
        }

        // Walls and obstacles: a breakable takes the damage, then the bullet bounces if it has bounces left, else it stops.
        private bool ResolveWallHit(in RaycastHit2D hit, IDamageable breakable)
        {
            breakable?.TakeDamage(damage);

            if (ProjectileRules.OnWallHit(ref state, velocity, hit.normal, out Vector2 reflected) == HitOutcome.Bounce)
            {
                // Step off the surface so the next sweep does not start inside it.
                transform.position = hit.centroid + hit.normal * 0.02f;
                velocity = reflected;
                PlayFeedback(VfxKind.Spark, hit.point, feedback.RicochetSparks, feedback.RicochetShake);
                if (BulletPathDebug.Enabled)
                {
                    BulletPathDebug.Segment(transform.position, transform.position, false);
                    BulletPathDebug.Marker(hit.point, BulletPathDebug.MarkerKind.Ricochet);
                }
                return true;
            }

            PlayFeedback(VfxKind.Debris, hit.point, feedback.WallImpactDebris, 0f);
            if (BulletPathDebug.Enabled)
                BulletPathDebug.Marker(hit.point, BulletPathDebug.MarkerKind.Stop);
            ReleaseToPool();
            return true;
        }

        // Feedback through the M8.6 toolkit: a particle burst at the point (drawn with the bullet lift) and a little camera shake.
        private void PlayFeedback(VfxKind kind, Vector2 point, int count, float shake)
        {
            if (count > 0)
                FeedbackHub.Play(kind, new Vector3(point.x, point.y + perspective.BulletVisualLift, 0f), count);
            if (shake > 0f)
                CameraShake.Add(shake);
        }

        private bool WasHit(Collider2D collider)
        {
            for (int i = 0; i < recentCount; i++)
                if (recentHits[i] == collider)
                    return true;
            return false;
        }

        private void RememberHit(Collider2D collider)
        {
            recentHits[recentNext] = collider;
            recentNext = (recentNext + 1) % RecentCapacity;
            if (recentCount < RecentCapacity)
                recentCount++;
        }

        private void ReleaseToPool()
        {
            if (released)
                return;
            released = true;
            pool.Release(this);
        }
    }
}
