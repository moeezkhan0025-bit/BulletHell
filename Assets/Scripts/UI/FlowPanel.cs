using System;
using PrimeTween;
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

        private CanvasGroup group;

        private void Awake()
        {
            if (!TryGetComponent(out group))
                group = gameObject.AddComponent<CanvasGroup>();
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
            // Screen transition: fades and settles in. Unscaled, because time is frozen on these screens.
            Tween.StopAll(group);
            Tween.StopAll(transform);
            group.alpha = 0f;
            transform.localScale = Vector3.one * 0.95f;
            Tween.Alpha(group, 1f, 0.2f, Ease.OutQuad, useUnscaledTime: true);
            Tween.Scale(transform, 1f, 0.25f, Ease.OutBack, useUnscaledTime: true);
            UIFocusGuard.Focus((showContinue ? continueButton : menuButton).gameObject);
        }

        public void Hide() => gameObject.SetActive(false);

        private void OnDisable()
        {
            Tween.StopAll(group);
            Tween.StopAll(transform);
        }
    }
}
