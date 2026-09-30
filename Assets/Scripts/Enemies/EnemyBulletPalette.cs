using System;
using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>Which colour family an enemy bullet uses.</summary>
    public enum BulletStyle
    {
        /// <summary>Electric Violet: every ordinary enemy shot.</summary>
        Standard,
        /// <summary>Hot Magenta: boss and special shots.</summary>
        Special,
    }

    /// <summary>
    /// The colours of every enemy bullet, in one place (ART_SPEC section 7: enemy bullets pop against the floor, also in
    /// grayscale). Two families, each a white core, a coloured body and a solid dark outline; an optional faint glow behind.
    /// Nothing else in the game (arm ID colours, rarity frames, ammo tints, floor decals, UI) may use these two hues:
    /// <see cref="IsReservedHue"/> is what the tests check that with.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyBulletPalette", menuName = "BulletHell/Enemy Bullet Palette")]
    public sealed class EnemyBulletPalette : ScriptableObject
    {
        [Serializable]
        public struct Look
        {
            public Color Core;
            public Color Body;
            public Color Outline;
        }

        [Header("Families")]
        [Tooltip("Electric Violet: all ordinary enemy bullets.")]
        [SerializeField] private Look standard = new Look { Core = Color.white, Body = Hex(0xB4, 0x4B, 0xFF), Outline = Hex(0x1B, 0x07, 0x30) };
        [Tooltip("Hot Magenta: boss and special shots.")]
        [SerializeField] private Look special = new Look { Core = Color.white, Body = Hex(0xFF, 0x3D, 0xCB), Outline = Hex(0x1B, 0x07, 0x30) };

        [Header("Shape")]
        [Tooltip("Outline thickness in screen pixels at 1080p. Painted 2x bullet art uses double this (4 px).")]
        [SerializeField, Min(0f)] private float outlinePixels = 2f;
        [Tooltip("Core diameter as a fraction of the bullet's body.")]
        [SerializeField, Range(0.1f, 0.8f)] private float coreScale = 0.42f;
        [Tooltip("Faint soft glow behind the bullet, normal blending (never additive). 0 = none, at most 0.3.")]
        [SerializeField, Range(0f, 0.3f)] private float glowOpacity = 0.25f;
        [Tooltip("Glow diameter as a multiple of the bullet's size.")]
        [SerializeField, Range(1f, 3f)] private float glowScale = 1.6f;

        [Header("High contrast (Settings > Gameplay)")]
        [Tooltip("Smallest outline thickness, in pixels at 1080p, when high-contrast bullets are on.")]
        [SerializeField, Min(0f)] private float highContrastOutlinePixels = 3.5f;
        [Tooltip("The white ring around the dark outline that makes a bullet readable on any floor.")]
        [SerializeField] private Color haloColor = Color.white;
        [SerializeField, Range(0f, 1f)] private float haloOpacity = 0.95f;
        [SerializeField, Range(1f, 2.5f)] private float haloScale = 1.4f;

        [Header("Reserved hues (for the colour-clash check)")]
        [Tooltip("How many degrees around each body colour no other game colour may use (when it is saturated and bright enough to read as that colour).")]
        [SerializeField, Range(5f, 40f)] private float reservedHueRange = 22f;

        private static EnemyBulletPalette fallback;

        /// <summary>Built-in values, used when GameConfig has no palette asset.</summary>
        public static EnemyBulletPalette Fallback => fallback != null ? fallback : fallback = CreateInstance<EnemyBulletPalette>();

        public Look LookOf(BulletStyle style) => style == BulletStyle.Special ? special : standard;
        public float OutlinePixels => outlinePixels;
        public float CoreScale => coreScale;
        public float GlowOpacity => Mathf.Min(glowOpacity, 0.3f);
        public float GlowScale => glowScale;
        public float HighContrastOutlinePixels => highContrastOutlinePixels;
        public Color HaloColor => haloColor;
        public float HaloOpacity => haloOpacity;
        public float HaloScale => haloScale;

        /// <summary>True when a colour is in either family's hue band and vivid enough to be mistaken for an enemy bullet.</summary>
        public bool IsReservedHue(Color color)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            if (saturation < 0.35f || value < 0.4f)
                return false;
            return HueDistance(hue, standard.Body) <= reservedHueRange || HueDistance(hue, special.Body) <= reservedHueRange;
        }

        private static float HueDistance(float hue01, Color other)
        {
            Color.RGBToHSV(other, out float otherHue, out _, out _);
            float d = Mathf.Abs(hue01 - otherHue);
            return Mathf.Min(d, 1f - d) * 360f;
        }

        private static Color Hex(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);
    }
}
