using UnityEngine;

namespace BulletHell.Core
{
    /// <summary>
    /// Frame-rate independent fire cadence. Time left over inside a frame carries into the next one,
    /// but an idle pause never stores up a burst.
    /// </summary>
    public sealed class FireTimer
    {
        private const int MaxShotsPerTick = 4;
        private float cooldown;

        /// <summary>Advances time and returns how many shots to fire this tick.</summary>
        public int Tick(float deltaTime, bool firing, float shotsPerSecond)
        {
            cooldown -= deltaTime;
            if (!firing)
            {
                cooldown = Mathf.Max(cooldown, 0f);
                return 0;
            }

            float interval = 1f / Mathf.Max(0.01f, shotsPerSecond);
            int shots = 0;
            while (cooldown <= 0f && shots < MaxShotsPerTick)
            {
                shots++;
                cooldown += interval;
            }
            cooldown = Mathf.Max(cooldown, 0f);
            return shots;
        }
    }
}
