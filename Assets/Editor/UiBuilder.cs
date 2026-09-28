using BulletHell.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.EditorTools
{
    /// <summary>Helpers that build plain skeleton uGUI (legacy Text, no art) from editor setup scripts.</summary>
    public static class UiBuilder
    {
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.78f);
        public static readonly Color BoxColor = new Color(0.12f, 0.12f, 0.16f, 0.96f);

        private static Font font;

        private static Font Font => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void Center(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>A full-screen dimmed backdrop that also blocks clicks to whatever is behind it.</summary>
        public static RectTransform CreateDimmer(string name, Transform parent)
        {
            RectTransform rect = CreateRect(name, parent);
            Stretch(rect);
            rect.gameObject.AddComponent<Image>().color = Dim;
            return rect;
        }

        /// <summary>A centered box with a vertical layout; children stretch to its width.</summary>
        public static RectTransform CreateVerticalBox(string name, Transform parent, Vector2 size, float spacing, int padding)
        {
            RectTransform rect = CreateRect(name, parent);
            Center(rect, size);
            rect.gameObject.AddComponent<Image>().color = BoxColor;
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        public static Text CreateText(string name, Transform parent, string text, int size, TextAnchor anchor, float minHeight = 0f)
        {
            RectTransform rect = CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.alignment = anchor;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            if (minHeight > 0f)
                rect.gameObject.AddComponent<LayoutElement>().minHeight = minHeight;
            return label;
        }

        /// <summary>The pieces of a list screen (Shop, Armory) that its component needs wired.</summary>
        public sealed class ListScreenParts
        {
            public RectTransform Root;
            public Text Title;
            public Text Info;
            public UIList List;
            public GameObject Footer;
            public Button Continue;
            public Button Menu;
        }

        /// <summary>
        /// Full-screen list screen: title, info text, a scrolling list of rows (which follows controller focus), and a
        /// footer with Continue and Main Menu buttons.
        /// </summary>
        public static ListScreenParts CreateListScreen(string name, Transform parent, MenuRow rowPrefab, string continueLabel)
        {
            var parts = new ListScreenParts { Root = CreateDimmer(name, parent) };
            RectTransform box = CreateVerticalBox("Box", parts.Root, new Vector2(1240f, 1000f), 16f, 34);
            parts.Title = CreateText("Title", box, "", 52, TextAnchor.MiddleCenter, 80f);
            parts.Info = CreateText("Info", box, "", 28, TextAnchor.UpperCenter, 120f);

            RectTransform scrollRoot = CreateRect("List", box);
            scrollRoot.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.3f);
            var layout = scrollRoot.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 300f;
            layout.flexibleHeight = 1f;

            RectTransform viewport = CreateRect("Viewport", scrollRoot);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var rows = content.gameObject.AddComponent<VerticalLayoutGroup>();
            rows.spacing = 8f;
            rows.padding = new RectOffset(12, 12, 12, 12);
            rows.childControlWidth = rows.childControlHeight = true;
            rows.childForceExpandWidth = true;
            rows.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var follow = scrollRoot.gameObject.AddComponent<ScrollFollowSelection>();
            new UnityEditor.SerializedObject(follow).Also(so => so.FindProperty("scroll").objectReferenceValue = scroll);

            parts.List = scrollRoot.gameObject.AddComponent<UIList>();
            new UnityEditor.SerializedObject(parts.List).Also(so =>
            {
                so.FindProperty("rowPrefab").objectReferenceValue = rowPrefab;
                so.FindProperty("content").objectReferenceValue = content;
            });

            RectTransform footer = CreateRect("Footer", box);
            var footerLayout = footer.gameObject.AddComponent<VerticalLayoutGroup>();
            footerLayout.spacing = 12f;
            footerLayout.childControlWidth = footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = true;
            footerLayout.childForceExpandHeight = false;
            parts.Footer = footer.gameObject;
            parts.Continue = CreateButton("Continue", footer, continueLabel, 90f);
            parts.Menu = CreateButton("MainMenu", footer, "Main Menu", 70f);
            return parts;
        }

        /// <summary>A list row: full-width button with a left-aligned dark label and a MenuRow component.</summary>
        public static GameObject CreateMenuRow()
        {
            RectTransform rect = CreateRect("MenuRow", null);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(0.82f, 0.84f, 0.9f);
            colors.highlightedColor = new Color(1f, 0.92f, 0.6f);
            colors.selectedColor = new Color(1f, 0.8f, 0.2f);
            colors.pressedColor = new Color(0.9f, 0.6f, 0.1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.6f);
            button.colors = colors;
            rect.gameObject.AddComponent<LayoutElement>().minHeight = 64f;

            Text label = CreateText("Label", rect, "", 28, TextAnchor.MiddleLeft);
            label.color = Color.black;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            Stretch((RectTransform)label.transform);
            ((RectTransform)label.transform).offsetMin = new Vector2(20f, 0f);

            var row = rect.gameObject.AddComponent<MenuRow>();
            var so = new UnityEditor.SerializedObject(row);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
            return rect.gameObject;
        }

        private static void Also(this UnityEditor.SerializedObject so, System.Action<UnityEditor.SerializedObject> edit)
        {
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A button with a dark label; the selected colour is bright so controller focus is obvious.</summary>
        public static Button CreateButton(string name, Transform parent, string label, float height)
        {
            RectTransform rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(0.82f, 0.84f, 0.9f);
            colors.highlightedColor = new Color(1f, 0.92f, 0.6f);
            colors.selectedColor = new Color(1f, 0.8f, 0.2f);
            colors.pressedColor = new Color(0.9f, 0.6f, 0.1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.6f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            rect.gameObject.AddComponent<LayoutElement>().minHeight = height;

            Text text = CreateText("Label", rect, label, 36, TextAnchor.MiddleCenter);
            text.color = Color.black;
            Stretch((RectTransform)text.transform);
            return button;
        }
    }
}
