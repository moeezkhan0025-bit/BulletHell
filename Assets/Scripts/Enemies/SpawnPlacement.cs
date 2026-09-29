using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>Where a spawned enemy goes. Plain math so it can be tested; the arena is the playable rectangle inset by the edge margin.</summary>
    public static class SpawnPlacement
    {
        public static Vector2 Position(SpawnPattern pattern, int index, int count, Rect arena, Vector2 playerPosition,
                                       float minDistanceFromPlayer, float ringRadius, float rowWidthFraction)
        {
            switch (pattern)
            {
                case SpawnPattern.Row:
                    return Row(index, count, arena, rowWidthFraction);
                case SpawnPattern.Ring:
                    return Ring(index, count, arena, ringRadius);
                default:
                    return Scatter(arena, playerPosition, minDistanceFromPlayer);
            }
        }

        /// <summary>The gate for the nth spawn (round-robin), nudged by up to jitter so enemies don't stack exactly.</summary>
        public static Vector2 GatePosition(int counter, System.Collections.Generic.IReadOnlyList<Vector2> gates, float jitter)
        {
            Vector2 gate = gates[((counter % gates.Count) + gates.Count) % gates.Count];
            return jitter > 0f ? gate + Random.insideUnitCircle * jitter : gate;
        }

        private static Vector2 Row(int index, int count, Rect arena, float widthFraction)
        {
            float t = count <= 1 ? 0.5f : index / (count - 1f);
            float halfWidth = arena.width * 0.5f * widthFraction;
            float x = arena.center.x + Mathf.Lerp(-halfWidth, halfWidth, t);
            return new Vector2(x, arena.yMax);
        }

        private static Vector2 Ring(int index, int count, Rect arena, float radius)
        {
            float angle = (90f + 360f * index / Mathf.Max(1, count)) * Mathf.Deg2Rad;
            Vector2 point = arena.center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            return Clamp(point, arena);
        }

        private static Vector2 Scatter(Rect arena, Vector2 player, float minDistance)
        {
            for (int attempt = 0; attempt < 16; attempt++)
            {
                var point = new Vector2(Random.Range(arena.xMin, arena.xMax), Random.Range(arena.yMin, arena.yMax));
                if ((point - player).sqrMagnitude >= minDistance * minDistance)
                    return point;
            }

            // The player is in the way everywhere we tried: use the arena corner farthest from them.
            Vector2 best = new Vector2(arena.xMin, arena.yMax);
            float bestSqr = -1f;
            foreach (Vector2 corner in new[] { new Vector2(arena.xMin, arena.yMin), new Vector2(arena.xMin, arena.yMax),
                                               new Vector2(arena.xMax, arena.yMin), new Vector2(arena.xMax, arena.yMax) })
            {
                float sqr = (corner - player).sqrMagnitude;
                if (sqr > bestSqr)
                {
                    bestSqr = sqr;
                    best = corner;
                }
            }
            return best;
        }

        private static Vector2 Clamp(Vector2 point, Rect arena) =>
            new Vector2(Mathf.Clamp(point.x, arena.xMin, arena.xMax), Mathf.Clamp(point.y, arena.yMin, arena.yMax));
    }
}
