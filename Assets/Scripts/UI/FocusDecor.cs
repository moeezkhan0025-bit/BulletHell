using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// Focus decoration for a Selectable: shows its decor objects (checker trims at the ends of a button, laurels beside
    /// it, a gold focus ring) while it is selected or hovered, and lifts it by the theme's focus lift. Decor objects are
    /// plain children, so the layout stays untouched.
    /// </summary>
    public sealed class FocusDecor : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Objects shown only while focused (trims, laurels, ring).")]
        [SerializeField] private GameObject[] shownWhenFocused = new GameObject[0];
        [Tooltip("Lift the rect on focus by the theme's focus lift (12 px @1080p).")]
        [SerializeField] private bool lift;
        [Tooltip("What lifts. Empty = this object's rect.")]
        [SerializeField] private RectTransform liftTarget;

        private bool selected;
        private bool hovered;
        private Vector2 rest;
        private bool restCached;

        public GameObject[] ShownWhenFocused
        {
            get => shownWhenFocused;
            set => shownWhenFocused = value;
        }

        public bool Lift
        {
            get => lift;
            set => lift = value;
        }

        public RectTransform LiftTarget
        {
            get => liftTarget;
            set => liftTarget = value;
        }

        private RectTransform Target => liftTarget != null ? liftTarget : (RectTransform)transform;

        private void OnEnable()
        {
            selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            hovered = false;
            Refresh(false);
        }

        private void OnDisable()
        {
            if (restCached)
            {
                Tween.StopAll(Target);
                Target.anchoredPosition = rest;
            }
            SetShown(false);
        }

        public void OnSelect(BaseEventData eventData) { selected = true; Refresh(true); }
        public void OnDeselect(BaseEventData eventData) { selected = false; Refresh(true); }
        public void OnPointerEnter(PointerEventData eventData) { hovered = true; Refresh(true); }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; Refresh(true); }

        private void Refresh(bool animate)
        {
            bool focused = selected || hovered;
            SetShown(focused);
            if (!lift)
                return;
            RectTransform target = Target;
            if (!restCached)
            {
                rest = target.anchoredPosition;
                restCached = true;
            }
            UITheme theme = UITheme.Current;
            float seconds = theme != null ? theme.FocusSeconds : 0.12f;
            float height = theme != null ? theme.FocusLiftPx : 12f;
            Vector2 goal = rest + (focused ? new Vector2(0f, height) : Vector2.zero);
            Tween.StopAll(target);
            if (animate && Application.isPlaying && isActiveAndEnabled)
                Tween.UIAnchoredPosition(target, goal, seconds, Ease.OutQuad, useUnscaledTime: true);
            else
                target.anchoredPosition = goal;
        }

        private void SetShown(bool shown)
        {
            foreach (GameObject go in shownWhenFocused)
                if (go != null)
                    go.SetActive(shown);
        }
    }
}
