using System;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BulletHell.Core;
using BulletHell.Cosmetics;
using BulletHell.Feedback;
using BulletHell.Input;
using BulletHell.Player;
using BulletHell.Save;
using BulletHell.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// CC1 setup: generates the placeholder paper-doll parts (3 clearly different variants per slot, all on the player's
    /// character canvas), the Sprite Library that holds them, the CosmeticPartData assets and their registry entries,
    /// the GladiatorDoll prefab, rebuilds the Player prefab from it, and rebuilds the Character Creation preview,
    /// layout and the HUD portrait in the scenes. The player's own Playersprite.png is Body variant 1 and is never touched.
    /// Safe to run again.
    /// </summary>
    public static class Cc1Setup
    {
        private const string PartsArt = "Assets/Art/Placeholder/Parts";
        private const string CosmeticsFolder = "Assets/Data/Cosmetics";
        private const string LibraryPath = CosmeticsFolder + "/GladiatorParts.asset";
        private const string DollPrefabPath = "Assets/Prefabs/GladiatorDoll.prefab";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string PlayerSpritePath = "Assets/Art/Player/Playersprite.png";
        private const string RegistryPath = "Assets/Data/AssetRegistry.asset";
        private const string MenuScene = "Assets/Scenes/MainMenu.unity";
        private const string GameScene = "Assets/Scenes/Game.unity";
        private const int W = 302, H = 315; // the player sprite's canvas; every part uses it so parts need no offsets
        private const float Ppu = 273.91304f; // 315 / 1.15: the player sprite at the locked 1.15x character scale

        // slot, id, display name, drawing (null = the player's own sprite)
        private static readonly (CosmeticSlot slot, string name, Action<Painter> draw)[] Parts =
        {
            (CosmeticSlot.Body, "Original", null),
            (CosmeticSlot.Body, "Gumdrop", p => p.Body_Gumdrop()),
            (CosmeticSlot.Body, "Mint Bean", p => p.Body_Mint()),
            (CosmeticSlot.Armor, "Plate", p => p.Armor_Plate()),
            (CosmeticSlot.Armor, "Stripes", p => p.Armor_Stripes()),
            (CosmeticSlot.Armor, "Belt", p => p.Armor_Belt()),
            (CosmeticSlot.Head, "Round", p => p.Head_Round()),
            (CosmeticSlot.Head, "Square", p => p.Head_Square()),
            (CosmeticSlot.Head, "Pointy", p => p.Head_Pointy()),
            (CosmeticSlot.Accessory1, "Crown", p => p.Acc1_Crown()),
            (CosmeticSlot.Accessory1, "Horns", p => p.Acc1_Horns()),
            (CosmeticSlot.Accessory1, "Bow", p => p.Acc1_Bow()),
            (CosmeticSlot.Accessory2, "Crimson Cape", p => p.Acc2_Cape()),
            (CosmeticSlot.Accessory2, "Banner", p => p.Acc2_Banner()),
            (CosmeticSlot.Accessory2, "Backpack", p => p.Acc2_Backpack()),
        };

        private static string LabelOf(string name) => name.ToLowerInvariant().Replace(' ', '_');
        private static string IdOf(CosmeticSlot slot, string name) => slot.ToString().ToLowerInvariant() + "_" + LabelOf(name);
        private static string PngPath(CosmeticSlot slot, string name) => $"{PartsArt}/part_{slot.ToString().ToLowerInvariant()}_{LabelOf(name)}.png";

        [MenuItem("BulletHell/CC1/Setup Everything")]
        public static void Run()
        {
            Directory.CreateDirectory(PartsArt);
            Directory.CreateDirectory(CosmeticsFolder);

            Sprite[] sprites = CreatePartSprites();
            SpriteLibraryAsset library = CreateLibrary(sprites);
            CosmeticPartData[] parts = CreatePartAssets();
            RegisterParts(parts, library);
            GameObject doll = CreateDollPrefab(library);
            SetupPlayerPrefab(doll);
            SetupMainMenu(doll);
            SetupGame();
            AssetDatabase.SaveAssets();
            Debug.Log("CC1 setup complete.");
        }

        // ---------------------------------------------------------------- Part art

        private static Sprite[] CreatePartSprites()
        {
            var result = new Sprite[Parts.Length];
            for (int i = 0; i < Parts.Length; i++)
            {
                if (Parts[i].draw == null)
                {
                    result[i] = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
                    continue;
                }
                string path = PngPath(Parts[i].slot, Parts[i].name);
                var painter = new Painter(W, H);
                Parts[i].draw(painter);
                File.WriteAllBytes(path, painter.ToPng());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = Ppu;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                result[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return result;
        }

        private static SpriteLibraryAsset CreateLibrary(Sprite[] sprites)
        {
            var library = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            for (int i = 0; i < Parts.Length; i++)
                library.AddCategoryLabel(sprites[i], CosmeticSlots.Category(Parts[i].slot), LabelOf(Parts[i].name));
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            return library;
        }

        // ---------------------------------------------------------------- Part assets and registry

        private static CosmeticPartData[] CreatePartAssets()
        {
            var created = new CosmeticPartData[Parts.Length];
            for (int i = 0; i < Parts.Length; i++)
            {
                string path = $"{CosmeticsFolder}/Part_{IdOf(Parts[i].slot, Parts[i].name)}.asset";
                var asset = AssetDatabase.LoadAssetAtPath<CosmeticPartData>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<CosmeticPartData>();
                    AssetDatabase.CreateAsset(asset, path);
                }
                asset.Configure(IdOf(Parts[i].slot, Parts[i].name), Parts[i].slot, Parts[i].name, LabelOf(Parts[i].name));
                created[i] = asset;
            }
            return created;
        }

        private static void RegisterParts(CosmeticPartData[] parts, SpriteLibraryAsset library)
        {
            var registry = AssetDatabase.LoadAssetAtPath<AssetRegistry>(RegistryPath);
            registry.SetCosmetics(parts); // the first part of each slot is that slot's default
            var so = new SerializedObject(registry);
            so.FindProperty("partLibrary").objectReferenceValue = library;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);

            // The retired 4-slot cosmetic assets (coating, headgear, cape, arm tint) are no longer referenced by anything.
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { CosmeticsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path).StartsWith("Cosmetic_"))
                    AssetDatabase.DeleteAsset(path);
            }
        }

        // ---------------------------------------------------------------- Doll prefab

        private static GameObject CreateDollPrefab(SpriteLibraryAsset library)
        {
            Material material = FindCharacterMaterial();
            int sortingLayer = ExistingBodySortingLayer();

            var root = new GameObject("GladiatorDoll");
            var spriteLibrary = root.AddComponent<SpriteLibrary>();
            spriteLibrary.spriteLibraryAsset = library;

            Transform backAnchor = new GameObject("BackAnchor").transform;
            backAnchor.SetParent(root.transform, false);
            Transform headAnchor = new GameObject("HeadAnchor").transform;
            headAnchor.SetParent(root.transform, false);

            (CosmeticSlot slot, Transform parent, int order)[] layout =
            {
                (CosmeticSlot.Accessory2, backAnchor, -1),
                (CosmeticSlot.Body, root.transform, 0),
                (CosmeticSlot.Armor, root.transform, 1),
                (CosmeticSlot.Head, root.transform, 2),
                (CosmeticSlot.Accessory1, headAnchor, 3),
            };

            var layers = new List<GladiatorCosmetics.Layer>();
            foreach (var (slot, parent, order) in layout)
            {
                var go = new GameObject(slot.ToString());
                go.transform.SetParent(parent, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingLayerID = sortingLayer;
                renderer.sortingOrder = order;
                if (material != null)
                    renderer.sharedMaterial = material;
                var resolver = go.AddComponent<SpriteResolver>();
                string firstLabel = LabelOf(Parts.First(p => p.slot == slot).name);
                resolver.SetCategoryAndLabel(CosmeticSlots.Category(slot), firstLabel);
                layers.Add(new GladiatorCosmetics.Layer { slot = slot, renderer = renderer, resolver = resolver });
            }

            var cosmetics = root.AddComponent<GladiatorCosmetics>();
            var so = new SerializedObject(cosmetics);
            SerializedProperty array = so.FindProperty("layers");
            array.arraySize = layers.Count;
            for (int i = 0; i < layers.Count; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("slot").enumValueIndex = (int)layers[i].slot;
                element.FindPropertyRelative("renderer").objectReferenceValue = layers[i].renderer;
                element.FindPropertyRelative("resolver").objectReferenceValue = layers[i].resolver;
            }
            so.FindProperty("headAnchor").objectReferenceValue = headAnchor;
            so.FindProperty("backAnchor").objectReferenceValue = backAnchor;
            so.FindProperty("applyProfileOnAwake").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DollPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Material FindCharacterMaterial()
        {
            string guid = AssetDatabase.FindAssets("Mat_SpriteCharacter t:Material").FirstOrDefault();
            return guid != null ? AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)) : null;
        }

        // The Player prefab's old Body renderer tells us which sorting layer characters draw on (kept from the previous prefab
        // or the doll prefab on a re-run).
        private static int ExistingBodySortingLayer()
        {
            GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                SpriteRenderer any = player.GetComponentsInChildren<SpriteRenderer>(true)
                    .FirstOrDefault(r => r.gameObject.name == "Body");
                return any != null ? any.sortingLayerID : 0;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        // ---------------------------------------------------------------- Player prefab

        private static void SetupPlayerPrefab(GameObject dollPrefab)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Transform motion = root.transform.Find("Visuals/Motion");
                foreach (string old in new[] { "Body", "Cape", "Headgear", "GladiatorDoll" })
                {
                    Transform t = motion.Find(old);
                    if (t != null)
                        Object.DestroyImmediate(t.gameObject);
                }
                foreach (var oldCosmetics in root.GetComponents<GladiatorCosmetics>())
                    Object.DestroyImmediate(oldCosmetics);

                var doll = (GameObject)PrefabUtility.InstantiatePrefab(dollPrefab, motion);
                doll.transform.localPosition = Vector3.zero;
                var cosmetics = doll.GetComponent<GladiatorCosmetics>();

                var fx = root.GetComponent<SpriteFx>();
                var fxSo = new SerializedObject(fx);
                SerializedProperty renderers = fxSo.FindProperty("renderers");
                renderers.arraySize = cosmetics.Renderers.Count;
                for (int i = 0; i < cosmetics.Renderers.Count; i++)
                    renderers.GetArrayElementAtIndex(i).objectReferenceValue = cosmetics.Renderers[i];
                fxSo.ApplyModifiedPropertiesWithoutUndo();

                var health = root.GetComponent<PlayerHealth>();
                var healthSo = new SerializedObject(health);
                healthSo.FindProperty("doll").objectReferenceValue = cosmetics;
                healthSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------------------------------------------------------------- Main menu: preview + layout

        private static void SetupMainMenu(GameObject dollPrefab)
        {
            EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            GameObject previewRoot = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.gameObject).FirstOrDefault(g => g.name == "GladiatorPreview");
            var screen = Object.FindFirstObjectByType<CustomizationScreen>(FindObjectsInactive.Include);
            if (previewRoot == null || screen == null)
            {
                Debug.LogError("CC1: the MainMenu scene has no GladiatorPreview / CustomizationScreen.");
                return;
            }

            bool wasActive = previewRoot.activeSelf;
            previewRoot.SetActive(true);

            foreach (var old in previewRoot.GetComponents<GladiatorCosmetics>())
                Object.DestroyImmediate(old);
            foreach (string name in new[] { "Body", "Cape", "Headgear", "Motion", "Pedestal" })
            {
                Transform t = previewRoot.transform.Find(name);
                if (t != null)
                    Object.DestroyImmediate(t.gameObject);
            }

            // Motion holds the doll and the arms; the toolkit's idle breathing animates it like the player.
            Transform motion = new GameObject("Motion").transform;
            motion.SetParent(previewRoot.transform, false);
            var arms = new List<Transform>();
            foreach (Transform child in previewRoot.transform)
                if (child.name.StartsWith("Arm"))
                    arms.Add(child);
            foreach (Transform arm in arms)
                arm.SetParent(motion, false);

            var doll = (GameObject)PrefabUtility.InstantiatePrefab(dollPrefab, motion);
            doll.transform.localPosition = Vector3.zero;
            doll.transform.SetAsFirstSibling();
            var cosmetics = doll.GetComponent<GladiatorCosmetics>();
            var cosmeticsSo = new SerializedObject(cosmetics);
            cosmeticsSo.FindProperty("applyProfileOnAwake").boolValue = false;
            cosmeticsSo.ApplyModifiedPropertiesWithoutUndo();

            foreach (var comp in previewRoot.GetComponents<Component>().OfType<ProceduralMotion>().ToArray())
                Object.DestroyImmediate(comp);
            var procedural = previewRoot.AddComponent<ProceduralMotion>();
            var procSo = new SerializedObject(procedural);
            procSo.FindProperty("motion").objectReferenceValue = motion;
            procSo.ApplyModifiedPropertiesWithoutUndo();
            if (previewRoot.GetComponent<GladiatorPreviewMotion>() == null)
                previewRoot.AddComponent<GladiatorPreviewMotion>();

            BuildPedestal(previewRoot.transform);

            // The screen: preview refs, the Square shortcut, and the new layout text.
            var screenSo = new SerializedObject(screen);
            screenSo.FindProperty("previewRoot").objectReferenceValue = previewRoot;
            screenSo.FindProperty("preview").objectReferenceValue = cosmetics;
            screenSo.FindProperty("menuInput").objectReferenceValue = Object.FindFirstObjectByType<MenuInputReader>(FindObjectsInactive.Include);
            screenSo.ApplyModifiedPropertiesWithoutUndo();

            Transform panel = screen.transform.Find("Panel");
            SetText(panel, "Title", "Character Creation");
            SetText(panel, "Randomize/Label", "Randomize  (Square)");
            SetText(panel, "Confirm/Label", "To the Arena!");
            SetText(panel, "Back/Label", "Back");
            var title = panel.Find("Title")?.GetComponent<LayoutElement>();
            if (title != null)
                title.minHeight = 70f;

            previewRoot.SetActive(false);
            _ = wasActive;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void SetText(Transform parent, string path, string text)
        {
            Transform t = parent.Find(path);
            if (t != null && t.TryGetComponent(out TMP_Text label))
            {
                label.text = text;
                EditorUtility.SetDirty(label);
            }
        }

        // A round wooden pedestal under the doll's feet: a dark base ellipse and a lighter top ellipse.
        private static void BuildPedestal(Transform previewRoot)
        {
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Circle.png");
            var pedestal = new GameObject("Pedestal").transform;
            pedestal.SetParent(previewRoot, false);
            pedestal.localPosition = new Vector3(0f, -0.8f, 0f);
            int layer = ExistingBodySortingLayer();

            AddEllipse(pedestal, "Base", circle, new Vector2(1.5f, 0.42f), new Vector3(0f, -0.1f, 0f), new Color(0.32f, 0.2f, 0.14f), -8, layer);
            AddEllipse(pedestal, "Top", circle, new Vector2(1.5f, 0.42f), Vector3.zero, new Color(0.86f, 0.7f, 0.46f), -7, layer);
            AddEllipse(pedestal, "Shadow", circle, new Vector2(0.95f, 0.24f), Vector3.zero, new Color(0.3f, 0.2f, 0.12f, 0.5f), -6, layer);
        }

        private static void AddEllipse(Transform parent, string name, Sprite sprite, Vector2 size, Vector3 position, Color color, int order, int layer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerID = layer;
            renderer.sortingOrder = order;
        }

        // ---------------------------------------------------------------- Game scene: HUD portrait

        private static void SetupGame()
        {
            EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            var portrait = Object.FindFirstObjectByType<HudPortrait>(FindObjectsInactive.Include);
            if (portrait == null)
            {
                Debug.LogError("CC1: the Game scene has no HudPortrait.");
                return;
            }

            Transform root = portrait.transform;
            foreach (string name in new[] { "Cape", "Body", "Headgear", "Window" })
            {
                Transform t = root.Find(name);
                if (t != null)
                    Object.DestroyImmediate(t.gameObject);
            }

            // A window that shows only the head area of the full-canvas part sprites, between the frame and the gold rim.
            RectTransform window = UiBuilder.CreateRect("Window", root);
            window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
            window.anchoredPosition = Vector2.zero;
            window.sizeDelta = new Vector2(104f, 104f);
            window.gameObject.AddComponent<RectMask2D>();
            Transform rim = root.Find("Rim");
            window.SetSiblingIndex(rim != null ? rim.GetSiblingIndex() : root.childCount);

            const float scale = 0.75f;
            Vector2 size = new Vector2(W, H) * scale;
            float headOffset = -(245f - H * 0.5f) * scale; // brings the head's centre (y = 245 on the canvas) to the window's centre
            Image head = CreatePortraitLayer(window, "Head", size, headOffset);
            Image accessory = CreatePortraitLayer(window, "Accessory", size, headOffset);

            var so = new SerializedObject(portrait);
            so.FindProperty("head").objectReferenceValue = head;
            so.FindProperty("accessory").objectReferenceValue = accessory;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static Image CreatePortraitLayer(Transform parent, string name, Vector2 size, float yOffset)
        {
            RectTransform rect = UiBuilder.CreateRect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(0f, yOffset);
            var image = rect.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        // ---------------------------------------------------------------- Placeholder painter

        /// <summary>Draws flat placeholder shapes (fill + dark outline) onto a transparent canvas the size of the player sprite.</summary>
        private sealed class Painter
        {
            private static readonly Color Outline = new Color(0.16f, 0.1f, 0.12f, 1f);
            private readonly int w, h;
            private readonly Color32[] pixels;

            public Painter(int width, int height)
            {
                w = width;
                h = height;
                pixels = new Color32[w * h];
            }

            public byte[] ToPng()
            {
                var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
                texture.SetPixels32(pixels);
                texture.Apply();
                byte[] png = texture.EncodeToPNG();
                Object.DestroyImmediate(texture);
                return png;
            }

            private bool[] Mask(Func<float, float, bool> inside)
            {
                var mask = new bool[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        mask[y * w + x] = inside(x + 0.5f, y + 0.5f);
                return mask;
            }

            private void Paint(bool[] mask, Color fill, int outlinePx = 4)
            {
                if (outlinePx > 0)
                {
                    var grown = new bool[mask.Length];
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            if (!mask[y * w + x])
                                continue;
                            for (int dy = -outlinePx; dy <= outlinePx; dy++)
                                for (int dx = -outlinePx; dx <= outlinePx; dx++)
                                {
                                    int nx = x + dx, ny = y + dy;
                                    if (nx < 0 || ny < 0 || nx >= w || ny >= h || dx * dx + dy * dy > outlinePx * outlinePx)
                                        continue;
                                    grown[ny * w + nx] = true;
                                }
                        }
                    for (int i = 0; i < grown.Length; i++)
                        if (grown[i])
                            pixels[i] = Outline;
                }
                for (int i = 0; i < mask.Length; i++)
                    if (mask[i])
                        pixels[i] = fill;
            }

            private void Flat(bool[] mask, Color fill) => Paint(mask, fill, 0);

            private static Func<float, float, bool> Ellipse(float cx, float cy, float rx, float ry) =>
                (x, y) => ((x - cx) * (x - cx)) / (rx * rx) + ((y - cy) * (y - cy)) / (ry * ry) <= 1f;

            private static Func<float, float, bool> Rect(float x0, float y0, float x1, float y1) =>
                (x, y) => x >= x0 && x <= x1 && y >= y0 && y <= y1;

            private static Func<float, float, bool> RoundRect(float x0, float y0, float x1, float y1, float r) => (x, y) =>
            {
                float cx = Mathf.Clamp(x, x0 + r, x1 - r), cy = Mathf.Clamp(y, y0 + r, y1 - r);
                return (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
            };

            private static Func<float, float, bool> Poly(params Vector2[] pts) => (x, y) =>
            {
                bool inside = false;
                for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
                    if ((pts[i].y > y) != (pts[j].y > y) && x < (pts[j].x - pts[i].x) * (y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                        inside = !inside;
                return inside;
            };

            private void Eyes(float cx, float cy, float spread, float r)
            {
                Flat(Mask(Ellipse(cx - spread, cy, r, r * 1.2f)), new Color(0.12f, 0.08f, 0.1f));
                Flat(Mask(Ellipse(cx + spread, cy, r, r * 1.2f)), new Color(0.12f, 0.08f, 0.1f));
            }

            // ---- Body (variants 2 and 3; variant 1 is the player's own sprite)
            public void Body_Gumdrop()
            {
                Paint(Mask(RoundRect(60, 26, 242, 250, 64)), new Color(1f, 0.55f, 0.75f));
                Flat(Mask(Ellipse(112, 190, 26, 40)), new Color(1f, 0.78f, 0.88f));
            }

            public void Body_Mint()
            {
                Paint(Mask(Ellipse(151, 140, 108, 118)), new Color(0.55f, 0.95f, 0.8f));
                Flat(Mask(Ellipse(112, 190, 26, 34)), new Color(0.8f, 1f, 0.92f));
            }

            // ---- Armor
            public void Armor_Plate()
            {
                Paint(Mask(RoundRect(72, 92, 230, 198, 14)), new Color(0.72f, 0.74f, 0.8f));
                foreach (var p in new[] { new Vector2(92, 176), new Vector2(210, 176), new Vector2(92, 112), new Vector2(210, 112) })
                    Flat(Mask(Ellipse(p.x, p.y, 6, 6)), new Color(0.3f, 0.3f, 0.36f));
            }

            public void Armor_Stripes()
            {
                bool[] all = Mask(RoundRect(66, 86, 236, 186, 12));
                Paint(all, new Color(1f, 0.85f, 0.2f));
                Flat(Mask((x, y) => all[(int)y * w + (int)x] && ((int)((y - 86) / 20f) % 2 == 1)), new Color(0.15f, 0.12f, 0.14f));
            }

            public void Armor_Belt()
            {
                Paint(Mask(Rect(52, 98, 250, 126)), new Color(0.75f, 0.2f, 0.15f));
                Paint(Mask(RoundRect(134, 92, 168, 132, 5)), new Color(0.95f, 0.8f, 0.2f));
            }

            // ---- Head
            public void Head_Round()
            {
                Paint(Mask(Ellipse(151, 245, 58, 58)), new Color(0.98f, 0.85f, 0.65f));
                Eyes(151, 250, 24, 7);
            }

            public void Head_Square()
            {
                Paint(Mask(RoundRect(88, 190, 214, 298, 20)), new Color(0.55f, 0.75f, 1f));
                Eyes(151, 250, 26, 8);
            }

            public void Head_Pointy()
            {
                Paint(Mask(Poly(new Vector2(151, 308), new Vector2(208, 246), new Vector2(151, 184), new Vector2(94, 246))), new Color(0.6f, 0.9f, 0.35f));
                Eyes(151, 246, 20, 6);
            }

            // ---- Accessory 1 (head anchor)
            public void Acc1_Crown()
            {
                Paint(Mask(Poly(new Vector2(100, 282), new Vector2(100, 308), new Vector2(126, 294), new Vector2(151, 311), new Vector2(176, 294), new Vector2(202, 308), new Vector2(202, 282))),
                    new Color(1f, 0.82f, 0.2f), 3);
            }

            public void Acc1_Horns()
            {
                Paint(Mask(Poly(new Vector2(92, 272), new Vector2(76, 310), new Vector2(116, 290))), new Color(0.98f, 0.95f, 0.85f), 3);
                Paint(Mask(Poly(new Vector2(210, 272), new Vector2(226, 310), new Vector2(186, 290))), new Color(0.98f, 0.95f, 0.85f), 3);
            }

            public void Acc1_Bow()
            {
                Color pink = new Color(1f, 0.4f, 0.62f);
                Paint(Mask(Poly(new Vector2(196, 288), new Vector2(160, 312), new Vector2(160, 264))), pink, 3);
                Paint(Mask(Poly(new Vector2(196, 288), new Vector2(238, 312), new Vector2(238, 264))), pink, 3);
                Paint(Mask(Ellipse(197, 288, 10, 10)), new Color(0.9f, 0.25f, 0.5f), 3);
            }

            // ---- Accessory 2 (back anchor, drawn behind the body)
            public void Acc2_Cape()
            {
                Paint(Mask(Poly(new Vector2(62, 222), new Vector2(240, 222), new Vector2(284, 30), new Vector2(18, 30))), new Color(0.78f, 0.1f, 0.2f));
            }

            public void Acc2_Banner()
            {
                Paint(Mask(Rect(250, 20, 258, 300)), new Color(0.55f, 0.38f, 0.2f), 3);
                Paint(Mask(Poly(new Vector2(258, 292), new Vector2(300, 270), new Vector2(258, 232))), new Color(0.6f, 0.3f, 0.9f), 3);
            }

            public void Acc2_Backpack()
            {
                Paint(Mask(RoundRect(186, 86, 268, 206, 20)), new Color(0.62f, 0.42f, 0.22f));
                Flat(Mask(Rect(192, 140, 262, 150)), new Color(0.4f, 0.26f, 0.14f));
            }
        }
    }
}
