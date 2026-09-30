using System;
using BulletHell.AI;
using BulletHell.Arena;
using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Feedback;
using BulletHell.Player;
using UnityEngine;

namespace BulletHell.Bosses
{
    /// <summary>
    /// The boss-only layer on top of a pooled Enemy: phases and the transition between them (roar, flash, shake, glow),
    /// the Jump &amp; Smash (jump, landing ring, radial ground damage) and the long death sequence. The movement and attack
    /// choices live in BossBehavior, which binds this on spawn. Every number comes from the EnemyData's BossData.
    /// </summary>
    public sealed class BossController : MonoBehaviour, IDeathSequence
    {
        private const float CandidateMargin = 0.6f;
        private const float StaggerPulseSeconds = 0.15f;
        private static readonly Collider2D[] Buffer = new Collider2D[64];

        [SerializeField] private Health health;
        [SerializeField] private Enemy enemy;
        [SerializeField] private SpriteFx fx;
        [SerializeField] private ProceduralMotion motion;
        [SerializeField] private TelegraphFx telegraph;
        [SerializeField] private BossJump jump;
        [SerializeField] private CapsuleCollider2D hitbox;

        private BossData data;
        private PlayerHealth player;
        private StatusEffects status;
        private SmashTelegraph ring;
        private ContactFilter2D enemyFilter;
        private bool bound;
        private int phaseIndex;
        private float glowPhase;

        private bool transitioning;
        private float transitionTime;
        private bool pendingTransition;

        private bool dying;
        private float deathTime;
        private int deathStage;
        private float staggerClock;
        private int burstsDone;
        private Action onDeathFinished;

        public BossData Data => data;
        public Health Health => health;
        public int PhaseIndex => phaseIndex;
        public BossPhase Phase => data != null && data.Phases.Length > 0 ? data.Phases[Mathf.Clamp(phaseIndex, 0, data.Phases.Length - 1)] : null;
        /// <summary>Transitioning or dying: the behaviour drops what it is doing and waits.</summary>
        public bool IsBusy => transitioning || dying;
        public bool IsDying => dying;
        public bool IsAirborne => jump != null && jump.IsAirborne;

        public event Action PhaseChanged;
        /// <summary>The landing hit the ground at this position.</summary>
        public event Action<Vector2> Smashed;

        private void Awake()
        {
            TryGetComponent(out status);
            enemyFilter = new ContactFilter2D { useLayerMask = true, layerMask = LayerMask.GetMask("Enemy"), useTriggers = true };
            if (jump != null)
                jump.Landed += OnLanded;
        }

        private void OnDestroy()
        {
            if (jump != null)
                jump.Landed -= OnLanded;
            ring?.Destroy();
        }

        // Recycled mid-sequence (round skipped): drop everything.
        private void OnDisable()
        {
            Unbind();
            dying = false;
            transitioning = false;
            pendingTransition = false;
            onDeathFinished = null;
            if (fx != null)
                fx.ClearAll();
        }

        /// <summary>A fresh spawn: phase 1, no glow, jump tuned, the field's active boss.</summary>
        public void Bind(EnemyAgent agent)
        {
            data = agent.Data.Boss;
            player = agent.Player;
            phaseIndex = 0;
            glowPhase = 0f;
            transitioning = false;
            pendingTransition = false;
            dying = false;
            if (fx != null)
                fx.ClearAll();
            if (hitbox != null)
                hitbox.enabled = true;

            if (ring == null)
                ring = new SmashTelegraph(transform.parent, data.Smash.RingSprite, GameServices.Ensure().Config.Perspective.ShadowFlatness);
            if (jump != null)
                jump.Configure(data.JumpTuning, agent.Arena, agent.Radius, data.Smash.HittableInAir);
            if (!bound)
            {
                health.Changed += OnHealthChanged;
                bound = true;
            }
            BossEvents.RaiseSpawned(this);
        }

        /// <summary>The behaviour ended (death, recycle). A running death sequence keeps its glow and the field's boss slot until it finishes.</summary>
        public void Unbind()
        {
            if (bound)
            {
                health.Changed -= OnHealthChanged;
                bound = false;
            }
            ring?.Hide();
            if (jump != null)
                jump.Cancel();
            // The brain stops just before the death sequence starts (health already 0): keep the glow and the field's
            // boss slot for the sequence, which clears both when it finishes. A live recycle clears them now.
            if (!dying && health.IsAlive)
            {
                SetGlow(null, 0f);
                BossEvents.Clear(this);
            }
        }

        /// <summary>Debug: set health to a fraction (0 kills it through the ordinary path).</summary>
        public void DebugSetHealth01(float fraction) => health.SetCurrent(health.Max * Mathf.Clamp01(fraction));

