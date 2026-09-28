using System;
using System.Collections;
using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>
    /// Binds an EnemyData to the shared Health / hit flash / health bar / patrol components, hides itself on death
    /// and respawns at its start position (unless a CombatController runs the round and turns auto-respawn off).
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
        private StatusEffects status;

        public EnemyData Data => data;
        public bool IsAlive => health.IsAlive;

        /// <summary>Respawn by itself after the data's delay. A round controller turns this off and calls ResetForRound instead.</summary>
        public bool AutoRespawn { get; set; } = true;

        /// <summary>Raised when this enemy dies.</summary>
        public event Action<Enemy> Defeated;

        private void Awake()
        {
            spawnPosition = transform.position;
            TryGetComponent(out status);
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
            if (status != null)
                status.Clear();
            SetAlive(false);
            if (AutoRespawn)
                StartCoroutine(RespawnAfterDelay());
            Defeated?.Invoke(this);
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(data.RespawnDelay);
            ResetForRound();
        }

        /// <summary>Back to full health at the start position, alive. Used at the start of every round.</summary>
        public void ResetForRound()
        {
            StopAllCoroutines();
            if (status != null)
                status.Clear();
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
