using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>Big centred message such as "Wave 2/4". Shows for a set time, fading out at the end. Uses game time, so it freezes with pause.</summary>
    public sealed class WaveBanner : MonoBehaviour
    {
        private const float FadeSeconds = 0.4f;

        [SerializeField] private Text label;

        private float timeLeft;
        private float duration;
        private Color baseColor;

        private void Awake()
        {
            baseColor = label.color;
            gameObject.SetActive(false);
        }

        public void Show(string main, string sub, float seconds)
        {
            label.text = string.IsNullOrEmpty(sub) ? main : main + "\n<size=48>" + sub + "</size>";
            duration = timeLeft = seconds;
            label.color = baseColor;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void Update()
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }

            Color color = baseColor;
            color.a = baseColor.a * Mathf.Clamp01(timeLeft / Mathf.Min(FadeSeconds, duration));
            label.color = color;
        }
    }
}
