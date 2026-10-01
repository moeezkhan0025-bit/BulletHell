using BulletHell.Core;
using BulletHell.Feedback;
using BulletHell.Telemetry;
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
        [Tooltip("The paper doll whose layers blink while invulnerable. Only their alpha is touched.")]
        [SerializeField] private BulletHell.Cosmetics.GladiatorCosmetics doll;
        [Tooltip("Centre of the small damage hitbox, low near the feet on the ground plane. Empty = the root.")]
        [SerializeField] private Transform core;

        private Health health;
        private RunManager run;
        private float invulnerableLeft;
        private bool dodgesBulletsInAir;
        private HitFeedback hitFeedback;

        /// <summary>Development tools only (the stress test): hits still land (feedback, invulnerability) but health never runs out.</summary>
        public bool DebugGodMode
        {
            get => debugGodMode;
            set
            {
                debugGodMode = value;
                if (value && run != null)
                    run.MarkDebug();   // telemetry leaves such runs out of the averages
            }
        }

        private bool debugGodMode;

        /// <summary>Capture tool only (R1): hits are ignored outright (no damage, no hit feedback) so hearts and the screen stay clean on camera. Marks the run as a debug run.</summary>
        public bool DebugImmune
        {
            get => debugImmune;
            set
            {
                debugImmune = value;
                if (value && run != null)
                    run.MarkDebug();
            }
        }

        private bool debugImmune;

        public float Current => health.Current;
        public float Max => health.Max;
        public bool IsAlive => health.IsAlive;
        public bool IsInvulnerable => invulnerableLeft > 0f;
        /// <summary>Airborne (a jump). Enemy bullets still hit unless the jump is set to dodge them.</summary>
        public bool IsAirborne { get; private set; }
        /// <summary>On the ground: ground hazards (traps, hazard zones) only hurt a grounded player.</summary>
        public bool IsGrounded => !IsAirborne;
        public bool CanBeHit => !debugImmune && health.IsAlive && invulnerableLeft <= 0f && !(IsAirborne && dodgesBulletsInAir);
        /// <summary>Where enemy bullets aim and hit: the damage core, low near the feet.</summary>
        public Vector2 Position => core != null ? core.position : transform.position;
        /// <summary>Where the player stands on the floor (traps, coins, spawn distances use this).</summary>
        public Vector2 FeetPosition => transform.position;
        public float HitRadius => data.HitboxRadius;
        /// <summary>Radius of the flat body footprint at the feet (what enemies touch on contact).</summary>
        public float BodyRadius => data.BodyRadius;

        private void Awake()
        {
            health = GetComponent<Health>();
            health.Initialize(data.MaxHealth);
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

        /// <summary>A hit reached the player (source names what did it, for the playtest telemetry). Returns false when the player could not be hit (invulnerable or dead).</summary>
        public bool TryHit(float damage, Vector2 travelDirection = default, string source = null)
        {
            if (!CanBeHit)
                return false;

            if (hitFeedback != null && travelDirection != Vector2.zero)
                hitFeedback.OnHitFrom(travelDirection);
            TelemetryEvents.RaisePlayerDamaged(damage, source);   // before the damage lands, so the death knows what killed it
            GameServices.Ensure().Audio.Play(BulletHell.Audio.SfxId.PlayerHit);
            if (DebugGodMode && health.Current <= damage)
                health.Revive();
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
            SetBlinkAlpha(1f);
        }

        private void Update()
        {
            if (invulnerableLeft <= 0f)
                return;

            invulnerableLeft -= Time.deltaTime;
            bool faded = invulnerableLeft > 0f && Mathf.FloorToInt(invulnerableLeft / data.BlinkInterval) % 2 == 0;
            SetBlinkAlpha(faded ? 0.35f : 1f);
        }

        private void SetBlinkAlpha(float alpha)
        {
            if (doll != null)
                doll.SetAlpha(alpha);
        }

        private void OnRoundStarted(int round) => ResetForRound();

        private void OnDied() => run.GameOver();
    }
}
