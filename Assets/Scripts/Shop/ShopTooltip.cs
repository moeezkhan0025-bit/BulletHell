using System.Collections.Generic;
using System.Text;
using BulletHell.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.Shop
{
    /// <summary>
    /// The tooltip next to the focused Shop card: a themed panel (UITheme Tooltip role) with the item text. The first
    /// line is the title. It sizes itself to the text and sits beside the card, kept inside the screen area.
    /// </summary>
    public sealed class ShopTooltip : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private Text body;
        [Tooltip("Space between the card and the tooltip, in canvas units.")]
        [SerializeField, Min(0f)] private float gap = 24f;

        private readonly StringBuilder builder = new StringBuilder(256);

        public bool IsShown => panel != null && panel.gameObject.activeSelf;

        private void Awake()
        {
            UITheme theme = UITheme.Current;
            if (theme != null)
                body.color = theme.TextOnLight;
            Hide();
        }

        /// <summary>Shows the lines beside a card. The first line is drawn bold.</summary>
        public void Show(RectTransform card, IReadOnlyList<string> lines, bool below = false)
        {
            builder.Clear();
            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                    builder.Append('\n');
                builder.Append(i == 0 ? "<b>" + lines[i] + "</b>" : lines[i]);
            }
            body.text = builder.ToString();

            panel.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            Place(card, below);
        }

        public void Hide()
        {
            if (panel != null)
                panel.gameObject.SetActive(false);
        }

        // Right of the card when it fits, else left; vertically centred on the card and clamped to the parent's rectangle.
        private void Place(RectTransform card, bool below)
        {
            var parent = (RectTransform)panel.parent;
            Vector3[] corners = new Vector3[4];
            card.GetWorldCorners(corners);
            Vector2 cardMin = parent.InverseTransformPoint(corners[0]);
            Vector2 cardMax = parent.InverseTransformPoint(corners[2]);

            Rect area = parent.rect;
            Vector2 size = panel.rect.size;
            if (below)
            {
                // Under the card, centred on it: used for the crate choices, where the tooltip must not hide the other choices.
                panel.pivot = new Vector2(0.5f, 1f);
                panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
                float bx = Mathf.Clamp((cardMin.x + cardMax.x) * 0.5f, area.xMin + size.x * 0.5f, Mathf.Max(area.xMin + size.x * 0.5f, area.xMax - size.x * 0.5f));
                panel.anchoredPosition = new Vector2(bx, cardMin.y - gap * 0.5f);
                return;
            }

            panel.pivot = new Vector2(0f, 0.5f);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);

            float x = cardMax.x + gap;
            if (x + size.x > area.xMax)
                x = cardMin.x - gap - size.x;
            x = Mathf.Clamp(x, area.xMin, Mathf.Max(area.xMin, area.xMax - size.x));

            float y = (cardMin.y + cardMax.y) * 0.5f;
            y = Mathf.Clamp(y, area.yMin + size.y * 0.5f, Mathf.Max(area.yMin + size.y * 0.5f, area.yMax - size.y * 0.5f));
            panel.anchoredPosition = new Vector2(x, y);
        }
    }
}
