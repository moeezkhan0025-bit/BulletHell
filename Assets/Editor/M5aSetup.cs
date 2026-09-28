using System.Linq;
using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Input;
using BulletHell.Player;
using BulletHell.Projectiles;
using BulletHell.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// One-shot M5a setup: the four test attack patterns and enemy types that use them, player health, the enemy
    /// attacker on the enemy prefab, the pool/combat wiring, the Pause and Game Over panels, and the Start-button
    /// routing in the Main Menu and Game scenes. Safe to run again.
    /// </summary>
    public static class M5aSetup
    {
        private const string PatternDir = "Assets/Data/Enemies/Patterns";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string EnemyPrefabPath = "Assets/Prefabs/TestEnemy.prefab";
        private const string MenuPath = "Assets/Scenes/MainMenu.unity";
        private const string GamePath = "Assets/Scenes/Game.unity";

        [MenuItem("BulletHell/M5a/Setup Combat")]
        public static void Run()
        {
            AttackPattern aimed = CreatePattern("Pattern_Aimed", so =>
            {
                SetEnum(so, "shape", AttackShape.Aimed);
                so.FindProperty("fireInterval").floatValue = 1.6f;
                so.FindProperty("bulletSpeed").floatValue = 5f;
            });
            AttackPattern spread = CreatePattern("Pattern_Spread", so =>
            {
                SetEnum(so, "shape", AttackShape.Spread);
                so.FindProperty("bulletCount").intValue = 5;
                so.FindProperty("spreadAngle").floatValue = 50f;
                so.FindProperty("fireInterval").floatValue = 2.2f;
                so.FindProperty("bulletSpeed").floatValue = 4.5f;
            });
            AttackPattern ring = CreatePattern("Pattern_Ring", so =>
            {
                SetEnum(so, "shape", AttackShape.Ring);
                so.FindProperty("bulletCount").intValue = 14;
                so.FindProperty("fireInterval").floatValue = 3f;
                so.FindProperty("bulletSpeed").floatValue = 3.5f;
            });
            AttackPattern spiral = CreatePattern("Pattern_Spiral", so =>
            {
                SetEnum(so, "shape", AttackShape.Spiral);
                so.FindProperty("bulletCount").intValue = 3;
                so.FindProperty("spiralStep").floatValue = 17f;
                so.FindProperty("fireInterval").floatValue = 0.2f;
                so.FindProperty("bulletSpeed").floatValue = 3.5f;
                so.FindProperty("bulletSize").floatValue = 0.25f;
            });

            EnemyData stationary = SetAttacks("Assets/Data/Enemies/Enemy_TestStatic.asset", aimed);
            SetAttacks("Assets/Data/Enemies/Enemy_TestMover.asset", spread);
            CreateEnemyVariant("Enemy_TestRing", "Test Ring Shooter", stationary, ring);
            CreateEnemyVariant("Enemy_TestSpiral", "Test Spiral Shooter", stationary, spiral);

            SetupPlayerPrefab();
            SetupEnemyPrefab();
            SetupMenuScene();
            SetupGameScene();
            AssetDatabase.SaveAssets();
            Debug.Log("M5a setup complete.");
        }

        // ---------------------------------------------------------------- Assets

        private static AttackPattern CreatePattern(string name, System.Action<SerializedObject> configure)
        {
            if (!AssetDatabase.IsValidFolder(PatternDir))
                AssetDatabase.CreateFolder("Assets/Data/Enemies", "Patterns");

            string path = $"{PatternDir}/{name}.asset";
            var pattern = AssetDatabase.LoadAssetAtPath<AttackPattern>(path);
            if (pattern == null)
            {
                pattern = ScriptableObject.CreateInstance<AttackPattern>();
                AssetDatabase.CreateAsset(pattern, path);
            }

            var so = new SerializedObject(pattern);
            so.FindProperty("bulletSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Circle.png");
            configure(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pattern);
            return pattern;
        }

        private static void SetEnum(SerializedObject so, string property, System.Enum value) =>
            so.FindProperty(property).enumValueIndex = System.Convert.ToInt32(value);

        private static EnemyData SetAttacks(string path, params AttackPattern[] patterns)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            var so = new SerializedObject(enemy);
            SerializedProperty list = so.FindProperty("attacks");
            list.arraySize = patterns.Length;
            for (int i = 0; i < patterns.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = patterns[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
            return enemy;
        }

        private static EnemyData CreateEnemyVariant(string name, string displayName, EnemyData source, AttackPattern pattern)
        {
            string path = $"Assets/Data/Enemies/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<EnemyData>(path) == null)
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), path);

            EnemyData enemy = SetAttacks(path, pattern);
            var so = new SerializedObject(enemy);
            so.FindProperty("displayName").stringValue = displayName;
            so.ApplyModifiedPropertiesWithoutUndo();
            return enemy;
        }

        // ---------------------------------------------------------------- Prefabs

        private static void SetupPlayerPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var health = root.GetComponent<PlayerHealth>() ?? root.AddComponent<PlayerHealth>();
                var so = new SerializedObject(health);
                so.FindProperty("data").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlayerData>("Assets/Data/Player/PlayerData.asset");
                so.FindProperty("body").objectReferenceValue = root.transform.Find("Body").GetComponent<SpriteRenderer>();
                so.ApplyModifiedPropertiesWithoutUndo();
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
                if (root.GetComponent<EnemyAttacker>() == null)
                    root.AddComponent<EnemyAttacker>();
                PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------------------------------------------------------------- Scenes

        private static void SetupMenuScene()
        {
            var scene = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<MenuPrimaryRouter>(FindObjectsInactive.Include) != null)
            {
                Debug.Log("MainMenu scene already has Start-button routing; left unchanged.");
                return;
            }

            var controller = Object.FindFirstObjectByType<MainMenuController>();
            Transform safe = FindCanvas("MenuCanvas").Find("SafeArea");
            var reader = controller.gameObject.AddComponent<MenuInputReader>();
            var router = controller.gameObject.AddComponent<MenuPrimaryRouter>();
            ConfigureRouter(router, reader, false, new[]
            {
                // While the overwrite dialog is up Start does nothing, so it can never confirm a destructive choice.
                (safe.Find("ConfirmOverwrite").gameObject, (Button)null),
                (safe.Find("Buttons").gameObject, safe.Find("Buttons/StartGame").GetComponent<Button>()),
            });

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetupGameScene()
        {
            var scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<MenuPrimaryRouter>(FindObjectsInactive.Include) != null)
            {
                Debug.Log("Game scene already has the M5a objects; left unchanged.");
                return;
            }

            var player = Object.FindFirstObjectByType<PlayerHealth>();
            var pool = Object.FindFirstObjectByType<ProjectilePool>();
            var combat = Object.FindFirstObjectByType<CombatController>();
            var flow = Object.FindFirstObjectByType<GameFlowUI>();
            Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).OrderBy(e => e.name).ToArray();

            // Enemy types: Static_2 rings, Static_3 spirals (the rest keep aimed / spread from their data).
            // Reload by path: assets loaded before OpenScene can be unloaded by it and come back null.
            SetEnemyData(enemies.First(e => e.name == "Static_2"), AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/Enemies/Enemy_TestRing.asset"));
            SetEnemyData(enemies.First(e => e.name == "Static_3"), AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/Enemies/Enemy_TestSpiral.asset"));

            var poolSo = new SerializedObject(pool);
            poolSo.FindProperty("playerTarget").objectReferenceValue = player;
            poolSo.FindProperty("prewarm").intValue = 512;
            poolSo.FindProperty("maxSize").intValue = 2048;
            poolSo.ApplyModifiedPropertiesWithoutUndo();

            var combatSo = new SerializedObject(combat);
            combatSo.FindProperty("pool").objectReferenceValue = pool;
            combatSo.FindProperty("player").objectReferenceValue = player;
            combatSo.ApplyModifiedPropertiesWithoutUndo();

            var hud = Object.FindFirstObjectByType<RunHud>();
            SetRef(hud, "playerHealth", player);
            SetRef(Object.FindFirstObjectByType<DebugOverlay>(), "playerHealth", player);

            Transform safe = FindCanvas("FlowUI").Find("SafeArea");
            FlowPanel pause = M4Setup.CreateFlowPanel(safe, "PausePanel", "Resume");
            FlowPanel gameOver = M4Setup.CreateFlowPanel(safe, "GameOverPanel", "Continue");
            var flowSo = new SerializedObject(flow);
            flowSo.FindProperty("pause").objectReferenceValue = pause;
            flowSo.FindProperty("gameOver").objectReferenceValue = gameOver;
            flowSo.ApplyModifiedPropertiesWithoutUndo();
            pause.gameObject.SetActive(false);
            gameOver.gameObject.SetActive(false);

            var reader = flow.gameObject.AddComponent<MenuInputReader>();
            var router = flow.gameObject.AddComponent<MenuPrimaryRouter>();
            ConfigureRouter(router, reader, true, new[]
            {
                (pause.gameObject, pause.transform.Find("Box/Continue").GetComponent<Button>()),
                (gameOver.gameObject, gameOver.transform.Find("Box/MainMenu").GetComponent<Button>()),
                (safe.Find("RoundResultsPanel").gameObject, safe.Find("RoundResultsPanel/Box/Continue").GetComponent<Button>()),
                (safe.Find("ShopScreen").gameObject, safe.Find("ShopScreen/Box/Footer/Continue").GetComponent<Button>()),
                (safe.Find("ArmoryScreen").gameObject, safe.Find("ArmoryScreen/Box/Footer/Continue").GetComponent<Button>()),
            });

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureRouter(MenuPrimaryRouter router, MenuInputReader reader, bool guardStates,
                                            (GameObject scope, Button button)[] routes)
        {
            var so = new SerializedObject(router);
            so.FindProperty("reader").objectReferenceValue = reader;
            so.FindProperty("guardRunStateChanges").boolValue = guardStates;
            SerializedProperty list = so.FindProperty("routes");
            list.arraySize = routes.Length;
            for (int i = 0; i < routes.Length; i++)
            {
                SerializedProperty route = list.GetArrayElementAtIndex(i);
                route.FindPropertyRelative("Scope").objectReferenceValue = routes[i].scope;
                route.FindPropertyRelative("Button").objectReferenceValue = routes[i].button;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnemyData(Enemy enemy, EnemyData data) => SetRef(enemy, "data", data);

        private static void SetRef(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindCanvas(string name) =>
            Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c => c.name == name).transform;
    }
}
