using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// One "label ... value" row of the Settings and Customization screens. Left/right on the stick or D-pad adjusts
    /// the value (-1 / +1); Submit (A / Cross, or a click) adjusts it by +1, which is what a toggle needs. Up/down moves
    /// between rows as usual (the Button's navigation must be Vertical, so left/right never move focus away).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class SettingRow : MonoBehaviour, IMoveHandler, ICancelHandler
    {
        [SerializeField] private Text label;
        [SerializeField] private Text value;

        private Func<string> valueText;
        private Action<int> adjust;

        public event Action Cancelled;

        public Button Button { get; private set; }

        private void Awake()
        {
            Button = GetComponent<Button>();
            Button.onClick.AddListener(() => Adjust(1));
        }

        /// <summary>Sets what the row shows and does. valueText is read on every refresh; adjust receives -1 or +1.</summary>
        public void Bind(string labelText, Func<string> valueSource, Action<int> onAdjust)
        {
            label.text = labelText;
            valueText = valueSource;
            adjust = onAdjust;
            Refresh();
        }

        public void Refresh() => value.text = valueText != null ? "<  " + valueText() + "  >" : "";

        public void OnMove(AxisEventData eventData)
        {
            if (eventData.moveDir == MoveDirection.Left)
                Adjust(-1);
            else if (eventData.moveDir == MoveDirection.Right)
                Adjust(1);
        }

        public void OnCancel(BaseEventData eventData) => Cancelled?.Invoke();

        private void Adjust(int direction)
        {
            adjust?.Invoke(direction);
            Refresh();
        }
    }
}
