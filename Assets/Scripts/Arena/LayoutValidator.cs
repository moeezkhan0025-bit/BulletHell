using System.Collections.Generic;
using UnityEngine;

namespace BulletHell.Arena
{
    /// <summary>
    /// Checks an arena layout against the rules every layout must keep: things inside the walls, a clear area around the
    /// player's spawn, a walkable lane from every enemy gate to the spawn, no trap on top of the spawn or a gate, and
    /// the round's <see cref="HazardBudget"/>. Plain math over an <see cref="ArenaGrid"/>, so the setup script and the
    /// tests can run it on every shipped layout without a scene.
    /// </summary>
    public static class LayoutValidator
    {
        public const float DefaultClearRadius = 1.4f;
        public const float DefaultLaneRadius = 0.45f;   // the walking body of the biggest enemy (EnemyAiTuning.navRadius)

        /// <summary>The footprints of a layout's obstacles on a fresh grid (the same shapes ArenaController stamps).</summary>
        public static ArenaGrid BuildGrid(ArenaLayoutData layout, float cellSize = 0.25f)
        {
            var grid = new ArenaGrid(layout.Arena.Bounds, cellSize);
            ObstaclePlacement[] placed = layout.Obstacles;
            for (int i = 0; i < placed.Length; i++)
            {
                if (placed[i].Data == null)
                    continue;
                if (placed[i].Data.Shape == ObstacleShape.Circle)
                    grid.AddEllipse(placed[i].Position, placed[i].Size * 0.5f, i);
                else
                    grid.AddBox(placed[i].Position, placed[i].Size, i);
            }
            return grid;
        }

        /// <summary>Runs every check; problems are appended to the list. Returns true when there are none.</summary>
        public static bool Validate(ArenaLayoutData layout, HazardBudget budget, List<string> problems,
                                    float clearRadius = DefaultClearRadius, float laneRadius = DefaultLaneRadius)
        {
            int before = problems.Count;
            if (layout == null || layout.Arena == null)
            {
                problems.Add("layout or its arena shell is missing");
                return false;
            }

            Rect bounds = layout.Arena.Bounds;
            foreach (ObstaclePlacement o in layout.Obstacles)
            {
                if (o.Data == null)
                    problems.Add("an obstacle placement has no data");
                else if (!Contains(bounds, o.Position, o.Size * 0.5f))
                    problems.Add($"{o.Data.name} at {o.Position} sticks out of the walls");
            }

            ArenaGrid grid = BuildGrid(layout);
            Vector2 spawn = layout.PlayerSpawn;
            // The circle is kept inside the walls: a spawn near the front wall still needs its area clear of obstacles, not of the wall.
            Vector2 areaCenter = new Vector2(
                Mathf.Clamp(spawn.x, bounds.xMin + clearRadius, bounds.xMax - clearRadius),
                Mathf.Clamp(spawn.y, bounds.yMin + clearRadius, bounds.yMax - clearRadius));
            if (grid.CircleBlocked(areaCenter, clearRadius))
                problems.Add($"player spawn {spawn} is not clear within {clearRadius}");

            foreach (TrapPlacement t in layout.Traps)
            {
                if (t.Data == null)
                {
                    problems.Add("a trap placement has no data");
                    continue;
                }
                float reach = TrapReach(t.Data);
                if (Vector2.Distance(t.Position, spawn) < clearRadius + reach)
                    problems.Add($"{t.Data.name} at {t.Position} reaches the player spawn area");
                foreach (Vector2 gate in layout.SpawnGates)
                    if (Vector2.Distance(t.Position, gate) < 0.5f + reach)
                        problems.Add($"{t.Data.name} at {t.Position} sits on the gate at {gate}");
            }

            if (layout.SpawnGates.Length == 0)
                problems.Add("no enemy gates");
            foreach (Vector2 gate in layout.SpawnGates)
                if (!HasLane(grid, gate, spawn, laneRadius))
                    problems.Add($"no walkable lane from the gate at {gate} to the player spawn");

            if (!budget.Allows(HazardBudget.Measure(layout), out string overBudget))
                problems.Add($"over the hazard budget: {overBudget}");

            return problems.Count == before;
        }

        /// <summary>True when a circle of this radius can walk from the gate to the spawn (8-way, no corner cutting).</summary>
        public static bool HasLane(ArenaGrid grid, Vector2 from, Vector2 to, float radius)
        {
            grid.WorldToCell(from, out int startC, out int startR);
            grid.WorldToCell(to, out int goalC, out int goalR);
            int columns = grid.Columns, rows = grid.Rows;

            bool Walkable(int c, int r) => c >= 0 && r >= 0 && c < columns && r < rows && !grid.CircleBlocked(grid.CellCenter(c, r), radius);

            if (!Walkable(startC, startR) || !Walkable(goalC, goalR))
                return false;

            var seen = new bool[columns * rows];
            var queue = new Queue<int>();
            seen[startR * columns + startC] = true;
            queue.Enqueue(startR * columns + startC);
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int c = index % columns, r = index / columns;
                if (c == goalC && r == goalR)
                    return true;

                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dc == 0 && dr == 0)
                            continue;
                        int nc = c + dc, nr = r + dr;
                        if (!Walkable(nc, nr) || seen[nr * columns + nc])
                            continue;
                        if (dc != 0 && dr != 0 && (!Walkable(c + dc, r) || !Walkable(c, r + dr)))
                            continue;
                        seen[nr * columns + nc] = true;
                        queue.Enqueue(nr * columns + nc);
                    }
                }
            }
            return false;
        }

        private static bool Contains(Rect bounds, Vector2 center, Vector2 half) =>
            center.x - half.x >= bounds.xMin && center.x + half.x <= bounds.xMax &&
            center.y - half.y >= bounds.yMin && center.y + half.y <= bounds.yMax;

        private static float TrapReach(TrapData trap) =>
            trap.IsBox ? Mathf.Max(trap.Size.x, trap.Size.y) * 0.5f : trap.Size.x;
    }
}
