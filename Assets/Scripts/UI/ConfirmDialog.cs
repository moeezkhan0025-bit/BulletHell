using System;
using TMPro;
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
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        [SerializeField] private TMP_Text yesLabel;
        [SerializeField] private TMP_Text noLabel;

        private static int openCount;
        private Action onYes;
        private Action onNo;
        private GameObject returnFocus;
        private bool open;
        private bool notifyOnly;
        private Vector2 yesAnchored;
        private Vector2 yesAnchorMin, yesAnchorMax, yesPivot;
        private bool yesCached;

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
            SetNotifyMode(false);
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

        /// <summary>
        /// A message with one OK button (focused). OK and Circle both close it, run onClosed, and put focus back where it was; the dim behind
        /// blocks the mouse like every dialog.
        /// </summary>
        public void Notify(string title, string message, string ok, Action onClosed = null)
        {
            SetNotifyMode(true);
            titleLabel.text = title;
            messageLabel.text = message;
            yesLabel.text = ok;
            onYes = onClosed;
            onNo = onClosed;

            returnFocus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (!open)
            {
                open = true;
                openCount++;
            }
            transform.SetAsLastSibling();
            ScreenTransition.In(gameObject);
            UIFocusGuard.Focus(yesButton.gameObject);
        }

        // One button centred, or the normal pair.
        private void SetNotifyMode(bool notify)
        {
            var rect = (RectTransform)yesButton.transform;
            if (!yesCached)
            {
                yesCached = true;
                yesAnchored = rect.anchoredPosition;
                yesAnchorMin = rect.anchorMin;
                yesAnchorMax = rect.anchorMax;
                yesPivot = rect.pivot;
            }
            notifyOnly = notify;
            noButton.gameObject.SetActive(!notify);
            if (notify)
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, yesAnchored.y);
            }
            else
            {
                rect.anchorMin = yesAnchorMin;
                rect.anchorMax = yesAnchorMax;
                rect.pivot = yesPivot;
                rect.anchoredPosition = yesAnchored;
            }
        }

        private void Answer(bool yes)
        {
            if (!gameObject.activeSelf)
                return;
            Close();
            Action action = yes ? onYes : onNo;
            onYes = onNo = null;
            action?.Invoke();
            if ((!yes || notifyOnly) && returnFocus != null && returnFocus.activeInHierarchy)
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
