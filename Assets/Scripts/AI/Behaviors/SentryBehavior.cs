using BulletHell.Feedback;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.AI
{
    /// <summary>
    /// Moves to a firing spot (a clear line, inside its range band), plants, streams bullets until the shared
    /// HeatComponent overheats, then stops firing and crawls while it cools: that is its vulnerable window.
    /// </summary>
    public sealed class SentryBehavior : IEnemyBehavior
    {
        private static readonly Color OverheatTint = new Color(0.55f, 0.6f, 0.75f);

        private HeatComponent heat = new HeatComponent();
        private HeatSettings settings;
        private EnemyAgent hooked;
        private bool tinted;

        public float Heat01 => heat.Heat01;
        public bool IsOverheated => heat.IsOverheated;

        public void Begin(EnemyAgent agent)
        {
            heat = new HeatComponent();
            settings = new HeatSettings(agent.Data.HeatPerShot, 0f, agent.Data.CoolPerSecond, agent.Data.RestartHeat);
            if (agent.Attacker != null)
            {
                agent.Attacker.Fired += OnFired;
                hooked = agent;
            }
            tinted = false;
        }

        public void End(EnemyAgent agent)
        {
            if (hooked != null && hooked.Attacker != null)
            {
                hooked.Attacker.Fired -= OnFired;
                hooked.Attacker.HoldFire = false;
            }
            hooked = null;
            if (tinted)
                agent.Enemy.ClearStatusTint();
            tinted = false;
        }

        private void OnFired() => heat.AddShot(settings);

        public void Tick(EnemyAgent agent, float dt)
        {
            bool line = agent.HasLineToPlayer();
            float distance = agent.DistanceToPlayer;
            Vector2 range = agent.Data.SentryRange;
            bool inSpot = line && distance >= range.x && distance <= range.y;

            Vector2 desired = Vector2.zero;
            if (!line || distance > range.y)
                desired = agent.PathDirection();
            else if (distance < range.x)
                desired = agent.FreeDirection(-agent.DirectionToPlayer);

            bool planted = inSpot && agent.Velocity.magnitude < agent.Tuning.PlantSpeed;
            bool canFire = planted && !heat.IsOverheated;
            if (agent.Attacker != null)
                agent.Attacker.HoldFire = !canFire;
            heat.Tick(dt, canFire, settings);

            float speedScale = heat.IsOverheated ? agent.Data.OverheatedSpeedFraction : 1f;
            agent.Move(desired, dt, speedScale);

            if (heat.IsOverheated != tinted)
            {
                tinted = heat.IsOverheated;
                if (tinted)
                {
                    agent.Enemy.SetStatusTint(OverheatTint);
                    FeedbackHub.Play(VfxKind.Steam, agent.Enemy.BodyCenter, 8);
                }
                else
                    agent.Enemy.ClearStatusTint();
            }
        }
    }
}
