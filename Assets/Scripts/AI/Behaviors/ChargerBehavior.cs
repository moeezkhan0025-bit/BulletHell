using UnityEngine;

namespace BulletHell.AI
{
    /// <summary>
    /// Approaches along the flow field until it has a clear straight run at the player, stops and telegraphs (warning
    /// line, pulsing tint), then dashes along the locked line. The dash hurts only a grounded player: jump over it.
    /// It stops at the first wall or obstacle, then stays put and vulnerable while it recovers.
    /// </summary>
    public sealed class ChargerBehavior : IEnemyBehavior
    {
        private enum State { Approach, Telegraph, Dash, Recover }

        private State state;
        private float timer;
        private float cooldown;
        private float travelled;
        private Vector2 dashDirection;

        public void Begin(EnemyAgent agent)
        {
            state = State.Approach;
            cooldown = agent.Data.ChargeCooldown * 0.5f;
        }

        public void End(EnemyAgent agent)
        {
            agent.Line?.Hide();
            agent.Enemy.ClearTint();
        }

        public void Tick(EnemyAgent agent, float dt)
        {
            switch (state)
            {
                case State.Approach: Approach(agent, dt); break;
                case State.Telegraph: Telegraph(agent, dt); break;
                case State.Dash: Dash(agent, dt); break;
                default: Recover(agent, dt); break;
            }
        }

        private void Approach(EnemyAgent agent, float dt)
        {
            cooldown -= dt;
            float distance = agent.DistanceToPlayer;

            // Ready to charge but too close for a run-up: back off first (or, cornered, just keep coming).
            Vector2 desired = agent.PathDirection();
            if (cooldown <= 0f && distance < agent.Data.ChargeMinRange)
            {
                Vector2 back = agent.FreeDirection(-agent.DirectionToPlayer);
                if (back != Vector2.zero)
                    desired = back;
            }
            agent.Move(desired, dt);
            agent.TryContactHit();

            bool clearRun = agent.HasGrid
                ? !agent.Arena.Grid.SegmentBlocked(agent.Position, agent.PlayerFeet, agent.Radius, out _)
                : true;
            if (cooldown <= 0f && clearRun && distance <= agent.Data.ChargeTriggerRange && distance >= agent.Data.ChargeMinRange)
            {
                state = State.Telegraph;
                timer = agent.Data.TelegraphSeconds;
                dashDirection = agent.DirectionToPlayer;
            }
        }

        private void Telegraph(EnemyAgent agent, float dt)
        {
            agent.Move(Vector2.zero, dt);
            timer -= dt;

            // The line follows the player until shortly before the dash, then commits.
            if (timer > agent.Data.TelegraphLockSeconds && agent.DirectionToPlayer != Vector2.zero)
                dashDirection = agent.DirectionToPlayer;

            float length = agent.HasGrid
                ? agent.Arena.Grid.RayDistance(agent.Position, dashDirection, agent.Data.DashDistance, agent.Radius)
                : agent.Data.DashDistance;
            EnemyAiTuning tuning = agent.Tuning;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 24f);
            Color line = tuning.WarningColor;
            line.a = Mathf.Lerp(0.35f, tuning.WarningColor.a, pulse);
            agent.Line?.Show(agent.Position, agent.Position + dashDirection * length, line, tuning.WarningLineWidth);
            agent.Enemy.SetTint(Color.Lerp(agent.Data.Color, Color.white, pulse * 0.6f));

            if (timer <= 0f)
            {
                agent.Line?.Hide();
                agent.Enemy.ClearTint();
                agent.Velocity = dashDirection * agent.Data.DashSpeed * agent.Difficulty.MoveSpeedMultiplier;
                travelled = 0f;
                state = State.Dash;
            }
        }

        private void Dash(EnemyAgent agent, float dt)
        {
            float speed = agent.Data.DashSpeed * agent.Difficulty.MoveSpeedMultiplier;
            float step = speed * dt;
            Vector2 moved = agent.Displace(dashDirection * step);
            travelled += moved.magnitude;
            agent.Velocity = dashDirection * speed;

            bool hit = agent.TryContactHit();
            bool blocked = moved.magnitude < step * 0.5f;
            if (hit || blocked || travelled >= agent.Data.DashDistance)
            {
                state = State.Recover;
                timer = agent.Data.RecoverSeconds;
                cooldown = agent.Data.ChargeCooldown;
            }
        }

        private void Recover(EnemyAgent agent, float dt)
        {
            agent.Move(Vector2.zero, dt);
            timer -= dt;
            if (timer <= 0f)
                state = State.Approach;
        }
    }
}
