using UnityEngine;

namespace BulletHell.Arena
{
    /// <summary>Overlap tests between a trap's area and a circle (the player's hitbox). Plain math.</summary>
    public static class TrapShape
    {
        public static bool CircleOverlapsCircle(Vector2 center, float radius, Vector2 point, float pointRadius)
        {
            float reach = radius + pointRadius;
            return (point - center).sqrMagnitude <= reach * reach;
        }

        /// <summary>A box (full size, rotated by angleDegrees counter-clockwise) against a circle.</summary>
        public static bool CircleOverlapsBox(Vector2 center, Vector2 size, float angleDegrees, Vector2 point, float pointRadius)
        {
            float radians = -angleDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians), sin = Mathf.Sin(radians);
            Vector2 offset = point - center;
            var local = new Vector2(offset.x * cos - offset.y * sin, offset.x * sin + offset.y * cos);

            Vector2 half = size * 0.5f;
            float dx = Mathf.Max(Mathf.Abs(local.x) - half.x, 0f);
            float dy = Mathf.Max(Mathf.Abs(local.y) - half.y, 0f);
            return dx * dx + dy * dy <= pointRadius * pointRadius;
        }
    }
}
