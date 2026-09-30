using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// One full-screen between-rounds screen (Round Results, Pause, Game Over): a title, some text, a Continue button and a
    /// Menu button. Opens with the transition, focuses its main button, and reports Circle as <see cref="CancelPressed"/>
    /// so the owner decides what "back" means here (Pause: resume).
    /// </summary>
    public sealed class FlowPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button menuButton;
        [Tooltip("Optional (the Pause panel has one).")]
        [SerializeField] private Button settingsButton;
        [Tooltip("Optional: the button prompt line at the bottom.")]
        [SerializeField] private TMP_Text hintLabel;

        public event Action ContinuePressed;
        public event Action MenuPressed;
        public event Action SettingsPressed;
        /// <summary>Circle / Cancel while this panel has focus.</summary>
        public event Action CancelPressed;

        private void Awake()
        {
            continueButton.onClick.AddListener(() => ContinuePressed?.Invoke());
            menuButton.onClick.AddListener(() => MenuPressed?.Invoke());
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => SettingsPressed?.Invoke());

            foreach (Button button in new[] { continueButton, menuButton, settingsButton })
            {
                if (button == null)
                    continue;
                if (!button.TryGetComponent(out CancelRelay relay))
                    relay = button.gameObject.AddComponent<CancelRelay>();
                relay.Cancelled += () => CancelPressed?.Invoke();
            }
        }

        /// <param name="backLabel">What Circle does on this panel (shown in the prompt line); null = Circle does nothing here.</param>
        public void Show(string titleText, string bodyText, bool showContinue = true, string backLabel = null)
        {
            title.text = titleText;
            body.text = bodyText;
            continueButton.gameObject.SetActive(showContinue);
            ScreenTransition.In(gameObject);
            UIFocusGuard.Focus((showContinue ? continueButton : menuButton).gameObject);

            if (backLabel != null)
                PromptHint.Show(hintLabel, PromptHint.P(UiAction.Confirm, "Select"), PromptHint.P(UiAction.Back, backLabel));
            else
                PromptHint.Show(hintLabel, PromptHint.P(UiAction.Confirm, "Select"));
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
