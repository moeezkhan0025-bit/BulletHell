using UnityEngine;

namespace BulletHell.Enemies
{
    public enum AttackShape
    {
        /// <summary>One bullet straight at the player.</summary>
        Aimed,
        /// <summary>Bullets fanned across an arc centred on the player.</summary>
        Spread,
        /// <summary>Bullets evenly around the enemy, not aimed.</summary>
        Ring,
        /// <summary>A ring that turns a little after every volley.</summary>
        Spiral,
    }

    /// <summary>
    /// One repeating enemy attack: its shape, timing and bullets. An enemy type lists one or more of these; bosses
    /// (M7) reuse the same assets in their phases.
    /// </summary>
    [CreateAssetMenu(fileName = "Pattern_", menuName = "BulletHell/Attack Pattern")]
    public sealed class AttackPattern : ScriptableObject
    {
        public const int MaxBulletsPerVolley = 64;

        [Header("Shape")]
        [SerializeField] private AttackShape shape = AttackShape.Aimed;
        [Tooltip("Bullets per volley (Spread, Ring, Spiral). Aimed always fires one.")]
        [SerializeField, Range(1, MaxBulletsPerVolley)] private int bulletCount = 1;
        [Tooltip("Spread only: total arc in degrees.")]
        [SerializeField, Range(0f, 360f)] private float spreadAngle = 45f;
        [Tooltip("Spiral only: degrees the ring turns after each volley.")]
        [SerializeField] private float spiralStep = 15f;

        [Header("Timing")]
        [Tooltip("Seconds between volleys at difficulty 1.")]
        [SerializeField, Min(0.05f)] private float fireInterval = 1.5f;
        [Tooltip("Seconds after the enemy appears before its first volley.")]
        [SerializeField, Min(0f)] private float initialDelay = 0.75f;

        [Header("Bullets")]
        [SerializeField, Min(0.1f)] private float bulletSpeed = 5f;
        [Tooltip("Bullet diameter in world units.")]
        [SerializeField, Min(0.05f)] private float bulletSize = 0.3f;
        [Tooltip("Damage to the player, in hits (the player has a few hits of health).")]
        [SerializeField, Min(0.1f)] private float damage = 1f;
        [Tooltip("Standard = Electric Violet, Special = Hot Magenta (boss and special shots). The colours live in the EnemyBulletPalette asset.")]
        [SerializeField] private BulletStyle bulletStyle = BulletStyle.Standard;
        [SerializeField] private Sprite bulletSprite;
        [Tooltip("Safety net: released after this long even if still on screen.")]
        [SerializeField, Min(0.5f)] private float bulletLifetime = 10f;

        public AttackShape Shape => shape;
        public int BulletCount => shape == AttackShape.Aimed ? 1 : bulletCount;
        public float SpreadAngle => spreadAngle;
        public float SpiralStep => spiralStep;
        public float FireInterval => fireInterval;
        public float InitialDelay => initialDelay;
        public float BulletSpeed => bulletSpeed;
        public float BulletSize => bulletSize;
        public float Damage => damage;
        public BulletStyle BulletStyle => bulletStyle;
        public Sprite BulletSprite => bulletSprite;
        public float BulletLifetime => bulletLifetime;
    }
}
