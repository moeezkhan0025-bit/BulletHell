using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BulletHell.UI
{
    /// <summary>
    /// Turns the UI's Cancel action (B / Circle on a gamepad, Esc on a keyboard) into an event. The EventSystem sends
    /// Cancel only to the selected object, so put this on every selectable of a screen that should go Back on Cancel.
    /// </summary>
    public sealed class CancelRelay : MonoBehaviour, ICancelHandler
    {
        public event Action Cancelled;

        public void OnCancel(BaseEventData eventData) => Cancelled?.Invoke();
    }
}
