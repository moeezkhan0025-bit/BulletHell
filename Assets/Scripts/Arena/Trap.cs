using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Player;
using UnityEngine;

namespace BulletHell.Arena
{
    /// <summary>
    /// A placed trap. It cycles Idle -> Telegraph (a visible warning) -> Active (strikes) -> Cooldown, but only while the
    /// run is in Combat, so it does nothing during the round intro and freezes with pause. A strike hurts the player
    /// (through PlayerHealth.TryHit, so invulnerability frames apply) and every enemy inside its area, so luring enemies
    /// onto it works; enemies it kills drop coins like any other kill.
    /// </summary>
    public sealed class Trap : MonoBehaviour
    {
        private const float IdleAlpha = 0.14f;
        private const float CandidateMargin = 0.6f;
        private static readonly Collider2D[] EnemyBuffer = new Collider2D[64];

        [SerializeField] private SpriteRenderer art;

        private TrapData data;
        private TrapTimer timer;
        private RunManager run;
        private PlayerHealth player;
        private ContactFilter2D enemyFilter;
        private float angle;

        public TrapPhase Phase => timer != null ? timer.Phase : TrapPhase.Idle;
        public TrapData Data => data;

        /// <summary>Fills this (pooled) object in for a placement. It stays idle until Begin.</summary>
        public void Setup(TrapData trapData, Vector2 position, float rotation, float extraStartDelay,
                          RunManager runManager, PlayerHealth playerHealth, Sprite circleSprite, Sprite squareSprite)
        {
            data = trapData;
            run = runManager;
            player = playerHealth;
            angle = rotation;
            timer = data.CreateTimer(extraStartDelay);
            enemyFilter = new ContactFilter2D { useLayerMask = true, layerMask = LayerMask.GetMask("Enemy"), useTriggers = true };

            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, 0f, rotation);

            Sprite sprite = data.Sprite != null ? data.Sprite : (data.IsBox ? squareSprite : circleSprite);
            art.sprite = sprite;
            art.sortingLayerID = SortingLayers.Id(SortingLayers.Ground);
            Vector2 spriteSize = sprite != null ? (Vector2)sprite.bounds.size : Vector2.one;
            Vector2 area = data.IsBox ? data.Size : new Vector2(data.Size.x * 2f, data.Size.x * 2f);
            art.transform.localScale = new Vector3(area.x / spriteSize.x, area.y / spriteSize.y, 1f);
            ResetTrap();
        }

        /// <summary>Combat has begun: start the cycle (start delay, then the first telegraph).</summary>
        public void Begin()
        {
            timer.Start();
            ApplyLook();
        }

        /// <summary>Back to idle (round intro, round over): no timers, no strikes, just the faint floor marking.</summary>
        public void ResetTrap()
        {
            timer.Reset();
            ApplyLook();
        }

        private void Update()
        {
            if (timer == null || !timer.IsRunning || run.Machine.Current != GameState.Combat)
                return;

            int strikes = timer.Tick(Time.deltaTime);
            ApplyLook();
            for (int i = 0; i < strikes; i++)
                Strike();
        }

        private void Strike()
        {
            if (data.HurtsEnemies && data.DamageToEnemies > 0f)
            {
                // The physics query only finds candidates (hurtboxes stand over the feet, so it is inflated a little);
                // what counts is the enemy's footprint on the floor, like for the player.
                Vector2 center = transform.position;
                int count = data.IsBox
                    ? Physics2D.OverlapBox(center, data.Size + Vector2.one * CandidateMargin * 2f, angle, enemyFilter, EnemyBuffer)
                    : Physics2D.OverlapCircle(center, data.Size.x + CandidateMargin, enemyFilter, EnemyBuffer);
                for (int i = 0; i < count; i++)
                {
                    Collider2D candidate = EnemyBuffer[i];
                    if (!candidate.TryGetComponent(out IDamageable target) || !target.IsAlive)
                        continue;
                    if (candidate.TryGetComponent(out Enemy enemy) && !FootprintInside(enemy.Position, enemy.FootprintRadius))
                        continue;
                    target.TakeDamage(data.DamageToEnemies);
                }
            }

            if (data.HurtsPlayer && player != null && player.IsGrounded && player.CanBeHit && FootprintInside(player.FeetPosition, player.HitRadius))
                player.TryHit(data.DamageToPlayer);
        }

        private bool FootprintInside(Vector2 feet, float radius) => data.IsBox
            ? TrapShape.CircleOverlapsBox(transform.position, data.Size, angle, feet, radius)
            : TrapShape.CircleOverlapsCircle(transform.position, data.Size.x, feet, radius);

        private void ApplyLook()
        {
            Color color;
            switch (timer.Phase)
            {
                case TrapPhase.Telegraph:
                    // A warning that grows and pulses until the trap goes off.
                    color = data.TelegraphColor;
                    color.a = Mathf.Lerp(0.25f, 0.7f, timer.Progress) * (0.8f + 0.2f * Mathf.Sin(Time.time * 14f));
                    break;
                case TrapPhase.Active:
                    color = data.ActiveColor;
                    color.a = 0.9f;
                    break;
                default:
                    color = data.TelegraphColor;
                    color.a = IdleAlpha;
                    break;
            }
            art.color = color;
        }
    }
}
