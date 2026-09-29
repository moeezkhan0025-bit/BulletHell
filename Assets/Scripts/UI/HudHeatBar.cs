using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// Heat of the selected arm. Fills while firing heat ammo, blends cool to hot, flashes when overheated, drains as it
    /// cools. With no arm selected it shows the last selected arm, dimmed.
    /// </summary>
    public sealed class HudHeatBar : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [Tooltip("Outline in the colour of the arm being shown.")]
        [SerializeField] private Image frame;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Color coolColor = new Color(0.3f, 0.85f, 1f);
        [SerializeField] private Color hotColor = new Color(1f, 0.55f, 0.1f);
        [SerializeField] private Color overheatColorA = new Color(1f, 0.15f, 0.1f);
        [SerializeField] private Color overheatColorB = Color.white;
        [SerializeField, Min(0.5f)] private float flashesPerSecond = 5f;
        [SerializeField, Range(0f, 1f)] private float dimmedAlpha = 0.55f;

        /// <summary>Heat01 is 0..1; armColor tints the frame; dimmed = no arm is selected right now.</summary>
        public void Refresh(float heat01, bool overheated, bool dimmed, Color armColor)
        {
            fill.fillAmount = Mathf.Clamp01(heat01);
            if (overheated)
            {
                bool on = Mathf.FloorToInt(Time.unscaledTime * flashesPerSecond * 2f) % 2 == 0;
                fill.color = on ? overheatColorA : overheatColorB;
            }
            else
            {
                fill.color = Color.Lerp(coolColor, hotColor, heat01);
            }
            if (frame != null)
                frame.color = armColor;
            group.alpha = dimmed ? dimmedAlpha : 1f;
        }
    }
}
