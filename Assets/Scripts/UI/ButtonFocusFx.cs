using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// The VoxKit focus treatment for every themed button, on controller focus and on mouse hover: a gold ring around the button and a lift
    /// of the theme's focus height (12 px @1080p, 0.12 s ease-out), and a small press-down while pressed. ThemedButton adds it, so no button
    /// has to opt in: the primary (red) buttons, whose focused sprite is the same as their normal one, show focus through it too. A button
    /// that has a FocusDecor (the Main Menu list: checker trims, laurels) keeps that and gets only the ring and press. Inside a layout group
    /// the lift is a scale instead of a move, so the layout never fights it. Disabled buttons show nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ButtonFocusFx : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler,
                                                      IPointerDownHandler, IPointerUpHandler
    {
        private const float RingOutset = 12f;
        private const float LayoutLiftScale = 1.05f;
        private const float PressedScale = 0.97f;

        private Button button;
        private RectTransform rect;
        private RectTransform ring;
        private bool selected;
        private bool hovered;
        private bool pressed;
        private bool inLayout;
        private bool hasDecor;
        private bool built;
        private Vector2 rest;
        private bool restCached;
        private Tween scaleTween;
        private Tween moveTween;

        private bool Focused => (selected || hovered) && button != null && button.IsInteractable();

        private void Awake() => Build();

        private void Build()
        {
            if (built)
                return;
            built = true;
            button = GetComponent<Button>();
            rect = (RectTransform)transform;
            hasDecor = GetComponent<FocusDecor>() != null;
            inLayout = transform.parent != null && transform.parent.GetComponent<LayoutGroup>() != null;

            UITheme theme = UITheme.Current;
            Sprite sprite = theme != null ? theme.RowFocusRing : null;
            if (sprite == null)
                return;
            var go = new GameObject("FocusRing", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();
            ring = (RectTransform)go.transform;
            ring.anchorMin = Vector2.zero;
            ring.anchorMax = Vector2.one;
            ring.offsetMin = new Vector2(-RingOutset, -RingOutset);
            ring.offsetMax = new Vector2(RingOutset, RingOutset);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = theme.BorderMultiplier;
            image.raycastTarget = false;
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            go.SetActive(false);
        }

        private void OnEnable()
        {
            Build();
            selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            hovered = pressed = false;
            Apply(false);
        }

        private void OnDisable()
        {
            if (!built)
                return;
            scaleTween.Stop();
            moveTween.Stop();
            rect.localScale = Vector3.one;
            if (restCached && !inLayout && !hasDecor)
                rect.anchoredPosition = rest;
            restCached = false;
            if (ring != null)
                ring.gameObject.SetActive(false);
        }

        public void OnSelect(BaseEventData eventData) { selected = true; Apply(true); }
        public void OnDeselect(BaseEventData eventData) { selected = false; pressed = false; Apply(true); }
        public void OnPointerEnter(PointerEventData eventData) { hovered = true; Apply(true); }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; Apply(true); }
        public void OnPointerDown(PointerEventData eventData) { pressed = true; Apply(true); }
        public void OnPointerUp(PointerEventData eventData) { pressed = false; Apply(true); }

        private void Update()
        {
            // A button that is switched to non-interactable while focused loses its ring.
            if (ring != null && ring.gameObject.activeSelf && !Focused)
                Apply(true);
        }

        private void Apply(bool animate)
        {
            if (!built)
                return;
            bool focused = Focused;
            if (ring != null)
                ring.gameObject.SetActive(focused);

            UITheme theme = UITheme.Current;
            float seconds = theme != null ? theme.FocusSeconds : 0.12f;
            float lift = theme != null ? theme.FocusLiftPx : 12f;
            bool play = animate && Application.isPlaying && isActiveAndEnabled;

            // Scale: the press-down, and the lift inside a layout group (a move would be overwritten by the layout).
            float scale = pressed && focused ? PressedScale : 1f;
            if (focused && inLayout && !hasDecor)
                scale *= LayoutLiftScale;
            scaleTween.Stop();
            if (play && !Mathf.Approximately(rect.localScale.x, scale))
                scaleTween = Tween.Scale(rect, scale, seconds, Ease.OutQuad, useUnscaledTime: true);
            else
                rect.localScale = Vector3.one * scale;

            // Lift: a move, only for a button the layout and a FocusDecor do not already own. The rest position is read when the
            // button is first lifted (after layout), and written back only by this component.
            if (inLayout || hasDecor)
                return;
            if (!restCached)
            {
                if (!focused)
                    return;
                rest = rect.anchoredPosition;
                restCached = true;
            }
            Vector2 goal = focused ? rest + new Vector2(0f, lift) : rest;
            moveTween.Stop();
            if (play && rect.anchoredPosition != goal)
                moveTween = Tween.UIAnchoredPosition(rect, goal, seconds, Ease.OutQuad, useUnscaledTime: true);
            else
                rect.anchoredPosition = goal;
            if (!focused && !play)
                restCached = false;
        }
    }
}
