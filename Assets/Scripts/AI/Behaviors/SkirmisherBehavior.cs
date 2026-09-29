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
        }

        public void Tick(EnemyAgent agent, float dt)
        {
            bool line = agent.HasLineToPlayer();
            if (agent.Attacker != null)
                agent.Attacker.HoldFire = !line;
            agent.Move(positioner.Desired(agent, line, dt), dt);
        }
    }
}
