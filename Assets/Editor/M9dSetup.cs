using System.Linq;
using TMPro;
using BulletHell.Core;
using BulletHell.Platform;
using BulletHell.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// One-shot M9d setup: fills the ButtonGlyphLibrary with prompt text per device (plus a Keyboard set), puts it on
    /// GameConfig, builds the shared ConfirmDialog prefab, drops it into the Game and Main Menu scenes (replacing the Main
    /// Menu's own overwrite panel), and adds a prompt line to every screen. Safe to run again.
    /// </summary>
    public static class M9dSetup
    {
        private const string GlyphsPath = "Assets/Data/UI/ButtonGlyphs.asset";
        private const string ConfigPath = "Assets/Resources/GameConfig.asset";
        private const string DialogPrefabPath = "Assets/Prefabs/UI/ConfirmDialog.prefab";
        private const string GameScene = "Assets/Scenes/Game.unity";
        private const string MenuScene = "Assets/Scenes/MainMenu.unity";

        [MenuItem("BulletHell/M9d/Setup Everything")]
        public static void Run()
        {
            FillGlyphLibrary();
            GameObject dialog = BuildDialogPrefab();
            SetupGameScene(dialog);
            SetupMenuScene(dialog);
            AssetDatabase.SaveAssets();
            Debug.Log("M9d setup complete.");
        }

        // ---------------------------------------------------------------- glyph library

        // Order: Confirm, Back, Randomize, Details, Reroll, Remove, TabPrev, TabNext, Start ("" = no such button)
        private static void FillGlyphLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<ButtonGlyphLibrary>(GlyphsPath);
            var sets = library.Sets.Where(s => s.Family != GlyphFamily.Keyboard).ToList();

            void Labels(GlyphFamily family, params string[] labels)
            {
                ButtonGlyphLibrary.FamilySet set = sets.FirstOrDefault(s => s.Family == family);
                if (set != null)
                    set.ActionLabels = labels;
            }

            Labels(GlyphFamily.PlayStation, "Cross", "Circle", "Square", "Square", "Triangle", "Triangle", "L1", "R1", "Options");
            Labels(GlyphFamily.Xbox, "A", "B", "X", "X", "Y", "Y", "LB", "RB", "Menu");
            Labels(GlyphFamily.Nintendo, "B", "A", "Y", "Y", "X", "X", "L", "R", "+");
            Labels(GlyphFamily.Touch, "Tap", "Back", "", "", "", "", "", "", "");

            // Keyboard: the debug fallback keys (1-4 pick ammo; the letters are the Menu map bindings).
            var disc = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Glyph_Disc.png");
            var keycap = new Color(0.85f, 0.85f, 0.88f);
            var dark = new Color(0.08f, 0.08f, 0.1f);
            ButtonGlyph Key(string label) => new ButtonGlyph { Icon = disc, Tint = keycap, Label = label, LabelColor = dark };
            sets.Add(new ButtonGlyphLibrary.FamilySet
            {
                Family = GlyphFamily.Keyboard,
                South = Key("1"), East = Key("2"), West = Key("3"), North = Key("4"),
                ActionLabels = new[] { "Enter", "Esc", "R", "Q", "T", "X", "[", "]", "" },
            });
            library.SetSets(sets.ToArray());

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var so = new SerializedObject(config);
            so.FindProperty("buttonGlyphs").objectReferenceValue = library;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        // ---------------------------------------------------------------- dialog prefab

        private static GameObject BuildDialogPrefab()
        {
            var root = new GameObject("ConfirmDialog", typeof(RectTransform), typeof(Image), typeof(ConfirmDialog));
            var rootRect = (RectTransform)root.transform;
            UiBuilder.Stretch(rootRect);
            root.GetComponent<Image>().color = UiBuilder.Dim;   // blocks the mouse behind the dialog

            RectTransform box = Rect("Box", rootRect);
            Place(box, Vector2.zero, new Vector2(900f, 440f));
            box.gameObject.AddComponent<Image>();
            box.gameObject.AddComponent<ThemedImage>().Role = ThemeRole.Panel;

            TMP_Text title = UiBuilder.CreateText("Title", box, "Are you sure?", 44, TextAnchor.MiddleCenter);
            Place((RectTransform)title.transform, new Vector2(0f, 150f), new Vector2(780f, 70f));
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.33f, 0.16f, 0.1f);

            TMP_Text message = UiBuilder.CreateText("Message", box, "", 30, TextAnchor.MiddleCenter);
            Place((RectTransform)message.transform, new Vector2(0f, 30f), new Vector2(780f, 150f));
            message.textWrappingMode = TextWrappingModes.Normal;
            message.color = new Color(0.33f, 0.16f, 0.1f);

            Button yes = UiBuilder.CreateButton("Yes", box, "Yes", 80f);
            Place((RectTransform)yes.transform, new Vector2(-200f, -140f), new Vector2(340f, 84f));
            Button no = UiBuilder.CreateButton("No", box, "No", 80f);
            Place((RectTransform)no.transform, new Vector2(200f, -140f), new Vector2(340f, 84f));
            yes.gameObject.AddComponent<CancelRelay>();
            no.gameObject.AddComponent<CancelRelay>();
            TMP_Text yesLabel = yes.GetComponentInChildren<TMP_Text>();
            TMP_Text noLabel = no.GetComponentInChildren<TMP_Text>();
            yesLabel.fontSize = noLabel.fontSize = 38;

            var dialog = root.GetComponent<ConfirmDialog>();
            var so = new SerializedObject(dialog);
            so.FindProperty("titleLabel").objectReferenceValue = title;
            so.FindProperty("messageLabel").objectReferenceValue = message;
            so.FindProperty("yesButton").objectReferenceValue = yes;
            so.FindProperty("noButton").objectReferenceValue = no;
            so.FindProperty("yesLabel").objectReferenceValue = yesLabel;
            so.FindProperty("noLabel").objectReferenceValue = noLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
            root.SetActive(false);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DialogPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static ConfirmDialog PlaceDialog(GameObject prefab, Transform parent)
        {
            foreach (ConfirmDialog old in parent.GetComponentsInChildren<ConfirmDialog>(true))
                Object.DestroyImmediate(old.gameObject);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.SetActive(false);
            go.transform.SetAsLastSibling();
            return go.GetComponent<ConfirmDialog>();
        }

        // ---------------------------------------------------------------- scenes

        private static void SetupGameScene(GameObject dialogPrefab)
        {
            EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            Transform safe = GameObject.Find("FlowUI/SafeArea").transform;
            ConfirmDialog dialog = PlaceDialog(dialogPrefab, safe);

            var flow = Object.FindFirstObjectByType<GameFlowUI>(FindObjectsInactive.Include);
            var flowSo = new SerializedObject(flow);
            flowSo.FindProperty("confirm").objectReferenceValue = dialog;
            flowSo.ApplyModifiedPropertiesWithoutUndo();

            foreach (FlowPanel panel in Object.FindObjectsByType<FlowPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                AddHint(panel, panel.transform);
            foreach (SettingsScreen settings in Object.FindObjectsByType<SettingsScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                AddHint(settings, settings.transform);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void SetupMenuScene(GameObject dialogPrefab)
        {
            EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            Transform safe = GameObject.Find("MenuCanvas/SafeArea").transform;

            Transform old = safe.Find("ConfirmOverwrite");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            ConfirmDialog dialog = PlaceDialog(dialogPrefab, safe);

            var menu = Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
            var so = new SerializedObject(menu);
            so.FindProperty("confirmDialog").objectReferenceValue = dialog;
            so.ApplyModifiedPropertiesWithoutUndo();
            AddHint(menu, safe);

            foreach (SettingsScreen settings in Object.FindObjectsByType<SettingsScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                AddHint(settings, settings.transform);
            foreach (CustomizationScreen custom in Object.FindObjectsByType<CustomizationScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                AddHint(custom, custom.transform);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // A centered prompt line along the bottom edge of `parent`, wired to the component's hintLabel field.
        private static void AddHint(Component owner, Transform parent)
        {
            var so = new SerializedObject(owner);
            SerializedProperty field = so.FindProperty("hintLabel");
            if (field == null)
                return;

            Transform existing = parent.Find("Hint");
            TMP_Text hint = existing != null ? existing.GetComponent<TMP_Text>() : null;
            if (hint == null)
            {
                hint = UiBuilder.CreateText("Hint", parent, "", 26, TextAnchor.MiddleCenter);
                var rect = (RectTransform)hint.transform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 18f);
                rect.sizeDelta = new Vector2(-80f, 40f);
                hint.color = new Color(0.9f, 0.84f, 0.72f);
                hint.raycastTarget = false;
            }
            hint.transform.SetAsLastSibling();
            field.objectReferenceValue = hint;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- helpers

        private static RectTransform Rect(string name, Transform parent) => UiBuilder.CreateRect(name, parent);

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
