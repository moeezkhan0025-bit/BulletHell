using PrimeTween;
using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>
    /// The one transition every screen uses when it appears: a short fade plus a small upward slide (PrimeTween, unscaled
    /// time because the game is often paused). Fast enough never to feel slow; nothing waits for it, so input works from
    /// the first frame. Screens call <see cref="In"/> instead of SetActive(true).
    /// </summary>
    public sealed class ScreenTransition : MonoBehaviour
    {
        public const float Seconds = 0.2f;   // fallback; the theme (manifest: fade/slide 0.2 s) wins
        public const float SlidePixels = 26f;

        private CanvasGroup group;
        private RectTransform rect;
        private Vector2 rest;
        private bool ready;

        private void Prepare()
        {
            if (ready)
                return;
            ready = true;
            rect = transform as RectTransform;
            if (rect != null)
                rest = rect.anchoredPosition;
            if (!TryGetComponent(out group))
                group = gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>Activates the object and plays the fade + slide.</summary>
        public static void In(GameObject screen)
        {
            if (!screen.TryGetComponent(out ScreenTransition transition))
            {
                // The rest position must be read before the object is moved, so prepare while it is still where the scene put it.
                transition = screen.AddComponent<ScreenTransition>();
                transition.Prepare();
            }
            screen.SetActive(true);
            transition.Play();
        }

        private void Play()
        {
            Prepare();
            UITheme theme = UITheme.Current;
            float seconds = theme != null ? theme.ScreenTransitionSeconds : Seconds;
            Tween.StopAll(group);
            if (rect != null)
            {
                Tween.StopAll(rect);
                rect.anchoredPosition = rest + new Vector2(0f, -SlidePixels);
                Tween.UIAnchoredPosition(rect, rest, seconds, Ease.OutCubic, useUnscaledTime: true);
            }
            group.alpha = 0f;
            Tween.Alpha(group, 1f, seconds, Ease.OutQuad, useUnscaledTime: true);
        }

        private void OnDisable()
        {
            if (!ready)
                return;
            Tween.StopAll(group);
            if (rect != null)
            {
                Tween.StopAll(rect);
                rect.anchoredPosition = rest;
            }
            group.alpha = 1f;
        }
    }
}
