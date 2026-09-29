using UnityEngine;

namespace BulletHell.Arena
{
    /// <summary>
    /// Numbers that tie the 3/4 art to the flat 2D gameplay plane: how far bullets reach above an obstacle's footprint,
    /// how big an enemy's footprint and hurtbox are compared to its sprite, and how the sprite sits over its feet.
    /// Everything is a footprint at the feet (the object's transform); art hangs above it.
    /// </summary>
    [CreateAssetMenu(fileName = "PerspectiveTuning", menuName = "BulletHell/Perspective Tuning")]
    public sealed class PerspectiveTuning : ScriptableObject
    {
        [Header("Obstacles")]
        [Tooltip("Bullets are blocked by an obstacle's footprint plus this much extra reach upwards (the obstacle's body). Movement uses the plain footprint.")]
        [SerializeField, Min(0f)] private float obstacleBulletAllowance = 0.35f;

        [Tooltip("Same, for Low obstacles (low walls, crates): they are short, so bullets clear them sooner. Still blocks all bullets.")]
        [SerializeField, Min(0f)] private float lowObstacleBulletAllowance = 0.2f;

        [Tooltip("A Tall obstacle fades to this opacity while a character stands behind it.")]
        [SerializeField, Range(0.1f, 1f)] private float tallFadeAlpha = 0.4f;
        [Tooltip("Seconds to fade in / out.")]
        [SerializeField, Min(0.01f)] private float tallFadeSeconds = 0.15f;
        [Tooltip("A character counts as behind a Tall obstacle when it is this far (world units) past the sides of its art.")]
        [SerializeField, Min(0f)] private float tallFadeSideMargin = 0.25f;

        [Tooltip("Bullets fly at body height, so they may travel this far past the back edge of the floor (up the back wall) before it stops them. Without it a character at the top edge could not be shot, or shoot.")]
        [SerializeField, Min(0f)] private float bulletHeadroom = 1f;

        [Header("Enemies (fractions of the enemy's Size)")]
        [Tooltip("Movement footprint radius as a fraction of half the sprite size.")]
        [SerializeField, Range(0.2f, 1f)] private float enemyFootprintRadius = 0.6f;
        [Tooltip("How far the sprite hangs below the feet, so the footprint overlaps the sprite's lower edge.")]
        [SerializeField, Range(0f, 0.4f)] private float enemyFeetInset = 0.12f;
        [Tooltip("Player bullets hit this box, which stands on the feet. Fraction of the sprite width.")]
        [SerializeField, Range(0.3f, 1.2f)] private float enemyHurtboxWidth = 0.9f;
        [Tooltip("Fraction of the sprite height the hurtbox reaches up from the feet.")]
        [SerializeField, Range(0.3f, 1.2f)] private float enemyHurtboxHeight = 1f;

        [Header("Ground shadows")]
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.3f);
        [Tooltip("Shadow width as a multiple of the footprint diameter.")]
        [SerializeField, Min(0.2f)] private float shadowWidth = 1.5f;
        [Tooltip("Shadow height / width: flat, because it lies on the floor.")]
        [SerializeField, Range(0.1f, 1f)] private float shadowFlatness = 0.45f;

        [Header("Placeholder look (until final art)")]
        [Tooltip("Soft ellipse used for the contact shadows (the circle placeholder sprite).")]
        [SerializeField] private Sprite shadowSprite;
        [Tooltip("Placeholder pickups, coins and obstacles: colour saturation and brightness multipliers, so they sit in the painted backdrop's palette.")]
        [SerializeField, Range(0.2f, 1f)] private float placeholderSaturation = 0.7f;
        [SerializeField, Range(0.5f, 1f)] private float placeholderValue = 0.92f;
        [SerializeField] private Color placeholderOutline = new Color(0.14f, 0.09f, 0.07f, 0.95f);
        [Tooltip("Outline thickness as a multiple of the sprite size (1.2 = 10% each side).")]
        [SerializeField, Range(1f, 1.6f)] private float placeholderOutlineScale = 1.22f;
        [Tooltip("Contact shadow size on placeholder pickups and obstacles: width as a multiple of the footprint.")]
        [SerializeField, Range(0.5f, 2.5f)] private float contactShadowWidth = 1.35f;

        [Header("Bullets (player and enemy)")]
        [Tooltip("Bullets collide on the ground plane but are drawn this far above it, with a tiny shadow on the ground, so they read as flying.")]
        [SerializeField, Min(0f)] private float bulletVisualLift = 0.25f;
        [Tooltip("Bullet shadow width as a multiple of the bullet's size.")]
        [SerializeField, Range(0.2f, 1.5f)] private float bulletShadowScale = 0.7f;
        [Tooltip("Opacity of the bullet shadow.")]
        [SerializeField, Range(0f, 1f)] private float bulletShadowAlpha = 0.3f;

        public float BulletVisualLift => bulletVisualLift;
        public float BulletShadowScale => bulletShadowScale;
        public float BulletShadowAlpha => bulletShadowAlpha;
        public float ObstacleBulletAllowance => obstacleBulletAllowance;
        public float BulletHeadroom => bulletHeadroom;
        public float TallFadeAlpha => tallFadeAlpha;
        public float TallFadeSeconds => tallFadeSeconds;
        public float TallFadeSideMargin => tallFadeSideMargin;

        /// <summary>How far above its footprint an obstacle of this class still blocks bullets.</summary>
        public float BulletReachFor(ObstacleHeightClass heightClass) =>
            heightClass == ObstacleHeightClass.Low ? lowObstacleBulletAllowance : obstacleBulletAllowance;
        public float EnemyFeetInset => enemyFeetInset;
        public Color ShadowColor => shadowColor;
        public Sprite ShadowSprite => shadowSprite;
        public Color PlaceholderOutline => placeholderOutline;
        public float PlaceholderOutlineScale => placeholderOutlineScale;
        public float ContactShadowWidth => contactShadowWidth;

        /// <summary>A placeholder colour pulled towards the backdrop palette: less saturated and a touch darker.</summary>
        public Color Muted(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            Color muted = Color.HSVToRGB(h, s * placeholderSaturation, v * placeholderValue);
            muted.a = color.a;
            return muted;
        }
        public float ShadowWidth => shadowWidth;
        public float ShadowFlatness => shadowFlatness;

        public float EnemyFootprintRadiusFor(float enemySize) => enemySize * 0.5f * enemyFootprintRadius;
        public Vector2 EnemyHurtboxSizeFor(float enemySize) => new Vector2(enemySize * enemyHurtboxWidth, enemySize * enemyHurtboxHeight);

        private static PerspectiveTuning fallback;

        /// <summary>The default numbers, for scenes and tests that have no asset assigned.</summary>
        public static PerspectiveTuning Fallback => fallback != null ? fallback : fallback = CreateInstance<PerspectiveTuning>();
    }
}
