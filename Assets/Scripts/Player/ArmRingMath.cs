using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>
    /// Pure maths of the arm ring. Angles are compass degrees like ArmSelector: 0 = N (+Y, the back of the ring on
    /// screen), 90 = E, 180 = S (the front), increasing clockwise.
    /// </summary>
    public static class ArmRingMath
    {
        /// <summary>Point on the ellipse at a compass angle, relative to the feet, lifted by verticalOffset.</summary>
        public static Vector2 Position(float compassDegrees, float radiusX, float radiusY, float verticalOffset)
        {
            float radians = compassDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians) * radiusX, Mathf.Cos(radians) * radiusY + verticalOffset);
        }

        /// <summary>True on the upper half of the ring (further up the screen than the feet): drawn behind the body.</summary>
        public static bool IsBack(float compassDegrees, float deadzone) =>
            Mathf.Cos(compassDegrees * Mathf.Deg2Rad) > deadzone;

        /// <summary>0 at the very back of the ring (N), 1 at the very front (S), 0.5 at the sides.</summary>
        public static float Depth01(float compassDegrees) =>
            (1f - Mathf.Cos(compassDegrees * Mathf.Deg2Rad)) * 0.5f;

        /// <summary>Moves the ring angle towards the target by the shortest way. speed 0 (or less) snaps.</summary>
        public static float MoveAngle(float current, float target, float degreesPerSecond, float deltaTime) =>
            degreesPerSecond <= 0f ? target : Mathf.MoveTowardsAngle(current, target, degreesPerSecond * deltaTime);
    }
}
