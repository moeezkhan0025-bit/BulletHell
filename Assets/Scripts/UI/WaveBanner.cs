using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>Big centred message such as "Wave 2/4". Pops in, shows for a set time, then fades out. Uses game time, so it freezes with pause.</summary>
    public sealed class WaveBanner : MonoBehaviour
    {
        private const float FadeSeconds = 0.4f;
        private const float PopSeconds = 0.25f;

        [SerializeField] private TMP_Text label;

        private Color baseColor;

        private void Awake()
        {
            baseColor = label.color;
            gameObject.SetActive(false);
        }

        public void Show(string main, string sub, float seconds)
        {
            label.text = string.IsNullOrEmpty(sub) ? main : main + "\n<size=48>" + sub + "</size>";
            Tween.StopAll(label);
            Tween.StopAll(label.transform);
            label.color = baseColor;
            label.transform.localScale = Vector3.one * 0.7f;
            gameObject.SetActive(true);

            Tween.Scale(label.transform, 1f, PopSeconds, Ease.OutBack);
            float fade = Mathf.Min(FadeSeconds, seconds);
            Tween.Alpha(label, baseColor.a, 0f, fade, Ease.Linear, startDelay: seconds - fade)
                .OnComplete(Hide);
        }

        public void Hide()
        {
            Tween.StopAll(label);
            Tween.StopAll(label.transform);
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            Tween.StopAll(label);
            Tween.StopAll(label.transform);
        }
    }
}
