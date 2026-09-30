using UnityEngine;

namespace BulletHell.Projectiles
{
    /// <summary>What a bullet does after touching something.</summary>
    public enum HitOutcome
    {
        /// <summary>Keeps flying in the same direction (it pierced an enemy).</summary>
        Continue,
        /// <summary>Was reflected off a wall or obstacle and keeps flying.</summary>
        Bounce,
        /// <summary>Stops here (goes back to the pool).</summary>
        Stop,
    }

    /// <summary>The counters that decide what a bullet survives. Copied from ShotProperties when the bullet launches.</summary>
    public struct ShotState
    {
        public int PierceLeft;
        public int BouncesLeft;
        /// <summary>Set after every pierce and bounce: a homing bullet must pick a new target.</summary>
        public bool NeedsRetarget;
    }

    /// <summary>
    /// The armament interaction rules as plain functions (Docs/ARMAMENTS.md), so they are testable without physics:
    /// - Pierce is used up before a bullet stops: an enemy hit with pierce left keeps the bullet flying.
    /// - Ricochet counts only wall/obstacle bounces: an enemy hit never spends a bounce.
    /// - Homing re-targets after every bounce or pierce.
    /// </summary>
    public static class ProjectileRules
    {
        /// <summary>A bullet damaged an enemy.</summary>
        public static HitOutcome OnEnemyHit(ref ShotState state)
        {
            if (state.PierceLeft > 0)
            {
                state.PierceLeft--;
                state.NeedsRetarget = true;
                return HitOutcome.Continue;
            }
            return HitOutcome.Stop;
        }

        /// <summary>A bullet touched a wall or obstacle. On a bounce the velocity is reflected about the surface normal.</summary>
        public static HitOutcome OnWallHit(ref ShotState state, Vector2 velocity, Vector2 normal, out Vector2 newVelocity)
        {
            if (state.BouncesLeft > 0)
            {
                state.BouncesLeft--;
                state.NeedsRetarget = true;
                newVelocity = Vector2.Reflect(velocity, normal);
                return HitOutcome.Bounce;
            }
            newVelocity = velocity;
            return HitOutcome.Stop;
        }

        /// <summary>
        /// Whether a target is a valid homing target: ahead of the bullet inside a cone (total width in degrees) and within
        /// range. sqrDistance is the squared distance, for picking the nearest.
        /// </summary>
        public static bool InCone(Vector2 position, Vector2 forward, Vector2 target, float coneDegrees, float range, out float sqrDistance)
        {
            Vector2 to = target - position;
            sqrDistance = to.sqrMagnitude;
            if (sqrDistance > range * range)
                return false;
            if (sqrDistance < 0.0001f)
                return true; // on top of the bullet
            return Vector2.Angle(forward, to) <= coneDegrees * 0.5f;
        }

        /// <summary>Rotates a velocity towards a target position by at most maxDegrees, keeping its speed.</summary>
        public static Vector2 TurnToward(Vector2 velocity, Vector2 position, Vector2 target, float maxDegrees)
        {
            float speed = velocity.magnitude;
            Vector2 to = target - position;
            if (speed < 0.0001f || to.sqrMagnitude < 0.0001f)
                return velocity;
            float current = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            float wanted = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
            float next = Mathf.MoveTowardsAngle(current, wanted, maxDegrees) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(next), Mathf.Sin(next)) * speed;
        }
    }
}
