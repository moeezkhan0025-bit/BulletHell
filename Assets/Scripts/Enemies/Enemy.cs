using System.Collections;
using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>
    /// Binds an EnemyData to the shared Health / hit flash / health bar / patrol components, hides itself on death
    /// and respawns at its start position. Real enemies (M4) build on this.
    /// </summary>
    public sealed class Enemy : MonoBehaviour
    {
        [SerializeField] private EnemyData data;
        [SerializeField] private Health health;
        [SerializeField] private CircleCollider2D hitbox;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private HitFlash hitFlash;
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private PatrolMover patrol;

        private Vector2 spawnPosition;

        public EnemyData Data => data;

        private void Awake()
        {
            spawnPosition = transform.position;
            body.transform.localScale = Vector3.one * data.Size;
            hitbox.radius = data.Size * 0.5f;
            health.Initialize(data.MaxHealth);
            hitFlash.Configure(data.Color, data.HitFlashDuration);
            healthBar.Layout(Mathf.Max(0.6f, data.Size), data.Size * 0.5f + 0.25f);
            patrol.Configure(spawnPosition, data.MoveSpeed, data.MoveRange, data.MoveAxis);
            health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (health != null)
                health.Died -= OnDied;
        }

        private void OnDied()
        {
            SetAlive(false);
            StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(data.RespawnDelay);
            patrol.Restart();
            health.Revive();
            hitFlash.Clear();
            SetAlive(true);
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
