using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// Heat of the selected arm. Fills while firing heat ammo, tints leaf -> carrot -> tomato as it rises (UITheme heat
    /// colors), flashes tomato and marble when overheated, drains as it cools. With no arm selected it shows the last
    /// selected arm, dimmed. The fill is a sliced sprite whose right edge follows the heat (anchorMax.x), so its rounded
    /// ends never stretch.
    /// </summary>
    public sealed class HudHeatBar : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [Tooltip("Outline in the colour of the arm being shown. Leave empty to keep the themed track untinted.")]
        [SerializeField] private Image frame;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Color overheatColorB = new Color(0.957f, 0.933f, 0.863f);
        [SerializeField, Min(0.5f)] private float flashesPerSecond = 5f;
        [SerializeField, Range(0f, 1f)] private float dimmedAlpha = 0.55f;
        [Tooltip("Below this heat the fill is hidden (a sliced sprite cannot be narrower than its borders).")]
        [SerializeField, Range(0f, 0.2f)] private float minVisibleHeat = 0.03f;

        /// <summary>Heat01 is 0..1; armColor tints the frame; dimmed = no arm is selected right now.</summary>
        public void Refresh(float heat01, bool overheated, bool dimmed, Color armColor)
        {
            UITheme theme = UITheme.Current;
            float heat = Mathf.Clamp01(heat01);
            RectTransform rect = fill.rectTransform;
            rect.anchorMax = new Vector2(heat, rect.anchorMax.y);
            fill.enabled = heat >= minVisibleHeat;

            if (overheated)
            {
                bool on = Mathf.FloorToInt(Time.unscaledTime * flashesPerSecond * 2f) % 2 == 0;
                fill.color = on ? (theme != null ? theme.Tomato : Color.red) : overheatColorB;
            }
            else
            {
                fill.color = theme != null ? theme.GetHeatColor(heat) : Color.Lerp(Color.green, Color.red, heat);
            }
            group.alpha = dimmed ? dimmedAlpha : 1f;
        }
    }
}
