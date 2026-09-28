using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// Scrolls a ScrollRect so the controller-selected row stays visible (the EventSystem moves selection but never
    /// scrolls). Expects vertical content with its pivot at the top, as the skeleton lists are built.
    /// </summary>
    public sealed class ScrollFollowSelection : MonoBehaviour
    {
        [SerializeField] private ScrollRect scroll;

        private static readonly Vector3[] Corners = new Vector3[4];
        private GameObject lastSelected;

        private void LateUpdate()
        {
            EventSystem events = EventSystem.current;
            GameObject selected = events != null ? events.currentSelectedGameObject : null;
            if (selected == null || selected == lastSelected)
                return;

            lastSelected = selected;
            if (selected.transform.IsChildOf(scroll.content))
                Reveal((RectTransform)selected.transform);
        }

        private void Reveal(RectTransform item)
        {
            RectTransform content = scroll.content;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            float viewHeight = scroll.viewport.rect.height;
            if (content.rect.height <= viewHeight)
                return;

            item.GetWorldCorners(Corners);
            float top = -content.InverseTransformPoint(Corners[1]).y;
            float bottom = -content.InverseTransformPoint(Corners[0]).y;

            Vector2 position = content.anchoredPosition;
            if (top < position.y)
                position.y = top;
            else if (bottom > position.y + viewHeight)
                position.y = bottom - viewHeight;
            content.anchoredPosition = position;
        }
    }
}
