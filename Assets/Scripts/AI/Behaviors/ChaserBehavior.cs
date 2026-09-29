using UnityEngine;

namespace BulletHell.AI
{
    /// <summary>
    /// Pursues the player's feet (their shadow while jumping) along the flow field and hurts on contact. It stops just
    /// short of the player's footprint instead of piling on top of it.
    /// </summary>
    public sealed class ChaserBehavior : IEnemyBehavior
    {
        public void Begin(EnemyAgent agent) { }

        public void End(EnemyAgent agent) { }

        public void Tick(EnemyAgent agent, float dt)
        {
            // Ease off as it closes in, so it settles against the player's footprint instead of jittering on it.
            float contact = agent.Radius + agent.Player.BodyRadius;
            float throttle = Mathf.Clamp01((agent.DistanceToPlayer - contact * 0.7f) / (contact * 0.6f));
            agent.Move(agent.PathDirection() * throttle, dt);
            agent.TryContactHit();
        }
    }
}
