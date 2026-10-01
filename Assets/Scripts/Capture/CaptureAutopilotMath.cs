using UnityEngine;

namespace BulletHell.Capture
{
    /// <summary>
    /// R1 portfolio capture kit - the pure maths of the capture autopilot (no scene access, no allocation), so the EditMode
    /// tests can check it: bullet threat prediction, dodge direction, distance keeping, wall push, target switching,
    /// aim leading and a small reaction-delay line.
    /// </summary>
    public static class CaptureAutopilotMath
    {
        /// <summary>
        /// Predicts a straight-flying bullet against the player. Returns true when its path passes within safeRadius of the
        /// player inside the horizon. urgency is 0..1 (1 = about to hit), dodge is a unit vector pointing away from the path.
        /// tieBreak (+1 / -1) picks the side when the bullet comes straight at the player.
        /// </summary>
        public static bool BulletThreat(Vector2 bulletPosition, Vector2 bulletVelocity, Vector2 player, float safeRadius, float horizon,
                                        float tieBreak, out float urgency, out Vector2 dodge)
        {
            urgency = 0f;
            dodge = Vector2.zero;
            Vector2 rel = bulletPosition - player;
            float speedSqr = bulletVelocity.sqrMagnitude;
            if (speedSqr < 1e-6f)
                return false;

            float t = Mathf.Clamp(-Vector2.Dot(rel, bulletVelocity) / speedSqr, 0f, horizon);
            Vector2 closest = rel + bulletVelocity * t;   // where the bullet is, relative to the player, at the closest approach
            float miss = closest.magnitude;
            if (miss >= safeRadius)
                return false;

            Vector2 path = bulletVelocity * (1f / Mathf.Sqrt(speedSqr));
            Vector2 side = new Vector2(-path.y, path.x);
            float along = Vector2.Dot(closest, side);
            float sign = Mathf.Abs(along) < 0.02f ? (tieBreak >= 0f ? -1f : 1f) : (along > 0f ? -1f : 1f);
            dodge = side * sign;

            float timeFactor = 1f - t / Mathf.Max(horizon, 0.0001f);
            float depth = 1f - miss / Mathf.Max(safeRadius, 0.0001f);
            urgency = Mathf.Clamp01(timeFactor) * (0.4f + 0.6f * Mathf.Clamp01(depth));
            return true;
        }

        /// <summary>Move vector that holds a preferred distance: towards the enemy when too far, away when too near. Length 0..1.</summary>
        public static Vector2 KeepDistance(Vector2 player, Vector2 enemy, float preferred)
        {
            Vector2 to = enemy - player;
            float distance = to.magnitude;
            if (distance < 0.0001f)
                return Vector2.zero;
            float band = Mathf.Max(preferred * 0.5f, 0.1f);
            float error = Mathf.Clamp((distance - preferred) / band, -1f, 1f);
            return to / distance * error;
        }

        /// <summary>Perpendicular to the direction to the enemy (circling it). sign +1 = clockwise seen from above, -1 = anticlockwise.</summary>
        public static Vector2 Strafe(Vector2 player, Vector2 enemy, float sign)
        {
            Vector2 to = enemy - player;
            if (to.sqrMagnitude < 0.0001f)
                return Vector2.zero;
            to.Normalize();
            return new Vector2(to.y, -to.x) * (sign >= 0f ? 1f : -1f);
        }

        /// <summary>Push away from the bounds, growing from 0 at the margin to 1 at the wall. Zero in the open.</summary>
        public static Vector2 WallPush(Vector2 position, Rect bounds, float margin)
        {
            margin = Mathf.Max(margin, 0.01f);
            float x = Mathf.Clamp01((bounds.xMin + margin - position.x) / margin) - Mathf.Clamp01((position.x - (bounds.xMax - margin)) / margin);
            float y = Mathf.Clamp01((bounds.yMin + margin - position.y) / margin) - Mathf.Clamp01((position.y - (bounds.yMax - margin)) / margin);
            return new Vector2(x, y);
        }

        /// <summary>Whether to give up the current target for a candidate: only when it is clearly nearer (bias = required fraction).</summary>
        public static bool ShouldSwitchTarget(float currentDistance, float candidateDistance, float bias)
        {
            if (currentDistance < 0f)
                return candidateDistance >= 0f;   // no current target
            return candidateDistance >= 0f && candidateDistance < currentDistance * (1f - Mathf.Clamp01(bias));
        }

        /// <summary>The point to aim at so a bullet of the given speed meets a target moving with targetVelocity (first-order lead).</summary>
        public static Vector2 LeadPoint(Vector2 origin, Vector2 targetPosition, Vector2 targetVelocity, float projectileSpeed)
        {
            if (projectileSpeed < 0.01f)
                return targetPosition;
            float flight = Vector2.Distance(origin, targetPosition) / projectileSpeed;
            return targetPosition + targetVelocity * Mathf.Min(flight, 0.8f);
        }

        /// <summary>Unit stick vector for a compass angle (0 = up, clockwise), the same convention as ArmSelector.</summary>
        public static Vector2 StickFromCompass(float compassDegrees)
        {
            float radians = compassDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }

        /// <summary>Smallest absolute angle between two compass angles, 0..180.</summary>
        public static float AngleBetween(float a, float b) => Mathf.Abs(Mathf.DeltaAngle(a, b));

        /// <summary>Whether a jump started now lands the player mid-air when the smash lands in remaining seconds.</summary>
        public static bool ShouldJumpForSmash(float remainingSeconds, float lead, bool insideRing)
        {
            return insideRing && remainingSeconds > 0f && remainingSeconds <= lead;
        }

        /// <summary>
        /// A fixed-size history of values by time. Sample(t) returns the newest value pushed at or before t, so reading it with
        /// (now - delay) gives a reaction delay without allocating.
        /// </summary>
        public sealed class DelayLine
        {
            private readonly float[] times;
            private readonly Vector2[] values;
            private int head;    // next write
            private int count;

            public DelayLine(int capacity)
            {
                times = new float[Mathf.Max(2, capacity)];
                values = new Vector2[times.Length];
            }

            public int Count => count;

            public void Clear()
            {
                head = 0;
                count = 0;
            }

            public void Push(float time, Vector2 value)
            {
                times[head] = time;
                values[head] = value;
                head = (head + 1) % times.Length;
                if (count < times.Length)
                    count++;
            }

            public Vector2 Sample(float time)
            {
                if (count == 0)
                    return Vector2.zero;
                int length = times.Length;
                int index = (head - 1 + length) % length;
                for (int i = 0; i < count; i++)
                {
                    if (times[index] <= time)
                        return values[index];
                    index = (index - 1 + length) % length;
                }
                // Nothing that old yet: the oldest value (the autopilot just started).
                return values[(head - count + length) % length];
            }
        }
    }
}
