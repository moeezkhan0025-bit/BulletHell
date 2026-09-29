using System.Collections.Generic;
using BulletHell.Arena;
using BulletHell.Core;
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
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Projectile : MonoBehaviour
    {
        private const int RecentCapacity = 8;

        private static readonly RaycastHit2D[] HitBuffer = new RaycastHit2D[8];
        private static readonly Collider2D[] BounceBuffer = new Collider2D[16];

        // Targets already hit by this bullet: it never hits them twice, so piercing/ricocheting can't stick in one enemy.
        private readonly Collider2D[] recentHits = new Collider2D[RecentCapacity];
        private int recentCount;
        private int recentNext;

        private SpriteRenderer spriteRenderer;
        private ProjectilePool pool;
        private IReadOnlyList<ArmEffect> hitEffects;
        private Vector2 velocity;
        private float damage;
        private float radius;
        private float lifeLeft;
        private int pierceLeft;
        private int bouncesLeft;
        private float bounceRange;
        private bool released;
        private bool hostile;

        /// <summary>Index in the pool's list of active projectiles (managed by ProjectilePool).</summary>
        public int PoolIndex { get; set; } = -1;

        public void Bind(ProjectilePool owner)
        {
            pool = owner;
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Launch(Vector2 position, Vector2 direction, float speed, float damageAmount,
                           Color color, float size, Sprite sprite, float maxLifetime,
                           ShotProperties shot, IReadOnlyList<ArmEffect> effects)
        {
            transform.position = position;
            transform.localScale = Vector3.one * size;
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            velocity = direction.normalized * speed;
            damage = damageAmount;
            radius = size * 0.5f;
            lifeLeft = maxLifetime;
            pierceLeft = shot.Pierce;
            bouncesLeft = shot.Bounces;
            bounceRange = shot.BounceRange;
            hitEffects = effects;
            hostile = false;
            recentCount = 0;
            recentNext = 0;
            released = false;
        }

        /// <summary>An enemy bullet: hurts only the player, ignores enemies, carries no arm effects.</summary>
        public void LaunchHostile(Vector2 position, Vector2 direction, float speed, float damageAmount,
                                  Color color, float size, Sprite sprite, float maxLifetime)
        {
            Launch(position, direction, speed, damageAmount, color, size, sprite, maxLifetime, default, null);
            hostile = true;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (hostile)
            {
                HostileStep(dt);
                return;
            }

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
            lifeLeft -= dt;
            if (lifeLeft <= 0f || !pool.ViewBounds.Contains(end))
                ReleaseToPool();
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
                    target.TryHit(damage);
                    ReleaseToPool();
                    return;
                }
            }

            transform.position = end;
            lifeLeft -= dt;
            if (lifeLeft <= 0f || !pool.ViewBounds.Contains(end))
                ReleaseToPool();
        }

        /// <summary>Handles a hit. Returns true when this frame's movement is done (released or redirected).</summary>
        private bool ResolveHit(in RaycastHit2D hit)
        {
            Collider2D target = hit.collider;
            if (!target.TryGetComponent(out IDamageable damageable) || !damageable.IsAlive)
            {
                ReleaseToPool(); // walls and dead targets stop the bullet
                return true;
            }

            damageable.TakeDamage(damage);
            pool.RegisterHit();
            if (hitEffects != null)
                for (int i = 0; i < hitEffects.Count; i++)
                    if (hitEffects[i] != null)
                        hitEffects[i].OnHit(target, damage);
            RememberHit(target);

            if (pierceLeft > 0)
            {
                pierceLeft--;
                return false; // keeps flying through the target
            }

            if (bouncesLeft > 0)
            {
                bouncesLeft--;
                Redirect(hit);
                return true;
            }

            ReleaseToPool();
            return true;
        }

        /// <summary>Points the bullet at the nearest target it hasn't hit yet, or reflects it off the surface if none is in range.</summary>
        private void Redirect(in RaycastHit2D hit)
        {
            float speed = velocity.magnitude;
            Vector2 origin = hit.centroid;
            Vector2 direction = Vector2.Reflect(velocity / speed, hit.normal);

            int count = Physics2D.OverlapCircle(origin, bounceRange, pool.HitFilter, BounceBuffer);
            float bestSqr = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider2D candidate = BounceBuffer[i];
                if (WasHit(candidate) || candidate.gameObject.layer == pool.ObstacleLayer ||
                    !candidate.TryGetComponent(out IDamageable damageable) || !damageable.IsAlive)
                    continue;
                Vector2 toCandidate = (Vector2)candidate.bounds.center - origin;
                float sqr = toCandidate.sqrMagnitude;
                if (sqr >= bestSqr || sqr < 0.0001f)
                    continue;
                bestSqr = sqr;
                direction = toCandidate / Mathf.Sqrt(sqr);
            }

            transform.position = origin;
            velocity = direction * speed;
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
