using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>World-space health bar (two sprites under "visuals"). Hidden at full health and when dead.</summary>
    public sealed class HealthBar : MonoBehaviour
    {
        [SerializeField] private Health health;
        [Tooltip("Parent of the background and fill sprites; scaled to the bar width.")]
        [SerializeField] private Transform visuals;
        [Tooltip("Fill sprite, a child of visuals with unit width.")]
        [SerializeField] private Transform fill;

        public void Layout(float barWidth, float heightAbove)
        {
            transform.localPosition = new Vector3(0f, heightAbove, 0f);
            visuals.localScale = new Vector3(barWidth, visuals.localScale.y, 1f);
            Refresh();
        }

        private void Awake() => health.Changed += Refresh;

        private void OnDestroy()
        {
            if (health != null)
                health.Changed -= Refresh;
        }

        private void Refresh()
        {
            float fraction = health.Fraction;
            bool show = health.IsAlive && fraction < 1f;
            visuals.gameObject.SetActive(show);
            if (!show)
                return;
            // Fill spans local x in [-0.5, 0.5] of the scaled visuals; shrink it toward the left edge.
            fill.localScale = new Vector3(fraction, fill.localScale.y, 1f);
            fill.localPosition = new Vector3(-0.5f * (1f - fraction), 0f, -0.01f);
        }
    }
}
