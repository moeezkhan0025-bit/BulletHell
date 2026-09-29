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
        public float EnemyFeetInset => enemyFeetInset;
        public Color ShadowColor => shadowColor;
        public float ShadowWidth => shadowWidth;
        public float ShadowFlatness => shadowFlatness;

        public float EnemyFootprintRadiusFor(float enemySize) => enemySize * 0.5f * enemyFootprintRadius;
        public Vector2 EnemyHurtboxSizeFor(float enemySize) => new Vector2(enemySize * enemyHurtboxWidth, enemySize * enemyHurtboxHeight);

        private static PerspectiveTuning fallback;

        /// <summary>The default numbers, for scenes and tests that have no asset assigned.</summary>
        public static PerspectiveTuning Fallback => fallback != null ? fallback : fallback = CreateInstance<PerspectiveTuning>();
    }
}
