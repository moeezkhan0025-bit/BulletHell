using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>Where a themed image gets its sprite from.</summary>
    public enum ThemeRole
    {
        Panel,
        Inset,
        Card,
        Slot,
        Tooltip,
        Header,
        Tab,
        TabSelected,
    }

    /// <summary>
    /// Maps UI ROLES (panel, button states, tab, tooltip, slider, toggle, card frame, fonts, sounds) to sprites, colors
    /// and a font. Screens and shared components never reference a UI pack directly; swapping a pack means editing this
    /// asset. Built from the Dobo "Mega Cozy UI Pack" demo (see Docs/CREDITS.md). Lives on GameConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "UITheme", menuName = "BulletHell/UI Theme")]
    public sealed class UITheme : ScriptableObject
    {
        [System.Serializable]
        public struct ButtonSprites
        {
            public Sprite normal;
            public Sprite highlighted;
            public Sprite selected;
            public Sprite pressed;
            public Sprite disabled;
        }

        [Header("Scale")]
        [Tooltip("Pack art is tiny pixel art. Sliced borders are drawn this many times bigger than the source pixels.")]
        [SerializeField, Min(0.25f)] private float pixelScale = 4f;

        [Header("Panels and frames (9-sliced)")]
        [SerializeField] private Sprite panel;
        [SerializeField] private Color panelColor = Color.white;
        [Tooltip("A recessed well inside a panel (lists, scroll areas).")]
        [SerializeField] private Sprite inset;
        [SerializeField] private Color insetColor = new Color(0f, 0f, 0f, 0.35f);
        [Tooltip("Card-like frame (shop and armory cards, item cards).")]
        [SerializeField] private Sprite cardFrame;
        [SerializeField] private Sprite cardFrameFocused;
        [SerializeField] private Sprite tooltip;
        [Tooltip("Ribbon behind screen titles.")]
        [SerializeField] private Sprite header;

        [Header("Buttons (sprite swap per state)")]
        [SerializeField] private ButtonSprites button;

        [Header("Tabs")]
        [SerializeField] private Sprite tab;
        [SerializeField] private Sprite tabSelected;

        [Header("Slider")]
        [SerializeField] private Sprite sliderTrack;
        [SerializeField] private Sprite sliderFill;
        [SerializeField] private Sprite sliderHandle;

        [Header("Toggle")]
        [SerializeField] private Sprite toggleOff;
        [SerializeField] private Sprite toggleOn;

        [Header("Adjust arrows (settings rows)")]
        [SerializeField] private Sprite arrow;

        [Header("HUD frames")]
        [Tooltip("Ammo slot and portrait frame, and its focused variant.")]
        [SerializeField] private Sprite hudFrame;
        [SerializeField] private Sprite hudFrameFocused;
        [SerializeField] private Color hudSlotFilled = Color.white;
        [SerializeField] private Color hudSlotEmpty = new Color(1f, 1f, 1f, 0.55f);

        [Header("Text")]
        [Tooltip("Empty = the built-in font.")]
        [SerializeField] private Font font;
        [SerializeField] private Color textOnPanel = new Color(1f, 0.97f, 0.88f);
        [SerializeField] private Color textOnButton = new Color(1f, 0.97f, 0.88f);
        [SerializeField] private Color textOnButtonDim = new Color(0.78f, 0.7f, 0.62f);
        [SerializeField] private Color textOnLight = new Color(0.33f, 0.16f, 0.1f);

        [Header("Sounds (empty until audio exists)")]
        [SerializeField] private AudioClip focusSound;
        [SerializeField] private AudioClip submitSound;
        [SerializeField] private AudioClip cancelSound;

        private static UITheme current;

        /// <summary>The theme on GameConfig (Resources), cached. Null when GameConfig has none.</summary>
        public static UITheme Current
        {
            get
            {
                if (current == null)
                {
                    var config = Resources.Load<Core.GameConfig>(Core.GameConfig.ResourcePath);
                    current = config != null ? config.UITheme : null;
                }
                return current;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => current = null;

        public float PixelScale => pixelScale;
        /// <summary>Image.pixelsPerUnitMultiplier that draws sliced borders <see cref="PixelScale"/> times bigger.</summary>
        public float BorderMultiplier => 1f / pixelScale;
        public ButtonSprites Button => button;
        public Sprite SliderTrack => sliderTrack;
        public Sprite SliderFill => sliderFill;
        public Sprite SliderHandle => sliderHandle;
        public Sprite ToggleOff => toggleOff;
        public Sprite ToggleOn => toggleOn;
        public Sprite Arrow => arrow;
        public Sprite HudFrame => hudFrame;
        public Sprite HudFrameFocused => hudFrameFocused;
        public Color HudSlotFilled => hudSlotFilled;
        public Color HudSlotEmpty => hudSlotEmpty;
        public Font Font => font;
        public Color TextOnPanel => textOnPanel;
        public Color TextOnButton => textOnButton;
        public Color TextOnButtonDim => textOnButtonDim;
        public Color TextOnLight => textOnLight;
        public AudioClip FocusSound => focusSound;
        public AudioClip SubmitSound => submitSound;
        public AudioClip CancelSound => cancelSound;

        public Sprite GetSprite(ThemeRole role)
        {
            switch (role)
            {
                case ThemeRole.Panel: return panel;
                case ThemeRole.Inset: return inset;
                case ThemeRole.Card: return cardFrame;
                case ThemeRole.Slot: return hudFrame;
                case ThemeRole.Tooltip: return tooltip;
                case ThemeRole.Header: return header;
                case ThemeRole.Tab: return tab;
                case ThemeRole.TabSelected: return tabSelected;
                default: return null;
            }
        }

        public Color GetColor(ThemeRole role)
        {
            switch (role)
            {
                case ThemeRole.Panel: return panelColor;
                case ThemeRole.Inset: return insetColor;
                default: return Color.white;
            }
        }

        /// <summary>The focused variant of a role's sprite, or the normal one when there is none.</summary>
        public Sprite GetFocusedSprite(ThemeRole role)
        {
            switch (role)
            {
                case ThemeRole.Card: return cardFrameFocused != null ? cardFrameFocused : cardFrame;
                case ThemeRole.Slot: return hudFrameFocused != null ? hudFrameFocused : hudFrame;
                default: return GetSprite(role);
            }
        }
    }
}
