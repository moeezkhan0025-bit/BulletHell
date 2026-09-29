using System.Collections.Generic;
using BulletHell.Arena;
using BulletHell.Core;
using BulletHell.Player;
using BulletHell.Platform;
using BulletHell.Pickups;
using BulletHell.Projectiles;
using BulletHell.UI;
using BulletHell.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// One-shot M7.5 setup: sorting layers and Custom Axis (0,1,0) sorting, the placeholder art, the perspective tuning
    /// asset, footprints and feet-pivoted structure for the player / enemy / obstacle prefabs, the layered arena
    /// scenery, ammo icons, the button glyph library and the combat HUD. Safe to run again: existing assets are updated,
    /// existing art files are kept, the HUD and scenery objects in the Game scene are rebuilt.
    /// </summary>
    public static class M75Setup
    {
        private const string GamePath = "Assets/Scenes/Game.unity";
        private const string ConfigPath = "Assets/Resources/GameConfig.asset";
        private const string RendererPath = "Assets/Settings/Renderer2D.asset";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        private const string ObstaclePrefabPath = "Assets/Prefabs/Obstacle.prefab";
        private const string PlayerDataPath = "Assets/Data/Player/PlayerData.asset";
        private const string ArenaPath = "Assets/Data/Arenas/Arena_Colosseum01.asset";
        private const string ObstacleFolder = "Assets/Data/Arenas/Obstacles";
        private const string TuningPath = "Assets/Data/Arenas/PerspectiveTuning.asset";
        private const string GlyphFolder = "Assets/Data/UI";
        private const string GlyphPath = "Assets/Data/UI/ButtonGlyphs.asset";

        private static readonly string[] SortingLayerOrder = { "Background", "Ground", "Default", "Bullets", "Foreground" };

        [MenuItem("BulletHell/M7.5/Setup Everything")]
        public static void Run()
        {
            EnsureSortingLayers();
            SetCustomAxisSorting();
            M75Art.EnsureAll();
            AssetDatabase.Refresh();

            PerspectiveTuning tuning = GetOrCreate<PerspectiveTuning>(TuningPath);
            AssignConfig(tuning);
            ConfigureObstacles();
            ConfigureArena();
            AssignAmmoIcons();
            ButtonGlyphLibrary glyphs = CreateGlyphLibrary();

            SetupPlayerPrefab();
            SetupEnemyPrefab();
            SetupObstaclePrefab();
            SetupSimplePrefabs();
            SetupGameScene(glyphs);
            AssetDatabase.SaveAssets();
            Debug.Log("M7.5 setup complete.");
        }

        // ---------------------------------------------------------------- Sorting

        private static void EnsureSortingLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("m_SortingLayers");

            var existingIds = new Dictionary<string, int>();
            for (int i = 0; i < layers.arraySize; i++)
            {
                SerializedProperty element = layers.GetArrayElementAtIndex(i);
                existingIds[element.FindPropertyRelative("name").stringValue] = element.FindPropertyRelative("uniqueID").intValue;
            }

            layers.ClearArray();
            for (int i = 0; i < SortingLayerOrder.Length; i++)
            {
                string layerName = SortingLayerOrder[i];
                layers.InsertArrayElementAtIndex(i);
                SerializedProperty element = layers.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("name").stringValue = layerName;
                element.FindPropertyRelative("uniqueID").intValue = layerName == "Default" ? 0
                    : existingIds.TryGetValue(layerName, out int id) ? id : 7500 + i;
                element.FindPropertyRelative("locked").boolValue = false;
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        // 2D Renderer: lower on screen draws in front. Enum TransparencySortMode: Default, Perspective, Orthographic, CustomAxis.
        private static void SetCustomAxisSorting()
        {
            var renderer = AssetDatabase.LoadMainAssetAtPath(RendererPath);
            var so = new SerializedObject(renderer);
            so.FindProperty("m_TransparencySortMode").intValue = (int)TransparencySortMode.CustomAxis;
            so.FindProperty("m_TransparencySortAxis").vector3Value = new Vector3(0f, 1f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);
        }

        // ---------------------------------------------------------------- Assets

        private static void AssignConfig(PerspectiveTuning tuning)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var so = new SerializedObject(config);
            so.FindProperty("perspective").objectReferenceValue = tuning;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Footprints are the gameplay shapes; the art is a separate, taller sprite with its pivot at the base.
        private static void ConfigureObstacles()
        {
            Obstacle("Pillar", ObstacleKind.Solid, ObstacleShape.Circle, new Vector2(0.9f, 0.55f), new Vector2(1.1f, 2.3f), "Pillar", 1f);
            Obstacle("LowWall", ObstacleKind.Solid, ObstacleShape.Box, new Vector2(1f, 0.5f), new Vector2(1f, 0.85f), "LowWall", 1f);
            Obstacle("Crate", ObstacleKind.Breakable, ObstacleShape.Box, new Vector2(0.85f, 0.55f), new Vector2(0.95f, 1f), "Crate", 30f);
            Obstacle("Pumpkin", ObstacleKind.Breakable, ObstacleShape.Circle, new Vector2(0.9f, 0.55f), new Vector2(1.05f, 0.95f), "Pumpkin", 40f);
            Obstacle("Cabbage", ObstacleKind.Breakable, ObstacleShape.Circle, new Vector2(0.8f, 0.5f), new Vector2(0.95f, 0.9f), "Cabbage", 25f);
        }

        private static void Obstacle(string name, ObstacleKind kind, ObstacleShape shape, Vector2 footprint, Vector2 art, string spriteName, float health)
        {
            var data = AssetDatabase.LoadAssetAtPath<ObstacleData>($"{ObstacleFolder}/Obstacle_{name}.asset");
            if (data == null)
            {
                Debug.LogWarning($"M7.5: {ObstacleFolder}/Obstacle_{name}.asset is missing; run BulletHell/M7/Setup Everything first.");
                return;
            }
            data.Configure(kind, shape, footprint, Color.white, health);
            data.ConfigureArt(M75Art.Load(spriteName), art);
        }

        // Back-wall arches for the top gates, small doors in the side walls; sandy floor, light stone walls, dark stands.
        private static void ConfigureArena()
        {
            var arena = AssetDatabase.LoadAssetAtPath<ArenaData>(ArenaPath);
            if (arena == null)
            {
                Debug.LogWarning("M7.5: the M7 arena asset is missing; run BulletHell/M7/Setup Everything first.");
                return;
            }
            var so = new SerializedObject(arena);
            SerializedProperty gates = so.FindProperty("spawnGates");
            var positions = new[] { new Vector2(-5f, 3.8f), new Vector2(5f, 3.8f), new Vector2(-7.2f, 0.5f), new Vector2(7.2f, 0.5f) };
            gates.arraySize = positions.Length;
            for (int i = 0; i < positions.Length; i++)
                gates.GetArrayElementAtIndex(i).vector2Value = positions[i];
            so.FindProperty("floorColor").colorValue = new Color(0.95f, 0.72f, 0.48f);
            so.FindProperty("wallColor").colorValue = new Color(0.85f, 0.72f, 0.55f);
            so.FindProperty("standsColor").colorValue = new Color(0.16f, 0.1f, 0.14f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(arena);
        }

        private static void AssignAmmoIcons()
        {
            SetIcon("Ammo_Basic", "Icon_Basic");
            SetIcon("Ammo_Shotgun", "Icon_Shotgun");
            SetIcon("Ammo_Laser", "Icon_Laser");
            SetIcon("Ammo_Gatling", "Icon_Gatling");
            SetIcon("Ammo_TestSpare", "Icon_Spare");
        }

        private static void SetIcon(string ammoAsset, string spriteName)
        {
            var ammo = AssetDatabase.LoadAssetAtPath<AmmoTypeData>($"Assets/Data/Ammo/{ammoAsset}.asset");
            if (ammo == null)
                return;
            var so = new SerializedObject(ammo);
            so.FindProperty("icon").objectReferenceValue = M75Art.Load(spriteName);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ButtonGlyphLibrary CreateGlyphLibrary()
        {
            EnsureFolder(GlyphFolder);
            var library = GetOrCreate<ButtonGlyphLibrary>(GlyphPath);
            Sprite disc = M75Art.Load("Glyph_Disc");
            var white = Color.white;
            var dark = new Color(0.08f, 0.08f, 0.1f);

            ButtonGlyph Shape(string name) => new ButtonGlyph { Icon = M75Art.Load(name), Tint = white, Label = "", LabelColor = white };
            ButtonGlyph Letter(string label, Color tint, Color text) => new ButtonGlyph { Icon = disc, Tint = tint, Label = label, LabelColor = text };

            var sets = new[]
            {
                new ButtonGlyphLibrary.FamilySet
                {
                    Family = GlyphFamily.PlayStation,
                    South = Shape("Glyph_PS_Cross"), East = Shape("Glyph_PS_Circle"),
                    West = Shape("Glyph_PS_Square"), North = Shape("Glyph_PS_Triangle"),
                },
                new ButtonGlyphLibrary.FamilySet
                {
                    Family = GlyphFamily.Xbox,
                    South = Letter("A", new Color(0.35f, 0.78f, 0.35f), dark), East = Letter("B", new Color(0.9f, 0.32f, 0.3f), dark),
                    West = Letter("X", new Color(0.32f, 0.55f, 0.95f), dark), North = Letter("Y", new Color(0.98f, 0.82f, 0.25f), dark),
                },
                new ButtonGlyphLibrary.FamilySet
                {
                    Family = GlyphFamily.Nintendo,
                    South = Letter("B", new Color(0.85f, 0.85f, 0.88f), dark), East = Letter("A", new Color(0.85f, 0.85f, 0.88f), dark),
                    West = Letter("Y", new Color(0.85f, 0.85f, 0.88f), dark), North = Letter("X", new Color(0.85f, 0.85f, 0.88f), dark),
                },
                new ButtonGlyphLibrary.FamilySet
                {
                    Family = GlyphFamily.Touch,
                    South = Letter("1", new Color(0.6f, 0.75f, 0.95f), dark), East = Letter("2", new Color(0.6f, 0.75f, 0.95f), dark),
                    West = Letter("3", new Color(0.6f, 0.75f, 0.95f), dark), North = Letter("4", new Color(0.6f, 0.75f, 0.95f), dark),
                },
            };
            library.SetSets(sets);
            return library;
        }

        // ---------------------------------------------------------------- Prefabs

        private static void SetupPlayerPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Transform visuals = Child(root.transform, "Visuals");
                foreach (string name in new[] { "Body", "Arms", "Cape", "Headgear" })
                {
                    Transform t = root.transform.Find(name);
                    if (t != null)
                        t.SetParent(visuals, false);
                }
                Transform core = Child(visuals, "Core");

                Transform shadowT = Child(root.transform, "Shadow");
                SpriteRenderer shadow = GetOrAdd<SpriteRenderer>(shadowT.gameObject);
                shadow.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Circle.png");
                shadow.sortingOrder = -10;

                SortingGroup group = GetOrAdd<SortingGroup>(root);
                group.sortingLayerID = 0;

                PlayerVisualRig rig = GetOrAdd<PlayerVisualRig>(root);
                var rigSo = new SerializedObject(rig);
                rigSo.FindProperty("data").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlayerData>(PlayerDataPath);
                rigSo.FindProperty("visuals").objectReferenceValue = visuals;
                rigSo.FindProperty("core").objectReferenceValue = core;
                rigSo.FindProperty("shadow").objectReferenceValue = shadow;
                rigSo.ApplyModifiedPropertiesWithoutUndo();

                var healthSo = new SerializedObject(root.GetComponent<PlayerHealth>());
                healthSo.FindProperty("core").objectReferenceValue = core;
                healthSo.ApplyModifiedPropertiesWithoutUndo();

                SortByPivot(root);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupEnemyPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
            try
            {
                var circle = root.GetComponent<CircleCollider2D>();
                if (circle != null)
                    Object.DestroyImmediate(circle);
                CapsuleCollider2D capsule = GetOrAdd<CapsuleCollider2D>(root);
                capsule.direction = CapsuleDirection2D.Vertical;
                capsule.size = new Vector2(0.9f, 1f);

                Transform rig = Child(root.transform, "Rig");
                foreach (string name in new[] { "Body", "HealthBar" })
                {
                    Transform t = root.transform.Find(name);
                    if (t != null)
                        t.SetParent(rig, false);
                }

                Transform shadowT = Child(root.transform, "Shadow");
                SpriteRenderer shadow = GetOrAdd<SpriteRenderer>(shadowT.gameObject);
                shadow.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Circle.png");
                shadow.sortingOrder = -10;

                SortingGroup group = GetOrAdd<SortingGroup>(root);
                group.sortingLayerID = 0;

                var so = new SerializedObject(root.GetComponent<Enemies.Enemy>());
                so.FindProperty("hitbox").objectReferenceValue = capsule;
                so.FindProperty("rig").objectReferenceValue = rig;
                so.FindProperty("shadow").objectReferenceValue = shadow;
                so.ApplyModifiedPropertiesWithoutUndo();

                SortByPivot(root);
                PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupObstaclePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ObstaclePrefabPath);
            try
            {
                var circle = root.GetComponent<CircleCollider2D>();
                if (circle != null)
                    Object.DestroyImmediate(circle);
                PolygonCollider2D polygon = GetOrAdd<PolygonCollider2D>(root);
                SortingGroup group = GetOrAdd<SortingGroup>(root);
                group.sortingLayerID = 0;

                var so = new SerializedObject(root.GetComponent<Obstacle>());
                so.FindProperty("ellipseCollider").objectReferenceValue = polygon;
                so.FindProperty("sortingGroup").objectReferenceValue = group;
                so.ApplyModifiedPropertiesWithoutUndo();

                SortByPivot(root);
                foreach (SpriteRenderer r in root.GetComponentsInChildren<SpriteRenderer>(true))
                    r.sortingOrder = 0;
                PrefabUtility.SaveAsPrefabAsset(root, ObstaclePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Bullets above the characters, coins and pickups on the ground.
        private static void SetupSimplePrefabs()
        {
            SetLayer("Assets/Prefabs/Projectile.prefab", SortingLayers.Bullets);
            SetLayer("Assets/Prefabs/Coin.prefab", SortingLayers.Ground);
            SetLayer("Assets/Prefabs/AmmoPickup.prefab", SortingLayers.Ground);
            SetLayer("Assets/Prefabs/Trap.prefab", SortingLayers.Ground);
        }

        private static void SetLayer(string path, string layer)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (SpriteRenderer r in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    r.sortingLayerID = SortingLayers.Id(layer);
                    r.spriteSortPoint = SpriteSortPoint.Pivot;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SortByPivot(GameObject root)
        {
            foreach (SpriteRenderer r in root.GetComponentsInChildren<SpriteRenderer>(true))
                r.spriteSortPoint = SpriteSortPoint.Pivot;
        }

        // ---------------------------------------------------------------- Game scene

        private static void SetupGameScene(ButtonGlyphLibrary glyphs)
        {
            var scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);

            Camera camera = Camera.main;
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = new Vector3(0f, 1f, 0f);

            IncludeAllSortingLayersInLights();
            SetupArena();
            foreach (SpriteRenderer r in Object.FindFirstObjectByType<AmmoPickup>(FindObjectsInactive.Include).transform.parent.GetComponentsInChildren<SpriteRenderer>(true))
            {
                r.sortingLayerID = SortingLayers.Id(SortingLayers.Ground);
                r.spriteSortPoint = SpriteSortPoint.Pivot;
            }

            BuildHud(glyphs);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // 2D lights only light the sorting layers they list, so every light has to include the new layers.
        private static void IncludeAllSortingLayersInLights()
        {
            foreach (Light2D light in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(light);
                SerializedProperty layers = so.FindProperty("m_ApplyToSortingLayers");
                SortingLayer[] all = SortingLayer.layers;
                layers.arraySize = all.Length;
                for (int i = 0; i < all.Length; i++)
                    layers.GetArrayElementAtIndex(i).intValue = all[i].id;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetupArena()
        {
            var controller = Object.FindFirstObjectByType<ArenaController>(FindObjectsInactive.Include);
            GameObject arena = controller.gameObject;

            // The old flat floor and wall rectangles are replaced by the layered scenery.
            foreach (string name in new[] { "Floor", "WallTop", "WallBottom", "WallLeft", "WallRight" })
            {
                Transform old = arena.transform.Find(name);
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);
            }

            ArenaScenery scenery = GetOrAdd<ArenaScenery>(arena);
            var sceneryOs = new SerializedObject(scenery);
            sceneryOs.FindProperty("squareSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Square.png");
            sceneryOs.FindProperty("circleSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Circle.png");
            sceneryOs.FindProperty("ringSprite").objectReferenceValue = M75Art.Load("Ring");
            sceneryOs.FindProperty("floorTile").objectReferenceValue = M75Art.Load("FloorTile");
            sceneryOs.FindProperty("stoneTile").objectReferenceValue = M75Art.Load("StoneTile");
            sceneryOs.FindProperty("crowdTile").objectReferenceValue = M75Art.Load("CrowdTile");
            sceneryOs.FindProperty("checkerTile").objectReferenceValue = M75Art.Load("CheckerTile");
            sceneryOs.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(controller);
            so.FindProperty("scenery").objectReferenceValue = scenery;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- HUD

        private static void BuildHud(ButtonGlyphLibrary glyphs)
        {
            GameObject flowUi = GameObject.Find("FlowUI");
            Transform safeArea = flowUi.transform.Find("SafeArea");
            Transform existing = safeArea.Find("CombatHud");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            Sprite roundRect = M75Art.Load("UIRoundRect");
            Sprite roundOutline = M75Art.Load("UIRoundRectOutline");
            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Square.png");
            Sprite ring = M75Art.Load("Ring");
            Sprite bodySprite = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath).transform.Find("Visuals/Body").GetComponent<SpriteRenderer>().sprite;

            RectTransform hud = UiBuilder.CreateRect("CombatHud", safeArea);
            hud.SetSiblingIndex(0);
            UiBuilder.Stretch(hud);
            var combat = hud.gameObject.AddComponent<CombatHud>();
            RectTransform content = UiBuilder.CreateRect("Content", hud);
            UiBuilder.Stretch(content);

            // ---- bottom left: portrait, hearts, heat bar
            RectTransform left = Anchored("BottomLeft", content, new Vector2(0f, 0f), new Vector2(24f, 20f), new Vector2(470f, 130f));

            RectTransform portraitRect = Anchored("Portrait", left, new Vector2(0f, 0f), Vector2.zero, new Vector2(128f, 128f));
            Pic(portraitRect, "Frame", roundRect, new Color(0.1f, 0.08f, 0.14f, 0.92f), Vector2.zero, Vector2.zero, true, Image.Type.Sliced);
            Image cape = PicAt(portraitRect, "Cape", null, Color.white, new Vector2(0f, -8f), new Vector2(104f, 104f));
            Image body = PicAt(portraitRect, "Body", bodySprite, Color.white, new Vector2(0f, 0f), new Vector2(104f, 108f));
            body.preserveAspect = true;
            Image headgear = PicAt(portraitRect, "Headgear", null, Color.white, new Vector2(0f, 44f), new Vector2(60f, 60f));
            Pic(portraitRect, "Rim", roundOutline, new Color(0.95f, 0.78f, 0.3f), Vector2.zero, Vector2.zero, true, Image.Type.Sliced);
            var portrait = portraitRect.gameObject.AddComponent<HudPortrait>();
            Wire(portrait, ("body", body), ("headgear", headgear), ("cape", cape));

            Sprite heartFull = M75Art.Load("HeartFull"), heartEmpty = M75Art.Load("HeartEmpty");
            RectTransform heartsRect = Anchored("Hearts", left, new Vector2(0f, 0f), new Vector2(144f, 74f), new Vector2(290f, 50f));
            var heartImages = new Image[8];
            for (int i = 0; i < heartImages.Length; i++)
            {
                heartImages[i] = PicAt(heartsRect, "Heart" + (i + 1), heartFull, Color.white, new Vector2(25f + i * 52f, 25f), new Vector2(48f, 48f), TopLeft);
                heartImages[i].gameObject.SetActive(i < 5);
            }
            var hearts = heartsRect.gameObject.AddComponent<HudHearts>();
            WireArray(hearts, "hearts", heartImages);
            var heartsSo = new SerializedObject(hearts);
            heartsSo.FindProperty("fullHeart").objectReferenceValue = heartFull;
            heartsSo.FindProperty("emptyHeart").objectReferenceValue = heartEmpty;
            heartsSo.ApplyModifiedPropertiesWithoutUndo();

            RectTransform heatRect = Anchored("HeatBar", left, new Vector2(0f, 0f), new Vector2(144f, 22f), new Vector2(290f, 32f));
            Pic(heatRect, "Back", roundRect, new Color(0.08f, 0.07f, 0.1f, 0.9f), Vector2.zero, Vector2.zero, true, Image.Type.Sliced);
            Image fill = Pic(heatRect, "Fill", square, new Color(0.3f, 0.85f, 1f), new Vector2(5f, 5f), new Vector2(-5f, -5f), true, Image.Type.Filled);
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            Image heatFrame = Pic(heatRect, "Frame", roundOutline, new Color(0.75f, 0.75f, 0.8f), Vector2.zero, Vector2.zero, true, Image.Type.Sliced);
            var group = heatRect.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var heat = heatRect.gameObject.AddComponent<HudHeatBar>();
            Wire(heat, ("fill", fill), ("frame", heatFrame), ("group", group));

            // ---- bottom right: 4 ammo slots
            RectTransform right = Anchored("BottomRight", content, new Vector2(1f, 0f), new Vector2(-24f, 20f), new Vector2(448f, 116f));
            var slots = new HudAmmoSlot[AmmoSlotSet.Count];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = BuildSlot(right, i, roundRect, roundOutline, ring, square);

            // ---- controller
            var player = Object.FindFirstObjectByType<PlayerHealth>();
            var so = new SerializedObject(combat);
            so.FindProperty("health").objectReferenceValue = player;
            so.FindProperty("arms").objectReferenceValue = player.GetComponent<ArmSelectionController>();
            so.FindProperty("fire").objectReferenceValue = player.GetComponent<ArmFireController>();
            so.FindProperty("ammo").objectReferenceValue = player.GetComponent<AmmoSlots>();
            so.FindProperty("pickups").objectReferenceValue = player.GetComponent<AmmoPickupCollector>();
            so.FindProperty("content").objectReferenceValue = content.gameObject;
            so.FindProperty("portrait").objectReferenceValue = portrait;
            so.FindProperty("hearts").objectReferenceValue = hearts;
            so.FindProperty("heatBar").objectReferenceValue = heat;
            so.FindProperty("glyphs").objectReferenceValue = glyphs;
            SerializedProperty slotList = so.FindProperty("slots");
            slotList.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
                slotList.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            // The old HUD readout only keeps round and currency, in a corner.
            var runHud = Object.FindFirstObjectByType<RunHud>(FindObjectsInactive.Include);
            if (runHud != null)
            {
                var rect = (RectTransform)runHud.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(24f, -16f);
                rect.sizeDelta = new Vector2(760f, 44f);
                var text = runHud.GetComponent<Text>();
                text.fontSize = 26;
                text.alignment = TextAnchor.UpperLeft;
                text.raycastTarget = false;
                if (text.GetComponent<Outline>() == null)
                    text.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
            }
        }

        private static HudAmmoSlot BuildSlot(RectTransform parent, int index, Sprite roundRect, Sprite roundOutline, Sprite ring, Sprite square)
        {
            RectTransform slot = Anchored("Slot" + (index + 1), parent, new Vector2(0f, 0f), new Vector2(index * 116f, 8f), new Vector2(100f, 100f));
            Image highlight = Pic(slot, "Highlight", roundOutline, new Color(1f, 0.85f, 0.25f), new Vector2(-7f, -7f), new Vector2(7f, 7f), true, Image.Type.Sliced);
            Image frame = Pic(slot, "Frame", roundRect, new Color(0.14f, 0.12f, 0.2f, 0.92f), Vector2.zero, Vector2.zero, true, Image.Type.Sliced);
            Pic(slot, "Rim", roundOutline, new Color(0.9f, 0.85f, 0.95f, 0.6f), Vector2.zero, Vector2.zero, true, Image.Type.Sliced);
            Image icon = Pic(slot, "Icon", square, Color.white, new Vector2(14f, 14f), new Vector2(-14f, -14f), true, Image.Type.Simple);
            icon.preserveAspect = true;
            Image hold = Pic(slot, "HoldRing", ring, new Color(1f, 1f, 1f, 0.95f), new Vector2(-11f, -11f), new Vector2(11f, 11f), true, Image.Type.Filled);
            hold.fillMethod = Image.FillMethod.Radial360;
            hold.fillOrigin = 2;
            hold.fillClockwise = true;
            hold.fillAmount = 0f;

            RectTransform glyphRect = Anchored("Glyph", slot, new Vector2(1f, 0f), new Vector2(6f, -6f), new Vector2(40f, 40f));
            Image glyph = Pic(glyphRect, "Disc", null, Color.white, Vector2.zero, Vector2.zero, true, Image.Type.Simple);
            Text label = UiBuilder.CreateText("Label", glyphRect, "", 24, TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
            UiBuilder.Stretch(label.rectTransform);

            var component = slot.gameObject.AddComponent<HudAmmoSlot>();
            Wire(component, ("frame", frame), ("icon", icon), ("highlight", highlight), ("holdRing", hold), ("glyph", glyph), ("glyphLabel", label));
            return component;
        }

        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);

        /// <summary>A rect anchored to a corner of its parent (anchor = pivot = the corner), offset from it.</summary>
        private static RectTransform Anchored(string name, Transform parent, Vector2 corner, Vector2 offset, Vector2 size)
        {
            RectTransform rect = UiBuilder.CreateRect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = corner;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>An image that stretches over its parent, inset by the offsets (offsetMin, offsetMax).</summary>
        private static Image Pic(Transform parent, string name, Sprite sprite, Color color, Vector2 offsetMin, Vector2 offsetMax, bool stretch, Image.Type type)
        {
            RectTransform rect = UiBuilder.CreateRect(name, parent);
            if (stretch)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = offsetMin;
                rect.offsetMax = offsetMax;
            }
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = type;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A fixed-size image centred on its parent (or anchored at the given corner) at an offset.</summary>
        private static Image PicAt(Transform parent, string name, Sprite sprite, Color color, Vector2 offset, Vector2 size, Vector2? corner = null)
        {
            Vector2 anchor = corner ?? new Vector2(0.5f, 0.5f);
            RectTransform rect = UiBuilder.CreateRect(name, parent);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchor == TopLeft ? new Vector2(offset.x, -offset.y) : offset;
            rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Wire(Object target, params (string field, Object value)[] fields)
        {
            var so = new SerializedObject(target);
            foreach ((string field, Object value) in fields)
                so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireArray(Object target, string field, Object[] items)
        {
            var so = new SerializedObject(target);
            SerializedProperty list = so.FindProperty(field);
            list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- Helpers

        private static Transform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
                return child;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
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