        // ---- attacks the behaviour asks for

        /// <summary>Recoil on a volley.</summary>
        public void Recoil()
        {
            if (motion != null && data != null)
                motion.Punch(data.RecoilPunch);
        }

        /// <summary>The crouch before a jump.</summary>
        public void Crouch()
        {
            if (motion != null && data != null)
                motion.Squash(data.Smash.CrouchSquash);
        }

        /// <summary>Jumps towards a ground target and shows the landing ring. False when it can't right now.</summary>
        public bool TryJump(Vector2 target)
        {
            if (jump == null || data == null || IsBusy || !jump.TryStart(target, data.Smash.MaxJumpDistance))
                return false;
            ring.Show(jump.LandingPoint, data.Smash.Radius, data.Smash.TelegraphColor);
            return true;
        }

        private void OnLanded(Vector2 at)
        {
            if (dying || data == null)
                return;
            Smash(at);
            if (pendingTransition)
                StartTransition();
        }

        // Like Trap.Strike: candidates by physics, what counts is the footprint on the floor. A jumping player is over it.
        private void Smash(Vector2 at)
        {
            SmashSettings smash = data.Smash;
            if (smash.DamageToEnemies > 0f)
            {
                int count = Physics2D.OverlapCircle(at, smash.Radius + CandidateMargin, enemyFilter, Buffer);
                for (int i = 0; i < count; i++)
                {
                    Collider2D candidate = Buffer[i];
                    if (candidate.gameObject == gameObject)
                        continue;
                    if (!candidate.TryGetComponent(out IDamageable target) || !target.IsAlive)
                        continue;
                    if (candidate.TryGetComponent(out Enemy other) && !TrapShape.CircleOverlapsCircle(at, smash.Radius, other.Position, other.FootprintRadius))
                        continue;
                    target.TakeDamage(smash.DamageToEnemies);
                }
            }

            if (player != null && smash.DamageToPlayer > 0f && player.IsGrounded && player.CanBeHit &&
                TrapShape.CircleOverlapsCircle(at, smash.Radius, player.FeetPosition, player.HitRadius))
                player.TryHit(smash.DamageToPlayer, (player.FeetPosition - at).normalized);

            ring.Flash();
            FeedbackHub.Play(VfxKind.Dust, at, smash.DustCount);
            FeedbackHub.Play(VfxKind.Debris, at, smash.DebrisCount);
            CameraShake.Add(smash.Shake);
            if (smash.HitstopSeconds > 0f)
                GameClock.Hitstop(smash.HitstopSeconds);
            Smashed?.Invoke(at);
        }

        // ---- phases

        private void OnHealthChanged()
        {
            if (data == null || IsBusy || !health.IsAlive)
                return;
            int next = phaseIndex + 1;
            if (next >= data.Phases.Length || health.Fraction > data.Phases[next].EnterBelowHp01)
                return;
            if (IsAirborne)
                pendingTransition = true;   // finish the jump first, the smash still lands
            else
                StartTransition();
        }

        private void StartTransition()
        {
            pendingTransition = false;
            transitioning = true;
            transitionTime = 0f;
            TransitionSettings t = data.Transition;
            if (t.Invulnerable && hitbox != null)
                hitbox.enabled = false;
            status?.Clear();
            if (motion != null)
                motion.Punch(t.RoarInflate);
            CameraShake.Add(t.Shake);
            if (t.HitstopSeconds > 0f)
                GameClock.Hitstop(t.HitstopSeconds);
            FeedbackHub.Play(VfxKind.Smoke, transform.position, 10);
        }

        private void TickTransition(float dt)
        {
            TransitionSettings t = data.Transition;
            transitionTime += dt;
            float progress = Mathf.Clamp01(transitionTime / t.Seconds);
            BossPhase nextPhase = data.Phases[phaseIndex + 1];

            telegraph.SetWindup(1f);
            // First half: the roar flashes. Second half: the new phase's glow comes up.
            bool flashOn = progress < 0.5f && Mathf.Sin(progress * 2f * Mathf.PI * t.FlashCycles) > 0f;
            fx.SetFlash(flashOn ? 0.85f : 0f);
            SetGlow(nextPhase, nextPhase.GlowAmount * Mathf.Clamp01((progress - 0.5f) * 2f));

            if (progress < 1f)
                return;
            transitioning = false;
            phaseIndex++;
            telegraph.ClearWindup();
            fx.SetFlash(0f);
            if (hitbox != null && !dying)
                hitbox.enabled = true;
            PhaseChanged?.Invoke();
            BossEvents.RaisePhaseChanged(this);
        }

