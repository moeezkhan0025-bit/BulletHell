using System;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// One full-screen between-rounds screen (Round Results, Shop, Armory): a title, some text, a Continue button and a
    /// Menu button. Skeleton UI; the real Shop and Armory replace the body later.
    /// </summary>
    public sealed class FlowPanel : MonoBehaviour
    {
        [SerializeField] private Text title;
        [SerializeField] private Text body;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button menuButton;
        [Tooltip("Optional (the Pause panel has one).")]
        [SerializeField] private Button settingsButton;

        public event Action ContinuePressed;
        public event Action MenuPressed;
        public event Action SettingsPressed;

        private void Awake()
        {
            continueButton.onClick.AddListener(() => ContinuePressed?.Invoke());
            menuButton.onClick.AddListener(() => MenuPressed?.Invoke());
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => SettingsPressed?.Invoke());
        }

        public void Show(string titleText, string bodyText, bool showContinue = true)
        {
            title.text = titleText;
            body.text = bodyText;
            continueButton.gameObject.SetActive(showContinue);
            gameObject.SetActive(true);
            UIFocusGuard.Focus((showContinue ? continueButton : menuButton).gameObject);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
