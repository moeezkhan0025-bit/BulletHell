using System.Collections.Generic;
using System.IO;
using BulletHell.Core;
using BulletHell.UI;
using UnityEditor;
using UnityEngine;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// UI1 setup. Copies the sprites we use out of the Dobo "Mega Cozy UI Pack" demo into Assets/UI/DoboCozy (the pack
    /// folder itself is never touched), imports them as Point-filtered single sprites with 9-slice borders, and builds
    /// the UITheme asset that maps UI roles to them. Safe to run again.
    /// </summary>
    public static class UiThemeSetup
    {
        private const string PackRoot = "Assets/ThirdParty/UI-PackDWNLD/DEMO_MegaCozyUIPack_doboui - copia/";
        private const string CopyRoot = "Assets/UI/DoboCozy/";
        private const string ThemePath = "Assets/Data/UI/UITheme.asset";
        private const string ConfigPath = "Assets/Resources/GameConfig.asset";

        // pack-relative path -> border in source pixels (left, bottom, right, top); all zero = not sliced.
        private static readonly (string path, Vector4 border)[] Sprites =
        {
            ("Containers/SimpleColorContainers/SimpleColorContainer1_marine.png", new Vector4(7, 7, 7, 7)),
            ("Containers/SimpleColorContainers/SimpleColorContainer1_wood.png", new Vector4(7, 7, 7, 7)),
            ("Buttons/Square/SquareButton1_wood.png", new Vector4(8, 8, 8, 8)),
            ("Buttons/Square/SquareButton1_pink.png", new Vector4(8, 8, 8, 8)),
            ("Buttons/Square/IsPressed/SquarePressedButton1_wood.png", new Vector4(8, 8, 8, 8)),
            ("Buttons/Inflated/IsPressed/InflatedlPressedButton1_gray.png", new Vector4(8, 8, 8, 8)),
            ("Cards/Card1_gray.png", new Vector4(6, 6, 6, 17)),
            ("Cards/Card1_purple.png", new Vector4(6, 6, 6, 17)),
            ("Frames/Frame1_cream.png", new Vector4(9, 9, 9, 9)),
            ("Frames/Frame1_wood.png", new Vector4(9, 9, 9, 9)),
            ("Labels/Label1_blue.png", new Vector4(6, 2, 6, 2)),
            ("Labels/Label1_yellow.png", new Vector4(6, 2, 6, 2)),
            ("Headers/Header1_green.png", Vector4.zero),
            ("Inventory/ItemSlots/ItemSlotsDefault/SlotDefaultl1_cream.png", new Vector4(8, 8, 8, 8)),
            ("Inventory/ItemSlots/ItemSlotsSelected/SlotHover1_cream.png", new Vector4(9, 9, 9, 9)),
            ("ProgressBars/ProgressBar/ProgressBarFill1_wood.png", new Vector4(4, 3, 4, 3)),
            ("Buttons/Round/SmallSize/SmallRoundButton1_cream.png", Vector4.zero),
            ("Toggles/Toggle4.png", Vector4.zero),
            ("Toggles/Toggle8.png", Vector4.zero),
            ("Arrows/Arrow5_cream.png", Vector4.zero),
        };

        [MenuItem("BulletHell/UI1/1 Import Theme Sprites And Create Theme")]
        public static void Run()
        {
            CopyAndImportSprites();
            UITheme theme = CreateTheme();
            AssignToConfig(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("UI1: theme sprites imported to " + CopyRoot + " and UITheme created at " + ThemePath);
        }

        private static string DestPath(string packPath) => CopyRoot + Path.GetFileName(packPath);

        private static void CopyAndImportSprites()
        {
            Directory.CreateDirectory(CopyRoot);
            AssetDatabase.Refresh();
            foreach (var (path, _) in Sprites)
            {
                string dest = DestPath(path);
                if (!File.Exists(dest))
                    File.Copy(PackRoot + path, dest);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (var (path, border) in Sprites)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(DestPath(path));
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }
        }

        private static Sprite S(string packPath) => AssetDatabase.LoadAssetAtPath<Sprite>(DestPath(packPath));

        private static UITheme CreateTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ThemePath));
                theme = ScriptableObject.CreateInstance<UITheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            var so = new SerializedObject(theme);
            Set(so, "panel", S("Containers/SimpleColorContainers/SimpleColorContainer1_marine.png"));
            Set(so, "inset", S("Frames/Frame1_wood.png"));
            Set(so, "cardFrame", S("Cards/Card1_gray.png"));
            Set(so, "cardFrameFocused", S("Cards/Card1_purple.png"));
            Set(so, "tooltip", S("Frames/Frame1_cream.png"));
            Set(so, "header", S("Headers/Header1_green.png"));
            Set(so, "button.normal", S("Buttons/Square/SquareButton1_wood.png"));
            Set(so, "button.highlighted", S("Buttons/Square/SquareButton1_pink.png"));
            Set(so, "button.selected", S("Buttons/Square/SquareButton1_pink.png"));
            Set(so, "button.pressed", S("Buttons/Square/IsPressed/SquarePressedButton1_wood.png"));
            Set(so, "button.disabled", S("Buttons/Inflated/IsPressed/InflatedlPressedButton1_gray.png"));
            Set(so, "tab", S("Labels/Label1_blue.png"));
            Set(so, "tabSelected", S("Labels/Label1_yellow.png"));
            Set(so, "sliderTrack", S("Frames/Frame1_wood.png"));
            Set(so, "sliderFill", S("ProgressBars/ProgressBar/ProgressBarFill1_wood.png"));
            Set(so, "sliderHandle", S("Buttons/Round/SmallSize/SmallRoundButton1_cream.png"));
            Set(so, "toggleOff", S("Toggles/Toggle4.png"));
            Set(so, "toggleOn", S("Toggles/Toggle8.png"));
            Set(so, "arrow", S("Arrows/Arrow5_cream.png"));
            Set(so, "hudFrame", S("Inventory/ItemSlots/ItemSlotsDefault/SlotDefaultl1_cream.png"));
            Set(so, "hudFrameFocused", S("Inventory/ItemSlots/ItemSlotsSelected/SlotHover1_cream.png"));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            return theme;
        }

        private static void Set(SerializedObject so, string property, Sprite sprite)
        {
            SerializedProperty p = so.FindProperty(property);
            if (p == null || sprite == null)
            {
                Debug.LogWarning("UI1: could not set " + property);
                return;
            }
            p.objectReferenceValue = sprite;
        }

        private static void AssignToConfig(UITheme theme)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var so = new SerializedObject(config);
            so.FindProperty("uiTheme").objectReferenceValue = theme;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        // ---- Step 2: apply the theme to the existing screens

        private static readonly string[] ScenePaths = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Game.unity" };
        private static readonly string[] PrefabPaths = { "Assets/Prefabs/UI/MenuRow.prefab", "Assets/Prefabs/UI/SettingRow.prefab" };

        [MenuItem("BulletHell/UI1/2 Apply Theme To Scenes And Prefabs")]
        public static void ApplyAll()
        {
            UITheme theme = UITheme.Current;
            if (theme == null)
            {
                Debug.LogError("UI1: run step 1 first (GameConfig has no UITheme).");
                return;
            }

            foreach (string path in PrefabPaths)
            {
                GameObject contents = UnityEditor.PrefabUtility.LoadPrefabContents(path);
                ApplyTo(contents.transform, theme);
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(contents, path);
                UnityEditor.PrefabUtility.UnloadPrefabContents(contents);
            }

            foreach (string path in ScenePaths)
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
                foreach (GameObject root in scene.GetRootGameObjects())
                    ApplyTo(root.transform, theme);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("UI1: theme applied to scenes and row prefabs.");
        }

        private static T Ensure<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static bool HasAncestorNamed(Transform t, params string[] names)
        {
            for (Transform p = t.parent; p != null; p = p.parent)
                foreach (string n in names)
                    if (p.name == n)
                        return true;
            return false;
        }

        private static void ApplyTo(Transform root, UITheme theme)
        {
            foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                GameObject go = image.gameObject;
                string n = go.name;
                Transform parent = go.transform.parent;
                string parentName = parent != null ? parent.name : "";

                if (go.GetComponent<BulletHell.Shop.ShopCard>() != null)
                    continue; // Shop cards have their own look (rarity frame); not a themed button

                if (go.GetComponent<UnityEngine.UI.Button>() != null)
                {
                    Ensure<ThemedButton>(go).Apply();
                }
                else if (n == "Box" || n == "Panel")
                {
                    Ensure<ThemedImage>(go).Role = ThemeRole.Panel;
                }
                else if (n == "List")
                {
                    Ensure<ThemedImage>(go).Role = ThemeRole.Inset;
                }
                else if (n == "Frame" && parentName == "Portrait")
                {
                    var themed = Ensure<ThemedImage>(go);
                    themed.PixelScaleOverride = 3f;
                    themed.Role = ThemeRole.Slot;
                }
                else if (n == "Frame" && parentName.StartsWith("Slot"))
                {
                    var themed = Ensure<ThemedImage>(go);
                    themed.PixelScaleOverride = 3f;
                    themed.KeepColor = true;
                    themed.Role = ThemeRole.Slot;
                    image.color = theme.HudSlotFilled;
                }
                else if (n == "Back" && parentName == "HeatBar")
                {
                    var themed = Ensure<ThemedImage>(go);
                    themed.PixelScaleOverride = 2f;
                    themed.Role = ThemeRole.Inset;
                }
                EditorUtility.SetDirty(go);
            }

            // Text on panels (titles, body): the theme's panel text color. Button labels are handled by ThemedButton.
            foreach (var text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                if (text.GetComponentInParent<UnityEngine.UI.Button>() != null)
                    continue;
                if (HasAncestorNamed(text.transform, "Box", "Panel"))
                {
                    text.color = theme.TextOnPanel;
                    EditorUtility.SetDirty(text);
                }
            }
        }
    }
}
