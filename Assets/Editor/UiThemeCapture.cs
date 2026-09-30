using System.Collections.Generic;
using System.IO;
using BulletHell.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// UI1 screenshots, no Play mode needed: each screen is switched on in the open scene, its canvases are pointed at a
    /// camera, and the camera renders 1920x1080 into a PNG under Docs/Screenshots/UI1. Scene changes are discarded by
    /// reopening the scene afterwards. Shop and Armory lists are empty at edit time, so sample rows are added for the shot.
    /// </summary>
    public static class UiThemeCapture
    {
        private const string OutDir = "Docs/Screenshots/UI1";
        private const string MenuScene = "Assets/Scenes/MainMenu.unity";
        private const string GameScene = "Assets/Scenes/Game.unity";
        private const string RowPrefab = "Assets/Prefabs/UI/MenuRow.prefab";
        private const int Width = 1920, Height = 1080;

        private static readonly string[] MenuScreens = { "Title", "Buttons", "Message", "ConfirmOverwrite", "SettingsScreen", "CustomizationScreen", "GladiatorPreview" };
        private static BulletHell.Cosmetics.ProfileService captureProfile;
        private static readonly string[] GameScreens =
        {
            "CombatHud", "RoundIntro", "RunHud", "WaveBanner", "RoundResultsPanel", "ShopScreen", "ArmoryScreen", "PausePanel",
            "GameOverPanel", "SettingsScreen",
        };

        [MenuItem("BulletHell/UI1/3 Capture Screens")]
        public static void Run()
        {
            Directory.CreateDirectory(OutDir);

            EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            Shoot("main_menu", MenuScreens, "Title", "Buttons");
            Shoot("settings", MenuScreens, "SettingsScreen");
            captureProfile = new BulletHell.Cosmetics.ProfileService(AssetDatabase.LoadAssetAtPath<BulletHell.Save.AssetRegistry>("Assets/Data/AssetRegistry.asset"),
                new BulletHell.Save.JsonFileStore<BulletHell.Cosmetics.ProfileData>(Path.Combine(Path.GetTempPath(), "cc1_capture_profile.json")));
            Shoot("character_creation", MenuScreens, "CustomizationScreen", "GladiatorPreview");
            captureProfile.Randomize(n => 1 + (n > 2 ? 1 : 0));
            Shoot("character_creation_random", MenuScreens, "CustomizationScreen", "GladiatorPreview");
            captureProfile.Randomize(_ => 0);
            Shoot("confirm_overwrite", MenuScreens, "Title", "Buttons", "ConfirmOverwrite");

            EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            Shoot("hud", GameScreens, "CombatHud");
            captureProfile.Randomize(n => 1);
            Shoot("hud_random_look", GameScreens, "CombatHud");
            Shoot("round_results", GameScreens, "RoundResultsPanel");
            Shoot("shop", GameScreens, true, "ShopScreen");
            Shoot("armory", GameScreens, true, "ArmoryScreen");
            Shoot("pause", GameScreens, "PausePanel");
            Shoot("game_over", GameScreens, "GameOverPanel");

            ShootThemeSheet();

            EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single); // discards the temporary changes
            Debug.Log("UI1: screenshots written to " + OutDir);
        }

        private static void Shoot(string file, string[] all, params string[] show) => Shoot(file, all, false, show);

        private static void Shoot(string file, string[] all, bool sampleRows, params string[] show)
        {
            var wanted = new HashSet<string>(show);
            foreach (Transform t in AllTransforms())
            {
                if (System.Array.IndexOf(all, t.name) < 0)
                    continue;
                t.gameObject.SetActive(wanted.Contains(t.name));
            }

            PrepareSpecial(file);

            if (sampleRows)
                AddSampleRows(show[0]);

            Camera cam = PrepareCamera();
            Render(cam, Path.Combine(OutDir, file + ".png"));
        }

        // Runtime-built parts are missing at edit time: fill the creation rows and dress the preview / portrait from a scratch profile.
        private static void PrepareSpecial(string file)
        {
            if (file.StartsWith("character_creation"))
            {
                var screen = Object.FindFirstObjectByType<CustomizationScreen>(FindObjectsInactive.Include);
                Transform rows = screen.transform.Find("Panel");
                var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/SettingRow.prefab").GetComponent<SettingRow>();
                foreach (var old in Object.FindObjectsByType<SettingRow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    Object.DestroyImmediate(old.gameObject);
                Transform container = rows.Find("Rows") != null ? rows.Find("Rows") : rows;
                for (int i = 0; i < BulletHell.Cosmetics.CosmeticSlots.Count; i++)
                {
                    var slot = (BulletHell.Cosmetics.CosmeticSlot)i;
                    SettingRow row = (SettingRow)PrefabUtility.InstantiatePrefab(rowPrefab, container);
                    row.Bind(BulletHell.Cosmetics.CosmeticSlots.Label(slot), () => captureProfile.Get(slot).DisplayName, _ => { });
                    row.transform.SetSiblingIndex(i);
                    row.Refresh();
                }
                Object.FindFirstObjectByType<BulletHell.Cosmetics.GladiatorCosmetics>(FindObjectsInactive.Include).Apply(captureProfile);
            }
            else if (file.StartsWith("hud"))
            {
                Object.FindFirstObjectByType<HudPortrait>(FindObjectsInactive.Include).Apply(captureProfile);
            }
        }

        private static IEnumerable<Transform> AllTransforms()
        {
            var scene = EditorSceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    yield return t;
        }

        private static void AddSampleRows(string screenName)
        {
            Transform content = null;
            foreach (Transform t in AllTransforms())
                if (t.name == screenName)
                    content = t.Find("Box/List/Viewport/Content");
            if (content == null)
                return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefab);
            string[] rows = screenName == "ShopScreen"
                ? new[] { "Spread Arm - 120", "Homing Armament - 60", "Velocity Armament - 45", "Pierce Armament - 80", "Reroll - 15" }
                : new[] { "Slot E: Blue Arm (2 armaments)", "Slot N: empty", "Slot W: Green Arm (1 armament)", "Slot S: empty" };
            foreach (string text in rows)
            {
                GameObject row = (GameObject)PrefabUtility.InstantiatePrefab(prefab, content);
                row.GetComponent<MenuRow>().Setup(text, null, null, false);
            }
        }

        // Points every root canvas at one camera so a plain camera render includes the UI.
        private static Camera PrepareCamera()
        {
            Camera cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            if (cam == null)
                cam = new GameObject("CaptureCamera", typeof(Camera)).GetComponent<Camera>();
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas)
                    continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }
            return cam;
        }

        private static void Render(Camera cam, string path)
        {
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previousTarget = cam.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            cam.targetTexture = rt;
            for (int i = 0; i < 2; i++) // the second pass picks up text glyphs built by the first
            {
                Canvas.ForceUpdateCanvases();
                cam.Render();
            }
            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            cam.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }

        // A single sheet of every themed role, so the sprite mapping and the 9-slice borders can be checked at a glance.
        private static void ShootThemeSheet()
        {
            UITheme theme = UITheme.Current;
            foreach (Transform t in AllTransforms())
                if (System.Array.IndexOf(GameScreens, t.name) >= 0)
                    t.gameObject.SetActive(false);

            var canvasGo = new GameObject("ThemeSheet", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var bg = UiBuilder.CreateRect("Bg", canvasGo.transform);
            UiBuilder.Stretch(bg);
            bg.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.2f);

            float x = -800f, y = 400f;
            UiBuilder.CreateText("Caption", canvasGo.transform, "UITheme sheet: panel, buttons (normal / highlighted / selected / pressed / disabled), tab, tooltip, card, slot, slider, toggle", 26, TextAnchor.UpperLeft);
            var caption = (RectTransform)canvasGo.transform.Find("Caption");
            caption.anchoredPosition = new Vector2(0f, 500f);
            caption.sizeDelta = new Vector2(1800f, 40f);

            void Box(string name, ThemeRole role, Vector2 pos, Vector2 size)
            {
                RectTransform r = UiBuilder.CreateRect(name, canvasGo.transform);
                r.anchoredPosition = pos;
                r.sizeDelta = size;
                r.gameObject.AddComponent<Image>();
                r.gameObject.AddComponent<ThemedImage>().Role = role;
            }

            void Pic(string name, Sprite sprite, Vector2 pos, Vector2 size, bool sliced)
            {
                RectTransform r = UiBuilder.CreateRect(name, canvasGo.transform);
                r.anchoredPosition = pos;
                r.sizeDelta = size;
                var img = r.gameObject.AddComponent<Image>();
                img.sprite = sprite;
                img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
                img.pixelsPerUnitMultiplier = theme.BorderMultiplier;
                img.preserveAspect = !sliced;
            }

            Box("Panel", ThemeRole.Panel, new Vector2(x + 200f, y - 120f), new Vector2(400f, 240f));
            Box("Card", ThemeRole.Card, new Vector2(x + 500f, y - 150f), new Vector2(200f, 280f));
            Box("Tooltip", ThemeRole.Tooltip, new Vector2(x + 800f, y - 120f), new Vector2(360f, 200f));
            Box("Inset", ThemeRole.Inset, new Vector2(x + 1150f, y - 120f), new Vector2(300f, 200f));
            Box("Slot", ThemeRole.Slot, new Vector2(x + 1400f, y - 120f), new Vector2(120f, 120f));
            Box("Header", ThemeRole.Header, new Vector2(x + 1400f, y - 250f), new Vector2(300f, 60f));
            Box("Tab", ThemeRole.Tab, new Vector2(x + 200f, y - 340f), new Vector2(260f, 70f));
            Box("TabSelected", ThemeRole.TabSelected, new Vector2(x + 500f, y - 340f), new Vector2(260f, 70f));

            UITheme.ButtonSprites b = theme.Button;
            Sprite[] states = { b.normal, b.highlighted, b.selected, b.pressed, b.disabled };
            string[] names = { "normal", "highlighted", "selected", "pressed", "disabled" };
            for (int i = 0; i < states.Length; i++)
            {
                Pic("Button_" + names[i], states[i], new Vector2(x + 150f + i * 300f, y - 520f), new Vector2(260f, 90f), true);
                Text label = UiBuilder.CreateText("L" + i, canvasGo.transform, names[i], 30, TextAnchor.MiddleCenter);
                label.color = theme.TextOnButton;
                ((RectTransform)label.transform).anchoredPosition = new Vector2(x + 150f + i * 300f, y - 520f);
                ((RectTransform)label.transform).sizeDelta = new Vector2(260f, 90f);
            }

            Pic("SliderTrack", theme.SliderTrack, new Vector2(x + 300f, y - 700f), new Vector2(500f, 50f), true);
            Pic("SliderFill", theme.SliderFill, new Vector2(x + 200f, y - 700f), new Vector2(280f, 30f), true);
            Pic("SliderHandle", theme.SliderHandle, new Vector2(x + 340f, y - 700f), new Vector2(60f, 66f), false);
            Pic("ToggleOff", theme.ToggleOff, new Vector2(x + 800f, y - 700f), new Vector2(136f, 64f), false);
            Pic("ToggleOn", theme.ToggleOn, new Vector2(x + 1000f, y - 700f), new Vector2(136f, 64f), false);
            Pic("Arrow", theme.Arrow, new Vector2(x + 1200f, y - 700f), new Vector2(80f, 60f), false);

            foreach (RectTransform r in canvasGo.GetComponentsInChildren<RectTransform>())
                if (r != canvasGo.transform && r.name != "Bg" && r.name != "Caption")
                    r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);

            Camera cam = PrepareCamera();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 1000;
            Render(cam, Path.Combine(OutDir, "theme_sheet.png"));
        }
    }
}
