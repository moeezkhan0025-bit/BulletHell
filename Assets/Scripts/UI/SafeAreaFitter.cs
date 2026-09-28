using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>Fits a full-screen RectTransform to Screen.safeArea (phone notches, rounded corners).</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rect;
        private Rect appliedArea;
        private Vector2Int appliedScreen;

        private void Awake() => rect = (RectTransform)transform;

        private void Update()
        {
            Rect area = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (area == appliedArea && screen == appliedScreen)
                return;

            appliedArea = area;
            appliedScreen = screen;
            rect.anchorMin = new Vector2(area.xMin / screen.x, area.yMin / screen.y);
            rect.anchorMax = new Vector2(area.xMax / screen.x, area.yMax / screen.y);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
