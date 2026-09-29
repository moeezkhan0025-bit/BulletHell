using UnityEngine;

namespace BulletHell.Arena
{
    /// <summary>
    /// Keeps the fixed camera on exactly one aspect ratio (the painted arena's, 16:9): the camera's viewport is the
    /// largest centred rectangle of that aspect, and a second camera clears the bars around it. So the whole backdrop
    /// is always visible and centred, at any window shape from 4:3 to 21:9 and on phones. The HUD asks
    /// <see cref="NormalizedViewport"/> so it stays inside the same rectangle.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class CameraLetterbox : MonoBehaviour
    {
        /// <summary>The camera's viewport in normalised screen space (0..1). The whole screen when no letterbox is active.</summary>
        public static Rect NormalizedViewport { get; private set; } = new Rect(0f, 0f, 1f, 1f);

        [SerializeField] private Color barColor = new Color(0.1f, 0.07f, 0.09f);

        private Camera viewCamera;
        private Camera barCamera;
        private float targetAspect = 16f / 9f;
        private Vector2Int appliedScreen;

        public void SetTargetAspect(float aspect)
        {
            if (aspect > 0.1f && !Mathf.Approximately(aspect, targetAspect))
            {
                targetAspect = aspect;
                appliedScreen = Vector2Int.zero;
            }
        }

        private void Awake()
        {
            viewCamera = GetComponent<Camera>();
            var go = new GameObject("LetterboxBars");
            go.transform.SetParent(transform, false);
            barCamera = go.AddComponent<Camera>();
            barCamera.clearFlags = CameraClearFlags.SolidColor;
            barCamera.backgroundColor = barColor;
            barCamera.cullingMask = 0;
            barCamera.orthographic = true;
            barCamera.depth = viewCamera.depth - 1f;
            barCamera.allowHDR = false;
            barCamera.allowMSAA = false;
        }

        private void OnDestroy() => NormalizedViewport = new Rect(0f, 0f, 1f, 1f);

        private void LateUpdate()
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (screen == appliedScreen || screen.x <= 0 || screen.y <= 0)
                return;
            appliedScreen = screen;

            float screenAspect = screen.x / (float)screen.y;
            Rect rect;
            if (screenAspect > targetAspect)
            {
                float w = targetAspect / screenAspect;
                rect = new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
            else
            {
                float h = screenAspect / targetAspect;
                rect = new Rect(0f, (1f - h) * 0.5f, 1f, h);
            }
            viewCamera.rect = rect;
            NormalizedViewport = rect;
        }
    }
}
