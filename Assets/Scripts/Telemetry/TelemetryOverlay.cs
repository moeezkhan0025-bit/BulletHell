using System.Collections.Generic;
using System.Text;
using BulletHell.Core;
using BulletHell.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BulletHell.Telemetry
{
    /// <summary>
    /// Debug summary screen for the playtest log: averages and distributions over every logged run (round reached, combat time, damage,
    /// currency, purchases, cause of death, boss phase, time and damage per round). Development builds and the editor only. It builds its
    /// own canvas when opened (F9, or the Stats button on the Pause screen), reads the CSV fresh each time, and closes with its
    /// Close button or F9. Runs that used a debug tool are left out unless "Include debug runs" is pressed.
    /// </summary>
    public sealed class TelemetryOverlay : MonoBehaviour
    {
        private const float ColumnWidth = 540f;
        private const float BarMaxWidth = 190f;
        private const float LabelWidth = 240f;
        private const float RowHeight = 30f;
        private const int MaxNameChars = 24;
        private const int MaxHistogramRounds = 9;
        private const int MaxPerRoundRows = 10;

        private static TelemetryOverlay instance;

        private readonly StringBuilder text = new StringBuilder(256);
        private RectTransform leftColumn;
        private RectTransform middleColumn;
        private RectTransform rightColumn;
        private TMP_Text header;
        private TMP_Text pathLabel;
        private TMP_Text includeLabel;
        private Button closeButton;
        private bool includeDebug;
        private List<RunRecord> records = new List<RunRecord>();
        private string csvPath = "";

        public static bool IsOpen => instance != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        /// <summary>Opens the screen, or closes it when it is open. Does nothing in release builds.</summary>
        public static void Toggle()
        {
            if (!Debug.isDebugBuild)
                return;
            if (instance != null)
            {
                Destroy(instance.gameObject);
                instance = null;
                return;
            }

            var go = new GameObject("TelemetryOverlay");
            instance = go.AddComponent<TelemetryOverlay>();
            instance.Build();
            instance.Refresh();
        }

        public static void Close()
        {
            if (instance != null)
                Toggle();
        }

        private void Refresh()
        {
            TelemetryService service = GameServices.Ensure().Telemetry;
            csvPath = service.CsvPath;
            records = service.ReadAll();
            Rebuild();
        }

        // ------------------------------------------------------------------ content

        private void Rebuild()
        {
            Clear(leftColumn);
            Clear(middleColumn);
            Clear(rightColumn);
            TelemetrySummary s = TelemetrySummary.Compute(records, includeDebug);

            pathLabel.text = csvPath;
            includeLabel.text = includeDebug ? "Debug runs: included" : "Debug runs: left out";
            if (s.Counted == 0)
            {
                header.text = s.Logged == 0
                    ? "No runs logged yet. A row is written when a run ends (death, or leaving it)."
                    : s.Logged + " runs logged, all of them used a debug tool. Press \"Debug runs\" to include them.";
                return;
            }
            header.text = s.Counted + " runs counted   " + s.Died + " died   " + s.Quit + " quit"
                          + (s.DebugExcluded > 0 ? "   (" + s.DebugExcluded + " debug runs left out)" : "");

            Section(leftColumn, "AVERAGES");
            Line(leftColumn, "Round reached", Stat3(s.RoundReached, "0.0") + "   best " + Mathf.RoundToInt(s.RoundReached.Max));
            Line(leftColumn, "Combat time per run", Duration(s.CombatSeconds.Mean) + "   median " + Duration(s.CombatSeconds.Median));
            Line(leftColumn, "Damage taken per run", Stat3(s.DamageTaken, "0.0"));
            Line(leftColumn, "Currency earned / spent", Mathf.RoundToInt(s.CurrencyEarned.Mean) + " / " + Mathf.RoundToInt(s.CurrencySpent.Mean));
            Line(leftColumn, "Items bought per run", s.ItemsBought.Mean.ToString("0.0"));
            Line(leftColumn, "Armaments equipped at end", s.ArmamentsEquipped.Mean.ToString("0.0"));

            Section(middleColumn, "WHERE RUNS END (round reached)");
            var histogram = new SortedDictionary<int, int>();
            foreach (var pair in s.RoundHistogram)
            {
                int bucket = Mathf.Min(pair.Key, MaxHistogramRounds);   // the last row is "9+"
                histogram[bucket] = histogram.TryGetValue(bucket, out int have) ? have + pair.Value : pair.Value;
            }
            int histMax = 1;
            foreach (var pair in histogram)
                histMax = Mathf.Max(histMax, pair.Value);
            foreach (var pair in histogram)
                Bar(middleColumn, "Round " + pair.Key + (pair.Key == MaxHistogramRounds ? "+" : ""), pair.Value, histMax, pair.Value + " run" + (pair.Value == 1 ? "" : "s"));

            Section(middleColumn, "BOSS PHASE REACHED");
            string[] phaseNames = { "No boss met", "Phase 1", "Phase 2", "Phase 3+" };
            int phaseMax = 1;
            foreach (int n in s.BossPhases)
                phaseMax = Mathf.Max(phaseMax, n);
            for (int i = 0; i < s.BossPhases.Length; i++)
                Bar(middleColumn, phaseNames[i], s.BossPhases[i], phaseMax, s.BossPhases[i].ToString());
            Line(middleColumn, "Bosses beaten", s.BossesDefeated.ToString());

            Section(rightColumn, "CAUSE OF DEATH");
            if (s.Causes.Count == 0)
                Line(rightColumn, "", "no deaths logged");
            int causeMax = 1;
            foreach (var pair in s.Causes)
                causeMax = Mathf.Max(causeMax, pair.Value);
            for (int i = 0; i < s.Causes.Count && i < 8; i++)
                Bar(rightColumn, s.Causes[i].Key, s.Causes[i].Value, causeMax, s.Causes[i].Value.ToString());

            Section(rightColumn, "PER ROUND (averages)");
            float damageMax = 0.1f;
            foreach (RoundAverage r in s.PerRound)
                damageMax = Mathf.Max(damageMax, r.Damage);
            int shown = 0;
            foreach (RoundAverage r in s.PerRound)
            {
                if (shown++ >= MaxPerRoundRows)
                    break;
                Bar(rightColumn, "R" + r.Round + "  " + Duration(r.Seconds) + "  (" + r.Runs + (r.Runs == 1 ? " run)" : " runs)"), r.Damage, damageMax,
                    r.Damage.ToString("0.0") + " dmg", true);
            }
        }

        private static string Stat3(Stat stat, string format) =>
            "avg " + stat.Mean.ToString(format) + "   median " + stat.Median.ToString(format);

        private static string Duration(float seconds)
        {
            int total = Mathf.RoundToInt(seconds);
            return total >= 60 ? (total / 60) + "m " + (total % 60).ToString("00") + "s" : total + "s";
        }

        // ------------------------------------------------------------------ layout helpers

        private static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        private void Section(RectTransform column, string title)
        {
            TMP_Text label = Label(column, title, 22, TextFont.BodyBold, TextTone.Muted, TextAlignmentOptions.Left);
            label.GetComponent<ThemedText>().Caps = true;
            label.margin = new Vector4(0f, 14f, 0f, 2f);
        }

        private void Line(RectTransform column, string name, string value)
        {
            text.Clear();
            if (name.Length > 0)
                text.Append("<b>").Append(name).Append("</b>   ");
            text.Append(value);
            Label(column, text.ToString(), 22, TextFont.Body, TextTone.OnPanel, TextAlignmentOptions.Left).richText = true;
        }

        private void Bar(RectTransform column, string name, float value, float max, string valueText, bool warm = false)
        {
            RectTransform row = NewRect("Bar", column);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = RowHeight;
            row.gameObject.GetComponent<LayoutElement>().minHeight = RowHeight;
            if (name.Length > MaxNameChars)
                name = name.Substring(0, MaxNameChars - 2) + "..";
            TMP_Text label = Label(row, name, 22, TextFont.Body, TextTone.OnPanel, TextAlignmentOptions.Left);
            label.textWrappingMode = TextWrappingModes.NoWrap;   // Overflow, not Ellipsis: a row squeezed in height must not blank its text
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(LabelWidth, 0f);
            labelRect.anchoredPosition = Vector2.zero;
            label.gameObject.GetComponent<LayoutElement>().ignoreLayout = true;

            float width = max > 0f ? Mathf.Max(value > 0f ? 6f : 0f, BarMaxWidth * value / max) : 0f;
            RectTransform bar = NewRect("Fill", row);
            var image = bar.gameObject.AddComponent<Image>();
            UITheme theme = UITheme.Current;
            image.color = theme != null ? (warm ? theme.Carrot : theme.Leaf) : Color.green;
            image.raycastTarget = false;
            bar.anchorMin = bar.anchorMax = new Vector2(0f, 0.5f);
            bar.pivot = new Vector2(0f, 0.5f);
            bar.anchoredPosition = new Vector2(LabelWidth + 10f, 0f);
            bar.sizeDelta = new Vector2(width, 18f);
            bar.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            TMP_Text number = Label(row, valueText, 20, TextFont.BodyBold, TextTone.Muted, TextAlignmentOptions.Left);
            number.textWrappingMode = TextWrappingModes.NoWrap;
            RectTransform numberRect = number.rectTransform;
            numberRect.anchorMin = numberRect.anchorMax = new Vector2(0f, 0.5f);
            numberRect.pivot = new Vector2(0f, 0.5f);
            numberRect.anchoredPosition = new Vector2(LabelWidth + 10f + width + 8f, 0f);
            numberRect.sizeDelta = new Vector2(150f, 26f);
            number.gameObject.GetComponent<LayoutElement>().ignoreLayout = true;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static TMP_Text Label(Transform parent, string content, float size, TextFont font, TextTone tone, TextAlignmentOptions align)
        {
            RectTransform rect = NewRect("Text", parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = content;
            label.fontSize = size;
            label.alignment = align;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            var themed = rect.gameObject.AddComponent<ThemedText>();
            themed.Font = font;
            themed.Tone = tone;
            rect.gameObject.AddComponent<LayoutElement>();
            return label;
        }

        private Button MakeButton(Transform parent, string caption, float width, float height, System.Action onClick, out TMP_Text captionLabel)
        {
            RectTransform rect = NewRect(caption, parent);
            rect.sizeDelta = new Vector2(width, height);
            var image = rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            rect.gameObject.AddComponent<ThemedButton>();
            captionLabel = Label(rect, caption, 30, TextFont.Button, TextTone.OnPanel, TextAlignmentOptions.Center);
            captionLabel.textWrappingMode = TextWrappingModes.NoWrap;
            RectTransform labelRect = captionLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            captionLabel.gameObject.GetComponent<LayoutElement>().ignoreLayout = true;
            button.onClick.AddListener(() => onClick());
            rect.GetComponent<ThemedButton>().Apply();
            return button;
        }

        // ------------------------------------------------------------------ frame

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(gameObject);

            var dim = gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.78f);

            RectTransform panel = NewRect("Panel", transform);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1760f, 1000f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            var themedPanel = panel.gameObject.AddComponent<ThemedImage>();
            themedPanel.Role = ThemeRole.Panel;

            TMP_Text title = Label(panel, "PLAYTEST SUMMARY", 54, TextFont.Title, TextTone.OnPanel, TextAlignmentOptions.Center);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            Place(title.rectTransform, 0f, 1f, 1f, 1f, new Vector2(0f, -36f), new Vector2(0f, 70f));
            title.gameObject.GetComponent<LayoutElement>().ignoreLayout = true;

            header = Label(panel, "", 26, TextFont.BodyBold, TextTone.OnPanel, TextAlignmentOptions.Center);
            Place(header.rectTransform, 0f, 1f, 1f, 1f, new Vector2(0f, -112f), new Vector2(-80f, 40f));
            header.gameObject.GetComponent<LayoutElement>().ignoreLayout = true;

            pathLabel = Label(panel, "", 20, TextFont.Body, TextTone.Muted, TextAlignmentOptions.Center);
            pathLabel.textWrappingMode = TextWrappingModes.NoWrap;
            pathLabel.overflowMode = TextOverflowModes.Ellipsis;
            Place(pathLabel.rectTransform, 0f, 0f, 1f, 0f, new Vector2(0f, 96f), new Vector2(-80f, 32f));
            pathLabel.gameObject.GetComponent<LayoutElement>().ignoreLayout = true;

            leftColumn = Column(panel, "Left", -ColumnWidth - 40f);
            middleColumn = Column(panel, "Middle", 0f);
            rightColumn = Column(panel, "Right", ColumnWidth + 40f);

            closeButton = MakeButton(panel, "Close", 320f, 72f, Toggle, out _);
            Place((RectTransform)closeButton.transform, 0.5f, 0f, 0.5f, 0f, new Vector2(300f, 24f), new Vector2(320f, 72f));
            Button include = MakeButton(panel, "Debug runs", 360f, 72f, () =>
            {
                includeDebug = !includeDebug;
                Rebuild();
            }, out includeLabel);
            Place((RectTransform)include.transform, 0.5f, 0f, 0.5f, 0f, new Vector2(-300f, 24f), new Vector2(360f, 72f));
            includeLabel.fontSize = 26;

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }

        private static void Place(RectTransform rect, float minX, float minY, float maxX, float maxY, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.pivot = new Vector2(0.5f, minY >= 1f ? 1f : 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private RectTransform Column(Transform parent, string name, float x)
        {
            RectTransform rect = NewRect(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -160f);
            rect.sizeDelta = new Vector2(ColumnWidth, 690f);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
        }
    }
}
