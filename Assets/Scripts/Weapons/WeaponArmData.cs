using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>
    /// One weapon arm type: its art, identity and stats. Arm space: the pivot (attach point) is the origin
    /// and +X is the firing direction.
    /// </summary>
    [CreateAssetMenu(fileName = "Arm_", menuName = "BulletHell/Weapon Arm Data")]
    public sealed class WeaponArmData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Arm";
        [Tooltip("Colour that identifies this arm in UI and selection indicators.")]
        [SerializeField] private Color idColor = Color.white;

        [Header("Art")]
        [Tooltip("Pivot should sit at the attach point (set in the sprite's import settings).")]
        [SerializeField] private Sprite sprite;
        [Tooltip("Degrees the sprite is rotated so its barrel lines up with +X. Adjust in the Scene view via ArmVisual.")]
        [SerializeField] private float artRotation;
        [Tooltip("Barrel tip in arm space (world units from the attach point, +X = firing direction).")]
        [SerializeField] private Vector2 muzzleOffset = new Vector2(0.5f, 0f);

        [Header("Stats")]
        [SerializeField, Min(0f)] private float damage = 1f;
        [Tooltip("Shots per second while R1 is held.")]
        [SerializeField, Min(0.01f)] private float fireRate = 6f;
        [Tooltip("World units per second.")]
        [SerializeField, Min(0f)] private float projectileSpeed = 14f;
        [SerializeField, Min(1)] private int projectilesPerShot = 1;
        [Tooltip("Total spread angle in degrees across the projectiles of one shot.")]
        [SerializeField, Range(0f, 180f)] private float spread;
        [Tooltip("Bullet diameter in world units (also its hit radius). Heavier arms get bigger bullets.")]
        [SerializeField, Min(0.05f)] private float projectileSize = 0.25f;

        public string DisplayName => displayName;
        public Color IdColor => idColor;
        public Sprite Sprite => sprite;
        public float ArtRotation => artRotation;
        public Vector2 MuzzleOffset => muzzleOffset;
        public float Damage => damage;
        public float FireRate => fireRate;
        public float ProjectileSpeed => projectileSpeed;
        public int ProjectilesPerShot => projectilesPerShot;
        public float Spread => spread;
        public float ProjectileSize => projectileSize;

#if UNITY_EDITOR
        /// <summary>Editor-only: stores art rotation and muzzle tuned in the Scene view.</summary>
        public void SetArtAlignment(float rotation, Vector2 muzzle)
        {
            artRotation = rotation;
            muzzleOffset = muzzle;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
