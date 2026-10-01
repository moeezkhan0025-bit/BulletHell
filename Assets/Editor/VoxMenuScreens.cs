using System.Collections.Generic;
using BulletHell.Core;
using BulletHell.Cosmetics;
using BulletHell.Input;
using BulletHell.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// Builds the VOX VEGETALLIS look of the front-end screens (Main Menu, Settings, Character Creation, confirm dialog, flow
    /// panels) in the MainMenu and Game scenes. The screens' root objects and their components stay; their children are
    /// rebuilt and rewired, so scene references (routers, controllers) keep working. Safe to run again.
    /// </summary>
    public static class VoxMenuScreens
    {
        private const string MenuScene = "Assets/Scenes/MainMenu.unity";
        private const string GameScene = "Assets/Scenes/Game.unity";
        public const string SettingRowPrefab = "Assets/Prefabs/UI/SettingRow.prefab";
        public const string CosmeticRowPrefab = "Assets/Prefabs/UI/CosmeticRow.prefab";
        public const string BackdropPath = "Assets/Art/UI/Backdrop/backdrop_blur.png";
        private const string CirclePath = "Assets/Art/Placeholder/Circle.png";

        // ---------------------------------------------------------------- backdrop texture

        [MenuItem("BulletHell/Vox/5 Build Backdrop Texture")]
        public static void BuildBackdropTexture()
        {
            const string source = "Assets/Art/ScaleTest/arena01_backdrop.png";
            var big = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            big.LoadImage(System.IO.File.ReadAllBytes(source));

            // Blur = shrink hard, then let bilinear filtering stretch it back. 512x288 is plenty for a soft backdrop.
            const int smallW = 48, smallH = 27, outW = 512, outH = 288;
            var small = new Texture2D(smallW, smallH, TextureFormat.RGBA32, false);
            for (int y = 0; y < smallH; y++)
            {
                for (int x = 0; x < smallW; x++)
                {
                    // Average a block of the source so the shrink is a real box blur, not a point sample.
                    int bx0 = x * big.width / smallW, bx1 = (x + 1) * big.width / smallW;
                    int by0 = y * big.height / smallH, by1 = (y + 1) * big.height / smallH;
                    Color sum = Color.black;
                    int n = 0;
                    for (int sy = by0; sy < by1; sy += 6)
                        for (int sx = bx0; sx < bx1; sx += 6)
                        {
                            sum += big.GetPixel(sx, sy);
                            n++;
                        }
                    sum /= Mathf.Max(1, n);
                    sum.a = 1f;
                    small.SetPixel(x, y, sum);
                }
            }
            small.Apply();
            var result = new Texture2D(outW, outH, TextureFormat.RGB24, false);
            for (int y = 0; y < outH; y++)
                for (int x = 0; x < outW; x++)
                    result.SetPixel(x, y, small.GetPixelBilinear((x + 0.5f) / outW, (y + 0.5f) / outH));
            var px = result.GetPixels();
            for (int i = 0; i < px.Length; i++)
                px[i] = new Color(Mathf.Min(1f, px[i].r * 1.6f + 0.03f), Mathf.Min(1f, px[i].g * 1.5f + 0.02f), Mathf.Min(1f, px[i].b * 1.4f + 0.02f), 1f);
            result.SetPixels(px);
            result.Apply();

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(BackdropPath));
            System.IO.File.WriteAllBytes(BackdropPath, result.EncodeToPNG());
            Object.DestroyImmediate(big);
            Object.DestroyImmediate(small);
            Object.DestroyImmediate(result);
            AssetDatabase.ImportAsset(BackdropPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(BackdropPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = outW / 17.7778f; // exactly one 16:9 camera view (ortho size 5) wide
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            Debug.Log("Vox: backdrop texture built at " + BackdropPath);
        }

        // ---------------------------------------------------------------- row prefabs

        [MenuItem("BulletHell/Vox/6 Build Row Prefabs")]
        public static void BuildRowPrefabs()
        {
            BuildSettingRow();
            BuildCosmeticRow();
            AssetDatabase.SaveAssets();
        }

        private static SettingRow NewRowRoot(out RectTransform rect, out Image background, out GameObject ring, float height, string name)
        {
            rect = VoxUi.R(name, null);
            background = rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            // Vertical only: left/right belong to the row (they change its value).
            button.navigation = new Navigation { mode = Navigation.Mode.Vertical };
            rect.gameObject.AddComponent<LayoutElement>().minHeight = height;
            rect.gameObject.AddComponent<CancelRelay>();
            var row = rect.gameObject.AddComponent<SettingRow>();

            Image ringImage = VoxUi.Img("FocusRing", rect, UITheme.Current.RowFocusRing, Image.Type.Sliced);
            VoxUi.Stretch(ringImage.rectTransform, -14f, -14f, -14f, -14f);
            ring = ringImage.gameObject;
            ring.SetActive(false);
            return row;
        }

        private static void BuildSettingRow()
        {
            SettingRow row = NewRowRoot(out RectTransform rect, out Image background, out GameObject ring, 86f, "SettingRow");
            ring.transform.SetAsFirstSibling();

            // Label (+ optional small sub label after it)
            RectTransform labels = VoxUi.R("Labels", rect);
            labels.anchorMin = new Vector2(0f, 0f);
            labels.anchorMax = new Vector2(0.5f, 1f);
            labels.offsetMin = new Vector2(32f, 0f);
            labels.offsetMax = Vector2.zero;
            var hlg = labels.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.spacing = 16f;
            hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;
            TMP_Text label = VoxUi.Txt("Label", labels, "", 36, TextFont.Button, TextTone.OnPanel, TextAlignmentOptions.Left);
            TMP_Text sub = VoxUi.Txt("Sub", labels, "", 22, TextFont.BodyBold, TextTone.Muted, TextAlignmentOptions.Left);

            // Value (slider number / choice text)
            TMP_Text value = VoxUi.Txt("Value", rect, "", 36, TextFont.Button, TextTone.OnPanel, TextAlignmentOptions.Right);
            VoxUi.TopRight((RectTransform)value.transform, 36f, 13f, 110f, 60f);
            value.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            value.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            value.rectTransform.pivot = new Vector2(1f, 0.5f);
            value.rectTransform.anchoredPosition = new Vector2(-36f, 0f);

            // Slider
            RectTransform sliderGroup = VoxUi.R("Slider", rect);
            sliderGroup.anchorMin = sliderGroup.anchorMax = new Vector2(0f, 0.5f);
            sliderGroup.pivot = new Vector2(0f, 0.5f);
            sliderGroup.anchoredPosition = new Vector2(360f, 0f);
            sliderGroup.sizeDelta = new Vector2(730f, 34f);
            Image track = VoxUi.Img("Track", sliderGroup, UITheme.Current.SliderTrack, Image.Type.Sliced);
            VoxUi.Stretch(track.rectTransform);
            Image fill = VoxUi.Img("Fill", sliderGroup, UITheme.Current.SliderFill, Image.Type.Sliced, UITheme.Current.Leaf);
            fill.rectTransform.anchorMin = new Vector2(0f, 0f);
            fill.rectTransform.anchorMax = new Vector2(1f, 1f);
            fill.rectTransform.offsetMin = new Vector2(7f, 7f);
            fill.rectTransform.offsetMax = new Vector2(-7f, -7f);
            Image handle = VoxUi.Img("Handle", sliderGroup, UITheme.Current.SliderHandle, Image.Type.Simple);
            handle.preserveAspect = true;
            VoxUi.Center(handle.rectTransform, 0f, 0f, 51f, 57f);

            // Toggle
            RectTransform toggleGroup = VoxUi.R("Toggle", rect);
            toggleGroup.anchorMin = toggleGroup.anchorMax = new Vector2(1f, 0.5f);
            toggleGroup.pivot = new Vector2(1f, 0.5f);
            toggleGroup.anchoredPosition = new Vector2(-30f, 0f);
            toggleGroup.sizeDelta = new Vector2(114f, 60f);
            Image toggleTrack = VoxUi.Img("Track", toggleGroup, UITheme.Current.ToggleOff);
            VoxUi.Stretch(toggleTrack.rectTransform);
            Image knob = VoxUi.Img("Knob", toggleGroup, UITheme.Current.ToggleKnob);
            VoxUi.Center(knob.rectTransform, 0f, 0f, 42f, 42f);
            TMP_Text word = VoxUi.Txt("Word", toggleGroup, "ON", 24, TextFont.BodyBold, TextTone.Muted, TextAlignmentOptions.Right);
            word.rectTransform.anchorMin = word.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            word.rectTransform.pivot = new Vector2(1f, 0.5f);
            word.rectTransform.anchoredPosition = new Vector2(-14f, 0f);
            word.rectTransform.sizeDelta = new Vector2(70f, 30f);

            // Choice (arrows around a value)
            RectTransform choiceGroup = VoxUi.R("Choice", rect);
            choiceGroup.anchorMin = choiceGroup.anchorMax = new Vector2(1f, 0.5f);
            choiceGroup.pivot = new Vector2(1f, 0.5f);
            choiceGroup.anchoredPosition = new Vector2(-30f, 0f);
            choiceGroup.sizeDelta = new Vector2(520f, 66f);
            Button left = Arrow("ArrowLeft", choiceGroup, UITheme.Current.ButtonArrowLeft, 0f, 0f, 58f, 64f, new Vector2(0f, 0.5f));
            Button right = Arrow("ArrowRight", choiceGroup, UITheme.Current.ButtonArrowRight, 0f, 0f, 58f, 64f, new Vector2(1f, 0.5f));
            TMP_Text choiceValue = VoxUi.Txt("ChoiceValue", choiceGroup, "", 34, TextFont.Button, TextTone.OnPanel);
            VoxUi.Stretch(choiceValue.rectTransform, 70f, 0f, 70f, 0f);

            var so = new SerializedObject(row);
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("focusRing").objectReferenceValue = ring;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("subLabel").objectReferenceValue = sub;
            so.FindProperty("value").objectReferenceValue = value;
            so.FindProperty("choiceValue").objectReferenceValue = choiceValue;
            so.FindProperty("sliderGroup").objectReferenceValue = sliderGroup.gameObject;
            so.FindProperty("sliderTrack").objectReferenceValue = sliderGroup;
            so.FindProperty("sliderFill").objectReferenceValue = fill.rectTransform;
            so.FindProperty("sliderHandle").objectReferenceValue = handle;
            so.FindProperty("toggleGroup").objectReferenceValue = toggleGroup.gameObject;
            so.FindProperty("toggleTrack").objectReferenceValue = toggleTrack;
            so.FindProperty("toggleKnob").objectReferenceValue = knob.rectTransform;
            so.FindProperty("toggleWord").objectReferenceValue = word;
            so.FindProperty("choiceGroup").objectReferenceValue = choiceGroup.gameObject;
            so.FindProperty("arrowLeft").objectReferenceValue = left;
            so.FindProperty("arrowRight").objectReferenceValue = right;
            so.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(rect.gameObject, SettingRowPrefab);
            Object.DestroyImmediate(rect.gameObject);
        }

        private static void BuildCosmeticRow()
        {
            SettingRow row = NewRowRoot(out RectTransform rect, out Image background, out GameObject ring, 100f, "CosmeticRow");

            TMP_Text label = VoxUi.Txt("Label", rect, "", 24, TextFont.Title, TextTone.Muted, TextAlignmentOptions.Left, caps: true);
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(0f, 1f);
            label.rectTransform.pivot = new Vector2(0f, 0.5f);
            label.rectTransform.offsetMin = new Vector2(28f, 0f);
            label.rectTransform.offsetMax = new Vector2(268f, 0f);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.characterSpacing = 4f;
            var labelThemed = label.GetComponent<ThemedText>();
            labelThemed.Caps = false;

            RectTransform choice = VoxUi.R("Choice", rect);
            VoxUi.Stretch(choice);
            Button left = Arrow("ArrowLeft", choice, UITheme.Current.ButtonArrowLeft, 294f, 0f, 62f, 68f, new Vector2(0f, 0.5f));
            Button right = Arrow("ArrowRight", choice, UITheme.Current.ButtonArrowRight, 685f, 0f, 62f, 68f, new Vector2(0f, 0.5f));
            TMP_Text value = VoxUi.Txt("Value", choice, "", 40, TextFont.Button, TextTone.OnPanel);
            value.rectTransform.anchorMin = value.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            value.rectTransform.anchoredPosition = new Vector2(490f, 12f);
            value.rectTransform.sizeDelta = new Vector2(320f, 52f);
            TMP_Text sub = VoxUi.Txt("ValueSub", choice, "", 22, TextFont.BodyBold, TextTone.Muted);
            sub.rectTransform.anchorMin = sub.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            sub.rectTransform.anchoredPosition = new Vector2(490f, -26f);
            sub.rectTransform.sizeDelta = new Vector2(200f, 30f);

            var so = new SerializedObject(row);
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("focusRing").objectReferenceValue = ring;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("value").objectReferenceValue = value;
            so.FindProperty("valueSub").objectReferenceValue = sub;
            so.FindProperty("choiceGroup").objectReferenceValue = choice.gameObject;
            so.FindProperty("arrowLeft").objectReferenceValue = left;
            so.FindProperty("arrowRight").objectReferenceValue = right;
            so.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(rect.gameObject, CosmeticRowPrefab);
            Object.DestroyImmediate(rect.gameObject);
        }

        private static Button Arrow(string name, Transform parent, Sprite sprite, float x, float y, float w, float h, Vector2 anchor)
        {
            Image image = VoxUi.Img(name, parent, sprite, Image.Type.Simple, null, true);
            image.preserveAspect = true;
            RectTransform r = image.rectTransform;
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = anchor.x > 0.5f ? new Vector2(1f, 0.5f) : (anchor.x < 0.5f && x == 0f ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f));
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        // ---------------------------------------------------------------- Settings

        public static void RebuildSettings(SettingsScreen screen, float dimAlpha)
        {
            Transform root = screen.transform;
            VoxUi.ClearChildren(root);
            if (root.TryGetComponent(out Image dim))
                dim.color = new Color(0f, 0f, 0f, dimAlpha);

            var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingRowPrefab).GetComponent<SettingRow>();

            TMP_Text title = VoxUi.Txt("Title", root, "SETTINGS", 92, TextFont.Title, TextTone.Gold, TextAlignmentOptions.Center, outline: true);
            VoxUi.TopCenter(title.rectTransform, 0f, 40f, 900f, 110f);

            Image panel = VoxUi.Themed("Panel", root, ThemeRole.Panel);
            VoxUi.TopCenter(panel.rectTransform, 0f, 168f, 1351f, 790f);

            // Tab bar: L1 badge, tabs, checker strip, R1 badge
            RectTransform bar = VoxUi.R("Tabs", panel.transform);
            VoxUi.TopLeft(bar, 8f, 8f, 1335f, 68f);
            var barLayout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            barLayout.padding = new RectOffset(52, 52, 0, 0);
            barLayout.spacing = 0f;
            barLayout.childAlignment = TextAnchor.MiddleLeft;
            barLayout.childControlWidth = barLayout.childControlHeight = true;
            barLayout.childForceExpandWidth = barLayout.childForceExpandHeight = false;
            float[] widths = { 165f, 165f, 201f, 223f };
            var tabButtons = new Button[4];
            var tabLabels = new TMP_Text[4];
            string[] names = { "Audio", "Video", "Controls", "Gameplay" };
            for (int i = 0; i < 4; i++)
            {
                Image tabImage = VoxUi.Themed("Tab" + names[i], bar, ThemeRole.Tab, true);
                var le = tabImage.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = widths[i];
                le.minHeight = 68f;
                var button = tabImage.gameObject.AddComponent<Button>();
                button.targetGraphic = tabImage;
                button.transition = Selectable.Transition.None;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                tabImage.gameObject.AddComponent<CancelRelay>();
                TMP_Text tl = VoxUi.Txt("Label", tabImage.transform, names[i], 40, TextFont.Button, TextTone.Faint);
                VoxUi.Stretch(tl.rectTransform, 0f, 6f, 0f, 0f);
                tabButtons[i] = button;
                tabLabels[i] = tl;
            }
            Image strip = VoxUi.Trim("Strip", bar, TrimColor.Leaf);
            var stripLe = strip.gameObject.AddComponent<LayoutElement>();
            stripLe.flexibleWidth = 1f;
            stripLe.minHeight = 68f;
            Badge("BadgeL1", panel.transform, "L1", false, 22f);
            Badge("BadgeR1", panel.transform, "R1", true, 22f);

            // The rows scroll (the Controls tab lists every remappable button): a masked viewport, the row list inside it, and a component that
            // keeps the controller-selected row in view.
            RectTransform viewport = VoxUi.R("Viewport", panel.transform);
            VoxUi.Stretch(viewport, 48f, 28f, 48f, 100f);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform rows = VoxUi.R("Rows", viewport);
            rows.anchorMin = new Vector2(0f, 1f);
            rows.anchorMax = new Vector2(1f, 1f);
            rows.pivot = new Vector2(0.5f, 1f);
            rows.anchoredPosition = Vector2.zero;
            rows.sizeDelta = Vector2.zero;
            rows.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = rows;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = 60f;
            VoxUi.SetRef(viewport.gameObject.AddComponent<ScrollFollowSelection>(), "scroll", scroll);
            var rowsLayout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            rowsLayout.padding = new RectOffset(14, 14, 14, 14);   // room for the gold focus ring, which the mask would otherwise clip
            rowsLayout.spacing = 26f;
            rowsLayout.childAlignment = TextAnchor.UpperCenter;
            rowsLayout.childControlWidth = rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;

            TMP_Text sectionPrefab = VoxUi.Txt("SectionPrefab", panel.transform, "SECTION", 24, TextFont.BodyBold, TextTone.Muted, TextAlignmentOptions.Left, caps: true);
            sectionPrefab.gameObject.AddComponent<LayoutElement>().minHeight = 44f;
            sectionPrefab.margin = new Vector4(6f, 0f, 0f, 0f);
            sectionPrefab.gameObject.SetActive(false);

            Button back = VoxUi.Btn("Back", root, "Back", 210f, 70f, 36f);
            VoxUi.TopLeft((RectTransform)back.transform, 48f, 52f, 210f, 70f);
            TMP_Text note = VoxUi.Txt("Note", root, "Changes apply instantly", 24, TextFont.BodyBold, TextTone.OnDark, TextAlignmentOptions.Right);
            VoxUi.BottomRight(note.rectTransform, 36f, 34f, 520f, 40f);
            TMP_Text hint = VoxUi.HintPill("Hint", root, 36f, 28f, 690f, 62f);

            VoxUi.SetRef(screen, "rowPrefab", rowPrefab);
            VoxUi.SetRef(screen, "rowParent", rows);
            VoxUi.SetRef(screen, "backButton", back);
            VoxUi.SetRef(screen, "hintLabel", hint);
            VoxUi.SetRef(screen, "sectionPrefab", sectionPrefab);
            VoxUi.SetRef(screen, "menuInput", Object.FindFirstObjectByType<MenuInputReader>(FindObjectsInactive.Include));
            VoxUi.SetArray(screen, "tabButtons", tabButtons);
            VoxUi.SetArray(screen, "tabLabels", tabLabels);
            EditorUtility.SetDirty(screen);
        }

        /// <summary>A small dark round badge with a shoulder-button name (L1 / R1) in a panel's top corner.</summary>
        private static void Badge(string name, Transform parent, string text, bool right, float size)
        {
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
            Image disc = VoxUi.Img(name, parent, circle, Image.Type.Simple, UITheme.Current.InkSoil);
            RectTransform r = disc.rectTransform;
            if (right)
                VoxUi.TopRight(r, 16f, 8f + 34f - size * 0.5f, size * 1.6f, size * 1.6f);
            else
                VoxUi.TopLeft(r, 16f, 8f + 34f - size * 0.5f, size * 1.6f, size * 1.6f);
            TMP_Text label = VoxUi.Txt("Label", r, text, size * 0.62f, TextFont.Button, TextTone.OnDark);
            VoxUi.Stretch(label.rectTransform);
        }

        // ---------------------------------------------------------------- Confirm dialog

        public static void RebuildConfirm(ConfirmDialog dialog)
        {
            Transform root = dialog.transform;
            VoxUi.ClearChildren(root);
            if (root.TryGetComponent(out Image dim))
                dim.color = new Color(0f, 0f, 0f, 0.6f);

            Image box = VoxUi.Themed("Box", root, ThemeRole.Panel);
            VoxUi.Center(box.rectTransform, 0f, 0f, 860f, 470f);
            Image trim = VoxUi.Trim("Trim", box.transform, TrimColor.Leaf);
            VoxUi.TopLeft(trim.rectTransform, 16f, 12f, 828f, 42f);

            TMP_Text title = VoxUi.Txt("Title", box.transform, "", 52, TextFont.Button, TextTone.OnPanel);
            VoxUi.TopCenter(title.rectTransform, 0f, 84f, 780f, 70f);
            TMP_Text message = VoxUi.Txt("Message", box.transform, "", 30, TextFont.Body, TextTone.OnPanel);
            message.textWrappingMode = TextWrappingModes.Normal;
            VoxUi.TopCenter(message.rectTransform, 0f, 168f, 740f, 130f);
            Button yes = VoxUi.Btn("Yes", box.transform, "Yes", 330f, 92f, 42f, ButtonKind.Primary);
            VoxUi.BottomLeft((RectTransform)yes.transform, 60f, 50f, 330f, 92f);
            Button no = VoxUi.Btn("No", box.transform, "No", 330f, 92f, 42f);
            VoxUi.BottomRight((RectTransform)no.transform, 60f, 50f, 330f, 92f);

            VoxUi.SetRef(dialog, "titleLabel", title);
            VoxUi.SetRef(dialog, "messageLabel", message);
            VoxUi.SetRef(dialog, "yesButton", yes);
            VoxUi.SetRef(dialog, "noButton", no);
            VoxUi.SetRef(dialog, "yesLabel", yes.GetComponentInChildren<TMP_Text>());
            VoxUi.SetRef(dialog, "noLabel", no.GetComponentInChildren<TMP_Text>());
            EditorUtility.SetDirty(dialog);
        }

        // ---------------------------------------------------------------- Flow panels (results, pause, game over)

        public static void RestyleFlowPanel(FlowPanel panel)
        {
            Transform root = panel.transform;
            if (root.TryGetComponent(out Image dim))
                dim.color = new Color(0f, 0f, 0f, 0.7f);
            Transform box = root.Find("Box");
            if (box != null)
            {
                if (box.TryGetComponent(out ThemedImage themed))
                    themed.Role = ThemeRole.Panel;
                if (box.Find("Trim") == null)
                {
                    Image trim = VoxUi.Trim("Trim", box, TrimColor.Leaf);
                    trim.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                    VoxUi.TopLeft(trim.rectTransform, 14f, 12f, 0f, 42f);
                    trim.rectTransform.anchorMin = new Vector2(0f, 1f);
                    trim.rectTransform.anchorMax = new Vector2(1f, 1f);
                    trim.rectTransform.offsetMin = new Vector2(14f, -54f);
                    trim.rectTransform.offsetMax = new Vector2(-14f, -12f);
                    trim.transform.SetAsFirstSibling();
                }
                if (box.TryGetComponent(out VerticalLayoutGroup layout))
                {
                    layout.padding = new RectOffset(48, 48, 76, 40);
                    layout.spacing = 16f;
                }
                foreach (Transform child in box)
                {
                    if (child.name == "Title" && child.TryGetComponent(out TMP_Text title))
                    {
                        Style(title, TextFont.Button, TextTone.OnPanel, 64);
                    }
                    else if ((child.name == "Body") && child.TryGetComponent(out TMP_Text body))
                    {
                        Style(body, TextFont.Body, TextTone.OnPanel, 34);
                    }
                    else if (child.name == "DebugRoundRow")
                    {
                        foreach (var text in child.GetComponentsInChildren<TMP_Text>(true))
                            if (!text.GetComponentInParent<Button>())
                                Style(text, TextFont.BodyBold, TextTone.OnPanel, (int)text.fontSize);
                    }
                }
            }
            Transform hintOld = root.Find("HintPill") != null ? root.Find("HintPill") : root.Find("Hint");   // rebuilt each time so older fixed-width pills pick up the self-sizing one
            if (hintOld != null)
            {
                TMP_Text hint = RebuildHint(root, hintOld);
                VoxUi.SetRef(panel, "hintLabel", hint);
            }
            EditorUtility.SetDirty(panel);
        }

        public static void Style(TMP_Text text, TextFont font, TextTone tone, int size, bool caps = false, bool outline = false)
        {
            ThemedText themed = text.GetComponent<ThemedText>();
            if (themed == null)
                themed = text.gameObject.AddComponent<ThemedText>();
            themed.Font = font;
            themed.Tone = tone;
            themed.Caps = caps;
            themed.Outline = outline;
            if (size > 0)
                text.fontSize = size;
        }

        /// <summary>Replaces a plain hint text with the bottom-left prompt pill and returns the new label.</summary>
        public static TMP_Text RebuildHint(Transform parent, Transform old)
        {
            Object.DestroyImmediate(old.gameObject);
            return VoxUi.HintPill("Hint", parent, 36f, 28f, 690f, 62f);
        }

        // ---------------------------------------------------------------- driver

        [MenuItem("BulletHell/Vox/7 Build Main Menu Scene")]
        public static void BuildMainMenuScene()
        {
            BuildBackdropIfMissing();
            BuildRowPrefabs();
            Scene scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<MainMenuController>();
            Transform safe = GameObject.Find("MenuCanvas/SafeArea").transform;
            UITheme theme = UITheme.Current;

            // World backdrop: the blurred arena, covering the camera view (and ultrawide).
            GameObject old = GameObject.Find("MenuBackdrop");
            if (old != null)
                Object.DestroyImmediate(old);
            var backdropGo = new GameObject("MenuBackdrop");
            var sr = backdropGo.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackdropPath);
            sr.color = new Color(0.5f, 0.46f, 0.44f);
            sr.sortingOrder = -1000;
            backdropGo.transform.position = new Vector3(0f, 0f, 20f);
            backdropGo.transform.localScale = Vector3.one * 1.45f;

            // --- title group
            Transform oldTitle = safe.Find("Title");
            if (oldTitle != null)
                Object.DestroyImmediate(oldTitle.gameObject);
            RectTransform title = VoxUi.R("Title", safe);
            VoxUi.Stretch(title);
            title.SetAsFirstSibling();
            Image topStrip = VoxUi.Trim("TopStrip", title, TrimColor.Leaf);
            topStrip.rectTransform.anchorMin = new Vector2(0f, 1f);
            topStrip.rectTransform.anchorMax = new Vector2(1f, 1f);
            topStrip.rectTransform.pivot = new Vector2(0.5f, 1f);
            topStrip.rectTransform.offsetMin = new Vector2(-2000f, -42f);
            topStrip.rectTransform.offsetMax = new Vector2(2000f, 0f);
            TMP_Text tagline = VoxUi.Txt("Tagline", title, "THE VEGETABLE COLOSSEUM PRESENTS", 34, TextFont.Title, TextTone.Sage, TextAlignmentOptions.Center);
            VoxUi.TopCenter(tagline.rectTransform, 0f, 112f, 1500f, 56f);
            tagline.characterSpacing = 14f;
            // Logo: soil shadow, leaf shadow, corn face with a soil outline (three stacked copies).
            MakeLogoLayer("LogoSoil", title, new Vector2(11f, -13f), theme.InkSoil, false);
            MakeLogoLayer("LogoLeaf", title, new Vector2(6f, -8f), theme.Leaf, false);
            MakeLogoLayer("Logo", title, Vector2.zero, theme.Corn, true);

            // --- buttons
            RectTransform buttons = (RectTransform)safe.Find("Buttons");
            buttons.anchorMin = buttons.anchorMax = buttons.pivot = new Vector2(0.5f, 0.5f);
            buttons.anchoredPosition = new Vector2(0f, -150f);
            buttons.sizeDelta = new Vector2(540f, 450f);
            var layout = buttons.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 30f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            foreach (Transform child in buttons)
            {
                var le = child.GetComponent<LayoutElement>();
                if (le == null)
                    le = child.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 92f;
                le.preferredHeight = 92f;
                var themedButton = child.GetComponent<ThemedButton>();
                if (themedButton != null)
                    themedButton.Apply();
                foreach (string decorName in new[] { "FocusTrimL", "FocusTrimR", "LaurelL", "LaurelR" })
                {
                    Transform oldDecorChild = child.Find(decorName);
                    if (oldDecorChild != null)
                        Object.DestroyImmediate(oldDecorChild.gameObject);
                }
                var oldDecor = child.GetComponent<FocusDecor>();
                if (oldDecor != null)
                    Object.DestroyImmediate(oldDecor);
                AddMenuFocusDecor((RectTransform)child);
                var label = child.GetComponentInChildren<TMP_Text>();
                label.fontSize = 48;
            }
            TMP_Text message = VoxUi.Txt("MessageNew", safe, "", 30, TextFont.BodyBold, TextTone.Gold, TextAlignmentOptions.Center, outline: true);
            VoxUi.TopCenter(message.rectTransform, 0f, 930f, 1200f, 50f);
            var oldMessage = safe.Find("Message");
            if (oldMessage != null)
                Object.DestroyImmediate(oldMessage.gameObject);
            message.name = "Message";

            foreach (string leftover in new[] { "Hint", "HintPill", "Version" })
            {
                Transform t = safe.Find(leftover);
                if (t != null)
                    Object.DestroyImmediate(t.gameObject);
            }
            TMP_Text hint = VoxUi.HintPill("Hint", title, 36f, 28f, 420f, 62f);
            TMP_Text version = VoxUi.Txt("Version", title, BulletHell.Core.BuildInfo.MenuLine, 26, TextFont.BodyBold, TextTone.OnDark, TextAlignmentOptions.Right);
            VoxUi.BottomRight(version.rectTransform, 36f, 34f, 900f, 40f);

            VoxUi.SetRef(controller, "title", title.gameObject);
            VoxUi.SetRef(controller, "messageText", message);
            VoxUi.SetRef(controller, "hintLabel", hint);
            VoxUi.SetRef(controller, "versionLabel", version);

            // --- sub screens
            var settings = Object.FindFirstObjectByType<SettingsScreen>(FindObjectsInactive.Include);
            RebuildSettings(settings, 0.25f);
            var confirm = Object.FindFirstObjectByType<ConfirmDialog>(FindObjectsInactive.Include);
            RebuildConfirm(confirm);
            var customization = Object.FindFirstObjectByType<CustomizationScreen>(FindObjectsInactive.Include);
            RebuildCustomization(customization);
            // The controller found the settings/customization/confirm objects; re-point it (same components, so this is a no-op
            // unless a component was replaced).
            VoxUi.SetRef(controller, "settingsScreen", settings);
            VoxUi.SetRef(controller, "customizationScreen", customization);
            VoxUi.SetRef(controller, "confirmDialog", confirm);

            RewireRouter(confirm, settings, customization, buttons);

            // Keep the dialogs on top of the menu and the screens.
            confirm.transform.SetAsLastSibling();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Vox: Main Menu scene built.");
        }

        private static void BuildBackdropIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(BackdropPath) == null)
                BuildBackdropTexture();
        }

        private static void MakeLogoLayer(string name, Transform parent, Vector2 offset, Color color, bool outline)
        {
            TMP_Text text = VoxUi.Txt(name, parent, "VOX VEGETALLIS", 152, TextFont.Logo, TextTone.Custom, TextAlignmentOptions.Center, outline: outline);
            text.color = color;
            text.characterSpacing = -2f;
            VoxUi.TopCenter(text.rectTransform, offset.x, 160f - offset.y, 1800f, 230f);
            if (!outline)
                text.GetComponent<ThemedText>().OutlineColor = color;   // shadow layers: a thin outline in their own color keeps the letters solid
        }

        private static void AddMenuFocusDecor(RectTransform button)
        {
            UITheme theme = UITheme.Current;
            Image left = VoxUi.Trim("FocusTrimL", button, TrimColor.Leaf);
            left.rectTransform.anchorMin = new Vector2(0f, 0f);
            left.rectTransform.anchorMax = new Vector2(0f, 1f);
            left.rectTransform.pivot = new Vector2(0f, 0.5f);
            left.rectTransform.offsetMin = new Vector2(8f, 18f);
            left.rectTransform.offsetMax = new Vector2(38f, -8f);
            Image right = VoxUi.Trim("FocusTrimR", button, TrimColor.Leaf);
            right.rectTransform.anchorMin = new Vector2(1f, 0f);
            right.rectTransform.anchorMax = new Vector2(1f, 1f);
            right.rectTransform.pivot = new Vector2(1f, 0.5f);
            right.rectTransform.offsetMin = new Vector2(-38f, 18f);
            right.rectTransform.offsetMax = new Vector2(-8f, -8f);
            Image laurelL = VoxUi.Img("LaurelL", button, theme.Laurel, Image.Type.Simple);
            laurelL.preserveAspect = true;
            laurelL.rectTransform.anchorMin = laurelL.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            laurelL.rectTransform.pivot = new Vector2(0f, 0.5f);
            laurelL.rectTransform.anchoredPosition = new Vector2(-28f, 4f);
            laurelL.rectTransform.sizeDelta = new Vector2(81f, 60f);
            laurelL.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            Image laurelR = VoxUi.Img("LaurelR", button, theme.Laurel, Image.Type.Simple);
            laurelR.preserveAspect = true;
            laurelR.rectTransform.anchorMin = laurelR.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            laurelR.rectTransform.pivot = new Vector2(0f, 0.5f);
            laurelR.rectTransform.anchoredPosition = new Vector2(28f, 4f);
            laurelR.rectTransform.sizeDelta = new Vector2(81f, 60f);

            var decor = button.GetComponent<FocusDecor>();
            if (decor == null)
                decor = button.gameObject.AddComponent<FocusDecor>();
            decor.ShownWhenFocused = new[] { left.gameObject, right.gameObject, laurelL.gameObject, laurelR.gameObject };
            foreach (GameObject g in decor.ShownWhenFocused)
                g.SetActive(false);
            // The label sits above the trims (they are siblings added after it), so keep it last.
            button.Find("Label").SetAsLastSibling();
        }

        private static void RewireRouter(ConfirmDialog confirm, SettingsScreen settings, CustomizationScreen customization, RectTransform buttons)
        {
            var router = Object.FindFirstObjectByType<MenuPrimaryRouter>();
            if (router == null)
                return;
            var so = new SerializedObject(router);
            SerializedProperty routes = so.FindProperty("routes");
            var entries = new List<(GameObject scope, Button button)>
            {
                (confirm.gameObject, null),
                (settings.gameObject, null),
                (customization.gameObject, new SerializedObject(customization).FindProperty("confirmButton").objectReferenceValue as Button),
                (buttons.gameObject, buttons.Find("NewGame")?.GetComponent<Button>()),
            };
            routes.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                SerializedProperty route = routes.GetArrayElementAtIndex(i);
                route.FindPropertyRelative("Scope").objectReferenceValue = entries[i].scope;
                route.FindPropertyRelative("Button").objectReferenceValue = entries[i].button;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- Character creation

        public static void RebuildCustomization(CustomizationScreen screen)
        {
            UITheme theme = UITheme.Current;
            Transform root = screen.transform;
            VoxUi.ClearChildren(root);
            var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CosmeticRowPrefab).GetComponent<SettingRow>();

            Image strip = VoxUi.Trim("TopStrip", root, TrimColor.Leaf);
            strip.rectTransform.anchorMin = new Vector2(0f, 1f);
            strip.rectTransform.anchorMax = new Vector2(1f, 1f);
            strip.rectTransform.pivot = new Vector2(0.5f, 1f);
            strip.rectTransform.offsetMin = new Vector2(-2000f, -42f);
            strip.rectTransform.offsetMax = new Vector2(2000f, 0f);

            TMP_Text title = VoxUi.Txt("Title", root, "FORGE YOUR GLADIATOR", 70, TextFont.Title, TextTone.Gold, TextAlignmentOptions.Left, outline: true);
            VoxUi.TopLeft(title.rectTransform, 48f, 54f, 1000f, 96f);

            // Right panel with the five part rows
            Image panel = VoxUi.Themed("Panel", root, ThemeRole.Panel);
            VoxUi.TopRight(panel.rectTransform, 50f, 142f, 830f, 706f);
            Image trim = VoxUi.Trim("Trim", panel.transform, TrimColor.Leaf);
            VoxUi.TopLeft(trim.rectTransform, 12f, 10f, 806f, 42f);
            RectTransform rows = VoxUi.R("Rows", panel.transform);
            VoxUi.Stretch(rows, 32f, 26f, 32f, 70f);
            var layout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Bottom: HUD portrait preview chip (left of the primary button) and the primary button
            Image chip = VoxUi.Themed("PortraitChip", root, ThemeRole.HintBar);
            chip.GetComponent<ThemedImage>().KeepColor = true;
            chip.color = new Color(1f, 1f, 1f, 0.9f);
            VoxUi.BottomRight(chip.rectTransform, 512f, 30f, 290f, 138f);
            HudPortrait portrait = BuildPortrait(chip.transform, 0.62f);
            VoxUi.TopLeft((RectTransform)portrait.transform, 12f, 6f, 130f, 130f);
            TMP_Text chipText = VoxUi.Txt("Text", chip.transform, "HUD portrait\npreview", 24, TextFont.BodyBold, TextTone.OnDark, TextAlignmentOptions.Left);
            chipText.textWrappingMode = TextWrappingModes.Normal;
            chipText.rectTransform.anchorMin = chipText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            chipText.rectTransform.pivot = new Vector2(1f, 0.5f);
            chipText.rectTransform.anchoredPosition = new Vector2(-14f, 0f);
            chipText.rectTransform.sizeDelta = new Vector2(150f, 80f);

            Button confirm = VoxUi.Btn("Confirm", root, "To the Arena!", 424f, 110f, 54f, ButtonKind.Primary);
            VoxUi.BottomRight((RectTransform)confirm.transform, 50f, 60f, 424f, 110f);
            Button randomize = VoxUi.Btn("Randomize", root, "Randomize", 324f, 82f, 40f);
            VoxUi.BottomLeft((RectTransform)randomize.transform, 380f, 70f, 324f, 82f);
            Button back = VoxUi.Btn("Back", root, "Main Menu", 230f, 66f, 32f);
            VoxUi.TopLeft((RectTransform)back.transform, 48f, 168f, 230f, 66f);
            TMP_Text hint = VoxUi.HintPill("Hint", root, 40f, 66f, 560f, 62f, fromRight: true, fromTop: true);

            panel.name = "Panel";

            VoxUi.SetRef(screen, "rowPrefab", rowPrefab);
            VoxUi.SetRef(screen, "rowParent", rows);
            VoxUi.SetRef(screen, "randomizeButton", randomize);
            VoxUi.SetRef(screen, "confirmButton", confirm);
            VoxUi.SetRef(screen, "backButton", back);
            VoxUi.SetRef(screen, "hintLabel", hint);
            VoxUi.SetRef(screen, "portrait", portrait);
            VoxUi.SetRef(screen, "menuInput", Object.FindFirstObjectByType<MenuInputReader>(FindObjectsInactive.Include));
            EditorUtility.SetDirty(screen);

            RebuildPreviewStage(screen);
        }

        /// <summary>The checkered portrait ring with the head and accessory windows; also used by the HUD.</summary>
        public static HudPortrait BuildPortrait(Transform parent, float scale)
        {
            UITheme theme = UITheme.Current;
            RectTransform root = VoxUi.R("Portrait", parent);
            root.sizeDelta = new Vector2(174f, 183f);
            root.localScale = Vector3.one * scale;
            var component = root.gameObject.AddComponent<HudPortrait>();

            // Circular window (mask) inside the ring's 204 px (2x) hole = 102 canvas px
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
            Image window = VoxUi.Img("Window", root, circle, Image.Type.Simple, Color.white, false);
            VoxUi.Center(window.rectTransform, 0f, 7f, 104f, 104f);
            window.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            window.color = theme.InkSoil;
            Image head = VoxUi.Img("Head", window.transform, null);
            VoxUi.Center(head.rectTransform, 0f, -4f, 210f, 210f);
            head.preserveAspect = true;
            Image accessory = VoxUi.Img("Accessory", window.transform, null);
            VoxUi.Center(accessory.rectTransform, 0f, -4f, 210f, 210f);
            accessory.preserveAspect = true;
            Image ring = VoxUi.Img("Ring", root, theme.PortraitRing, Image.Type.Simple);
            VoxUi.Stretch(ring.rectTransform);
            ring.preserveAspect = true;

            VoxUi.SetRef(component, "head", head);
            VoxUi.SetRef(component, "accessory", accessory);
            return component;
        }

        // World side of Character Creation: the kit pedestal and spotlight under the doll, placed to match the mockup.
        private static void RebuildPreviewStage(CustomizationScreen screen)
        {
            var so = new SerializedObject(screen);
            var previewRoot = so.FindProperty("previewRoot").objectReferenceValue as GameObject;
            if (previewRoot == null)
                return;
            UITheme theme = UITheme.Current;
            bool wasActive = previewRoot.activeSelf;
            previewRoot.SetActive(true);

            Transform old = previewRoot.transform.Find("Pedestal");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            var pedestal = new GameObject("Pedestal").transform;
            pedestal.SetParent(previewRoot.transform, false);
            pedestal.localPosition = new Vector3(0f, -0.437f, 0f);   // the doll sprite is centered; its feet are 0.437 below

            int layer = 0;
            foreach (var r in previewRoot.GetComponentsInChildren<SpriteRenderer>(true))
                if (r.transform.parent != pedestal && r.sortingLayerID != 0)
                {
                    layer = r.sortingLayerID;
                    break;
                }

            // Kit sprites are 200 PPU; a doll is about 1 world unit wide, the pedestal about 1.9 (570 canvas px -> 1.9 units).
            const float s = 0.30f;
            AddSprite(pedestal, "Spotlight", theme.Spotlight, new Vector3(0f, 0.83f - 0.437f, 0f), s, new Color(1f, 1f, 1f, 0.75f), layer, -10);
            AddSprite(pedestal, "PedestalBase", theme.Pedestal, Vector3.zero, s, Color.white, layer, -8);

            // Doll feet sit at the pedestal's center, left of the panel, like the mockup (feet about 28% / 83% of the screen).
            previewRoot.transform.position = new Vector3(-3.85f, -1.92f, 0f);
            previewRoot.transform.localScale = Vector3.one * 3.1f;
            previewRoot.SetActive(wasActive);
            EditorUtility.SetDirty(previewRoot);
        }

        private static void AddSprite(Transform parent, string name, Sprite sprite, Vector3 localPosition, float scale, Color color, int layer, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerID = layer;
            renderer.sortingOrder = order;
        }
    }
}
