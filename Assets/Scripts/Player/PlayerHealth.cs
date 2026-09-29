using BulletHell.Core;
using BulletHell.Feedback;
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
        [Tooltip("Centre of the small damage hitbox, low near the feet on the ground plane. Empty = the root.")]
        [SerializeField] private Transform core;

        private Health health;
        private RunManager run;
        private Color bodyColor;
        private float invulnerableLeft;
        private bool dodgesBulletsInAir;
        private HitFeedback hitFeedback;

        public float Current => health.Current;
        public float Max => health.Max;
        public bool IsAlive => health.IsAlive;
        public bool IsInvulnerable => invulnerableLeft > 0f;
        /// <summary>Airborne (a jump). Enemy bullets still hit unless the jump is set to dodge them.</summary>
        public bool IsAirborne { get; private set; }
        /// <summary>On the ground: ground hazards (traps, hazard zones) only hurt a grounded player.</summary>
        public bool IsGrounded => !IsAirborne;
        public bool CanBeHit => health.IsAlive && invulnerableLeft <= 0f && !(IsAirborne && dodgesBulletsInAir);
        /// <summary>Where enemy bullets aim and hit: the damage core, low near the feet.</summary>
        public Vector2 Position => core != null ? core.position : transform.position;
        /// <summary>Where the player stands on the floor (traps, coins, spawn distances use this).</summary>
        public Vector2 FeetPosition => transform.position;
        public float HitRadius => data.HitboxRadius * CharacterScale.Value;
        /// <summary>Radius of the flat body footprint at the feet (what enemies touch on contact).</summary>
        public float BodyRadius => data.BodyRadius * CharacterScale.Value;

        private void Awake()
        {
            health = GetComponent<Health>();
            health.Initialize(data.MaxHealth);
            bodyColor = body.color;
            TryGetComponent(out hitFeedback);
            run = GameServices.Ensure().Run;
        }

        private void OnEnable()
        {
            run.RoundIntroStarted += OnRoundStarted;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            run.RoundIntroStarted -= OnRoundStarted;
            health.Died -= OnDied;
        }

        /// <summary>Set by the jump: while airborne the player can (optionally) pass through enemy bullets.</summary>
        public void SetAirborne(bool airborne, bool dodgesBullets)
        {
            IsAirborne = airborne;
            dodgesBulletsInAir = dodgesBullets;
        }

        /// <summary>An enemy bullet reached the player. Returns false when the player couldn't be hit (invulnerable or dead).</summary>
        public bool TryHit(float damage, Vector2 travelDirection = default)
        {
            if (!CanBeHit)
                return false;

            if (hitFeedback != null && travelDirection != Vector2.zero)
                hitFeedback.OnHitFrom(travelDirection);
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

        private void OnRoundStarted(int round) => ResetForRound();

        private void OnDied() => run.GameOver();
    }
}
