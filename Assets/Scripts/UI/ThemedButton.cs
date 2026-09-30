using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// Themes a Button: sprite swap (normal / highlighted / selected / pressed / disabled) from UITheme, and the color of
    /// every Text under it. Controller focus shows the "selected" sprite, so it must read clearly.
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(Button), typeof(Image))]
    public sealed class ThemedButton : MonoBehaviour
    {
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

            image.sprite = sprites.normal;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = theme.BorderMultiplier;
            image.color = Color.white;

            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = sprites.highlighted != null ? sprites.highlighted : sprites.normal,
                selectedSprite = sprites.selected != null ? sprites.selected : sprites.normal,
                pressedSprite = sprites.pressed != null ? sprites.pressed : sprites.normal,
                disabledSprite = sprites.disabled != null ? sprites.disabled : sprites.normal,
            };

            foreach (Text text in GetComponentsInChildren<Text>(true))
                text.color = theme.TextOnButton;
        }
    }
}
