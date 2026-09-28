using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Projectiles
{
    /// <summary>
    /// A pooled bullet. Moves in a straight line and sweeps a circle along each step, so fast bullets can't tunnel
    /// through targets. Released to its pool on hit, on leaving the screen, or after its safety lifetime.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Projectile : MonoBehaviour
    {
        private static readonly RaycastHit2D[] HitBuffer = new RaycastHit2D[4];

        private SpriteRenderer spriteRenderer;
        private ProjectilePool pool;
        private Vector2 velocity;
        private float damage;
        private float radius;
        private float lifeLeft;
        private bool released;

        public void Bind(ProjectilePool owner)
        {
            pool = owner;
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Launch(Vector2 position, Vector2 direction, float speed, float damageAmount,
                           Color color, float size, Sprite sprite, float maxLifetime)
        {
            transform.position = position;
            transform.localScale = Vector3.one * size;
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            velocity = direction.normalized * speed;
            damage = damageAmount;
            radius = size * 0.5f;
            lifeLeft = maxLifetime;
            released = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Vector2 start = transform.position;
            Vector2 step = velocity * dt;
            float distance = step.magnitude;

            if (distance > 0f)
            {
                int count = Physics2D.CircleCast(start, radius, step / distance, pool.HitFilter, HitBuffer, distance);
                if (count > 0)
                {
                    if (HitBuffer[0].collider.TryGetComponent(out IDamageable damageable) && damageable.IsAlive)
                    {
                        damageable.TakeDamage(damage);
                        pool.RegisterHit();
                    }
                    ReleaseToPool();
                    return;
                }
            }

            Vector2 end = start + step;
            transform.position = end;
            lifeLeft -= dt;
            if (lifeLeft <= 0f || !pool.ViewBounds.Contains(end))
                ReleaseToPool();
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
