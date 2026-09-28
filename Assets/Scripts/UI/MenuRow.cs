using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// One selectable row of a skeleton menu list. Submit (A / Cross) runs the select action; Cancel (B / Circle)
    /// runs the cancel action while this row is focused. Rows are pooled by UIList and reused.
    /// </summary>
    public sealed class MenuRow : MonoBehaviour, ICancelHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Text label;
        [SerializeField] private Color normalTextColor = Color.black;
        [SerializeField] private Color dimTextColor = new Color(0.45f, 0.45f, 0.5f);

        private Action onSelect;
        private Action onCancel;

        private void Awake() => button.onClick.AddListener(() => onSelect?.Invoke());

        public void Setup(string text, Action select, Action cancel, bool dim)
        {
            label.text = text;
            label.color = dim ? dimTextColor : normalTextColor;
            onSelect = select;
            onCancel = cancel;
        }

        public void OnCancel(BaseEventData eventData) => onCancel?.Invoke();
    }
}
