using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// The shared yes / no dialog for destructive actions (start over a save, quit a run, quit the game). The safe answer
    /// ("No") has focus when it opens; Circle / Cancel answers No; a full-screen dim blocks the mouse behind it and
    /// <see cref="AnyOpen"/> stops the Start shortcut from confirming it. Focus returns to where it was afterwards.
    /// </summary>
    public sealed class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text messageLabel;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        [SerializeField] private Text yesLabel;
        [SerializeField] private Text noLabel;

        private static int openCount;
        private Action onYes;
        private Action onNo;
        private GameObject returnFocus;
        private bool open;

        /// <summary>True while any dialog is showing.</summary>
        public static bool AnyOpen => openCount > 0;
        public bool IsOpen => gameObject.activeSelf;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => openCount = 0;

        private void Awake()
        {
            yesButton.onClick.AddListener(() => Answer(true));
            noButton.onClick.AddListener(() => Answer(false));
            foreach (Selectable s in new Selectable[] { yesButton, noButton })
            {
                CancelRelay relay = s.GetComponent<CancelRelay>();
                if (relay == null)
                    relay = s.gameObject.AddComponent<CancelRelay>();
                relay.Cancelled += () => Answer(false);
            }
            Wire();
        }

        private void Wire()
        {
            yesButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = noButton, selectOnLeft = noButton };
            noButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = yesButton, selectOnRight = yesButton };
        }

        /// <summary>Opens the dialog. onNo is optional (Circle and "No" both run it).</summary>
        public void Ask(string title, string message, string yes, string no, Action confirmed, Action declined = null)
        {
            titleLabel.text = title;
            messageLabel.text = message;
            yesLabel.text = yes;
            noLabel.text = no;
            onYes = confirmed;
            onNo = declined;

            returnFocus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (!open)
            {
                open = true;
                openCount++;
            }
            transform.SetAsLastSibling();
            ScreenTransition.In(gameObject);
            UIFocusGuard.Focus(noButton.gameObject);
        }

        private void Answer(bool yes)
        {
            if (!gameObject.activeSelf)
                return;
            Close();
            Action action = yes ? onYes : onNo;
            onYes = onNo = null;
            action?.Invoke();
            if (!yes && returnFocus != null && returnFocus.activeInHierarchy)
                UIFocusGuard.Focus(returnFocus);
        }

        private void Close()
        {
            Release();
            gameObject.SetActive(false);
        }

        private void Release()
        {
            if (!open)
                return;
            open = false;
            openCount = Mathf.Max(0, openCount - 1);
        }

        private void OnDisable()
        {
            // Disabled from outside (a scene change): make sure the Start shortcut is not blocked forever.
            Release();
        }
    }
}
