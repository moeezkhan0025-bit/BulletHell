using System.Linq;
using BulletHell.Core;
using BulletHell.Cosmetics;
using BulletHell.Enemies;
using BulletHell.Player;
using BulletHell.Save;
using BulletHell.Settings;
using BulletHell.UI;
using BulletHell.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// One-shot M6 setup: the settings defaults, the placeholder cosmetics (and their shape sprites) with the registry
    /// entries, the GameConfig references, the Player prefab layers, the SettingRow prefab, and the Main Menu / Game
    /// scene UI (Settings screen, customization screen and preview, Pause settings button, round intro banner).
    /// Safe to run again: existing assets are updated and scene objects that already exist are left alone.
    /// </summary>
    public static class M6Setup
    {
        private const string MenuPath = "Assets/Scenes/MainMenu.unity";
        private const string GamePath = "Assets/Scenes/Game.unity";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string RowPrefabPath = "Assets/Prefabs/UI/SettingRow.prefab";
        private const string ConfigPath = "Assets/Resources/GameConfig.asset";
        private const string RegistryPath = "Assets/Data/AssetRegistry.asset";
        private const string SettingsDefaultsPath = "Assets/Data/Settings/SettingsDefaults.asset";
        private const string CosmeticsFolder = "Assets/Data/Cosmetics";
        private const string PlaceholderFolder = "Assets/Art/Placeholder";

        [MenuItem("BulletHell/M6/Setup Everything")]
        public static void Run()
        {
            CreateSettingsDefaults();
            Sprite circle = PlaceholderSprite("Circle", null);
            Sprite square = PlaceholderSprite("Square", null);
            Sprite triangle = PlaceholderSprite("Triangle", (x, y) => Mathf.Abs(x - 0.5f) < (1f - y) * 0.5f ? 1f : 0f);
            Sprite diamond = PlaceholderSprite("Diamond", (x, y) => Mathf.Abs(x - 0.5f) + Mathf.Abs(y - 0.5f) < 0.5f ? 1f : 0f);
            CreateCosmetics(circle, square, triangle, diamond);
            AssignConfig();
            SetupPlayerPrefab();
            SetupRowPrefab();
            SetupMainMenu();
            SetupGame();
            AssetDatabase.SaveAssets();
            Debug.Log("M6 setup complete.");
        }

        // ---------------------------------------------------------------- Assets

        private static void CreateSettingsDefaults()
        {
            EnsureFolder("Assets/Data/Settings");
            if (AssetDatabase.LoadAssetAtPath<SettingsDefaults>(SettingsDefaultsPath) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<SettingsDefaults>(), SettingsDefaultsPath);
        }

        /// <summary>The white placeholder shape sprite at Assets/Art/Placeholder/&lt;name&gt;.png, generated when missing.</summary>
        private static Sprite PlaceholderSprite(string name, System.Func<float, float, float> shape)
        {
            string path = $"{PlaceholderFolder}/{name}.png";
            if (!System.IO.File.Exists(path) && shape != null)
            {
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, shape((x + 0.5f) / size, (y + 0.5f) / size)));
                System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = size;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void CreateCosmetics(Sprite circle, Sprite square, Sprite triangle, Sprite diamond)
        {
            EnsureFolder(CosmeticsFolder);
            Color C(float r, float g, float b) => new Color(r, g, b, 1f);

            var items = new (string key, CosmeticSlot slot, string name, Color tint, Sprite sprite)[]
            {
                ("Coating_0_Plain", CosmeticSlot.CandyCoating, "Plain", Color.white, null),
                ("Coating_1_Strawberry", CosmeticSlot.CandyCoating, "Strawberry", C(1f, 0.55f, 0.7f), null),
                ("Coating_2_Lime", CosmeticSlot.CandyCoating, "Lime", C(0.6f, 1f, 0.5f), null),
                ("Coating_3_Blueberry", CosmeticSlot.CandyCoating, "Blueberry", C(0.55f, 0.65f, 1f), null),
                ("Coating_4_Lemon", CosmeticSlot.CandyCoating, "Lemon", C(1f, 0.95f, 0.5f), null),

                ("Headgear_0_None", CosmeticSlot.Headgear, "None", Color.white, null),
                ("Headgear_1_GoldHelmet", CosmeticSlot.Headgear, "Gold Helmet", C(1f, 0.82f, 0.2f), circle),
                ("Headgear_2_RedCrest", CosmeticSlot.Headgear, "Red Crest", C(0.95f, 0.2f, 0.2f), triangle),
                ("Headgear_3_MintCrown", CosmeticSlot.Headgear, "Mint Crown", C(0.5f, 1f, 0.8f), diamond),

                ("Cape_0_None", CosmeticSlot.Cape, "None", Color.white, null),
                ("Cape_1_Crimson", CosmeticSlot.Cape, "Crimson Cape", C(0.75f, 0.1f, 0.2f), square),
                ("Cape_2_Royal", CosmeticSlot.Cape, "Royal Cape", C(0.55f, 0.3f, 0.9f), triangle),
                ("Cape_3_Teal", CosmeticSlot.Cape, "Teal Trail", C(0.1f, 0.75f, 0.75f), diamond),

                ("ArmTint_0_Natural", CosmeticSlot.ArmTint, "Natural", Color.white, null),
                ("ArmTint_1_Gold", CosmeticSlot.ArmTint, "Gold", C(1f, 0.85f, 0.4f), null),
                ("ArmTint_2_Bubblegum", CosmeticSlot.ArmTint, "Bubblegum", C(1f, 0.6f, 0.85f), null),
                ("ArmTint_3_Frost", CosmeticSlot.ArmTint, "Frost", C(0.6f, 0.9f, 1f), null),
            };

            var created = new CosmeticData[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                string assetPath = $"{CosmeticsFolder}/Cosmetic_{items[i].key}.asset";
                var asset = AssetDatabase.LoadAssetAtPath<CosmeticData>(assetPath);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<CosmeticData>();
                    AssetDatabase.CreateAsset(asset, assetPath);
                }
                asset.Configure(items[i].key.ToLowerInvariant(), items[i].slot, items[i].name, items[i].tint, items[i].sprite);
                created[i] = asset;
            }

            // Registry order = slot default first: the list is already ordered by slot, then by the numbered key.
            var registry = AssetDatabase.LoadAssetAtPath<AssetRegistry>(RegistryPath);
            registry.SetCosmetics(created);
            Debug.Log($"Cosmetics: {created.Length} items registered.");
        }

        private static void AssignConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var so = new SerializedObject(config);
            so.FindProperty("settingsDefaults").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SettingsDefaults>(SettingsDefaultsPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        // ---------------------------------------------------------------- Player prefab

        private static void SetupPlayerPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                if (root.GetComponent<GladiatorCosmetics>() == null)
                {
                    Transform bodyTransform = root.transform.Find("Body");
                    var body = bodyTransform.GetComponent<SpriteRenderer>();
                    float height = body.bounds.size.y;

                    SpriteRenderer cape = CreateLayer(root.transform, "Cape", new Vector3(0f, -height * 0.3f, 0f), height * 0.85f, -1);
                    SpriteRenderer headgear = CreateLayer(root.transform, "Headgear", new Vector3(0f, height * 0.42f, 0f), height * 0.5f, body.sortingOrder + 2);
                    cape.sortingLayerID = headgear.sortingLayerID = body.sortingLayerID;
                    cape.sortingOrder = body.sortingOrder - 1;

                    var cosmetics = root.AddComponent<GladiatorCosmetics>();
                    var so = new SerializedObject(cosmetics);
                    so.FindProperty("body").objectReferenceValue = body;
                    so.FindProperty("headgear").objectReferenceValue = headgear;
                    so.FindProperty("cape").objectReferenceValue = cape;
                    so.ApplyModifiedPropertiesWithoutUndo();

                    var arms = root.GetComponentInChildren<ArmSelectionController>(true);
                    var armsSo = new SerializedObject(arms);
                    armsSo.FindProperty("cosmetics").objectReferenceValue = cosmetics;
                    armsSo.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static SpriteRenderer CreateLayer(Transform parent, string name, Vector3 localPosition, float scale, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = false; // GladiatorCosmetics turns it on when the chosen item has art
            return renderer;
        }

        // ---------------------------------------------------------------- Row prefab

        private static void SetupRowPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath) != null)
                return;

            RectTransform rect = UiBuilder.CreateRect("SettingRow", null);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ApplyButtonColors(button);
            // Vertical only: left/right belong to the row (they change its value).
            button.navigation = new Navigation { mode = Navigation.Mode.Vertical };
            rect.gameObject.AddComponent<LayoutElement>().minHeight = 68f;

            Text label = UiBuilder.CreateText("Label", rect, "", 32, TextAnchor.MiddleLeft);
            label.color = Color.black;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiBuilder.Stretch((RectTransform)label.transform);
            ((RectTransform)label.transform).offsetMin = new Vector2(24f, 0f);

            Text value = UiBuilder.CreateText("Value", rect, "", 32, TextAnchor.MiddleRight);
            value.color = Color.black;
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiBuilder.Stretch((RectTransform)value.transform);
            ((RectTransform)value.transform).offsetMax = new Vector2(-24f, 0f);

            var row = rect.gameObject.AddComponent<SettingRow>();
            var so = new SerializedObject(row);
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("value").objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(rect.gameObject, RowPrefabPath);
            Object.DestroyImmediate(rect.gameObject);
        }

        private static void ApplyButtonColors(Button button)
        {
            var colors = button.colors;
            colors.normalColor = new Color(0.82f, 0.84f, 0.9f);
            colors.highlightedColor = new Color(1f, 0.92f, 0.6f);
            colors.selectedColor = new Color(1f, 0.8f, 0.2f);
            colors.pressedColor = new Color(0.9f, 0.6f, 0.1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.6f);
            button.colors = colors;
        }

        // ---------------------------------------------------------------- Shared UI pieces

        private static SettingRow RowPrefab() => AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath).GetComponent<SettingRow>();

        private static Button CreateCancelButton(string name, Transform parent, string label, float height)
        {
            Button button = UiBuilder.CreateButton(name, parent, label, height);
            button.gameObject.AddComponent<CancelRelay>();
            return button;
        }

        private static RectTransform CreateRowsContainer(Transform parent)
        {
            RectTransform rows = UiBuilder.CreateRect("Rows", parent);
            var layout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            rows.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            return rows;
        }

        private static SettingsScreen CreateSettingsScreen(Transform parent)
        {
            RectTransform root = UiBuilder.CreateDimmer("SettingsScreen", parent);
            RectTransform box = UiBuilder.CreateVerticalBox("Box", root, new Vector2(1100f, 940f), 14f, 34);
            UiBuilder.CreateText("Title", box, "Settings", 56, TextAnchor.MiddleCenter, 80f);
            RectTransform rows = CreateRowsContainer(box);
            Button back = CreateCancelButton("Back", box, "Back", 80f);

            var screen = root.gameObject.AddComponent<SettingsScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("rowPrefab").objectReferenceValue = RowPrefab();
            so.FindProperty("rowParent").objectReferenceValue = rows;
            so.FindProperty("backButton").objectReferenceValue = back;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.gameObject.SetActive(false);
            return screen;
        }

        // ---------------------------------------------------------------- Main Menu

        private static void SetupMainMenu()
        {
            EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<MainMenuController>();
            Transform safe = GameObject.Find("SafeArea").transform;

            // Wired on every run so an older M6 scene picks it up too.
            SetRef(controller, "title", safe.Find("Title").gameObject);
            if (Object.FindFirstObjectByType<CustomizationScreen>(FindObjectsInactive.Include) != null)
            {
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
                EditorSceneManager.SaveScene(controller.gameObject.scene);
                Debug.Log("MainMenu already has the M6 screens; left unchanged.");
                return;
            }

            Transform buttons = safe.Find("Buttons");

            // New Game / Continue / Settings / Quit
            Button newGame = buttons.Find("StartGame").GetComponent<Button>();
            newGame.name = "NewGame";
            newGame.GetComponentInChildren<Text>().text = "New Game";
            Button cont = buttons.Find("Continue").GetComponent<Button>();
            Button quit = buttons.Find("Quit").GetComponent<Button>();
            Button settingsButton = UiBuilder.CreateButton("Settings", buttons, "Settings", 100f);
            settingsButton.transform.SetSiblingIndex(quit.transform.GetSiblingIndex());
            ((RectTransform)buttons).sizeDelta = new Vector2(520f, 540f);

            SettingsScreen settings = CreateSettingsScreen(safe);
            CustomizationScreen customization = CreateCustomizationScreen(safe);

            var so = new SerializedObject(controller);
            so.FindProperty("menuButtons").objectReferenceValue = buttons.gameObject;
            so.FindProperty("newGameButton").objectReferenceValue = newGame;
            so.FindProperty("continueButton").objectReferenceValue = cont;
            so.FindProperty("settingsButton").objectReferenceValue = settingsButton;
            so.FindProperty("quitButton").objectReferenceValue = quit;
            so.FindProperty("settingsScreen").objectReferenceValue = settings;
            so.FindProperty("customizationScreen").objectReferenceValue = customization;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Start (Options) presses: dialogs first. The customization screen's main button is Confirm; Settings swallows it.
            var router = Object.FindFirstObjectByType<MenuPrimaryRouter>();
            var routerSo = new SerializedObject(router);
            SerializedProperty routes = routerSo.FindProperty("routes");
            GameObject confirmScope = routes.GetArrayElementAtIndex(0).FindPropertyRelative("Scope").objectReferenceValue as GameObject;
            SetRoutes(routes, new (GameObject, Button)[]
            {
                (confirmScope, null),
                (settings.gameObject, null),
                (customization.gameObject, customization.transform.Find("Panel/Confirm").GetComponent<Button>()),
                (buttons.gameObject, newGame),
            });
            routerSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            EditorSceneManager.SaveScene(controller.gameObject.scene);
        }

        private static void SetRoutes(SerializedProperty routes, (GameObject scope, Button button)[] entries)
        {
            routes.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty route = routes.GetArrayElementAtIndex(i);
                route.FindPropertyRelative("Scope").objectReferenceValue = entries[i].scope;
                route.FindPropertyRelative("Button").objectReferenceValue = entries[i].button;
            }
        }

        private static CustomizationScreen CreateCustomizationScreen(Transform safe)
        {
            // A full-screen holder without a backdrop, so the world-space preview shows through on the left.
            RectTransform root = UiBuilder.CreateRect("CustomizationScreen", safe);
            UiBuilder.Stretch(root);

            RectTransform panel = UiBuilder.CreateRect("Panel", root);
            panel.anchorMin = new Vector2(0.5f, 0.08f);
            panel.anchorMax = new Vector2(0.97f, 0.94f);
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            panel.gameObject.AddComponent<Image>().color = UiBuilder.BoxColor;
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(34, 34, 34, 34);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            UiBuilder.CreateText("Title", panel, "Customize your Gladiator", 50, TextAnchor.MiddleCenter, 80f);
            RectTransform rows = CreateRowsContainer(panel);
            Button randomize = CreateCancelButton("Randomize", panel, "Randomize", 80f);
            Button confirm = CreateCancelButton("Confirm", panel, "Confirm", 90f);
            Button back = CreateCancelButton("Back", panel, "Back", 70f);

            GladiatorCosmetics preview = CreatePreview();

            var screen = root.gameObject.AddComponent<CustomizationScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("rowPrefab").objectReferenceValue = RowPrefab();
            so.FindProperty("rowParent").objectReferenceValue = rows;
            so.FindProperty("randomizeButton").objectReferenceValue = randomize;
            so.FindProperty("confirmButton").objectReferenceValue = confirm;
            so.FindProperty("backButton").objectReferenceValue = back;
            so.FindProperty("preview").objectReferenceValue = preview;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.gameObject.SetActive(false);
            return screen;
        }

        /// <summary>The gladiator shown on the left of the customization screen: the player's layers plus four arms.</summary>
        private static GladiatorCosmetics CreatePreview()
        {
            GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            Sprite bodySprite;
            float height;
            Vector3 headgearPosition, capePosition, bodyScale;
            float headgearScale, capeScale;
            float ringRadius = 0.45f;
            try
            {
                var body = player.transform.Find("Body").GetComponent<SpriteRenderer>();
                bodySprite = body.sprite;
                bodyScale = body.transform.localScale;
                height = body.bounds.size.y;
                Transform headgear = player.transform.Find("Headgear");
                Transform cape = player.transform.Find("Cape");
                headgearPosition = headgear.localPosition;
                headgearScale = headgear.localScale.x;
                capePosition = cape.localPosition;
                capeScale = cape.localScale.x;
                var data = AssetDatabase.LoadAssetAtPath<PlayerData>("Assets/Data/Player/PlayerData.asset");
                if (data != null)
                    ringRadius = data.ArmRingRadius;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }

            var root = new GameObject("GladiatorPreview");
            root.transform.position = new Vector3(-4.6f, -0.2f, 0f);
            root.transform.localScale = Vector3.one * (3.4f / height);

            var bodyRenderer = new GameObject("Body").AddComponent<SpriteRenderer>();
            bodyRenderer.transform.SetParent(root.transform, false);
            bodyRenderer.sprite = bodySprite;
            bodyRenderer.transform.localScale = bodyScale;
            SpriteRenderer cape2 = CreateLayer(root.transform, "Cape", capePosition, capeScale, -1);
            SpriteRenderer headgear2 = CreateLayer(root.transform, "Headgear", headgearPosition, headgearScale, 2);

            // Four of the arm types around the body, drawn like in the game (attach point on the ring, pointing outward).
            var armAssets = AssetDatabase.FindAssets("t:WeaponArmData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<WeaponArmData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(a => a != null && a.Sprite != null)
                .OrderBy(a => a.name)
                .Take(4)
                .ToArray();
            var armRenderers = new SpriteRenderer[armAssets.Length];
            for (int i = 0; i < armAssets.Length; i++)
            {
                float compass = 45f + i * 90f;
                float radians = compass * Mathf.Deg2Rad;
                var armGo = new GameObject("Arm" + i);
                armGo.transform.SetParent(root.transform, false);
                armGo.transform.localPosition = new Vector3(Mathf.Sin(radians), Mathf.Cos(radians), 0f) * ringRadius;
                armGo.transform.localRotation = Quaternion.Euler(0f, 0f, 90f - compass + armAssets[i].ArtRotation);
                var renderer = armGo.AddComponent<SpriteRenderer>();
                renderer.sprite = armAssets[i].Sprite;
                renderer.sortingOrder = 1;
                armRenderers[i] = renderer;
            }

            var cosmetics = root.AddComponent<GladiatorCosmetics>();
            var so = new SerializedObject(cosmetics);
            so.FindProperty("body").objectReferenceValue = bodyRenderer;
            so.FindProperty("headgear").objectReferenceValue = headgear2;
            so.FindProperty("cape").objectReferenceValue = cape2;
            so.FindProperty("applyProfileOnAwake").boolValue = false;
            SerializedProperty arms = so.FindProperty("previewArms");
            arms.arraySize = armRenderers.Length;
            for (int i = 0; i < armRenderers.Length; i++)
                arms.GetArrayElementAtIndex(i).objectReferenceValue = armRenderers[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return cosmetics;
        }

        // ---------------------------------------------------------------- Game scene

        private static void SetupGame()
        {
            EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);
            var flow = Object.FindFirstObjectByType<GameFlowUI>();
            if (Object.FindFirstObjectByType<RoundIntroBanner>() != null)
            {
                Debug.Log("Game scene already has the M6 objects; left unchanged.");
                return;
            }

            Transform safe = GameObject.Find("FlowUI/SafeArea").transform;

            // Settings screen + a Settings button on the Pause panel.
            SettingsScreen settings = CreateSettingsScreen(safe);
            Transform pauseBox = safe.Find("PausePanel/Box");
            Button menuButton = pauseBox.Find("MainMenu").GetComponent<Button>();
            Button pauseSettings = UiBuilder.CreateButton("Settings", pauseBox, "Settings", 80f);
            pauseSettings.transform.SetSiblingIndex(menuButton.transform.GetSiblingIndex());
            var pauseRect = (RectTransform)pauseBox;
            pauseRect.sizeDelta += new Vector2(0f, 110f);

            var pausePanel = safe.Find("PausePanel").GetComponent<FlowPanel>();
            SetRef(pausePanel, "settingsButton", pauseSettings);
            SetRef(flow, "settings", settings);

            // Round intro banner: a big label in the upper part of the screen.
            var bannerRoot = UiBuilder.CreateRect("RoundIntro", safe);
            UiBuilder.Stretch(bannerRoot);
            Text label = UiBuilder.CreateText("Label", bannerRoot, "", 130, TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(1f, 0.92f, 0.45f);
            label.gameObject.AddComponent<Outline>().effectDistance = new Vector2(4f, -4f);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = new Vector2(0.05f, 0.58f);
            labelRect.anchorMax = new Vector2(0.95f, 0.92f);
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

            var banner = bannerRoot.gameObject.AddComponent<RoundIntroBanner>();
            var spawner = Object.FindFirstObjectByType<WaveSpawner>();
            var spawnerSo = new SerializedObject(spawner);
            var bannerSo = new SerializedObject(banner);
            bannerSo.FindProperty("tuning").objectReferenceValue = spawnerSo.FindProperty("tuning").objectReferenceValue;
            bannerSo.FindProperty("label").objectReferenceValue = label;
            bannerSo.ApplyModifiedPropertiesWithoutUndo();

            // The intro banner sits under the panels so screens still cover it; make it the first child of the safe area.
            bannerRoot.SetAsFirstSibling();

            // Debug overlay follows the "Show debug overlay" setting.
            var overlay = Object.FindFirstObjectByType<DebugOverlay>();
            if (overlay != null)
            {
                var toggleHost = new GameObject("DebugOverlayToggle").AddComponent<DebugOverlayToggle>();
                var toggleSo = new SerializedObject(toggleHost);
                SerializedProperty targets = toggleSo.FindProperty("targets");
                targets.arraySize = 1;
                targets.GetArrayElementAtIndex(0).objectReferenceValue = overlay.gameObject;
                toggleSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Start (Options) must not do anything while Settings is open.
            var router = Object.FindFirstObjectByType<MenuPrimaryRouter>();
            if (router != null)
            {
                var routerSo = new SerializedObject(router);
                SerializedProperty routes = routerSo.FindProperty("routes");
                int count = routes.arraySize;
                routes.InsertArrayElementAtIndex(0);
                SerializedProperty first = routes.GetArrayElementAtIndex(0);
                first.FindPropertyRelative("Scope").objectReferenceValue = settings.gameObject;
                first.FindPropertyRelative("Button").objectReferenceValue = null;
                routerSo.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"Game scene router now has {count + 1} routes.");
            }

            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
            EditorSceneManager.SaveScene(flow.gameObject.scene);
        }

        // ---------------------------------------------------------------- Helpers

        private static void SetRef(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
