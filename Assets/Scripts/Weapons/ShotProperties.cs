namespace BulletHell.Weapons
{
    /// <summary>
    /// Per-projectile behaviour granted by an arm's effects (its own plus its armaments'). Built once per arm change,
    /// copied into each projectile when it launches. The interaction rules are in Docs/ARMAMENTS.md.
    /// </summary>
    public struct ShotProperties
    {
        /// <summary>Extra enemies a projectile passes through before it stops. Used up before the bullet stops.</summary>
        public int Pierce;
        /// <summary>Wall/obstacle bounces before the bullet stops. Enemy hits never spend a bounce.</summary>
        public int Bounces;
        /// <summary>Homing: degrees per second the bullet can turn towards its target (0 = no homing).</summary>
        public float HomingTurnRate;
        /// <summary>Homing: total width in degrees of the forward cone a target must be inside.</summary>
        public float HomingCone;
        /// <summary>Homing: how far ahead a target can be, in world units.</summary>
        public float HomingRange;
        /// <summary>Auto-fire: fire-rate multiplier while the arm is not selected (0 = the arm has no auto-fire).</summary>
        public float AutoFireRate;
        /// <summary>Auto-fire: half-width in degrees around the arm slot direction in which enemies are shot.</summary>
        public float AutoFireArc;

        public bool HasHoming => HomingTurnRate > 0f;
        public bool HasAutoFire => AutoFireRate > 0f;
    }
}
