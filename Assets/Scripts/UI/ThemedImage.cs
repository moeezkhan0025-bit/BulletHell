using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// Puts a UITheme role's sprite and color on the Image next to it (9-sliced, borders drawn at the theme's pixel
    /// scale). Re-applied on enable, so editing the theme updates every screen.
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(Image))]
    public sealed class ThemedImage : MonoBehaviour
    {
        [SerializeField] private ThemeRole role = ThemeRole.Panel;
        [Tooltip("Keep the color already on the Image (for frames tinted at runtime, e.g. by arm color).")]
        [SerializeField] private bool keepColor;
        [Tooltip("0 = the theme's pixel scale. Use a smaller value for small HUD frames so borders do not swallow them.")]
        [SerializeField, Min(0f)] private float pixelScaleOverride;

        public float PixelScaleOverride
        {
            get => pixelScaleOverride;
            set { pixelScaleOverride = value; Apply(); }
        }

        public bool KeepColor
        {
            get => keepColor;
            set { keepColor = value; Apply(); }
        }

        public ThemeRole Role
        {
            get => role;
            set { role = value; Apply(); }
        }

        private void OnEnable() => Apply();

        private void OnValidate() => Apply();

        public void Apply()
        {
            UITheme theme = UITheme.Current;
            if (theme == null)
                return;
            Sprite sprite = theme.GetSprite(role);
            if (sprite == null)
                return;
            var image = GetComponent<Image>();
            image.sprite = sprite;
            bool sliced = sprite.border.sqrMagnitude > 0f;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced;
            image.pixelsPerUnitMultiplier = pixelScaleOverride > 0f ? 1f / pixelScaleOverride : theme.BorderMultiplier;
            if (!keepColor)
                image.color = theme.GetColor(role);
        }
    }
}
