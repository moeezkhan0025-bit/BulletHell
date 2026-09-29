using UnityEngine;

namespace BulletHell.Arena
{
    /// <summary>
    /// The arena's collision picture: a uniform grid over the playable rectangle where every cell is free or owned by an
    /// obstacle. Plain math, no engine objects, no allocations in the queries, so hostile bullets, player and enemy
    /// movement and spawn placement can all ask "is this blocked?" cheaply (and M8's flow field can be built from it).
    /// Anything outside the rectangle counts as blocked (the walls), with owner <see cref="Border"/>.
    /// </summary>
    public sealed class ArenaGrid
    {
        public const int Free = -1;
        /// <summary>Owner reported for the arena walls (outside the playable rectangle).</summary>
        public const int Border = -2;

        private readonly int[] cells;
        private readonly int columns;
        private readonly int rows;

        public Rect Bounds { get; }
        public float CellSize { get; }

        public ArenaGrid(Rect bounds, float cellSize)
        {
            Bounds = bounds;
            CellSize = Mathf.Max(0.05f, cellSize);
            columns = Mathf.Max(1, Mathf.CeilToInt(bounds.width / CellSize));
            rows = Mathf.Max(1, Mathf.CeilToInt(bounds.height / CellSize));
            cells = new int[columns * rows];
            Clear();
        }

        public int Columns => columns;
        public int Rows => rows;

        /// <summary>Owner of a cell: an obstacle id, or Free. Cells outside the grid are Border.</summary>
        public int OwnerAt(int column, int row) =>
            column < 0 || row < 0 || column >= columns || row >= rows ? Border : cells[row * columns + column];

        public void Clear()
        {
            for (int i = 0; i < cells.Length; i++)
                cells[i] = Free;
        }

        /// <summary>Marks every cell an axis-aligned box touches as owned. Later owners overwrite earlier ones.</summary>
        public void AddBox(Vector2 center, Vector2 size, int owner)
        {
            Vector2 half = size * 0.5f;
            ForCells(center - half, center + half, (c, r) =>
            {
                Rect cell = CellRect(c, r);
                return cell.xMax > center.x - half.x && cell.xMin < center.x + half.x &&
                       cell.yMax > center.y - half.y && cell.yMin < center.y + half.y;
            }, owner);
        }

        public void AddCircle(Vector2 center, float radius, int owner)
        {
            var extent = new Vector2(radius, radius);
            ForCells(center - extent, center + extent, (c, r) => DistanceSqrToCell(center, c, r) < radius * radius, owner);
        }

        /// <summary>Marks every cell an axis-aligned ellipse (the flat footprint of a pillar or crate) touches as owned.</summary>
        public void AddEllipse(Vector2 center, Vector2 radii, int owner)
        {
            radii = new Vector2(Mathf.Max(0.001f, radii.x), Mathf.Max(0.001f, radii.y));
            ForCells(center - radii, center + radii, (c, r) =>
            {
                float minX = Bounds.xMin + c * CellSize, minY = Bounds.yMin + r * CellSize;
                float dx = Mathf.Max(minX - center.x, 0f, center.x - (minX + CellSize)) / radii.x;
                float dy = Mathf.Max(minY - center.y, 0f, center.y - (minY + CellSize)) / radii.y;
                return dx * dx + dy * dy < 1f;
            }, owner);
        }

