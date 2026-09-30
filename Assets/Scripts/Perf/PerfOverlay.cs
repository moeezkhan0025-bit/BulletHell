using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.Perf
{
    /// <summary>
    /// The performance overlay: FPS, frame time, p99, max, garbage per frame, object counts, and a frame-time graph (the
    /// last 128 frames, green up to 60 fps, yellow to 50, red beyond, guide lines at 60 and 30 fps). It has its own canvas,
    /// so it never dirties the HUD or the debug overlay canvas. The text refreshes at 4 Hz and the graph at 10 Hz into
    /// buffers created once; numbers are appended digit by digit, so the only per-update garbage is the final text string.
    /// </summary>
    public sealed class PerfOverlay : MonoBehaviour
    {
        private const int Bins = 128;
        private const int GraphHeight = 64;
        private const float PixelsPerMs = 1.5f;

        private GameObject canvasRoot;
        private TMP_Text label;
        private Texture2D texture;
        private Color32[] pixels;
        private readonly StringBuilder sb = new StringBuilder(384);
        private float nextText;
        private float nextGraph;

        public bool Visible => canvasRoot != null && canvasRoot.activeSelf;

        public void SetVisible(bool visible)
        {
            if (visible && canvasRoot == null)
                Build();
            if (canvasRoot != null)
                canvasRoot.SetActive(visible);
            nextText = nextGraph = 0f;
        }

        public void Toggle() => SetVisible(!Visible);

        public void Tick(PerfStats stats)
        {
            if (!Visible)
                return;
            float now = Time.unscaledTime;
            if (now >= nextText)
            {
                nextText = now + 0.25f;
                UpdateText(stats);
            }
            if (now >= nextGraph)
            {
                nextGraph = now + 0.1f;
                DrawGraph(stats);
            }
        }

        private void Build()
        {
            canvasRoot = new GameObject("PerfOverlayCanvas");
            canvasRoot.transform.SetParent(transform, false);
            var canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(canvasRoot.transform, false);
            label = textGo.AddComponent<TextMeshProUGUI>();
            label.fontSize = 22;
            label.alignment = TextAlignmentOptions.TopRight;
            label.color = Color.white;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.outlineWidth = 0.2f;
            label.outlineColor = new Color32(0, 0, 0, 230);
            Anchor((RectTransform)textGo.transform, new Vector2(-12f, -12f), new Vector2(560f, 130f));

            texture = new Texture2D(Bins, GraphHeight, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "PerfGraph" };
            pixels = new Color32[Bins * GraphHeight];
            var graphGo = new GameObject("Graph", typeof(RectTransform));
            graphGo.transform.SetParent(canvasRoot.transform, false);
            var image = graphGo.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            Anchor((RectTransform)graphGo.transform, new Vector2(-12f, -146f), new Vector2(Bins * 3f, GraphHeight * 3f));
        }

        private static void Anchor(RectTransform rect, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }

        private void UpdateText(PerfStats stats)
        {
            sb.Length = 0;
            sb.Append("FPS ");
            AppendInt(sb, Mathf.RoundToInt(stats.Fps));
            sb.Append("   ");
            AppendTenths(sb, stats.AvgMs);
            sb.Append(" ms   p99 ");
            AppendTenths(sb, stats.P99Ms);
            sb.Append("   max ");
            AppendTenths(sb, stats.MaxMs);
            sb.Append("\nGC ");
            AppendBytes(sb, stats.GcBytes);
            sb.Append("/frame   max ");
            AppendBytes(sb, stats.GcBytesMax);
            sb.Append("\nBullets ");
            AppendInt(sb, stats.Bullets);
            sb.Append("  pool ");
            AppendInt(sb, stats.BulletsPooled);
            sb.Append("  made ");
            AppendInt(sb, stats.BulletsCreated);
            sb.Append("\nEnemies ");
            AppendInt(sb, stats.Enemies);
            sb.Append("  particles ");
            AppendInt(sb, stats.Particles);
            label.text = sb.ToString();
        }

        private void DrawGraph(PerfStats stats)
        {
            var background = new Color32(0, 0, 0, 150);
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = background;

            for (int x = 0; x < Bins; x++)
            {
                int framesAgo = Bins - 1 - x;
                if (framesAgo >= stats.Count)
                    continue;
                float ms = stats.FrameMsAgo(framesAgo);
                int height = Mathf.Min(GraphHeight, Mathf.RoundToInt(ms * PixelsPerMs));
                Color32 color = ms <= 16.9f ? new Color32(80, 220, 110, 255) : ms <= 20f ? new Color32(240, 210, 60, 255) : new Color32(240, 70, 60, 255);
                for (int y = 0; y < height; y++)
                    pixels[y * Bins + x] = color;
            }

            var guide = new Color32(255, 255, 255, 140);
            DrawGuide(Mathf.RoundToInt(16.7f * PixelsPerMs), guide);
            DrawGuide(Mathf.RoundToInt(33.3f * PixelsPerMs), guide);
            texture.SetPixels32(pixels);
            texture.Apply(false);
        }

        private void DrawGuide(int row, Color32 color)
        {
            if (row < 0 || row >= GraphHeight)
                return;
            for (int x = 0; x < Bins; x++)
                pixels[row * Bins + x] = color;
        }

        // Digit-by-digit appends: StringBuilder.Append(int / float) can allocate on this runtime.
        private static void AppendInt(StringBuilder builder, int value)
        {
            if (value < 0)
            {
                builder.Append('-');
                value = -value;
            }
            if (value >= 10)
                AppendInt(builder, value / 10);
            builder.Append((char)('0' + value % 10));
        }

        private static void AppendTenths(StringBuilder builder, float value)
        {
            int tenths = Mathf.RoundToInt(Mathf.Max(0f, value) * 10f);
            AppendInt(builder, tenths / 10);
            builder.Append('.');
            builder.Append((char)('0' + tenths % 10));
        }

        private static void AppendBytes(StringBuilder builder, long bytes)
        {
            if (bytes < 1024)
            {
                AppendInt(builder, (int)bytes);
                builder.Append(" B");
                return;
            }
            AppendTenths(builder, bytes / 1024f);
            builder.Append(" KB");
        }
    }
}
