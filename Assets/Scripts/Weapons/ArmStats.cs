namespace BulletHell.Weapons
{
    public enum StatType { Damage, FireRate, ProjectileSpeed, ProjectilesPerShot, Spread }

    /// <summary>The tunable stats of one arm. Immutable value; ammo multipliers are applied on top of it when firing.</summary>
    public readonly struct ArmStats
    {
        public readonly float Damage;
        public readonly float FireRate;
        public readonly float ProjectileSpeed;
        public readonly int ProjectilesPerShot;
        public readonly float Spread;

        public ArmStats(float damage, float fireRate, float projectileSpeed, int projectilesPerShot, float spread)
        {
            Damage = damage;
            FireRate = fireRate;
            ProjectileSpeed = projectileSpeed;
            ProjectilesPerShot = projectilesPerShot;
            Spread = spread;
        }

        public static ArmStats FromData(WeaponArmData data) => new ArmStats(
            data.Damage, data.FireRate, data.ProjectileSpeed, data.ProjectilesPerShot, data.Spread);
    }
}
