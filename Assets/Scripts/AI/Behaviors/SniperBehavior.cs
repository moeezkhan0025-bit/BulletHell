using BulletHell.Enemies;
using UnityEngine;

namespace BulletHell.AI
{
    /// <summary>
    /// Keeps far from the player with a clear line (repositioning when it has none), then stops, shows a warning line
    /// that tracks the player and locks shortly before the shot, and fires one fast bullet along it.
    /// </summary>
    public sealed class SniperBehavior : IEnemyBehavior
    {
        private enum State { Position, Aim }

        private readonly RangedPositioner positioner = new RangedPositioner();
        private State state;
        private float timer;
        private float cooldown;
        private Vector2 aim;

        public void Begin(EnemyAgent agent)
        {
            positioner.Reset(agent);
            state = State.Position;
            cooldown = agent.Data.SniperCooldown * 0.5f;
        }

        public void End(EnemyAgent agent)
        {
            agent.Line?.Hide();
            agent.Enemy.ClearTint();
        }

        public void Tick(EnemyAgent agent, float dt)
        {
            if (state == State.Position)
                Reposition(agent, dt);
            else
                Aim(agent, dt);
        }

        private void Reposition(EnemyAgent agent, float dt)
        {
            cooldown -= dt;
            bool line = agent.HasLineToPlayer();
            agent.Move(positioner.Desired(agent, line, dt), dt);

            float nearest = agent.Data.PreferredDistance - agent.Data.DistanceTolerance;
            if (line && cooldown <= 0f && agent.DistanceToPlayer >= nearest)
            {
                state = State.Aim;
                timer = agent.Data.AimSeconds;
                aim = ShotDirection(agent);
            }
        }

        private void Aim(EnemyAgent agent, float dt)
        {
            agent.Move(Vector2.zero, dt);
            timer -= dt;

            if (timer > agent.Data.AimLockSeconds)
            {
                Vector2 tracked = ShotDirection(agent);
                if (tracked != Vector2.zero)
                    aim = tracked;
            }

            AttackPattern shot = agent.Data.SniperShot;
            Vector2 origin = agent.Enemy.BodyCenter;
            float reach = agent.Arena != null && agent.Arena.IsBuilt
                ? agent.Arena.BulletRayDistance(origin, aim, 30f, shot != null ? shot.BulletSize * 0.5f : 0.1f)
                : 30f;
            EnemyAiTuning tuning = agent.Tuning;
            float progress = 1f - Mathf.Clamp01(timer / agent.Data.AimSeconds);
            Color color = tuning.WarningColor;
            color.a = Mathf.Lerp(0.25f, 1f, progress);
            agent.Line?.Show(origin, origin + aim * reach, color, tuning.WarningLineWidth * Mathf.Lerp(0.5f, 1f, progress));
            agent.Enemy.SetTint(Color.Lerp(agent.Data.Color, Color.white, progress * 0.5f));

            if (timer <= 0f)
            {
                Fire(agent, shot, origin);
                agent.Line?.Hide();
                agent.Enemy.ClearTint();
                cooldown = agent.Data.SniperCooldown / Mathf.Max(0.05f, agent.Difficulty.FireRateMultiplier);
                state = State.Position;
            }
        }

        // Aims at the damage core, like every enemy bullet.
        private static Vector2 ShotDirection(EnemyAgent agent)
        {
            Vector2 to = agent.Player.Position - agent.Enemy.BodyCenter;
            return to.sqrMagnitude > 1e-6f ? to.normalized : Vector2.zero;
        }

        private void Fire(EnemyAgent agent, AttackPattern shot, Vector2 origin)
        {
            if (shot == null || agent.Pool == null || aim == Vector2.zero)
                return;
            agent.Pool.Get().LaunchHostile(origin, aim, shot.BulletSpeed * agent.Difficulty.BulletSpeedMultiplier,
                                           shot.Damage, shot.BulletColor, shot.BulletSize, shot.BulletSprite, shot.BulletLifetime);
        }
    }
}
