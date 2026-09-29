using System;
using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>Finds the spot a landing player is moved to when the landing spot is taken.</summary>
    public static class LandingResolver
    {
        /// <summary>
        /// The position itself when it is free, else the closest free spot, searched in growing rings.
        /// Returns the position unchanged when nothing within maxDistance is free.
        /// </summary>
        public static Vector2 Resolve(Vector2 position, Func<Vector2, bool> isBlocked, float step, float maxDistance)
        {
            if (!isBlocked(position))
                return position;

            step = Mathf.Max(0.01f, step);
            for (float distance = step; distance <= maxDistance; distance += step)
            {
                int samples = Mathf.Max(8, Mathf.CeilToInt(2f * Mathf.PI * distance / step));
                for (int i = 0; i < samples; i++)
                {
                    float angle = i * (2f * Mathf.PI / samples);
                    var candidate = new Vector2(position.x + Mathf.Cos(angle) * distance, position.y + Mathf.Sin(angle) * distance);
                    if (!isBlocked(candidate))
                        return candidate;
                }
            }
            return position;
        }
    }
}
