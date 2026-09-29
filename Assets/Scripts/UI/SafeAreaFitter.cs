using BulletHell.Arena;
using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>
    /// Fits a full-screen RectTransform to Screen.safeArea (phone notches, rounded corners), intersected with the
    /// letterboxed arena viewport so the HUD always sits inside the 16:9 picture and never over the bars.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rect;
        private Rect appliedArea;
        private Rect appliedViewport;
        private Vector2Int appliedScreen;

        private void Awake() => rect = (RectTransform)transform;

        private void Update()
        {
            Rect area = Screen.safeArea;
            Rect viewport = CameraLetterbox.NormalizedViewport;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (area == appliedArea && screen == appliedScreen && viewport == appliedViewport)
                return;

            appliedArea = area;
            appliedViewport = viewport;
            appliedScreen = screen;
            Vector2 min = new Vector2(Mathf.Max(area.xMin / screen.x, viewport.xMin), Mathf.Max(area.yMin / screen.y, viewport.yMin));
            Vector2 max = new Vector2(Mathf.Min(area.xMax / screen.x, viewport.xMax), Mathf.Min(area.yMax / screen.y, viewport.yMax));
            rect.anchorMin = min;
            rect.anchorMax = Vector2.Max(min, max);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
