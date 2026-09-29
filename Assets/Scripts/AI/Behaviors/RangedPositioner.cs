using UnityEngine;

namespace BulletHell.AI
{
    /// <summary>
    /// Where a ranged enemy wants to go: without a clear line, or too far, it advances along the flow field; too close,
    /// it backs off (sliding sideways if something is behind it); in the comfortable band it strafes and flips
    /// direction now and then or when it meets an obstacle. Shared by the Skirmisher and the Sniper.
    /// </summary>
    public sealed class RangedPositioner
    {
        private float strafeSign = 1f;
        private float strafeTimer;

        public void Reset(EnemyAgent agent)
        {
            strafeSign = Random.value < 0.5f ? -1f : 1f;
            RollTimer(agent);
        }

        /// <summary>Wanted direction (length = throttle) given whether there is a clear line to the player.</summary>
        public Vector2 Desired(EnemyAgent agent, bool lineClear, float dt)
        {
            float distance = agent.DistanceToPlayer;
            float preferred = agent.Data.PreferredDistance;
            float tolerance = agent.Data.DistanceTolerance;
            Vector2 toPlayer = agent.DirectionToPlayer;

            strafeTimer -= dt;
            if (strafeTimer <= 0f)
            {
                strafeSign = -strafeSign;
                RollTimer(agent);
            }

            if (!lineClear || distance > preferred + tolerance)
                return agent.PathDirection();

            Vector2 sideways = new Vector2(-toPlayer.y, toPlayer.x);
            if (distance < preferred - tolerance)
            {
                Vector2 back = agent.FreeDirection(-toPlayer);   // slips along a wall when directly blocked
                if (back != Vector2.zero)
                    return back;
                return Strafe(agent, sideways, 1f);   // boxed in: strafe instead of pushing into the wall
            }

            return Strafe(agent, sideways, agent.Data.StrafeSpeedFraction);
        }

        private Vector2 Strafe(EnemyAgent agent, Vector2 sideways, float throttle)
        {
            Vector2 direction = sideways * strafeSign;
            if (agent.Blocked(direction, agent.ProbeDistance))
            {
                strafeSign = -strafeSign;
                direction = -direction;
                RollTimer(agent);
            }
            return direction * throttle;
        }

        private void RollTimer(EnemyAgent agent)
        {
            Vector2 range = agent.Data.StrafeSwitchSeconds;
            strafeTimer = Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
        }
    }
}
