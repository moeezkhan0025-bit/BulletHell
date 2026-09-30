namespace BulletHell.AI
{
    /// <summary>
    /// Ranged harasser: holds its preferred distance, strafes, backs off when approached and fires its patterns, but
    /// only with a clear line to the player; without one it repositions.
    /// </summary>
    public sealed class SkirmisherBehavior : IEnemyBehavior
    {
        private readonly RangedPositioner positioner = new RangedPositioner();

        public void Begin(EnemyAgent agent) => positioner.Reset(agent);

        public void End(EnemyAgent agent)
        {
            if (agent.Attacker != null)
                agent.Attacker.HoldFire = false;
            agent.Enemy.ClearWindup();
        }

        public void Tick(EnemyAgent agent, float dt)
        {
            bool line = agent.HasLineToPlayer();
            if (agent.Attacker != null)
            {
                agent.Attacker.HoldFire = !line;
                UpdateShotWarning(agent, line);
            }
            agent.Move(positioner.Desired(agent, line, dt), dt);
        }

        // Visual only: pulse during the last moments before a shot. Shot timing is never changed.
        private static void UpdateShotWarning(EnemyAgent agent, bool line)
        {
            float lead = agent.Data.ShotWarningSeconds;
            float left = agent.Attacker.SecondsUntilNextShot;
            if (lead > 0f && line && left > 0f && left <= lead)
                agent.Enemy.SetWindup(1f - left / lead);
            else
                agent.Enemy.ClearWindup();
        }
    }
}
