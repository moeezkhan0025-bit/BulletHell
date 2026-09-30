using System;
using TMPro;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// One selectable row of a skeleton menu list. Submit (A / Cross) runs the select action; Cancel (B / Circle)
    /// runs the cancel action while this row is focused. Rows are pooled by UIList and reused.
    /// </summary>
    public sealed class MenuRow : MonoBehaviour, ICancelHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [Tooltip("Used only when there is no UITheme.")]
        [SerializeField] private Color normalTextColor = Color.black;
        [SerializeField] private Color dimTextColor = new Color(0.45f, 0.45f, 0.5f);

        private Action onSelect;
        private Action onCancel;

        private void Awake() => button.onClick.AddListener(() => onSelect?.Invoke());

        public void Setup(string text, Action select, Action cancel, bool dim)
        {
            label.text = text;
            UITheme theme = UITheme.Current;
            Color normal = theme != null ? theme.TextOnButton : normalTextColor;
            Color dimmed = theme != null ? theme.TextOnButtonDim : dimTextColor;
            label.color = dim ? dimmed : normal;
            onSelect = select;
            onCancel = cancel;
        }

        public void OnCancel(BaseEventData eventData) => onCancel?.Invoke();

        // Focus lift: the focused row swells a little. Unscaled so it works while the game is paused.
        public void OnSelect(BaseEventData eventData) => Lift(1.06f);

        public void OnDeselect(BaseEventData eventData) => Lift(1f);

        private void OnDisable()
        {
            Tween.StopAll(transform);
            transform.localScale = Vector3.one;
        }

        private void Lift(float scale)
        {
            Tween.StopAll(transform);
            if (Mathf.Approximately(transform.localScale.x, scale))
                return;   // PrimeTween warns when a tween ends where it already is
            Tween.Scale(transform, scale, 0.12f, Ease.OutQuad, useUnscaledTime: true);
        }
    }
}
