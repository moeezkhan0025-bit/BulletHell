using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// A checker trim strip: the kit's tileable checker drawn tiled on the Image next to it (panel tops, pills, button ends).
    /// Tile size follows the theme's scale, so a strip is always a whole number of checks in the theme's pixels.
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(Image))]
    public sealed class ThemedTrim : MonoBehaviour
    {
        [SerializeField] private TrimColor color = TrimColor.Leaf;

        public TrimColor Color
        {
            get => color;
            set { color = value; Apply(); }
        }

        private void OnEnable() => Apply();

        private void OnValidate() => Apply();

        public void Apply()
        {
            UITheme theme = UITheme.Current;
            if (theme == null)
                return;
            Sprite sprite = theme.GetTrim(color);
            if (sprite == null)
                return;
            var image = GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Tiled;
            image.pixelsPerUnitMultiplier = theme.BorderMultiplier;
            image.color = UnityEngine.Color.white;
            image.raycastTarget = false;
        }
    }
}
