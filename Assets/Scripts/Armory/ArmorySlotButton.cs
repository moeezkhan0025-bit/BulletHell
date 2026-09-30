using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BulletHell.Armory
{
    /// <summary>
    /// One of the 8 arm slots on the ring: a transparent hit area with a small frame that shows whether the slot is empty
    /// or holds an arm. Focus (stick or mouse hover) scales it and shows a ring. The arm itself is drawn by ArmoryRing.
    /// </summary>
    public sealed class ArmorySlotButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform body;
        [SerializeField] private Image frame;
        [SerializeField] private Image focusRing;
        [SerializeField] private Text label;
        [SerializeField, Range(0f, 1f)] private float emptyAlpha = 0.55f;
        [SerializeField, Range(0f, 1f)] private float filledAlpha = 0.18f;
        [SerializeField, Min(1f)] private float focusScale = 1.15f;

        public Button Button => button;
        public int Slot { get; set; }

        private void Awake()
        {
            if (focusRing != null)
                focusRing.enabled = false;
        }

        /// <summary>Sets the slot name ("NE") and whether it holds an arm (a filled slot's frame is nearly invisible: the arm is the picture).</summary>
        public void Set(string slotName, bool filled)
        {
            label.text = filled ? "" : slotName;
            Color color = frame.color;
            color.a = filled ? filledAlpha : emptyAlpha;
            frame.color = color;
        }

        public void OnSelect(BaseEventData eventData) => Focus(true);

        public void OnDeselect(BaseEventData eventData) => Focus(false);

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        private void OnDisable()
        {
            Tween.StopAll(body);
            body.localScale = Vector3.one;
            if (focusRing != null)
                focusRing.enabled = false;
        }

        private void Focus(bool on)
        {
            Tween.StopAll(body);
            if (focusRing != null)
                focusRing.enabled = on;
            Tween.Scale(body, on ? focusScale : 1f, 0.12f, Ease.OutQuad, useUnscaledTime: true);
        }
    }
}
