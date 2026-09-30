using System;
using BulletHell.Enemies;
using BulletHell.Feedback;
using BulletHell.Projectiles;
using Unity.Profiling;
using UnityEngine;

namespace BulletHell.Perf
{
    /// <summary>
    /// Live numbers for the performance overlay: frame time (a ring of the last 256 frames), average, p99, max, fps, garbage
    /// allocated per frame and the object counts (bullets, enemies, particles, pools). Allocation-free: the ring and the sort
    /// scratch array are created once, the slow numbers are refreshed at 4 Hz.
    /// </summary>
    public sealed class PerfStats : IDisposable
    {
        public const int Capacity = 256;

        private readonly float[] ring = new float[Capacity];
        private readonly float[] scratch = new float[Capacity];
        private ProfilerRecorder gcRecorder;
        private int head;
        private int count;
        private float nextSlow;

        private ProjectilePool bullets;
        private BulletHell.AI.NavigationService navigation;
        private EnemyPool enemyPool;
        private EnemyPool bossPool;

        public float LastMs { get; private set; }
        public float AvgMs { get; private set; }
        public float P99Ms { get; private set; }
        public float MaxMs { get; private set; }
        public float Fps { get; private set; }
        public long GcBytes { get; private set; }
        public long GcBytesMax { get; private set; }
        public int Bullets { get; private set; }
        public int BulletsPooled { get; private set; }
        public int BulletsCreated { get; private set; }
        public int Enemies { get; private set; }
        public int EnemyPoolActive { get; private set; }
        public int Particles { get; private set; }

        /// <summary>The frame-time ring, oldest to newest: index 0 is `Count - 1` frames ago.</summary>
        public int Count => count;
        public float FrameMsAgo(int framesAgo) => ring[(head - 1 - framesAgo + Capacity * 2) % Capacity];

        public PerfStats()
        {
            gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        }

        public void Dispose()
        {
            if (gcRecorder.Valid)
                gcRecorder.Dispose();
        }

        /// <summary>Looks up the scene objects whose counts are shown. Call after the Game scene loads.</summary>
        public void Bind()
        {
            bullets = UnityEngine.Object.FindFirstObjectByType<ProjectilePool>();
            navigation = UnityEngine.Object.FindFirstObjectByType<BulletHell.AI.NavigationService>();
            foreach (EnemyPool pool in UnityEngine.Object.FindObjectsByType<EnemyPool>(FindObjectsSortMode.None))
            {
                if (pool.name == "BossPool")
                    bossPool = pool;
                else
                    enemyPool = pool;
            }
        }

        /// <summary>Records one frame. `unscaledDelta` is the real frame time, so hitstops do not hide hitches.</summary>
        public void Record(float unscaledDelta)
        {
            float ms = unscaledDelta * 1000f;
            ring[head] = ms;
            head = (head + 1) % Capacity;
            if (count < Capacity)
                count++;
            LastMs = ms;
            GcBytes = gcRecorder.Valid ? gcRecorder.LastValue : 0;
            if (GcBytes > GcBytesMax)
                GcBytesMax = GcBytes;
            if (Time.unscaledTime >= nextSlow)
            {
                nextSlow = Time.unscaledTime + 0.25f;
                RefreshSlow();
            }
        }

        /// <summary>Clears the running maximum (GC and frame time) so a new test starts clean.</summary>
        public void ResetMax()
        {
            GcBytesMax = 0;
            MaxMs = 0f;
        }

        private void RefreshSlow()
        {
            if (count == 0)
                return;
            float sum = 0f;
            float max = 0f;
            for (int i = 0; i < count; i++)
            {
                float v = ring[i];
                scratch[i] = v;
                sum += v;
                if (v > max)
                    max = v;
            }
            Array.Sort(scratch, 0, count);
            AvgMs = sum / count;
            P99Ms = scratch[Mathf.Min(count - 1, (int)(count * 0.99f))];
            MaxMs = max;
            Fps = AvgMs > 0f ? 1000f / AvgMs : 0f;

            if (bullets == null || navigation == null || enemyPool == null)
                Bind();
            if (bullets != null)
            {
                Bullets = bullets.CountActive;
                BulletsPooled = bullets.CountInactive;
                BulletsCreated = bullets.TotalCreated;
            }
            Enemies = navigation != null ? navigation.Enemies.Count : 0;
            EnemyPoolActive = (enemyPool != null ? enemyPool.CountActive : 0) + (bossPool != null ? bossPool.CountActive : 0);
            Particles = FeedbackHub.CountLiveParticles();
        }
    }
}
