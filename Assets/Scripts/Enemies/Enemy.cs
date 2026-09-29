using System;
using BulletHell.Arena;
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
    /// The root stands on the floor at the enemy's FEET: it moves, sorts and collides as a flat footprint there. The
    /// body and health bar hang under the Rig child, which sits over the feet; player bullets hit a hurtbox that stands
    /// on the feet and reaches up over the body (PerspectiveTuning).
    /// </summary>
    public sealed class Enemy : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private CapsuleCollider2D hitbox;
        [Tooltip("Parent of the body and health bar; lifted above the feet by the enemy's size.")]
        [SerializeField] private Transform rig;
        [SerializeField] private SpriteRenderer body;
        [Tooltip("Flat ground shadow at the feet. Optional.")]
        [SerializeField] private SpriteRenderer shadow;
        [SerializeField] private HitFlash hitFlash;
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private PatrolMover patrol;

        private EnemyData data;
        private StatusEffects status;
        private EnemyAttacker attacker;

        public EnemyData Data => data;
        public bool IsAlive => health.IsAlive && body.enabled;
        /// <summary>The enemy's feet: where it stands on the floor.</summary>
        public Vector2 Position => transform.position;
        /// <summary>Middle of the body, where its bullets come out.</summary>
        public Vector2 BodyCenter => rig.position;
        /// <summary>Radius of the flat movement footprint at the feet.</summary>
        public float FootprintRadius { get; private set; }

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
                               ProjectilePool pool, PlayerHealth player, ArenaController arena = null)
        {
            data = enemyData;
            transform.position = position;

            PerspectiveTuning perspective = GameServices.Ensure().Config.Perspective;
            float size = data.Size;
            FootprintRadius = perspective.EnemyFootprintRadiusFor(size);
            float lift = size * (0.5f - perspective.EnemyFeetInset);
            rig.localPosition = new Vector3(0f, lift, 0f);
            body.transform.localScale = Vector3.one * size;

            Vector2 hurtbox = perspective.EnemyHurtboxSizeFor(size);
            hitbox.size = hurtbox;
            hitbox.offset = new Vector2(0f, -size * perspective.EnemyFeetInset + hurtbox.y * 0.5f);

            if (shadow != null)
            {
                float width = FootprintRadius * 2f * perspective.ShadowWidth;
                Vector2 native = shadow.sprite != null ? (Vector2)shadow.sprite.bounds.size : Vector2.one;
                shadow.transform.localScale = new Vector3(width / native.x, width * perspective.ShadowFlatness / native.y, 1f);
                shadow.color = perspective.ShadowColor;
            }

            health.Initialize(data.MaxHealth * difficulty.HealthMultiplier);
            hitFlash.Configure(data.Color, data.HitFlashDuration);
            healthBar.Layout(Mathf.Max(0.6f, size), size * 0.5f + 0.25f);
            patrol.Configure(position, data.MoveSpeed, data.MoveRange, data.MoveAxis, arena, FootprintRadius);
            if (status != null)
                status.Clear();
            SetAlive(true);

            if (attacker != null)
            {
                attacker.Configure(data.Attacks);
                attacker.Muzzle = rig;
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
            if (shadow != null)
                shadow.enabled = alive;
            if (!alive)
                patrol.enabled = false;
        }
    }
}
