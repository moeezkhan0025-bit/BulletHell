using System;
using UnityEngine;

namespace BulletHell.Arena
{
    /// <summary>
    /// How much hazard and clutter a round's layout may hold: a maximum count per trap kind and per obstacle class.
    /// Zero means none. Round 1 is all zeros for traps; later rounds raise one number at a time. A layout that goes over
    /// its round's budget is a bug (<see cref="LayoutValidator"/>). Breakables count in their height class too.
    /// </summary>
    [Serializable]
    public struct HazardBudget
    {
        [Min(0)] public int Vents;
        [Min(0)] public int Skewers;
        [Min(0)] public int Zones;
        [Min(0)] public int LowObstacles;
        [Min(0)] public int TallObstacles;
        [Tooltip("Of the obstacles above, how many may be breakable.")]
        [Min(0)] public int Breakables;

        public int Traps => Vents + Skewers + Zones;

        /// <summary>The counts a layout actually uses.</summary>
        public static HazardBudget Measure(ArenaLayoutData layout)
        {
            var used = new HazardBudget();
            if (layout == null)
                return used;

            foreach (ObstaclePlacement o in layout.Obstacles)
            {
                if (o.Data == null)
                    continue;
                if (o.Data.IsLow)
                    used.LowObstacles++;
                else
                    used.TallObstacles++;
                if (o.Data.IsBreakable)
                    used.Breakables++;
            }
            foreach (TrapPlacement t in layout.Traps)
            {
                if (t.Data == null)
                    continue;
                switch (t.Data.Kind)
                {
                    case TrapKind.Vent: used.Vents++; break;
                    case TrapKind.Skewer: used.Skewers++; break;
                    default: used.Zones++; break;
                }
            }
            return used;
        }

        /// <summary>True when every count of <paramref name="used"/> fits under this budget; otherwise says which one is over.</summary>
        public bool Allows(HazardBudget used, out string problem)
        {
            problem = null;
            if (used.Vents > Vents) problem = $"{used.Vents} vents (budget {Vents})";
            else if (used.Skewers > Skewers) problem = $"{used.Skewers} skewers (budget {Skewers})";
            else if (used.Zones > Zones) problem = $"{used.Zones} hazard zones (budget {Zones})";
            else if (used.LowObstacles > LowObstacles) problem = $"{used.LowObstacles} low obstacles (budget {LowObstacles})";
            else if (used.TallObstacles > TallObstacles) problem = $"{used.TallObstacles} tall obstacles (budget {TallObstacles})";
            else if (used.Breakables > Breakables) problem = $"{used.Breakables} breakables (budget {Breakables})";
            return problem == null;
        }

        /// <summary>How many trap kinds this budget allows that the previous round's did not (round progression adds one at a time).</summary>
        public int NewTrapKindsSince(HazardBudget previous)
        {
            int count = 0;
            if (Vents > 0 && previous.Vents == 0) count++;
            if (Skewers > 0 && previous.Skewers == 0) count++;
            if (Zones > 0 && previous.Zones == 0) count++;
            return count;
        }
    }
}
