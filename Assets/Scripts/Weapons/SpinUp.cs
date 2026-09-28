using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>Spin-up numbers for one ammo type. A spin-up time of 0 means no spin-up (always at the max rate multiplier).</summary>
    public readonly struct SpinSettings
    {
        public readonly float SpinUpTime;
        public readonly float SpinDownTime;
        public readonly float MinRateMultiplier;
        public readonly float MaxRateMultiplier;

        public SpinSettings(float spinUpTime, float spinDownTime, float minRateMultiplier, float maxRateMultiplier)
        {
            SpinUpTime = spinUpTime;
            SpinDownTime = spinDownTime;
            MinRateMultiplier = minRateMultiplier;
            MaxRateMultiplier = maxRateMultiplier;
        }
    }

    /// <summary>Gatling-style spin-up, one instance per arm: builds while firing, winds down when not.</summary>
    public sealed class SpinUp
    {
        public float Progress01 { get; private set; }

        /// <summary>Advances the spin and returns the fire-rate multiplier to use this frame.</summary>
        public float Tick(float deltaTime, bool firing, in SpinSettings settings)
        {
            if (settings.SpinUpTime <= 0f)
            {
                Progress01 = 1f;
            }
            else if (firing)
            {
                Progress01 = Mathf.Min(1f, Progress01 + deltaTime / settings.SpinUpTime);
            }
            else
            {
                float down = settings.SpinDownTime > 0f ? deltaTime / settings.SpinDownTime : 1f;
                Progress01 = Mathf.Max(0f, Progress01 - down);
            }

            return Mathf.Lerp(settings.MinRateMultiplier, settings.MaxRateMultiplier, Progress01);
        }
    }
}
