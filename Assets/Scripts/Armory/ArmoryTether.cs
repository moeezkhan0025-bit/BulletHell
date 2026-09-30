using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.Armory
{
    /// <summary>
    /// A thin glowing line from an arm to its bubble. It re-aims every frame, so it follows the bobbing bubble, and its glow
    /// pulses slowly. Two stacked Images: a faint wide one (the glow) and a bright thin one (the strand).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ArmoryTether : MonoBehaviour
    {
        [SerializeField] private Image strand;
        [SerializeField] private Image glow;
        [SerializeField, Min(1f)] private float strandWidth = 5f;
        [SerializeField, Min(1f)] private float glowWidth = 16f;
        [SerializeField, Min(0f)] private float pulseSpeed = 2.2f;
        [SerializeField, Range(0f, 1f)] private float glowAlpha = 0.28f;
        [SerializeField, Range(0f, 1f)] private float strandAlpha = 0.85f;

        private RectTransform from;
        private RectTransform to;
        private Color color = Color.white;

        /// <summary>Links two objects; the line runs from `start` to `end` and is tinted by `tint`.</summary>
        public void Link(RectTransform start, RectTransform end, Color tint)
        {
            from = start;
            to = end;
            color = tint;
            gameObject.SetActive(true);
            Apply(1f);
        }

        public void Unlink()
        {
            from = to = null;
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (from == null || to == null)
                return;
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * pulseSpeed);
            Apply(pulse);
        }

        private void Apply(float pulse)
        {
            if (from == null || to == null)
                return;

            var rect = (RectTransform)transform;
            Vector3 a = from.position;
            Vector3 b = to.position;
            Vector3 delta = b - a;
            float scale = rect.lossyScale.x > 0f ? rect.lossyScale.x : 1f;
            float length = delta.magnitude / scale;

            rect.position = (a + b) * 0.5f;
            rect.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            rect.sizeDelta = new Vector2(length, Mathf.Max(strandWidth, glowWidth));

            Size(strand, length, strandWidth, strandAlpha);
            Size(glow, length, glowWidth * (0.9f + 0.2f * pulse), glowAlpha * pulse);
        }

        private void Size(Image image, float length, float width, float alpha)
        {
            var r = image.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(length, width);
            r.anchoredPosition = Vector2.zero;
            Color c = color;
            c.a = alpha;
            image.color = c;
        }
    }
}