        /// <summary>
        /// Marks the area an ellipse covers while sliding up by reach (the footprint plus the body above it that bullets
        /// can hit): the ellipse is stamped along the way in steps of half a cell.
        /// </summary>
        public void AddEllipseReachingUp(Vector2 center, Vector2 radii, float reach, int owner)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(reach / (CellSize * 0.5f)));
            for (int i = 0; i <= steps; i++)
                AddEllipse(center + new Vector2(0f, reach * i / steps), radii, owner);
        }

        /// <summary>Frees every cell owned by an obstacle (it broke).</summary>
        public void ClearOwner(int owner)
        {
            for (int i = 0; i < cells.Length; i++)
                if (cells[i] == owner)
                    cells[i] = Free;
        }

        public bool CircleBlocked(Vector2 position, float radius) => CircleBlocked(position, radius, out _);

        /// <summary>True when a circle overlaps a wall or an owned cell; owner is the obstacle id (or Border).</summary>
        public bool CircleBlocked(Vector2 position, float radius, out int owner)
        {
            owner = Free;
            if (position.x - radius < Bounds.xMin || position.x + radius > Bounds.xMax ||
                position.y - radius < Bounds.yMin || position.y + radius > Bounds.yMax)
            {
                owner = Border;
                return true;
            }

            ToCell(position.x - radius, position.y - radius, out int minC, out int minR);
            ToCell(position.x + radius, position.y + radius, out int maxC, out int maxR);
            float radiusSqr = radius * radius;
            for (int r = minR; r <= maxR; r++)
            {
                for (int c = minC; c <= maxC; c++)
                {
                    int cellOwner = cells[r * columns + c];
                    if (cellOwner != Free && DistanceSqrToCell(position, c, r) < radiusSqr)
                    {
                        owner = cellOwner;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>Sweeps a circle from a to b in half-cell steps. Returns true at the first blocked step, with its owner.</summary>
        public bool SegmentBlocked(Vector2 a, Vector2 b, float radius, out int owner)
        {
            float length = Vector2.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(length / (CellSize * 0.5f)));
            for (int i = 0; i <= steps; i++)
            {
                if (CircleBlocked(Vector2.Lerp(a, b, i / (float)steps), radius, out owner))
                    return true;
            }
            owner = Free;
            return false;
        }

        /// <summary>
        /// Moves a circle by delta and slides along whatever it meets: X first, then Y, each as far as it can go
        /// (a short bisection, so the circle rests against the surface instead of stopping a step short).
        /// A circle that starts blocked is first pushed to the nearest free spot.
        /// </summary>
        public Vector2 Move(Vector2 from, Vector2 delta, float radius)
        {
            if (CircleBlocked(from, radius))
                from = NearestFree(from, radius);

            Vector2 position = Advance(from, new Vector2(delta.x, 0f), radius);
            return Advance(position, new Vector2(0f, delta.y), radius);
        }

        private Vector2 Advance(Vector2 from, Vector2 delta, float radius)
        {
            if (delta == Vector2.zero)
                return from;
            // Sweep in half-cell steps so a big step can never jump over a thin wall, then refine the last gap.
            int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / (CellSize * 0.5f)));
            float low = 0f;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                if (!CircleBlocked(from + delta * t, radius))
                {
                    low = t;
                    continue;
                }

                float high = t;
                for (int refine = 0; refine < 8; refine++)
                {
                    float mid = (low + high) * 0.5f;
                    if (CircleBlocked(from + delta * mid, radius))
                        high = mid;
                    else
                        low = mid;
                }
                break;
            }
            return from + delta * low;
        }

        /// <summary>The closest spot to a position where a circle fits (searches in growing rings). Returns the position itself when it is free.</summary>
        public Vector2 NearestFree(Vector2 position, float radius)
        {
            if (!CircleBlocked(position, radius))
                return position;

            float maxDistance = Mathf.Max(Bounds.width, Bounds.height);
            for (float distance = CellSize; distance <= maxDistance; distance += CellSize)
            {
                int samples = Mathf.Max(8, Mathf.CeilToInt(2f * Mathf.PI * distance / CellSize));
                for (int i = 0; i < samples; i++)
                {
                    float angle = i * (2f * Mathf.PI / samples);
                    var candidate = new Vector2(position.x + Mathf.Cos(angle) * distance, position.y + Mathf.Sin(angle) * distance);
                    if (!CircleBlocked(candidate, radius))
                        return candidate;
                }
            }
            return position;
        }

        // ---- helpers

        private void ToCell(float x, float y, out int column, out int row)
        {
            column = Mathf.Clamp(Mathf.FloorToInt((x - Bounds.xMin) / CellSize), 0, columns - 1);
            row = Mathf.Clamp(Mathf.FloorToInt((y - Bounds.yMin) / CellSize), 0, rows - 1);
        }

        private Rect CellRect(int column, int row) =>
            new Rect(Bounds.xMin + column * CellSize, Bounds.yMin + row * CellSize, CellSize, CellSize);

        private float DistanceSqrToCell(Vector2 point, int column, int row)
        {
            float minX = Bounds.xMin + column * CellSize, minY = Bounds.yMin + row * CellSize;
            float dx = Mathf.Max(minX - point.x, 0f, point.x - (minX + CellSize));
            float dy = Mathf.Max(minY - point.y, 0f, point.y - (minY + CellSize));
            return dx * dx + dy * dy;
        }

        private void ForCells(Vector2 min, Vector2 max, System.Func<int, int, bool> covers, int owner)
        {
            ToCell(min.x, min.y, out int minC, out int minR);
            ToCell(max.x, max.y, out int maxC, out int maxR);
            for (int r = minR; r <= maxR; r++)
                for (int c = minC; c <= maxC; c++)
                    if (covers(c, r))
                        cells[r * columns + c] = owner;
        }
    }
}
