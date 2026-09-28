namespace BulletHell.Weapons
{
    /// <summary>
    /// Per-projectile behaviour granted by an arm's effects (its own plus its armaments'). Built once per arm change,
    /// copied into each projectile when it launches.
    /// </summary>
    public struct ShotProperties
    {
        /// <summary>Extra enemies a projectile passes through before it stops.</summary>
        public int Pierce;
        /// <summary>Times a projectile redirects after a hit instead of stopping.</summary>
        public int Bounces;
        /// <summary>How far a ricochet looks for the next target, in world units.</summary>
        public float BounceRange;
    }
}
