using TMPro;
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
        PanelShade,
        PanelWood,
        PanelCorn,
        PillMarble,
        HintBar,
    }

    /// <summary>The four checker trim colors of the kit.</summary>
    public enum TrimColor
    {
        Leaf,
        Carrot,
        Tomato,
        Corn,
    }

    /// <summary>
    /// The one and only UI theme: VOX VEGETALLIS (Garden Colosseum). Maps UI ROLES (panels, buttons, cards, bubbles, trims,
    /// sliders, toggles, hearts, heat bar, fonts, palette, rarity colors, motion, sounds) to the sprites of
    /// Assets/Art/UI/VoxKit and the tokens of its manifest. Screens and shared components read this asset and never
    /// reference kit sprites directly. Built by the editor tool BulletHell/Vox/Build Theme. Lives on GameConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "VoxVegetallis", menuName = "BulletHell/UI Theme (Vox Vegetallis)")]
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

        [System.Serializable]
        public struct Palette
        {
            public Color inkSoil;
            public Color marble;
            public Color marbleShade;
            public Color gold;
            public Color goldDark;
            public Color tomato;
            public Color leaf;
            public Color carrot;
            public Color corn;
            public Color sage;
            public Color faintText;
            public Color mutedText;
        }

        [Header("Scale")]
        [Tooltip("Kit sprites import at PPU 200 (authored at 2x), so they already draw at 1/2 size on the 1080p canvas. Extra multiplier for sliced borders.")]
        [SerializeField, Min(0.25f)] private float pixelScale = 1f;

        [Header("Palette (manifest tokens)")]
        [SerializeField] private Palette palette;
        [SerializeField] private Color rarityCommon = new Color(0.725f, 0.69f, 0.612f);
        [SerializeField] private Color rarityRare = new Color(0.369f, 0.624f, 0.243f);
        [SerializeField] private Color rarityEpic = new Color(0.898f, 0.478f, 0.141f);
        [SerializeField] private Color rarityLegendary = new Color(0.914f, 0.714f, 0.227f);

        [Header("Fonts (TextMeshPro)")]
        [Tooltip("Cinzel Decorative 700: screen titles.")]
        [SerializeField] private TMP_FontAsset titleFont;
        [Tooltip("Cinzel Decorative 900: the game logo.")]
        [SerializeField] private TMP_FontAsset logoFont;
        [Tooltip("Lilita One: buttons and numbers.")]
        [SerializeField] private TMP_FontAsset buttonFont;
        [Tooltip("Nunito 600: body text.")]
        [SerializeField] private TMP_FontAsset bodyFont;
        [Tooltip("Nunito 800: small caps labels and emphasis.")]
        [SerializeField] private TMP_FontAsset bodyBoldFont;

        [Header("Panels and frames (9-sliced)")]
        [SerializeField] private Sprite panelMarble;
        [SerializeField] private Sprite panelShade;
        [SerializeField] private Sprite panelWood;
        [SerializeField] private Sprite panelCorn;
        [SerializeField] private Sprite pillMarble;
        [SerializeField] private Sprite pillHintbar;
        [SerializeField] private Sprite rowFocusRing;

        [Header("Buttons (sprite swap per state)")]
        [SerializeField] private ButtonSprites button;
        [SerializeField] private Sprite buttonPrimary;
        [SerializeField] private Sprite buttonArrowLeft;
        [SerializeField] private Sprite buttonArrowRight;
        [SerializeField] private Sprite laurel;

        [Header("Tabs")]
        [SerializeField] private Sprite tab;
        [SerializeField] private Sprite tabSelected;

        [Header("Checker trims (tiled)")]
        [SerializeField] private Sprite trimLeaf;
        [SerializeField] private Sprite trimCarrot;
        [SerializeField] private Sprite trimTomato;
        [SerializeField] private Sprite trimCorn;

        [Header("Cards")]
        [SerializeField] private Sprite cardCommon;
        [SerializeField] private Sprite cardRare;
        [SerializeField] private Sprite cardEpic;
        [SerializeField] private Sprite cardLegendary;
        [SerializeField] private Sprite cardFocusRing;
        [SerializeField] private Sprite stampSold;

        [Header("Slots and bubbles")]
        [SerializeField] private Sprite slotAmmo;
        [SerializeField] private Sprite slotAmmoEmpty;
        [SerializeField] private Sprite slotActiveRing;
        [SerializeField] private Sprite bubbleEmpty;
        [SerializeField] private Sprite bubbleFilledRim;
        [SerializeField] private Sprite bubbleFocused;
        [SerializeField] private Sprite armSlotFilled;
        [SerializeField] private Sprite armSlotSelected;
        [SerializeField] private Sprite tetherVine;

        [Header("HUD")]
        [SerializeField] private Sprite heartFull;
        [SerializeField] private Sprite heartEmpty;
        [SerializeField] private Sprite heatTrack;
        [SerializeField] private Sprite heatFill;
        [SerializeField] private Sprite portraitRing;
        [SerializeField] private Sprite coin;
        [SerializeField] private Color heatCool = new Color(0.369f, 0.624f, 0.243f);
        [SerializeField] private Color heatWarm = new Color(0.898f, 0.478f, 0.141f);
        [SerializeField] private Color heatHot = new Color(0.847f, 0.267f, 0.227f);
        [SerializeField] private Color hudSlotFilled = Color.white;
        [SerializeField] private Color hudSlotEmpty = new Color(1f, 1f, 1f, 0.75f);

        [Header("Slider")]
        [SerializeField] private Sprite sliderTrack;
        [SerializeField] private Sprite sliderFill;
        [SerializeField] private Sprite sliderHandle;
        [SerializeField] private Sprite sliderHandleFocused;

        [Header("Toggle")]
        [SerializeField] private Sprite toggleOff;
        [SerializeField] private Sprite toggleOn;
        [SerializeField] private Sprite toggleKnob;

        [Header("Stage (Character Creation, Armory)")]
        [SerializeField] private Sprite pedestal;
        [SerializeField] private Sprite spotlight;

        [Header("Icons")]
        [SerializeField] private Sprite harvestCrateIcon;

        [Header("Metrics at 1080p and motion (manifest)")]
        [SerializeField, Min(0f)] private float outlinePx = 6f;
        [SerializeField, Min(0f)] private float dropShadowPx = 9f;
        [SerializeField, Min(0f)] private float focusLiftPx = 12f;
        [SerializeField, Min(0.01f)] private float focusSeconds = 0.12f;
        [SerializeField, Min(0.01f)] private float cardBuySeconds = 0.35f;
        [SerializeField, Min(0.01f)] private float bubbleInSeconds = 0.25f;
        [SerializeField, Min(0f)] private float bubbleStaggerSeconds = 0.05f;
        [SerializeField, Min(0.01f)] private float screenTransitionSeconds = 0.2f;

        [Header("Text colors")]
        [SerializeField] private Color textOnPanel = new Color(0.18f, 0.12f, 0.078f);
        [SerializeField] private Color textOnButton = new Color(0.18f, 0.12f, 0.078f);
        [SerializeField] private Color textOnButtonDim = new Color(0.557f, 0.514f, 0.439f);
        [SerializeField] private Color textOnLight = new Color(0.18f, 0.12f, 0.078f);
        [SerializeField] private Color textOnDark = new Color(0.957f, 0.933f, 0.863f);

        [Header("Sound overrides (empty = the AudioLibrary sound plays)")]
        [SerializeField] private AudioClip focusSound;
        [SerializeField] private AudioClip submitSound;
        [SerializeField] private AudioClip cancelSound;
        [SerializeField] private AudioClip buySound;
        [SerializeField] private AudioClip equipSound;
        [SerializeField] private AudioClip errorSound;

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

        // Scale
        public float PixelScale => pixelScale;
        /// <summary>Image.pixelsPerUnitMultiplier that draws kit art at half size on the 1080p canvas.</summary>
        public float BorderMultiplier => 1f / pixelScale;

        // Palette
        public Palette Colors => palette;
        public Color Gold => palette.gold;
        public Color Corn => palette.corn;
        public Color Tomato => palette.tomato;
        public Color Leaf => palette.leaf;
        public Color Carrot => palette.carrot;
        public Color InkSoil => palette.inkSoil;
        public Color Marble => palette.marble;
        public Color FaintText => palette.faintText;
        public Color MutedText => palette.mutedText;
        public Color Sage => palette.sage;
        public Color MarbleShade => palette.marbleShade;
        public Color GoldDark => palette.goldDark;

        /// <summary>Rarity color: common stone, rare leaf, epic carrot, legendary gold. Index = ArmamentRarity.</summary>
        public Color GetRarityColor(int rarity)
        {
            switch (rarity)
            {
                case 1: return rarityRare;
                case 2: return rarityEpic;
                case 3: return rarityLegendary;
                default: return rarityCommon;
            }
        }

        /// <summary>Card frame sprite for a rarity (index = ArmamentRarity).</summary>
        public Sprite GetCardFrame(int rarity)
        {
            switch (rarity)
            {
                case 1: return cardRare;
                case 2: return cardEpic;
                case 3: return cardLegendary;
                default: return cardCommon;
            }
        }

        /// <summary>Heat bar tint: leaf -> carrot -> tomato as heat (0..1) rises.</summary>
        public Color GetHeatColor(float heat01)
        {
            heat01 = Mathf.Clamp01(heat01);
            return heat01 < 0.5f
                ? Color.Lerp(heatCool, heatWarm, heat01 / 0.5f)
                : Color.Lerp(heatWarm, heatHot, (heat01 - 0.5f) / 0.5f);
        }

        // Fonts
        public TMP_FontAsset TitleFont => titleFont;
        public TMP_FontAsset LogoFont => logoFont;
        public TMP_FontAsset ButtonFont => buttonFont;
        public TMP_FontAsset BodyFont => bodyFont;
        public TMP_FontAsset BodyBoldFont => bodyBoldFont;

        // Buttons, sliders, toggles
        public ButtonSprites Button => button;
        public Sprite ButtonPrimary => buttonPrimary;
        public Sprite ButtonArrowLeft => buttonArrowLeft;
        public Sprite ButtonArrowRight => buttonArrowRight;
        public Sprite Laurel => laurel;
        public Sprite RowFocusRing => rowFocusRing;
        public Sprite PanelCorn => panelCorn;
        public Sprite SliderTrack => sliderTrack;
        public Sprite SliderFill => sliderFill;
        public Sprite SliderHandle => sliderHandle;
        public Sprite SliderHandleFocused => sliderHandleFocused;
        public Sprite ToggleOff => toggleOff;
        public Sprite ToggleOn => toggleOn;
        public Sprite ToggleKnob => toggleKnob;

        // Cards, slots, bubbles
        public Sprite CardFocusRing => cardFocusRing;
        public Sprite StampSold => stampSold;
        public Sprite SlotAmmo => slotAmmo;
        public Sprite SlotAmmoEmpty => slotAmmoEmpty;
        public Sprite SlotActiveRing => slotActiveRing;
        public Sprite BubbleEmpty => bubbleEmpty;
        public Sprite BubbleFilledRim => bubbleFilledRim;
        public Sprite BubbleFocused => bubbleFocused;
        public Sprite ArmSlotFilled => armSlotFilled;
        public Sprite ArmSlotSelected => armSlotSelected;
        public Sprite TetherVine => tetherVine;

        // HUD
        public Sprite HeartFull => heartFull;
        public Sprite HeartEmpty => heartEmpty;
        public Sprite HeatTrack => heatTrack;
        public Sprite HeatFill => heatFill;
        public Sprite PortraitRing => portraitRing;
        public Sprite Coin => coin;
        public Sprite HudFrame => slotAmmo;
        public Sprite HudFrameFocused => cardFocusRing;
        public Color HudSlotFilled => hudSlotFilled;
        public Color HudSlotEmpty => hudSlotEmpty;

        // Stage and icons
        public Sprite Pedestal => pedestal;
        public Sprite Spotlight => spotlight;
        public Sprite HarvestCrateIcon => harvestCrateIcon;

        // Metrics and motion
        public float OutlinePx => outlinePx;
        public float DropShadowPx => dropShadowPx;
        public float FocusLiftPx => focusLiftPx;
        public float FocusSeconds => focusSeconds;
        public float CardBuySeconds => cardBuySeconds;
        public float BubbleInSeconds => bubbleInSeconds;
        public float BubbleStaggerSeconds => bubbleStaggerSeconds;
        public float ScreenTransitionSeconds => screenTransitionSeconds;

        // Text colors
        public Color TextOnPanel => textOnPanel;
        public Color TextOnButton => textOnButton;
        public Color TextOnButtonDim => textOnButtonDim;
        public Color TextOnLight => textOnLight;
        public Color TextOnDark => textOnDark;
        public AudioClip FocusSound => focusSound;
        public AudioClip SubmitSound => submitSound;
        public AudioClip CancelSound => cancelSound;

        /// <summary>The checker trim tile for a color (Wrap Mode Repeat; draw as a tiled Image).</summary>
        public Sprite GetTrim(TrimColor color)
        {
            switch (color)
            {
                case TrimColor.Carrot: return trimCarrot;
                case TrimColor.Tomato: return trimTomato;
                case TrimColor.Corn: return trimCorn;
                default: return trimLeaf;
            }
        }

        /// <summary>The clip for a UI sound, or null when none is assigned (the hook stays silent).</summary>
        public AudioClip GetSound(UiSoundKind kind)
        {
            switch (kind)
            {
                case UiSoundKind.Focus: return focusSound;
                case UiSoundKind.Confirm: return submitSound;
                case UiSoundKind.Back: return cancelSound;
                case UiSoundKind.Buy: return buySound;
                case UiSoundKind.Equip: return equipSound;
                default: return errorSound;
            }
        }

        public Sprite GetSprite(ThemeRole role)
        {
            switch (role)
            {
                case ThemeRole.Panel: return panelMarble;
                case ThemeRole.Inset: return panelShade;
                case ThemeRole.PanelShade: return panelShade;
                case ThemeRole.PanelWood: return panelWood;
                case ThemeRole.PanelCorn: return panelCorn;
                case ThemeRole.PillMarble: return pillMarble;
                case ThemeRole.HintBar: return pillHintbar;
                case ThemeRole.Card: return cardCommon;
                case ThemeRole.Slot: return slotAmmo;
                case ThemeRole.Tooltip: return panelMarble;
                case ThemeRole.Header: return pillMarble;
                case ThemeRole.Tab: return tab;
                case ThemeRole.TabSelected: return tabSelected;
                default: return null;
            }
        }

        public Color GetColor(ThemeRole role) => Color.white;

        /// <summary>The focused variant of a role's sprite, or the normal one when there is none.</summary>
        public Sprite GetFocusedSprite(ThemeRole role)
        {
            switch (role)
            {
                case ThemeRole.Card: return cardFocusRing;
                case ThemeRole.Slot: return slotActiveRing;
                case ThemeRole.PanelShade:
                case ThemeRole.Panel: return panelCorn;
                default: return GetSprite(role);
            }
        }
    }
}
