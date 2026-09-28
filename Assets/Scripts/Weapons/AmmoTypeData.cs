using UnityEngine;

namespace BulletHell.Weapons
{
    public enum AmmoBehavior { Projectile, Beam }

    /// <summary>
    /// Ammo type: defines how an arm shoots (projectile shape and count, or a continuous beam), plus heat and spin-up.
    /// The arm's stats (WeaponArmData, later modified by armaments) are the base; the multipliers here scale them.
    /// Heat values are fractions of the heat bar (1 = overheat).
    /// </summary>
    [CreateAssetMenu(fileName = "Ammo_", menuName = "BulletHell/Ammo Type Data")]
    public sealed class AmmoTypeData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Basic";
        [SerializeField] private Sprite projectileSprite;
        [Tooltip("Colour of the pickup and of the laser beam.")]
        [SerializeField] private Color tint = Color.white;

        [Header("Behaviour")]
        [SerializeField] private AmmoBehavior behavior = AmmoBehavior.Projectile;
        [Tooltip("Safety net: a projectile that somehow never leaves the screen is released after this long.")]
        [SerializeField, Min(0.1f)] private float maxLifetime = 6f;

        [Header("Scales the arm's stats")]
        [Tooltip("Projectile: per-projectile damage. Beam: damage per second is arm damage x arm fire rate x this.")]
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float fireRateMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float projectileSpeedMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float projectileSizeMultiplier = 1f;
        [Tooltip("Pellets added on top of the arm's projectiles per shot (Shotgun).")]
        [SerializeField, Min(0)] private int extraProjectiles;
        [SerializeField, Min(0f)] private float spreadMultiplier = 1f;
        [Tooltip("Degrees of spread added on top of the arm's spread.")]
        [SerializeField, Min(0f)] private float addedSpread;

        [Header("Beam (Behaviour = Beam)")]
        [SerializeField, Min(0.1f)] private float beamRange = 30f;
        [SerializeField, Min(0.01f)] private float beamWidth = 0.12f;

        [Header("Heat (fractions of the bar; cooling applies to every ammo so an overheated arm can always recover)")]
        [SerializeField] private bool usesHeat;
        [SerializeField, Min(0f)] private float heatPerShot;
        [SerializeField, Min(0f)] private float heatPerSecond;
        [SerializeField, Min(0f)] private float coolPerSecond = 0.35f;
        [Tooltip("An overheated arm can fire again once its heat falls to this.")]
        [SerializeField, Range(0f, 1f)] private float restartThreshold = 0.4f;

        [Header("Spin-up (Gatling). Spin-up time 0 = none.")]
        [SerializeField, Min(0f)] private float spinUpTime;
        [SerializeField, Min(0f)] private float spinDownTime = 1f;
        [SerializeField, Min(0.01f)] private float minRateMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float maxRateMultiplier = 1f;

        public string DisplayName => displayName;
        public Sprite ProjectileSprite => projectileSprite;
        public Color Tint => tint;
        public AmmoBehavior Behavior => behavior;
        public float MaxLifetime => maxLifetime;

        public float DamageMultiplier => damageMultiplier;
        public float FireRateMultiplier => fireRateMultiplier;
        public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;
        public float ProjectileSizeMultiplier => projectileSizeMultiplier;
        public int ExtraProjectiles => extraProjectiles;
        public float SpreadMultiplier => spreadMultiplier;
        public float AddedSpread => addedSpread;

        public float BeamRange => beamRange;
        public float BeamWidth => beamWidth;

        public bool UsesHeat => usesHeat;

        /// <summary>Heat gained per shot / per second only when UsesHeat; cooling always applies.</summary>
        public HeatSettings Heat => new HeatSettings(
            usesHeat ? heatPerShot : 0f, usesHeat ? heatPerSecond : 0f, coolPerSecond, restartThreshold);

        public SpinSettings Spin => new SpinSettings(spinUpTime, spinDownTime, minRateMultiplier, maxRateMultiplier);
    }
}
