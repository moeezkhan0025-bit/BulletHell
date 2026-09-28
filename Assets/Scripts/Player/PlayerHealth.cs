using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>
    /// The player's health and the target enemy bullets test against. Bullets check a small circle (the hitbox) with a
    /// swept distance test, not the physics system. A hit starts a short invulnerability during which bullets pass
    /// through and the body blinks. Health refills at the start of every round; dying ends the run (Game Over).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private PlayerData data;
        [Tooltip("Sprite that blinks while invulnerable. Only its tint is touched, never the art.")]
        [SerializeField] private SpriteRenderer body;

        private Health health;
        private RunManager run;
        private Color bodyColor;
        private float invulnerableLeft;

        public float Current => health.Current;
        public float Max => health.Max;
        public bool IsAlive => health.IsAlive;
        public bool IsInvulnerable => invulnerableLeft > 0f;
        public bool CanBeHit => health.IsAlive && invulnerableLeft <= 0f;
        public Vector2 Position => transform.position;
        public float HitRadius => data.HitboxRadius;

        private void Awake()
        {
            health = GetComponent<Health>();
            health.Initialize(data.MaxHealth);
            bodyColor = body.color;
            run = GameServices.Ensure().Run;
        }

        private void OnEnable()
        {
            run.Machine.StateChanged += OnStateChanged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            run.Machine.StateChanged -= OnStateChanged;
            health.Died -= OnDied;
        }

        /// <summary>An enemy bullet reached the player. Returns false when the player couldn't be hit (invulnerable or dead).</summary>
        public bool TryHit(float damage)
        {
            if (!CanBeHit)
                return false;

            health.TakeDamage(damage);
            if (health.IsAlive)
                invulnerableLeft = data.InvulnerabilitySeconds;
            return true;
        }

        /// <summary>Full health, no invulnerability. Called at the start of every round.</summary>
        public void ResetForRound()
        {
            health.Revive();
            invulnerableLeft = 0f;
            body.color = bodyColor;
        }

        private void Update()
        {
            if (invulnerableLeft <= 0f)
                return;

            invulnerableLeft -= Time.deltaTime;
            Color color = bodyColor;
            bool faded = invulnerableLeft > 0f && Mathf.FloorToInt(invulnerableLeft / data.BlinkInterval) % 2 == 0;
            color.a = faded ? bodyColor.a * 0.35f : bodyColor.a;
            body.color = color;
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Combat && from != GameState.Pause)
                ResetForRound();
        }

        private void OnDied() => run.GameOver();
    }
}
