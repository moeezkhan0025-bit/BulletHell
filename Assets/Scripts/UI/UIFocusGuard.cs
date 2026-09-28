using UnityEngine;
using UnityEngine.EventSystems;

namespace BulletHell.UI
{
    /// <summary>
    /// Keeps a menu controller-navigable: if nothing is selected (a mouse click on empty space, the selected button
    /// got disabled) the last valid selection is restored, so the stick or D-pad always has somewhere to start.
    /// Put on the EventSystem object.
    /// </summary>
    public sealed class UIFocusGuard : MonoBehaviour
    {
        private GameObject last;

        private void Update()
        {
            EventSystem events = EventSystem.current;
            if (events == null)
                return;

            GameObject current = events.currentSelectedGameObject;
            if (current != null && current.activeInHierarchy)
            {
                last = current;
                return;
            }

            if (last != null && last.activeInHierarchy)
                events.SetSelectedGameObject(last);
        }

        /// <summary>Selects an object now and remembers it as the fallback.</summary>
        public static void Focus(GameObject target)
        {
            if (EventSystem.current == null || target == null)
                return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target);
            if (EventSystem.current.TryGetComponent(out UIFocusGuard guard))
                guard.last = target;
        }
    }
}
