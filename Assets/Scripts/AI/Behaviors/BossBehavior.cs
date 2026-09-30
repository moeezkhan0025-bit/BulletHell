using BulletHell.Bosses;
using BulletHell.Enemies;
using UnityEngine;

namespace BulletHell.AI
{
    /// <summary>
    /// The boss's movement and attack choices: it holds a distance band from the player between attacks, then picks
    /// one of the current phase's attacks by weight (never the same kind twice in a row), winds it up with a readable
    /// telegraph (inflate + DANGER pulse for a Circle Spread, a warning line for the Fast Shot, a crouch for the jump),
    /// fires it through the EnemyAttacker, and recovers. The BossController it binds does the jump, the smash, the
    /// phase transition and the death; while that is busy this waits. Timings come from the BossData.
    /// </summary>
    public sealed class BossBehavior : IEnemyBehavior
    {
        private enum State { Enter, Reposition, Windup, Fire, Jump, PostSmash, Recover }

        private const float RecoverSeconds = 0.35f;
        private const float AbortCooldown = 0.6f;
        private const float StressCooldown = 0.15f;

        /// <summary>Development tools only (the stress test): the pause between attacks shrinks to a fraction of a second.</summary>
        public static bool DebugStress;

        private BossController boss;
        private State state;
        private BossAttack current;
        private float timer;
        private float cooldownLeft;
        private int volleysLeft;
        private int jumpsLeft;
        private float volleyClock;
        private float angle;
        private Vector2 aim;
        private int lastKind = -1;
        private bool smashed;
        private float strafeTimer;
        private float strafeSign = 1f;

        public void Begin(EnemyAgent agent)
        {
            boss = agent.Boss;
            if (boss == null || agent.Data.Boss == null)
            {
                Debug.LogWarning($"{agent.Data.DisplayName} has the Boss behaviour but no BossController or BossData: it stands still.", agent.Transform);
                boss = null;
                return;
            }
            boss.Bind(agent);
            boss.Smashed += OnSmashed;
            state = State.Enter;
            timer = boss.Data.EntrySettleSeconds;
            cooldownLeft = 0f;
            lastKind = -1;
            smashed = false;
            strafeTimer = 0f;
        }

        public void End(EnemyAgent agent)
        {
            agent.Line?.Hide();
            agent.Enemy.ClearWindup();
            if (boss == null)
                return;
            boss.Smashed -= OnSmashed;
            boss.Unbind();
            boss = null;
        }

        public void Tick(EnemyAgent agent, float dt)
        {
            if (boss == null)
            {
                agent.Move(Vector2.zero, dt);
                return;
            }
            if (boss.IsBusy)
            {
                Abort(agent);
                agent.Move(Vector2.zero, dt);
                return;
            }

            switch (state)
            {
                case State.Enter: Enter(agent, dt); break;
                case State.Reposition: Reposition(agent, dt); break;
                case State.Windup: Windup(agent, dt); break;
                case State.Fire: Fire(agent, dt); break;
                case State.Jump: Jump(agent, dt); break;
                case State.PostSmash: PostSmash(agent, dt); break;
                case State.Recover: Recover(agent, dt); break;
            }
        }

        private void OnSmashed(Vector2 at) => smashed = true;

        // Something bigger is happening (phase change, death): drop the attack and start again afterwards.
        private void Abort(EnemyAgent agent)
        {
            if (state == State.Reposition)
                return;
            agent.Line?.Hide();
            agent.Enemy.ClearWindup();
            state = State.Reposition;
            cooldownLeft = AbortCooldown;
        }

        private void Enter(EnemyAgent agent, float dt)
        {
            agent.Move(Vector2.zero, dt);
            timer -= dt;
            if (timer <= 0f)
                state = State.Reposition;
        }

        private void Reposition(EnemyAgent agent, float dt)
        {
            cooldownLeft -= dt;
            Vector2 band = boss.Data.PreferredDistance;
            float distance = agent.DistanceToPlayer;
            Vector2 toPlayer = agent.DirectionToPlayer;
            Vector2 desired;
            if (distance < band.x)
                desired = agent.FreeDirection(-toPlayer);
            else if (distance > band.y)
                desired = agent.PathDirection();
            else
            {
                strafeTimer -= dt;
                if (strafeTimer <= 0f)
                {
                    strafeSign = -strafeSign;
                    strafeTimer = Random.Range(1.5f, 3f);
                }
                desired = agent.FreeDirection(Steering.Rotate(toPlayer, 90f * strafeSign)) * 0.6f;
            }
            agent.Move(desired, dt, boss.Phase.MoveSpeedMultiplier);

            if (cooldownLeft <= 0f && Pick(boss.Phase))
                StartWindup(current.WindupSeconds);
        }

