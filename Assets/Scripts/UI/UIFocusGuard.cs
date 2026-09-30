using UnityEngine;
using UnityEngine.EventSystems;

namespace BulletHell.UI
{
    /// <summary>
    /// Keeps a menu controller-navigable: if nothing is selected (a mouse click on empty space, the selected button
    /// got disabled) the last valid selection is restored, so the stick or D-pad always has somewhere to start.
    /// Also plays the UI focus sound when the selection moves. Put on the EventSystem object.
    /// </summary>
    public sealed class UIFocusGuard : MonoBehaviour
    {
        private GameObject last;
        private GameObject announced;

        private void Update()
        {
            EventSystem events = EventSystem.current;
            if (events == null)
                return;

            GameObject current = events.currentSelectedGameObject;
            if (current != null && current.activeInHierarchy)
            {
                last = current;
                if (current != announced)
                {
                    // The first selection after nothing was selected is a screen opening, not the player moving.
                    if (announced != null)
                        UiSound.Play(UiSoundKind.Focus);
                    announced = current;
                }
                return;
            }

            announced = null;
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
            {
                guard.last = target;
                guard.announced = target;
            }
        }

        /// <summary>The object that had focus before it was lost or moved (used to return focus after a dialog).</summary>
        public static GameObject Remembered
        {
            get
            {
                if (EventSystem.current != null && EventSystem.current.TryGetComponent(out UIFocusGuard guard))
                    return guard.last;
                return null;
            }
        }
    }
}
