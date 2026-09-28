using System.Linq;
using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Input;
using BulletHell.Save;
using BulletHell.UI;
using BulletHell.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// One-shot M4 (part 1) setup: save IDs and the AssetRegistry, the GameConfig, the Boot and MainMenu scenes, the flow
    /// UI and controllers in the Game scene, and the build scene order. Safe to run again: existing assets are updated
    /// and existing scenes/objects are left alone.
    /// </summary>
    public static class M4Setup
    {
        private const string RegistryPath = "Assets/Data/AssetRegistry.asset";
        private const string ConfigPath = "Assets/Resources/GameConfig.asset";
        private const string BootPath = "Assets/Scenes/Boot.unity";
        private const string MenuPath = "Assets/Scenes/MainMenu.unity";
        private const string GamePath = "Assets/Scenes/Game.unity";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

        [MenuItem("BulletHell/M4/Setup Everything")]
        public static void Run()
        {
            CollectRegistry();
            CreateConfig();
            CleanPlayerPrefab();
            SetupBoot();
            SetupMainMenu();
            SetupGame();
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("M4 setup complete.");
        }

        /// <summary>Gives every arm, armament and ammo asset a save ID (its asset name, if it has none) and fills the registry.</summary>
        [MenuItem("BulletHell/Collect Asset Registry")]
        public static void CollectRegistry()
        {
            var arms = FindAll<WeaponArmData>();
            var armaments = FindAll<ArmamentData>();
            var ammo = FindAll<AmmoTypeData>();

            foreach (WeaponArmData arm in arms)
                if (string.IsNullOrEmpty(arm.Id))
                    arm.SetId(arm.name);
            foreach (ArmamentData armament in armaments)
                if (string.IsNullOrEmpty(armament.Id))
                    armament.SetId(armament.name);
            foreach (AmmoTypeData a in ammo)
                if (string.IsNullOrEmpty(a.Id))
                    a.SetId(a.name);

            var registry = AssetDatabase.LoadAssetAtPath<AssetRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<AssetRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }
            registry.Set(arms, armaments, ammo);
            AssetDatabase.SaveAssets();
            Debug.Log($"Asset registry: {arms.Length} arms, {armaments.Length} armaments, {ammo.Length} ammo types.");
        }

        private static T[] FindAll<T>() where T : Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name)
                .ToArray();

        private static void CreateConfig()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            var so = new SerializedObject(config);
            so.FindProperty("registry").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AssetRegistry>(RegistryPath);
            // The debug loadout keeps the current test setup (all 8 arms). Point this at StartingLoadout for a real run.
            so.FindProperty("newRunLoadout").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ArmLoadout>("Assets/Data/Loadouts/DebugLoadout.asset");
            SetList(so.FindProperty("startingAmmo"), new Object[]
            {
                AssetDatabase.LoadAssetAtPath<AmmoTypeData>("Assets/Data/Ammo/Ammo_Basic.asset"), null, null, null,
            });
            SetList(so.FindProperty("startingArmaments"), new Object[]
            {
                Armament("Damage"), Armament("FireRate"), Armament("BulletSpeed"), Armament("ExtraProjectile"),
                Armament("Pierce"), Armament("Pierce"), Armament("Burn"), Armament("Burn"),
                Armament("Stun"), Armament("Stun"), Armament("Ricochet"), Armament("Ricochet"),
            });
            SetList(so.FindProperty("startingSpareArms"), new Object[]
            {
                AssetDatabase.LoadAssetAtPath<WeaponArmData>("Assets/Data/Arms/Arm_Red.asset"),
                AssetDatabase.LoadAssetAtPath<WeaponArmData>("Assets/Data/Arms/Arm_Blue.asset"),
            });
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static ArmamentData Armament(string name) =>
            AssetDatabase.LoadAssetAtPath<ArmamentData>($"Assets/Data/Armaments/Armament_{name}.asset");

        private static void SetList(SerializedProperty list, Object[] items)
        {
            list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        /// <summary>Re-saves the Player prefab so fields that moved to GameConfig/RunState drop out of it.</summary>
        private static void CleanPlayerPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------------------------------------------------------------- Boot

        private static void SetupBoot()
        {
            var scene = EditorSceneManager.OpenScene(BootPath, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<Bootstrapper>() == null)
                new GameObject("Bootstrapper").AddComponent<Bootstrapper>();
            if (Camera.main == null && Object.FindFirstObjectByType<Camera>() == null)
                CreateCamera();
            EditorSceneManager.SaveScene(scene);
        }

        // ---------------------------------------------------------------- Main Menu

        private static void SetupMainMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath) != null)
            {
                Debug.Log("MainMenu scene already exists; left unchanged.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            GameObject events = CreateEventSystem();

            Canvas canvas = UiBuilder.CreateCanvas("MenuCanvas", 0);
            RectTransform safe = UiBuilder.CreateRect("SafeArea", canvas.transform);
            UiBuilder.Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            Text title = UiBuilder.CreateText("Title", safe, "BULLET HELL", 110, TextAnchor.MiddleCenter);
            var titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0f, 0.68f);
            titleRect.anchorMax = new Vector2(1f, 0.92f);
            titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;

            RectTransform buttons = UiBuilder.CreateRect("Buttons", safe);
            UiBuilder.Center(buttons, new Vector2(520f, 400f));
            buttons.anchoredPosition = new Vector2(0f, -70f);
            var layout = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 28f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Button start = UiBuilder.CreateButton("StartGame", buttons, "Start Game", 100f);
            Button cont = UiBuilder.CreateButton("Continue", buttons, "Continue", 100f);
            Button quit = UiBuilder.CreateButton("Quit", buttons, "Quit", 100f);

            Text message = UiBuilder.CreateText("Message", safe, "", 32, TextAnchor.MiddleCenter);
            var messageRect = (RectTransform)message.transform;
            messageRect.anchorMin = new Vector2(0f, 0.05f);
            messageRect.anchorMax = new Vector2(1f, 0.15f);
            messageRect.offsetMin = messageRect.offsetMax = Vector2.zero;

            // Overwrite confirmation (a sibling drawn last, so it covers the menu).
            RectTransform confirm = UiBuilder.CreateDimmer("ConfirmOverwrite", safe);
            RectTransform box = UiBuilder.CreateVerticalBox("Box", confirm, new Vector2(900f, 420f), 30f, 40);
            UiBuilder.CreateText("Question", box, "Overwrite your saved run?", 52, TextAnchor.MiddleCenter, 90f);
            UiBuilder.CreateText("Detail", box, "Starting a new game deletes the existing save.", 32, TextAnchor.MiddleCenter, 70f);
            RectTransform row = UiBuilder.CreateRect("Choices", box);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 40f;
            rowLayout.childControlWidth = rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().minHeight = 100f;
            Button yes = UiBuilder.CreateButton("Overwrite", row, "Overwrite", 100f);
            Button no = UiBuilder.CreateButton("Cancel", row, "Cancel", 100f);

            var controller = new GameObject("MainMenu").AddComponent<MainMenuController>();
            var so = new SerializedObject(controller);
            so.FindProperty("startButton").objectReferenceValue = start;
            so.FindProperty("continueButton").objectReferenceValue = cont;
            so.FindProperty("quitButton").objectReferenceValue = quit;
            so.FindProperty("messageText").objectReferenceValue = message;
            so.FindProperty("confirmPanel").objectReferenceValue = confirm.gameObject;
            so.FindProperty("confirmYesButton").objectReferenceValue = yes;
            so.FindProperty("confirmNoButton").objectReferenceValue = no;
            so.ApplyModifiedPropertiesWithoutUndo();

            confirm.gameObject.SetActive(false);
            Selection.activeGameObject = events;
            EditorSceneManager.SaveScene(scene, MenuPath);
        }

        // ---------------------------------------------------------------- Game scene

        private static void SetupGame()
        {
            var scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<GameSceneController>() != null)
            {
                Debug.Log("Game scene already has the M4 flow objects; left unchanged.");
                return;
            }

            var input = Object.FindFirstObjectByType<GameplayInputReader>();
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                CreateEventSystem();

            var flow = new GameObject("GameFlow");
            var sceneController = flow.AddComponent<GameSceneController>();
            SetRef(sceneController, "input", input);

            Canvas canvas = UiBuilder.CreateCanvas("FlowUI", 20);
            RectTransform safe = UiBuilder.CreateRect("SafeArea", canvas.transform);
            UiBuilder.Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            Text hudText = UiBuilder.CreateText("RunHud", safe, "", 34, TextAnchor.UpperRight);
            var hudRect = (RectTransform)hudText.transform;
            hudRect.anchorMin = hudRect.anchorMax = hudRect.pivot = new Vector2(1f, 1f);
            hudRect.sizeDelta = new Vector2(700f, 60f);
            hudRect.anchoredPosition = new Vector2(-24f, -20f);
            var hud = hudText.gameObject.AddComponent<RunHud>();
            SetRef(hud, "label", hudText);

            FlowPanel results = CreateFlowPanel(safe, "RoundResultsPanel", "Continue to Shop");
            FlowPanel shop = CreateFlowPanel(safe, "ShopPanel", "Continue to Armory");
            FlowPanel armory = CreateFlowPanel(safe, "ArmoryPanel", "Start Next Round");

            var ui = flow.AddComponent<GameFlowUI>();
            var uiSo = new SerializedObject(ui);
            uiSo.FindProperty("scene").objectReferenceValue = sceneController;
            uiSo.FindProperty("roundResults").objectReferenceValue = results;
            uiSo.FindProperty("shop").objectReferenceValue = shop;
            uiSo.FindProperty("armory").objectReferenceValue = armory;
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static FlowPanel CreateFlowPanel(Transform parent, string name, string continueLabel)
        {
            RectTransform root = UiBuilder.CreateDimmer(name, parent);
            RectTransform box = UiBuilder.CreateVerticalBox("Box", root, new Vector2(900f, 640f), 26f, 44);
            Text title = UiBuilder.CreateText("Title", box, "", 60, TextAnchor.MiddleCenter, 100f);
            Text body = UiBuilder.CreateText("Body", box, "", 36, TextAnchor.UpperCenter);
            body.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            Button cont = UiBuilder.CreateButton("Continue", box, continueLabel, 100f);
            Button menu = UiBuilder.CreateButton("MainMenu", box, "Main Menu", 80f);

            var panel = root.gameObject.AddComponent<FlowPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("continueButton").objectReferenceValue = cont;
            so.FindProperty("menuButton").objectReferenceValue = menu;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        // ---------------------------------------------------------------- Shared

        private static void SetBuildScenes()
        {
            EditorBuildSettings.scenes = new[] { BootPath, MenuPath, GamePath }
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();
        }

        private static void SetRef(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetList<T>(SerializedProperty list, T[] items) where T : Object =>
            SetList(list, items.Cast<Object>().ToArray());

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var cam = go.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.07f);
            go.transform.position = new Vector3(0f, 0f, -10f);
        }

        /// <summary>EventSystem with the new Input System UI module (stick/D-pad navigate, south button submits).</summary>
        private static GameObject CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule), typeof(UIFocusGuard));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            return go;
        }
    }
}
