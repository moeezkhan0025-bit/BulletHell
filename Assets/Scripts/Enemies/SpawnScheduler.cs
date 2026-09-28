using System.Collections.Generic;
using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>One enemy that is due to appear.</summary>
    public readonly struct SpawnRequest
    {
        public readonly EnemyData Enemy;
        public readonly SpawnPattern Pattern;
        /// <summary>Position of this enemy inside its group, 0..Count-1.</summary>
        public readonly int Index;
        /// <summary>Size of its group (after the difficulty count multiplier).</summary>
        public readonly int Count;

        public SpawnRequest(EnemyData enemy, SpawnPattern pattern, int index, int count)
        {
            Enemy = enemy;
            Pattern = pattern;
            Index = index;
            Count = count;
        }
    }

    /// <summary>
    /// Turns a wave into a timed list of spawns. Group sizes are scaled by the round's count multiplier (rounded, never
    /// below one enemy per group). Plain logic with no Unity objects besides the data, so it can be unit tested.
    /// </summary>
    public sealed class SpawnScheduler
    {
        private struct Entry
        {
            public float Time;
            public SpawnRequest Request;
        }

        private readonly List<Entry> entries = new List<Entry>();
        private int next;
        private float elapsed;

        /// <summary>Enemies this wave will spawn in total.</summary>
        public int TotalCount => entries.Count;
        public int Spawned => next;
        public bool Finished => next >= entries.Count;

        public static int ScaledCount(int baseCount, float countMultiplier) =>
            Mathf.Max(1, Mathf.RoundToInt(baseCount * countMultiplier));

        public void Begin(WaveData wave, float countMultiplier)
        {
            entries.Clear();
            next = 0;
            elapsed = 0f;

            foreach (SpawnGroup group in wave.Groups)
            {
                if (group.Enemy == null)
                    continue;

                int count = ScaledCount(Mathf.Max(1, group.Count), countMultiplier);
                for (int i = 0; i < count; i++)
                    entries.Add(new Entry
                    {
                        Time = group.Delay + i * group.Interval,
                        Request = new SpawnRequest(group.Enemy, group.Pattern, i, count),
                    });
            }

            // Stable insertion sort by time (waves are small).
            for (int i = 1; i < entries.Count; i++)
            {
                Entry item = entries[i];
                int j = i - 1;
                while (j >= 0 && entries[j].Time > item.Time)
                {
                    entries[j + 1] = entries[j];
                    j--;
                }
                entries[j + 1] = item;
            }
        }

        /// <summary>Advances time and appends every spawn that became due. Returns how many were added.</summary>
        public int Tick(float deltaTime, List<SpawnRequest> due)
        {
            elapsed += deltaTime;
            int added = 0;
            while (next < entries.Count && entries[next].Time <= elapsed)
            {
                due.Add(entries[next].Request);
                next++;
                added++;
            }
            return added;
        }
    }
}
