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
        [Tooltip("Stable ID used in save files. Never change it once players may have saves. Filled by BulletHell/Collect Asset Registry.")]
        [SerializeField] private string id;
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

        [Header("Effects")]
        [Tooltip("Special effects (pierce, burn, ...) this arm type always has, on top of its armaments'.")]
        [SerializeField] private ArmEffect[] effects = System.Array.Empty<ArmEffect>();
        [Tooltip("How many armaments this arm can carry (1-3). Rarer arms get more.")]
        [SerializeField, Range(1, 3)] private int armamentSlots = 3;
        [Header("Shop")]
        [SerializeField] private ArmamentRarity rarity = ArmamentRarity.Common;
        [Tooltip("Shop price tier 1-5 (the Shop scales the price with rarity and round).")]
        [SerializeField, Range(1, 5)] private int priceTier = 2;

        public string Id => id;
        public string DisplayName => displayName;
        public int ArmamentSlots => armamentSlots;
        public ArmamentRarity Rarity => rarity;
        public int PriceTier => priceTier;
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
        public System.Collections.Generic.IReadOnlyList<ArmEffect> Effects => effects ?? System.Array.Empty<ArmEffect>();

#if UNITY_EDITOR
        /// <summary>Editor-only: sets rarity and price tier for the Shop.</summary>
        public void SetShopMeta(ArmamentRarity newRarity, int newPriceTier)
        {
            rarity = newRarity;
            priceTier = Mathf.Clamp(newPriceTier, 1, 5);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>Editor-only: sets how many armament slots this arm has.</summary>
        public void SetArmamentSlots(int value)
        {
            armamentSlots = Mathf.Clamp(value, 1, 3);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>Editor-only: assigns the save ID.</summary>
        public void SetId(string value)
        {
            id = value;
            UnityEditor.EditorUtility.SetDirty(this);
        }

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
