using UnityEngine;

namespace BulletHell.AI
{
    /// <summary>
    /// Grounded steering: an enemy has a velocity with a heading it can only turn at a limited rate, accelerates up to
    /// its top speed, brakes when it should stop, and slows down while it is still turning towards where it wants to go.
    /// Pure maths, so it is unit-testable.
    /// </summary>
    public static class Steering
    {
        /// <summary>
        /// One steering step. desired is the wanted direction; its length (0..1) is the throttle, zero means "stop".
        /// </summary>
        /// <param name="turnRateDegrees">Most the heading can rotate per second.</param>
        /// <param name="turnSlowdown">Fraction of speed kept when the wanted direction is 90 degrees off the heading.</param>
        /// <param name="pivotSpeed">Below this speed the heading snaps to the wanted direction (standing still).</param>
        public static Vector2 Steer(Vector2 velocity, Vector2 desired, float maxSpeed, float acceleration, float brake,
                                    float turnRateDegrees, float turnSlowdown, float pivotSpeed, float dt)
        {
            float speed = velocity.magnitude;
            float throttle = Mathf.Clamp01(desired.magnitude);

            if (throttle < 0.001f)
            {
                if (speed < 0.0001f)
                    return Vector2.zero;
                float braked = Mathf.MoveTowards(speed, 0f, brake * dt);
                return velocity / speed * braked;
            }

            Vector2 wanted = desired / desired.magnitude;
            Vector2 heading = speed > pivotSpeed ? velocity / speed : wanted;

            float angle = Vector2.SignedAngle(heading, wanted);
            float maxTurn = turnRateDegrees * dt;
            float turned = Mathf.Clamp(angle, -maxTurn, maxTurn);
            heading = Rotate(heading, turned);

            // How far off the wanted direction the enemy still faces: 0 = aligned, 1 = 90 degrees or more away.
            float misaligned = Mathf.Clamp01(Mathf.Abs(angle - turned) / 90f);
            float targetSpeed = maxSpeed * throttle * Mathf.Lerp(1f, turnSlowdown, misaligned);

            float rate = speed < targetSpeed ? acceleration : brake;
            speed = Mathf.MoveTowards(speed, targetSpeed, rate * dt);
            return heading * speed;
        }

        public static Vector2 Rotate(Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians), sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        /// <summary>Push away from neighbours: each one within reach adds a push that grows as they overlap more.</summary>
        public static Vector2 PushAway(Vector2 self, Vector2 other, float reach)
        {
            Vector2 away = self - other;
            float distance = away.magnitude;
            if (distance >= reach)
                return Vector2.zero;
            if (distance < 0.0001f)
                return Vector2.right * 1f;   // exactly stacked: any direction will do, just get apart
            return away / distance * (1f - distance / reach);
        }
    }
}