        private void SetGlow(BossPhase phase, float amount)
        {
            if (fx != null)
                fx.SetGlow(phase != null ? phase.GlowColor : Color.black, amount);
        }

        // ---- death

        bool IDeathSequence.TryBegin(Action onFinished)
        {
            if (data == null)
                return false;
            dying = true;
            deathTime = 0f;
            deathStage = 0;
            staggerClock = 0f;
            burstsDone = 0;
            onDeathFinished = onFinished;
            transitioning = false;
            pendingTransition = false;

            if (jump != null)
                jump.Cancel();
            ring?.Hide();
            if (hitbox != null)
                hitbox.enabled = false;
            telegraph.ClearWindup();
            SetGlow(null, 0f);
            if (motion != null)
                motion.HoldHop = false;

            DeathSettings d = data.Death;
            CameraShake.Add(d.ShakePerStage);
            if (d.HitstopSeconds > 0f)
                GameClock.Hitstop(d.HitstopSeconds);
            return true;
        }

        private void TickDeath(float dt)
        {
            DeathSettings d = data.Death;
            deathTime += dt;
            float staggerEnd = d.StaggerSeconds;
            float squashEnd = staggerEnd + d.SquashSeconds;
            float dissolveEnd = squashEnd + d.DissolveSeconds;

            if (deathTime < staggerEnd)
            {
                // Staggering: shake pulses, debris bursts out of the body, the flash strobes.
                staggerClock -= dt;
                if (staggerClock <= 0f)
                {
                    staggerClock = StaggerPulseSeconds;
                    CameraShake.Add(d.ShakePerStage * 0.6f);
                    if (motion != null)
                        motion.Knock(UnityEngine.Random.insideUnitCircle.normalized, 0.08f);
                    if (burstsDone < d.DebrisBursts)
                    {
                        FeedbackHub.Play(VfxKind.Debris, RandomBodyPoint(), d.DebrisPerBurst);
                        burstsDone++;
                    }
                }
                float wave = Mathf.Sin(deathTime / staggerEnd * d.FlashCycles * 2f * Mathf.PI);
                fx.SetFlash(wave > 0f ? 0.9f : 0f);
                return;
            }

            if (deathStage == 0)
            {
                // Squash into the ground.
                deathStage = 1;
                if (motion != null)
                    motion.Squash(0.45f);
                FeedbackHub.Play(VfxKind.Dust, transform.position, 14);
                CameraShake.Add(d.ShakePerStage);
                fx.SetFlash(0.3f);
            }

            if (deathTime >= squashEnd && deathTime < dissolveEnd)
            {
                float progress = (deathTime - squashEnd) / d.DissolveSeconds;
                fx.SetDissolve(progress);
                if (deathStage == 1 && progress >= 0.3f)
                {
                    deathStage = 2;
                    FeedbackHub.Play(VfxKind.Smoke, enemy.BodyCenter, 8);
                }
                if (deathStage == 2 && progress >= 0.7f)
                {
                    deathStage = 3;
                    FeedbackHub.Play(VfxKind.Spark, enemy.BodyCenter, 12);
                }
                return;
            }

            if (deathTime >= dissolveEnd)
                FinishDeath();
        }

        private void FinishDeath()
        {
            DeathSettings d = data.Death;
            FeedbackHub.Play(VfxKind.Coin, transform.position, d.CoinBurst);
            CameraShake.Add(Mathf.Clamp01(d.ShakePerStage * 1.5f));
            fx.ClearAll();
            dying = false;
            BossEvents.RaiseDefeated(this);
            Action finished = onDeathFinished;
            onDeathFinished = null;
            finished?.Invoke();
        }

        private Vector2 RandomBodyPoint()
        {
            Vector2 extent = hitbox != null ? hitbox.size * 0.4f : Vector2.one * 0.5f;
            Vector2 offset = UnityEngine.Random.insideUnitCircle;
            return enemy.BodyCenter + new Vector2(offset.x * extent.x, offset.y * extent.y);
        }

        // ---- per frame

        private void Update()
        {
            float dt = Time.deltaTime;
            ring?.Tick(dt);
            if (data == null)
                return;

            if (dying)
            {
                TickDeath(dt);
                return;
            }
            if (transitioning)
            {
                TickTransition(dt);
                return;
            }

            if (jump != null && jump.IsAirborne)
                ring.SetProgress(jump.Progress01);

            BossPhase phase = Phase;
            if (phaseIndex > 0 && phase != null && phase.GlowAmount > 0f)
            {
                glowPhase += dt * phase.GlowPulseSpeed;
                SetGlow(phase, phase.GlowAmount * (0.8f + 0.2f * Mathf.Sin(glowPhase)));
            }
        }
    }
}
