using BulletHell.Arena;
using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Player;
using BulletHell.Projectiles;
using UnityEngine;

namespace BulletHell.AI
{
    /// <summary>One behaviour of an enemy (chase, skirmish, sentry...). Instances are reused between spawns.</summary>
    public interface IEnemyBehavior
    {
        void Begin(EnemyAgent agent);
        void Tick(EnemyAgent agent, float dt);
        /// <summary>The enemy died or was recycled: undo anything left running (event hooks, hold fire, tint).</summary>
        void End(EnemyAgent agent);
    }

    /// <summary>
    /// What a behaviour works with: where the enemy and the player are, how to steer along the ground plane, line of
    /// sight, contact hits. Everything is on the flat XY plane: positions are the FEET, the player's is its feet /
    /// shadow (which stays on the ground while jumping), movement uses the footprint grid and sorting stays by feet.
    /// </summary>
    public sealed class EnemyAgent
    {
        public Enemy Enemy;
        public EnemyData Data;
        public NavigationService Nav;
        public ArenaController Arena;
        public PlayerHealth Player;
        public EnemyAttacker Attacker;
        public ProjectilePool Pool;
        public RoundDifficulty Difficulty;
        public WarningLine Line;
        public Transform Transform;
        public Vector2 Velocity;
        /// <summary>Last line-of-sight answer, for the debug view.</summary>
        public bool LastLineClear;

        public EnemyAiTuning Tuning => Nav.Tuning;
        public Vector2 Position => Transform.position;
        public float Radius => Enemy.FootprintRadius;
        public float MaxSpeed => Data.MoveSpeed * Difficulty.MoveSpeedMultiplier;
        public Vector2 PlayerFeet => Player.FeetPosition;
        public float DistanceToPlayer => Vector2.Distance(Position, PlayerFeet);
        public Vector2 DirectionToPlayer
        {
            get
            {
                Vector2 to = PlayerFeet - Position;
                return to.sqrMagnitude > 1e-6f ? to.normalized : Vector2.zero;
            }
        }

        public bool HasGrid => Arena != null && Arena.IsBuilt;

        /// <summary>The way to walk towards the player (straight when clear, else around obstacles via the flow field).</summary>
        public Vector2 PathDirection() => Nav.Direction(Position, Radius);

        /// <summary>
        /// Clear line for bullets from this enemy's body to the player's damage core: the same test bullets fly by, so
        /// a ranged enemy only fires when its shots can actually arrive.
        /// </summary>
        public bool HasLineToPlayer()
        {
            bool clear = !HasGrid || !Arena.SegmentBlocked(Enemy.BodyCenter, Player.Position, Tuning.LosRadius, out _);
            LastLineClear = clear;
            return clear;
        }

        /// <summary>True when a walking circle would be blocked this far along a direction.</summary>
        public bool Blocked(Vector2 direction, float distance) =>
            HasGrid && Arena.Grid.CircleBlocked(Position + direction * distance, Radius);

        /// <summary>
        /// The preferred direction if nothing blocks it, else the nearest turn of 45 or 90 degrees either way that is
        /// free (so a retreating enemy slips along a wall instead of giving up). Zero when boxed in.
        /// </summary>
        public Vector2 FreeDirection(Vector2 preferred)
        {
            if (preferred == Vector2.zero)
                return Vector2.zero;
            float probe = ProbeDistance;
            if (!Blocked(preferred, probe))
                return preferred;
            for (int turn = 1; turn <= 2; turn++)
            {
                Vector2 left = Steering.Rotate(preferred, 45f * turn);
                if (!Blocked(left, probe))
                    return left;
                Vector2 right = Steering.Rotate(preferred, -45f * turn);
                if (!Blocked(right, probe))
                    return right;
            }
            return Vector2.zero;
        }

        /// <summary>Distance to probe ahead for a wall or obstacle while strafing or backing off.</summary>
        public float ProbeDistance => Radius + Tuning.NavRadius;

        /// <summary>
        /// Steers towards a wanted direction (length 0..1 = throttle; zero brakes to a stop) with acceleration and turn
        /// limits, pushed apart from other enemies, and moves along the ground grid (sliding along obstacles).
        /// </summary>
        public void Move(Vector2 desired, float dt, float speedScale = 1f)
        {
            EnemyAiTuning tuning = Tuning;
            Vector2 push = Nav.Separation(Enemy) * tuning.SeparationWeight;
            Vector2 want = desired + push;

            Velocity = Steering.Steer(Velocity, want, MaxSpeed * speedScale, Data.Acceleration, Data.Brake,
                                      Data.TurnRate, tuning.TurnSlowdown, tuning.PivotSpeed, dt);

            Vector2 intended = Velocity * dt;
            Vector2 moved = Displace(intended);
            // Blocked by something: what the enemy really did is its new velocity (it slides, it doesn't push into walls).
            if (dt > 0f && moved.sqrMagnitude < intended.sqrMagnitude * 0.81f)
                Velocity = moved / dt;
        }

        /// <summary>Moves the feet by a step along the ground grid. Returns the distance actually travelled as a vector.</summary>
        public Vector2 Displace(Vector2 delta)
        {
            Vector2 from = Position;
            Vector2 to = HasGrid ? Arena.Grid.Move(from, delta, Radius) : from + delta;
            Transform.position = new Vector3(to.x, to.y, Transform.position.z);
            return to - from;
        }

        /// <summary>Touching the player's footprint while the player is on the ground (an airborne player passes over).</summary>
        public bool TouchingPlayer =>
            Player.IsGrounded && Player.IsAlive && DistanceToPlayer < Radius + Player.BodyRadius;

        /// <summary>Contact damage to a grounded player that is touched. Returns true when a hit landed.</summary>
        public bool TryContactHit() => TouchingPlayer && Player.TryHit(Data.ContactDamage);
    }
}
