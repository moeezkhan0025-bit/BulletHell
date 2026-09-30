using TMPro;
using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>Which theme font a label uses.</summary>
    public enum TextFont
    {
        Body,
        BodyBold,
        Button,
        Title,
        Logo,
    }

    /// <summary>Which theme color a label uses. Custom leaves the color already on the label.</summary>
    public enum TextTone
    {
        OnPanel,
        OnDark,
        Gold,
        Muted,
        Faint,
        Tomato,
        Leaf,
        Sage,
        Custom,
    }

    /// <summary>
    /// Themes a TextMeshPro label: font asset and color from UITheme. Titles (Title / Logo fonts) get the soil outline the
    /// mockups use; Caps adds wide tracking for the small spaced labels ("ARMS", "HEAD", "FEEL").
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(TMP_Text))]
    public sealed class ThemedText : MonoBehaviour
    {
        [SerializeField] private TextFont font = TextFont.Body;
        [SerializeField] private TextTone tone = TextTone.OnPanel;
        [Tooltip("Wide letter spacing for small spaced labels.")]
        [SerializeField] private bool caps;
        [Tooltip("Soil-colored outline around the glyphs (titles on dark backgrounds).")]
        [SerializeField] private bool outline;
        [Tooltip("Outline color instead of the theme soil color (logo shadow layers).")]
        [SerializeField] private bool customOutlineColor;
        [SerializeField] private Color outlineColor = Color.black;

        public TextFont Font
        {
            get => font;
            set { font = value; Apply(); }
        }

        public TextTone Tone
        {
            get => tone;
            set { tone = value; Apply(); }
        }

        public bool Caps
        {
            get => caps;
            set { caps = value; Apply(); }
        }

        /// <summary>Turns the outline on with a specific color.</summary>
        public Color OutlineColor
        {
            get => outlineColor;
            set { outlineColor = value; customOutlineColor = true; outline = true; Apply(); }
        }

        public bool Outline
        {
            get => outline;
            set { outline = value; Apply(); }
        }

        private static readonly System.Collections.Generic.Dictionary<string, Material> OutlineMaterials =
            new System.Collections.Generic.Dictionary<string, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => OutlineMaterials.Clear();

        private static Material OutlineMaterial(TMP_FontAsset font, Color color)
        {
            string key = font.GetInstanceID() + "_" + ColorUtility.ToHtmlStringRGBA(color);
            if (OutlineMaterials.TryGetValue(key, out Material cached) && cached != null)
                return cached;
            var material = new Material(font.material) { name = font.name + " Outline (shared)", hideFlags = HideFlags.HideAndDontSave };
            material.EnableKeyword("OUTLINE_ON");
            material.SetFloat("_OutlineWidth", 0.2f);
            material.SetColor("_OutlineColor", color);
            OutlineMaterials[key] = material;
            return material;
        }

        private void OnEnable() => Apply();

        private void OnValidate() => Apply();

        public void Apply()
        {
            UITheme theme = UITheme.Current;
            if (theme == null)
                return;
            if (!isActiveAndEnabled)
                return;   // applied in OnEnable; TMP cannot create material instances on inactive objects
            var text = GetComponent<TMP_Text>();
            TMP_FontAsset asset = null;
            switch (font)
            {
                case TextFont.BodyBold: asset = theme.BodyBoldFont; break;
                case TextFont.Button: asset = theme.ButtonFont; break;
                case TextFont.Title: asset = theme.TitleFont; break;
                case TextFont.Logo: asset = theme.LogoFont; break;
                default: asset = theme.BodyFont; break;
            }
            if (asset != null && text.font != asset)
                text.font = asset;

            switch (tone)
            {
                case TextTone.OnPanel: text.color = theme.TextOnPanel; break;
                case TextTone.OnDark: text.color = theme.TextOnDark; break;
                case TextTone.Gold: text.color = theme.Corn; break;
                case TextTone.Muted: text.color = theme.MutedText; break;
                case TextTone.Faint: text.color = theme.FaintText; break;
                case TextTone.Tomato: text.color = theme.Tomato; break;
                case TextTone.Leaf: text.color = theme.Leaf; break;
                case TextTone.Sage: text.color = theme.Sage; break;
            }

            text.characterSpacing = caps ? 6f : 0f;

            // Outline is a shared material preset per font (never TMP's per-label outlineWidth: that creates a material instance
            // that goes stale when the font changes and then draws the wrong atlas).
            if (asset != null)
            {
                Material material = outline ? OutlineMaterial(asset, customOutlineColor ? outlineColor : theme.InkSoil) : asset.material;
                if (material != null && text.fontSharedMaterial != material)
                    text.fontSharedMaterial = material;
            }
        }
    }
}
