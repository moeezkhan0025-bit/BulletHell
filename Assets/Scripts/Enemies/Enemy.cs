using System;
using BulletHell.Core;
using BulletHell.Player;
using BulletHell.Projectiles;
using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>
    /// A pooled enemy. The wave spawner takes one from the EnemyPool and calls Initialize with the enemy type and the
    /// round's difficulty; this binds the EnemyData to the shared Health / hit flash / health bar / patrol / attacker
    /// components. When it dies it hides and raises Defeated; the spawner returns it to the pool.
    /// </summary>
    public sealed class Enemy : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private CircleCollider2D hitbox;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private HitFlash hitFlash;
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private PatrolMover patrol;

        private EnemyData data;
        private StatusEffects status;
        private EnemyAttacker attacker;

        public EnemyData Data => data;
        public bool IsAlive => health.IsAlive && body.enabled;
        public Vector2 Position => transform.position;

        /// <summary>Raised when this enemy dies.</summary>
        public event Action<Enemy> Defeated;

        private void Awake()
        {
            TryGetComponent(out status);
            TryGetComponent(out attacker);
            health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (health != null)
                health.Died -= OnDied;
        }

        /// <summary>Makes this enemy a fresh, alive enemy of the given type at a position, scaled by the round's difficulty.</summary>
        public void Initialize(EnemyData enemyData, Vector2 position, in RoundDifficulty difficulty,
                               ProjectilePool pool, PlayerHealth player)
        {
            data = enemyData;
            transform.position = position;

            body.transform.localScale = Vector3.one * data.Size;
            hitbox.radius = data.Size * 0.5f;
            health.Initialize(data.MaxHealth * difficulty.HealthMultiplier);
            hitFlash.Configure(data.Color, data.HitFlashDuration);
            healthBar.Layout(Mathf.Max(0.6f, data.Size), data.Size * 0.5f + 0.25f);
            patrol.Configure(position, data.MoveSpeed, data.MoveRange, data.MoveAxis);
            if (status != null)
                status.Clear();
            SetAlive(true);

            if (attacker != null)
            {
                attacker.Configure(data.Attacks);
                attacker.Bind(pool, player);
                attacker.FireRateMultiplier = difficulty.FireRateMultiplier;
                attacker.BulletSpeedMultiplier = difficulty.BulletSpeedMultiplier;
                attacker.Begin();
            }
        }

        private void OnDied()
        {
            if (status != null)
                status.Clear();
            if (attacker != null)
                attacker.Stop();
            SetAlive(false);
            Defeated?.Invoke(this);
        }

        private void SetAlive(bool alive)
        {
            body.enabled = alive;
            hitbox.enabled = alive;
            if (!alive)
                patrol.enabled = false;
        }
    }
}
