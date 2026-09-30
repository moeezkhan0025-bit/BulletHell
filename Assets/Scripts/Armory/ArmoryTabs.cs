using System;
using TMPro;
using BulletHell.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.Armory
{
    /// <summary>
    /// The Arms / Armaments tabs above the inventory grid. The active tab uses the theme's TabSelected sprite, the other the
    /// Tab sprite. L1 / R1 (or a click) switch tabs.
    /// </summary>
    public sealed class ArmoryTabs : MonoBehaviour
    {
        public const int Arms = 0;
        public const int Armaments = 1;

        [SerializeField] private Button armsButton;
        [SerializeField] private Button armamentsButton;
        [SerializeField] private Image armsImage;
        [SerializeField] private Image armamentsImage;
        [SerializeField] private TMP_Text armsLabel;
        [SerializeField] private TMP_Text armamentsLabel;

        public int Active { get; private set; } = Armaments;
        public Button ArmsButton => armsButton;
        public Button ArmamentsButton => armamentsButton;

        /// <summary>Raised when the player picks a tab (click or Cross on a tab button). Shoulder buttons go through the screen.</summary>
        public event Action<int> Picked;

        private void Awake()
        {
            armsButton.onClick.AddListener(() => Picked?.Invoke(Arms));
            armamentsButton.onClick.AddListener(() => Picked?.Invoke(Armaments));
        }

        public void SetActive(int tab)
        {
            Active = tab;
            UITheme theme = UITheme.Current;
            if (theme != null)
            {
                Skin(armsImage, theme, tab == Arms ? ThemeRole.TabSelected : ThemeRole.Tab);
                Skin(armamentsImage, theme, tab == Armaments ? ThemeRole.TabSelected : ThemeRole.Tab);
            }
            if (theme != null)
            {
                armsLabel.color = tab == Arms ? theme.TextOnPanel : theme.FaintText;
                armamentsLabel.color = tab == Armaments ? theme.TextOnPanel : theme.FaintText;
            }
        }

        private static void Skin(Image image, UITheme theme, ThemeRole role)
        {
            Sprite sprite = theme.GetSprite(role);
            if (sprite == null)
                return;
            image.sprite = sprite;
            image.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = theme.BorderMultiplier;
            image.color = theme.GetColor(role);
        }

        public void SetCounts(int arms, int armaments)
        {
            armsLabel.text = "Arms (" + arms + ")";
            armamentsLabel.text = "Armaments (" + armaments + ")";
        }
    }
}
