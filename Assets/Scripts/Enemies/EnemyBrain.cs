using BulletHell.AI;
using BulletHell.Arena;
using BulletHell.Core;
using BulletHell.Player;
using BulletHell.Projectiles;
using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>
    /// Runs an enemy's movement AI: picks the behaviour named by its EnemyData (Chaser, Skirmisher, Sentry, Charger,
    /// Sniper) and ticks it every frame. Patrol enemies don't use it (PatrolMover does). Stunned enemies brake to a stop;
    /// nothing moves once the player is dead. All behaviour objects are created once and reused between spawns.
    /// </summary>
    public sealed class EnemyBrain : MonoBehaviour
    {
        private readonly ChaserBehavior chaser = new ChaserBehavior();
        private readonly SkirmisherBehavior skirmisher = new SkirmisherBehavior();
        private readonly SentryBehavior sentry = new SentryBehavior();
        private readonly ChargerBehavior charger = new ChargerBehavior();
        private readonly SniperBehavior sniper = new SniperBehavior();
        private readonly EnemyAgent agent = new EnemyAgent();

        private IEnemyBehavior behavior;
        private StatusEffects status;
        private WarningLine line;

        /// <summary>Movement state for the debug view; null-safe when the enemy patrols.</summary>
        public EnemyAgent Agent => agent;
        public bool IsActive => behavior != null;
        public SentryBehavior Sentry => behavior == sentry ? sentry : null;

        private void Awake() => TryGetComponent(out status);

        private void OnDestroy() => line?.Destroy();

        /// <summary>Starts (or restarts) the AI for a freshly spawned enemy. Patrol enemies switch the brain off.</summary>
        public void Configure(Enemy enemy, EnemyData data, in RoundDifficulty difficulty, ProjectilePool pool,
                              PlayerHealth player, ArenaController arena, NavigationService navigation,
                              EnemyAttacker attacker, Material lineMaterial)
        {
            Stop();

            IEnemyBehavior chosen = Choose(data.Behavior);
            if (chosen != null && (navigation == null || player == null))
            {
                Debug.LogWarning($"{data.DisplayName} needs a NavigationService and a player to move: it stays put.", this);
                chosen = null;
            }
            if (chosen == null)
            {
                enabled = false;
                return;
            }

            if (line == null && lineMaterial != null)
                line = new WarningLine(transform.parent, lineMaterial, GameServices.Ensure().Config.Perspective.BulletVisualLift);

            agent.Enemy = enemy;
            agent.Data = data;
            agent.Nav = navigation;
            agent.Arena = arena;
            agent.Player = player;
            agent.Attacker = attacker;
            agent.Pool = pool;
            agent.Difficulty = difficulty;
            agent.Line = line;
            agent.Transform = transform;
            agent.Velocity = Vector2.zero;

            behavior = chosen;
            behavior.Begin(agent);
            enabled = true;
        }

        /// <summary>The enemy died or is being recycled: stop the behaviour and clean up its telegraphs.</summary>
        public void Stop()
        {
            if (behavior != null)
                behavior.End(agent);
            behavior = null;
            line?.Hide();
            agent.Velocity = Vector2.zero;
            enabled = false;
        }

        private IEnemyBehavior Choose(EnemyBehavior kind)
        {
            switch (kind)
            {
                case EnemyBehavior.Chaser: return chaser;
                case EnemyBehavior.Skirmisher: return skirmisher;
                case EnemyBehavior.Sentry: return sentry;
                case EnemyBehavior.Charger: return charger;
                case EnemyBehavior.Sniper: return sniper;
                default: return null;
            }
        }

        private void Update()
        {
            if (behavior == null || !agent.Enemy.IsAlive)
                return;

            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            if ((status != null && status.IsStunned) || !agent.Player.IsAlive)
            {
                agent.Move(Vector2.zero, dt);
                return;
            }

            behavior.Tick(agent, dt);
        }
    }
}
