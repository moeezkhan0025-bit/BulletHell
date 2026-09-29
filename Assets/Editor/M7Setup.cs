using System.Linq;
using BulletHell.Arena;
using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Player;
using BulletHell.Projectiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// One-shot M7 setup: the "Obstacle" physics layer, the obstacle / trap / arena assets (one test colosseum), the
    /// Obstacle and Trap prefabs, the Arena object in the Game scene wired into the projectile pool, player mover and wave
    /// spawner, the GameConfig default arena, and the wave groups that used the Row pattern now using Gates.
    /// Safe to run again: existing assets are updated and an existing Arena object is left alone.
    /// </summary>
    public static class M7Setup
    {
        private const string GamePath = "Assets/Scenes/Game.unity";
        private const string ConfigPath = "Assets/Resources/GameConfig.asset";
        private const string ArenaFolder = "Assets/Data/Arenas";
        private const string ObstacleFolder = "Assets/Data/Arenas/Obstacles";
        private const string TrapFolder = "Assets/Data/Arenas/Traps";
        private const string ObstaclePrefabPath = "Assets/Prefabs/Obstacle.prefab";
        private const string TrapPrefabPath = "Assets/Prefabs/Trap.prefab";
        private const string PlayerDataPath = "Assets/Data/Player/PlayerData.asset";
        private const int ObstacleLayerIndex = 7;

        [MenuItem("BulletHell/M7/Setup Everything")]
        public static void Run()
        {
            AddObstacleLayer();
            ObstacleData pillar = MakeObstacle("Pillar", ObstacleKind.Solid, ObstacleShape.Circle, new Vector2(1f, 1f), new Color(0.6f, 0.6f, 0.55f), 1f);
            ObstacleData wall = MakeObstacle("LowWall", ObstacleKind.Solid, ObstacleShape.Box, new Vector2(1f, 1f), new Color(0.5f, 0.42f, 0.36f), 1f);
            ObstacleData crate = MakeObstacle("Crate", ObstacleKind.Breakable, ObstacleShape.Box, new Vector2(0.9f, 0.9f), new Color(0.75f, 0.55f, 0.3f), 30f);
            ObstacleData pumpkin = MakeObstacle("Pumpkin", ObstacleKind.Breakable, ObstacleShape.Circle, new Vector2(1f, 1f), new Color(0.95f, 0.5f, 0.1f), 40f);
            ObstacleData cabbage = MakeObstacle("Cabbage", ObstacleKind.Breakable, ObstacleShape.Circle, new Vector2(0.9f, 0.9f), new Color(0.5f, 0.8f, 0.4f), 25f);

            TrapData vent = MakeTrap("Vent", TrapKind.Vent, new Vector2(1f, 1f), 1f, 12f, 1.5f, 1.2f, 0.35f, 3f, 0f);
            TrapData skewer = MakeTrap("Skewer", TrapKind.Skewer, new Vector2(0.5f, 4f), 1f, 15f, 2.5f, 1f, 0.5f, 3.5f, 0f);
            TrapData zone = MakeTrap("Zone", TrapKind.Zone, new Vector2(1.3f, 1.3f), 1f, 3f, 2f, 1.5f, 4f, 3f, 0.5f);

            CreateArena();   // the shell only; layouts (obstacles, traps, gates) come from BulletHell/M8.5/Setup Everything
            MakeWavesUseGates();
            CreatePrefabs();
            SetupGame();
            AssetDatabase.SaveAssets();
            Debug.Log("M7 setup complete.");
        }

        // ---------------------------------------------------------------- Layer

        private static void AddObstacleLayer()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layer = tagManager.FindProperty("layers").GetArrayElementAtIndex(ObstacleLayerIndex);
            if (layer.stringValue != "Obstacle")
            {
                if (!string.IsNullOrEmpty(layer.stringValue))
                    Debug.LogWarning($"Layer {ObstacleLayerIndex} was '{layer.stringValue}'; it is now 'Obstacle'.");
                layer.stringValue = "Obstacle";
                tagManager.ApplyModifiedProperties();
            }
        }

        // ---------------------------------------------------------------- Assets

        private static ObstacleData MakeObstacle(string name, ObstacleKind kind, ObstacleShape shape, Vector2 size, Color color, float health)
        {
            EnsureFolder(ObstacleFolder);
            string path = $"{ObstacleFolder}/Obstacle_{name}.asset";
            var asset = GetOrCreate<ObstacleData>(path);
            asset.Configure(kind, shape, size, color, health);
            return asset;
        }

        private static TrapData MakeTrap(string name, TrapKind kind, Vector2 size, float toPlayer, float toEnemies,
                                     float delay, float telegraph, float active, float cooldown, float interval)
        {
            EnsureFolder(TrapFolder);
            var asset = GetOrCreate<TrapData>($"{TrapFolder}/Trap_{name}.asset");
            asset.Configure(kind, size, toPlayer, toEnemies, delay, telegraph, active, cooldown, interval);
            return asset;
        }

        private static ArenaData CreateArena()
        {
            EnsureFolder(ArenaFolder);
            var arena = GetOrCreate<ArenaData>($"{ArenaFolder}/Arena_Colosseum01.asset");
            arena.Configure(new Vector2(16f, 9f));
            return arena;
        }

        /// <summary>Rounds are properly retuned for the arena in M8; for now the top-row spawns simply come out of the gates.</summary>
        private static void MakeWavesUseGates()
        {
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:WaveData"))
            {
                var wave = AssetDatabase.LoadAssetAtPath<WaveData>(AssetDatabase.GUIDToAssetPath(guid));
                var so = new SerializedObject(wave);
                SerializedProperty groups = so.FindProperty("groups");
                for (int i = 0; i < groups.arraySize; i++)
                {
                    SerializedProperty pattern = groups.GetArrayElementAtIndex(i).FindPropertyRelative("Pattern");
                    if (pattern.enumValueIndex == (int)SpawnPattern.Row)
                    {
                        pattern.enumValueIndex = (int)SpawnPattern.Gates;
                        changed++;
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(wave);
            }
            Debug.Log($"M7: {changed} Row spawn groups now use the arena gates.");
        }

        // ---------------------------------------------------------------- Prefabs

        private static void CreatePrefabs()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ObstaclePrefabPath) == null)
            {
                var root = new GameObject("Obstacle", typeof(Obstacle), typeof(BoxCollider2D), typeof(CircleCollider2D));
                root.layer = LayerMask.NameToLayer("Obstacle");
                var art = new GameObject("Art", typeof(SpriteRenderer));
                art.transform.SetParent(root.transform, false);

                var obstacle = root.GetComponent<Obstacle>();
                var so = new SerializedObject(obstacle);
                so.FindProperty("art").objectReferenceValue = art.GetComponent<SpriteRenderer>();
                so.FindProperty("boxCollider").objectReferenceValue = root.GetComponent<BoxCollider2D>();
                so.FindProperty("circleCollider").objectReferenceValue = root.GetComponent<CircleCollider2D>();
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, ObstaclePrefabPath);
                Object.DestroyImmediate(root);
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(TrapPrefabPath) == null)
            {
                var root = new GameObject("Trap", typeof(Trap));
                var art = new GameObject("Art", typeof(SpriteRenderer));
                art.transform.SetParent(root.transform, false);
                art.GetComponent<SpriteRenderer>().sortingOrder = -5;

                var so = new SerializedObject(root.GetComponent<Trap>());
                so.FindProperty("art").objectReferenceValue = art.GetComponent<SpriteRenderer>();
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, TrapPrefabPath);
                Object.DestroyImmediate(root);
            }
        }

        // ---------------------------------------------------------------- Game scene

        private static void SetupGame()
        {
            var scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<ArenaController>(FindObjectsInactive.Include) != null)
            {
                Debug.Log("Game scene already has an Arena; left unchanged.");
                return;
            }

            var square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Square.png");
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Circle.png");
            int obstacleLayer = LayerMask.NameToLayer("Obstacle");

            var arenaRoot = new GameObject("Arena");
            var controller = arenaRoot.AddComponent<ArenaController>();

            var floor = new GameObject("Floor", typeof(SpriteRenderer));
            floor.transform.SetParent(arenaRoot.transform, false);
            floor.GetComponent<SpriteRenderer>().sortingOrder = -20;

            var walls = new GameObject("Walls") { layer = obstacleLayer };
            walls.transform.SetParent(arenaRoot.transform, false);
            var wallArt = new SpriteRenderer[4];
            var wallColliders = new BoxCollider2D[4];
            string[] names = { "Top", "Bottom", "Left", "Right" };
            for (int i = 0; i < 4; i++)
            {
                var art = new GameObject("Wall" + names[i], typeof(SpriteRenderer));
                art.transform.SetParent(arenaRoot.transform, false);
                wallArt[i] = art.GetComponent<SpriteRenderer>();
                wallArt[i].sortingOrder = -10;
                wallColliders[i] = walls.AddComponent<BoxCollider2D>();
            }

            var so = new SerializedObject(controller);
            so.FindProperty("viewCamera").objectReferenceValue = Camera.main;
            so.FindProperty("player").objectReferenceValue = Object.FindFirstObjectByType<PlayerHealth>();
            so.FindProperty("playerData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlayerData>(PlayerDataPath);
            so.FindProperty("obstaclePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(ObstaclePrefabPath).GetComponent<Obstacle>();
            so.FindProperty("trapPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(TrapPrefabPath).GetComponent<Trap>();
            so.FindProperty("squareSprite").objectReferenceValue = square;
            so.FindProperty("circleSprite").objectReferenceValue = circle;
            so.FindProperty("floor").objectReferenceValue = floor.GetComponent<SpriteRenderer>();
            SetObjects(so.FindProperty("wallArt"), wallArt);
            SetObjects(so.FindProperty("wallColliders"), wallColliders);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Wire the arena into everything that has to respect it.
            var pool = Object.FindFirstObjectByType<ProjectilePool>();
            var poolSo = new SerializedObject(pool);
            poolSo.FindProperty("arena").objectReferenceValue = controller;
            SerializedProperty mask = poolSo.FindProperty("hitMask").FindPropertyRelative("m_Bits");
            mask.intValue |= 1 << obstacleLayer;
            poolSo.ApplyModifiedPropertiesWithoutUndo();

            SetRef(Object.FindFirstObjectByType<PlayerMover>(), "arena", controller);
            SetRef(Object.FindFirstObjectByType<WaveSpawner>(), "arena", controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ---------------------------------------------------------------- Helpers

        private static void SetObjects(SerializedProperty list, Object[] items)
        {
            list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        private static void SetRef(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
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