        // Weighted pick, never the same kind twice when another kind is available.
        private bool Pick(BossPhase phase)
        {
            int index = BossAttackPicker.Pick(phase.Attacks, lastKind, Random.value);
            if (index < 0)
                return false;
            current = phase.Attacks[index];
            lastKind = (int)current.Kind;
            return true;
        }

        private void StartWindup(float seconds)
        {
            state = State.Windup;
            timer = seconds;
            volleysLeft = current.Volleys;
            jumpsLeft = current.Kind == BossAttackKind.JumpSmash ? current.Volleys : 0;
            angle = 0f;
            if (current.Kind == BossAttackKind.JumpSmash)
                boss.Crouch();
        }

        private void Windup(EnemyAgent agent, float dt)
        {
            agent.Move(Vector2.zero, dt);
            timer -= dt;
            float total = state == State.Windup && jumpsLeft > 0 && jumpsLeft < current.Volleys
                ? boss.Data.Smash.SecondJumpWindup
                : current.WindupSeconds;
            float progress = 1f - Mathf.Clamp01(timer / Mathf.Max(0.01f, total));
            agent.Enemy.SetWindup(progress);

            if (current.Kind == BossAttackKind.FastShot)
            {
                if (timer > boss.Data.FastShotAimLockSeconds || aim == Vector2.zero)
                {
                    Vector2 tracked = ShotDirection(agent);
                    if (tracked != Vector2.zero)
                        aim = tracked;
                }
                Vector2 origin = agent.Enemy.BodyCenter;
                float reach = agent.HasGrid
                    ? agent.Arena.BulletRayDistance(origin, aim, 30f, current.Pattern.BulletSize * 0.5f)
                    : 30f;
                EnemyAiTuning tuning = agent.Tuning;
                Color color = tuning.WarningColor;
                color.a = Mathf.Lerp(0.25f, 1f, progress);
                agent.Line?.Show(origin, origin + aim * reach, color, tuning.WarningLineWidth * Mathf.Lerp(0.6f, 1.2f, progress));
            }

            if (timer > 0f)
                return;
            agent.Enemy.ClearWindup();
            agent.Line?.Hide();
            if (current.Kind == BossAttackKind.JumpSmash)
            {
                state = State.Jump;
                smashed = false;
                if (!boss.TryJump(agent.PlayerFeet))
                    StartRecover();
            }
            else
            {
                state = State.Fire;
                volleyClock = 0f;
            }
        }

        private void Fire(EnemyAgent agent, float dt)
        {
            agent.Move(Vector2.zero, dt);
            volleyClock -= dt;
            if (volleyClock > 0f)
                return;
            if (volleysLeft <= 0)
            {
                StartRecover();
                return;
            }
            Vector2 direction = current.Kind == BossAttackKind.FastShot ? aim : Vector2.zero;
            agent.Attacker?.FireVolley(current.Pattern, angle, direction);
            boss.Recoil();
            angle += current.AngleOffsetStepDeg;
            volleysLeft--;
            volleyClock = current.VolleyInterval;
        }

        private void Jump(EnemyAgent agent, float dt)
        {
            // The BossJump moves the root; nothing else steers until it lands.
            if (!smashed)
                return;
            smashed = false;
            state = State.PostSmash;
            volleysLeft = boss.Data.Smash.PostSpreads;
            volleyClock = 0f;
            angle = 0f;
        }

        private void PostSmash(EnemyAgent agent, float dt)
        {
            agent.Move(Vector2.zero, dt);
            volleyClock -= dt;
            if (volleyClock > 0f)
                return;
            if (volleysLeft > 0)
            {
                agent.Attacker?.FireVolley(current.Pattern, angle);
                boss.Recoil();
                angle += boss.Data.Smash.PostSpreadAngleStepDeg;
                volleysLeft--;
                volleyClock = boss.Data.Smash.PostSpreadInterval;
                return;
            }

            jumpsLeft--;
            if (jumpsLeft > 0)
            {
                // Phase 2 chains into another jump after a short crouch.
                state = State.Windup;
                timer = boss.Data.Smash.SecondJumpWindup;
                boss.Crouch();
            }
            else
                StartRecover();
        }

        private void StartRecover()
        {
            state = State.Recover;
            timer = RecoverSeconds;
        }

        private void Recover(EnemyAgent agent, float dt)
        {
            agent.Move(Vector2.zero, dt);
            timer -= dt;
            if (timer > 0f)
                return;
            state = State.Reposition;
            cooldownLeft = DebugStress ? StressCooldown : current.Cooldown / Mathf.Max(0.05f, agent.Difficulty.FireRateMultiplier);
            aim = Vector2.zero;
        }

        // Aims at the damage core, like every enemy bullet.
        private static Vector2 ShotDirection(EnemyAgent agent)
        {
            Vector2 to = agent.Player.Position - agent.Enemy.BodyCenter;
            return to.sqrMagnitude > 1e-6f ? to.normalized : Vector2.zero;
        }
    }
}
