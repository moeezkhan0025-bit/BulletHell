using BulletHell.Weapons;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BulletHell.Armory
{
    /// <summary>
    /// A floating armament slot: pops up above the selected arm, bobs gently, and is tied to the arm by a tether.
    /// Empty = "+", filled = the armament icon inside a rim tinted by its rarity. Focus scales it up with an outline.
    /// The root moves (pop and bob); the body inside scales for focus, so the two never fight over a transform.
    /// </summary>
    public sealed class ArmoryBubble : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform body;
        [SerializeField] private CanvasGroup group;
        [Tooltip("The outline that shows while focused.")]
        [SerializeField] private Image outline;
        [Tooltip("Ring tinted by rarity.")]
        [SerializeField] private Image rim;
        [SerializeField] private Image inner;
        [SerializeField] private Image icon;
        [SerializeField] private Text iconLetter;
        [SerializeField] private Text plus;
        [SerializeField] private Color emptyRim = new Color(0.75f, 0.75f, 0.8f, 0.9f);
        [SerializeField, Min(1f)] private float focusScale = 1.25f;
        [SerializeField, Min(0f)] private float bobHeight = 7f;
        [SerializeField, Min(0.2f)] private float bobSeconds = 1.3f;

        private Vector2 restPosition;
        private bool open;

        public Button Button => button;
        public RectTransform Rect => (RectTransform)transform;
        public int Slot { get; set; }

        private void Awake()
        {
            if (outline != null)
                outline.enabled = false;
        }

        /// <summary>Fills the bubble. armament null = an empty slot ("+").</summary>
        public void Set(ArmamentData armament, Color rarityColor, Sprite placeholder)
        {
            bool filled = armament != null;
            rim.color = filled ? rarityColor : emptyRim;
            plus.enabled = !filled;
            bool hasIcon = filled && armament.Icon != null;
            icon.enabled = filled && (hasIcon || placeholder != null);
            icon.sprite = hasIcon ? armament.Icon : placeholder;
            icon.color = hasIcon ? Color.white : rarityColor;
            iconLetter.enabled = filled && !hasIcon;
            iconLetter.text = filled && armament.DisplayName.Length > 0 ? armament.DisplayName.Substring(0, 1) : "";
        }

        /// <summary>Pops in at a position (fade + overshoot scale), then starts bobbing.</summary>
        public void Open(Vector2 anchoredPosition, float delay)
        {
            restPosition = anchoredPosition;
            open = true;
            gameObject.SetActive(true);
            Tween.StopAll(transform);
            Rect.anchoredPosition = anchoredPosition;
            transform.localScale = Vector3.zero;
            group.alpha = 0f;
            Tween.Scale(transform, 1f, 0.3f, Ease.OutBack, startDelay: delay, useUnscaledTime: true);
            Tween.Alpha(group, 1f, 0.18f, Ease.OutQuad, startDelay: delay, useUnscaledTime: true);

            // Each bubble bobs on its own rhythm so they never move in lockstep.
            float seconds = bobSeconds * (0.85f + 0.3f * Mathf.Repeat(Slot * 0.37f, 1f));
            Tween.UIAnchoredPosition(Rect, anchoredPosition + new Vector2(0f, bobHeight), seconds, Ease.InOutSine,
                                     cycles: -1, cycleMode: CycleMode.Yoyo, startDelay: delay + 0.3f, useUnscaledTime: true);
        }

        /// <summary>Shrinks and fades out, then deactivates.</summary>
        public void Close(float delay = 0f)
        {
            if (!open)
                return;
            open = false;
            Tween.StopAll(transform);
            Tween.Scale(transform, 0f, 0.18f, Ease.InBack, startDelay: delay, useUnscaledTime: true);
            Tween.Alpha(group, 0f, 0.16f, Ease.InQuad, startDelay: delay, useUnscaledTime: true)
                 .OnComplete(() =>
                 {
                     if (this != null && !open)
                         gameObject.SetActive(false);
                 });
        }

        /// <summary>A little pulse when an armament lands in the bubble.</summary>
        public void Pulse()
        {
            Tween.PunchScale(body, new Vector3(0.25f, 0.25f, 0f), 0.35f, 6, useUnscaledTime: true);
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
            Tween.StopAll(transform);
            Tween.StopAll(body);
            body.localScale = Vector3.one;
            if (outline != null)
                outline.enabled = false;
            open = false;
        }

        private void Focus(bool on)
        {
            Tween.StopAll(body);
            if (outline != null)
                outline.enabled = on;
            Tween.Scale(body, on ? focusScale : 1f, 0.12f, Ease.OutQuad, useUnscaledTime: true);
        }
    }
}
