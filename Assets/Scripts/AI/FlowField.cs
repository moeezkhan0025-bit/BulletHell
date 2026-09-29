using BulletHell.Arena;
using UnityEngine;

namespace BulletHell.AI
{
    /// <summary>
    /// A grid flow field over the arena: every free cell knows the direction of the cheapest walk to a target (the
    /// player's feet). Built from the arena's footprint grid, so it routes around solid and standing breakable obstacles
    /// and the walls, keeping clear by an agent radius. Rebuild the walkable mask when the grid changes (a breakable
    /// broke, a new round), and Build again whenever the target moves to another cell.
    /// Plain data and maths: no engine objects, no allocations after construction.
    /// </summary>
    public sealed class FlowField
    {
        private const float Diagonal = 1.41421356f;
        private static readonly int[] StepColumn = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] StepRow = { 0, 0, 1, -1, 1, -1, 1, -1 };

        private readonly ArenaGrid grid;
        private readonly int columns;
        private readonly int rows;
        private readonly bool[] walkable;
        private readonly float[] cost;
        private readonly Vector2[] flow;
        private readonly int[] heapCell;
        private readonly float[] heapCost;
        private int heapCount;

        public int Columns => columns;
        public int Rows => rows;
        /// <summary>Cell the field was last built towards (-1 before the first Build).</summary>
        public int TargetColumn { get; private set; } = -1;
        public int TargetRow { get; private set; } = -1;
        /// <summary>Incremented on every Build, so viewers know when to redraw.</summary>
        public int Version { get; private set; }
        public ArenaGrid Grid => grid;

        public FlowField(ArenaGrid arenaGrid, float navRadius)
        {
            grid = arenaGrid;
            columns = grid.Columns;
            rows = grid.Rows;
            int cells = columns * rows;
            walkable = new bool[cells];
            cost = new float[cells];
            flow = new Vector2[cells];
            heapCell = new int[cells * 10];
            heapCost = new float[cells * 10];
            for (int i = 0; i < cost.Length; i++)
                cost[i] = float.PositiveInfinity;
            RebuildMask(navRadius);
        }

        /// <summary>Recomputes which cells an agent of the given radius can stand in. Call when obstacles changed, then Build.</summary>
        public void RebuildMask(float navRadius)
        {
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                    walkable[r * columns + c] = !grid.CircleBlocked(grid.CellCenter(c, r), navRadius);
            TargetColumn = TargetRow = -1;
        }

        public bool IsWalkable(int column, int row) =>
            column >= 0 && row >= 0 && column < columns && row < rows && walkable[row * columns + column];

        /// <summary>Walking cost to the target from a cell (in cell lengths), or infinity when the target can't be reached.</summary>
        public float CostAt(int column, int row) =>
            column < 0 || row < 0 || column >= columns || row >= rows ? float.PositiveInfinity : cost[row * columns + column];

        /// <summary>Direction of travel out of a cell (unit length, or zero at the target and in unreachable cells).</summary>
        public Vector2 FlowAt(int column, int row) =>
            column < 0 || row < 0 || column >= columns || row >= rows ? Vector2.zero : flow[row * columns + column];

        /// <summary>Direction to walk from a world position towards the target (zero when unreachable or already there).</summary>
        public Vector2 Direction(Vector2 position)
        {
            grid.WorldToCell(position, out int c, out int r);
            return flow[r * columns + c];
        }

        public bool IsReachable(Vector2 position)
        {
            grid.WorldToCell(position, out int c, out int r);
            return !float.IsPositiveInfinity(cost[r * columns + c]);
        }

        /// <summary>Recomputes every cell's cost and direction towards a target position.</summary>
        public void Build(Vector2 target)
        {
            grid.WorldToCell(target, out int targetC, out int targetR);
            TargetColumn = targetC;
            TargetRow = targetR;
            Version++;

            for (int i = 0; i < cost.Length; i++)
            {
                cost[i] = float.PositiveInfinity;
                flow[i] = Vector2.zero;
            }

            heapCount = 0;
            int seed = targetR * columns + targetC;
            cost[seed] = 0f;
            Push(seed, 0f);

            while (heapCount > 0)
            {
                int cell = Pop(out float popped);
                if (popped > cost[cell])
                    continue;   // a cheaper route to this cell was found after it was queued

                int c = cell % columns, r = cell / columns;
                for (int n = 0; n < 8; n++)
                {
                    int nc = c + StepColumn[n], nr = r + StepRow[n];
                    if (!IsWalkable(nc, nr))
                        continue;
                    bool diagonal = n >= 4;
                    // No cutting corners: a diagonal step needs both cells it passes beside to be walkable too.
                    if (diagonal && (!IsWalkable(c + StepColumn[n], r) || !IsWalkable(c, r + StepRow[n])))
                        continue;

                    float next = popped + (diagonal ? Diagonal : 1f);
                    int neighbour = nr * columns + nc;
                    if (next < cost[neighbour])
                    {
                        cost[neighbour] = next;
                        Push(neighbour, next);
                    }
                }
            }

            // Each cell points at its cheapest neighbour. Cells inside the safety margin of an obstacle (not walkable)
            // point out to the nearest walkable neighbour, so an enemy that ends up there can still find its way.
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    int cell = r * columns + c;
                    if (cell == seed)
                        continue;

                    float best = walkable[cell] ? cost[cell] : float.PositiveInfinity;
                    int bestStep = -1;
                    for (int n = 0; n < 8; n++)
                    {
                        int nc = c + StepColumn[n], nr = r + StepRow[n];
                        if (nc < 0 || nr < 0 || nc >= columns || nr >= rows)
                            continue;
                        int neighbour = nr * columns + nc;
                        if (!walkable[neighbour] && neighbour != seed)
                            continue;
                        if (n >= 4 && walkable[cell] &&
                            (!IsWalkable(c + StepColumn[n], r) || !IsWalkable(c, r + StepRow[n])))
                            continue;

                        if (cost[neighbour] < best)
                        {
                            best = cost[neighbour];
                            bestStep = n;
                        }
                    }

                    if (bestStep >= 0)
                        flow[cell] = new Vector2(StepColumn[bestStep], StepRow[bestStep]).normalized;
                }
            }
        }

        // ---- tiny binary min-heap (allocation free)

        private void Push(int cell, float priority)
        {
            int i = heapCount++;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (heapCost[parent] <= priority)
                    break;
                heapCell[i] = heapCell[parent];
                heapCost[i] = heapCost[parent];
                i = parent;
            }
            heapCell[i] = cell;
            heapCost[i] = priority;
        }

        private int Pop(out float priority)
        {
            int top = heapCell[0];
            priority = heapCost[0];
            heapCount--;
            if (heapCount > 0)
            {
                int cell = heapCell[heapCount];
                float last = heapCost[heapCount];
                int i = 0;
                while (true)
                {
                    int child = 2 * i + 1;
                    if (child >= heapCount)
                        break;
                    if (child + 1 < heapCount && heapCost[child + 1] < heapCost[child])
                        child++;
                    if (heapCost[child] >= last)
                        break;
                    heapCell[i] = heapCell[child];
                    heapCost[i] = heapCost[child];
                    i = child;
                }
                heapCell[i] = cell;
                heapCost[i] = last;
            }
            return top;
        }
    }
}
