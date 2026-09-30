using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>Which kit button a ThemedButton wears.</summary>
    public enum ButtonKind
    {
        /// <summary>Marble button; corn when focused.</summary>
        Normal,
        /// <summary>The red "Fight!" / confirm button.</summary>
        Primary,
    }

    /// <summary>
    /// Themes a Button: sprite swap (normal / focused / pressed / disabled) from UITheme, plus the font and color of every
    /// TMP_Text under it. Controller focus shows the "selected" sprite, so it must read clearly. The pressed sprite is
    /// shifted in the art, so the label is nudged down when pressed by the sprite itself.
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(Button), typeof(Image))]
    public sealed class ThemedButton : MonoBehaviour
    {
        [SerializeField] private ButtonKind kind = ButtonKind.Normal;

        public ButtonKind Kind
        {
            get => kind;
            set { kind = value; Apply(); }
        }

        private void Awake()
        {
            if (Application.isPlaying)
                GetComponent<Button>().onClick.AddListener(() => UiSound.Play(UiSoundKind.Confirm));
        }

        private void OnEnable() => Apply();

        private void OnValidate() => Apply();

        public void Apply()
        {
            UITheme theme = UITheme.Current;
            if (theme == null || theme.Button.normal == null)
                return;
            var button = GetComponent<Button>();
            var image = GetComponent<Image>();
            UITheme.ButtonSprites sprites = theme.Button;
            bool primary = kind == ButtonKind.Primary && theme.ButtonPrimary != null;
            Sprite normal = primary ? theme.ButtonPrimary : sprites.normal;
            Sprite focused = primary ? theme.ButtonPrimary : (sprites.selected != null ? sprites.selected : sprites.normal);

            image.sprite = normal;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = theme.BorderMultiplier;
            image.color = Color.white;

            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = focused,
                selectedSprite = focused,
                pressedSprite = sprites.pressed != null ? sprites.pressed : normal,
                disabledSprite = sprites.disabled != null ? sprites.disabled : normal,
            };

            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (theme.ButtonFont != null)
                {
                    text.font = theme.ButtonFont;
                    if (text.isActiveAndEnabled && text.fontSharedMaterial != theme.ButtonFont.material)
                        text.fontSharedMaterial = theme.ButtonFont.material;
                }
                text.color = primary ? theme.TextOnDark : theme.TextOnButton;
            }
        }
    }
}
