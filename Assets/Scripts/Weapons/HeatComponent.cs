using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>
    /// Heat numbers for one ammo type. All values are fractions of the heat bar (1 = full), so an arm's heat stays
    /// meaningful when the active ammo changes.
    /// </summary>
    public readonly struct HeatSettings
    {
        public readonly float HeatPerShot;
        public readonly float HeatPerSecond;
        public readonly float CoolPerSecond;
        public readonly float RestartThreshold;

        public HeatSettings(float heatPerShot, float heatPerSecond, float coolPerSecond, float restartThreshold)
        {
            HeatPerShot = heatPerShot;
            HeatPerSecond = heatPerSecond;
            CoolPerSecond = coolPerSecond;
            RestartThreshold = restartThreshold;
        }
    }

    /// <summary>
    /// Shared heat model, one instance per arm. Heat builds while firing; at full heat the arm overheats and can't
    /// fire until it has cooled to the restart threshold. Heat only decays while the arm is not firing.
    /// </summary>
    public sealed class HeatComponent
    {
        public float Heat01 { get; private set; }
        public bool IsOverheated { get; private set; }

        /// <summary>Adds the per-shot heat of one shot (projectile ammo).</summary>
        public void AddShot(in HeatSettings settings) => Add(settings.HeatPerShot);

        /// <summary>Advances time: continuous heat while firing, cooling (and overheat recovery) while not.</summary>
        public void Tick(float deltaTime, bool firing, in HeatSettings settings)
        {
            if (firing)
            {
                Add(settings.HeatPerSecond * deltaTime);
                return;
            }

            Heat01 = Mathf.Max(0f, Heat01 - settings.CoolPerSecond * deltaTime);
            if (IsOverheated && Heat01 <= settings.RestartThreshold)
                IsOverheated = false;
        }

        private void Add(float amount)
        {
            if (amount <= 0f)
                return;
            Heat01 = Mathf.Min(1f, Heat01 + amount);
            if (Heat01 >= 1f)
                IsOverheated = true;
        }
    }
}
