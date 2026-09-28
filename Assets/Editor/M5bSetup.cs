using System.Collections.Generic;
using System.Linq;
using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Pickups;
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
    /// One-shot M5b setup: the enemy roster (Grunt, Weaver, Ringer, Spiraler + a boss placeholder), waves and rounds 1-7,
    /// the difficulty curve, combat and coin tuning, the coin prefab, and the Game scene wiring that replaces the stub
    /// round with the wave spawner. Safe to run again: assets are updated in place, scene objects only added if missing.
    /// </summary>
    public static class M5bSetup
    {
        private const string EnemyDir = "Assets/Data/Enemies";
        private const string WaveDir = "Assets/Data/Waves";
        private const string RoundDir = "Assets/Data/Waves/Rounds";
        private const string PatternDir = "Assets/Data/Enemies/Patterns";
        private const string ConfigPath = "Assets/Resources/GameConfig.asset";
        private const string GamePath = "Assets/Scenes/Game.unity";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        private const string CoinPrefabPath = "Assets/Prefabs/Coin.prefab";

        private static readonly (string key, string asset)[] Roster =
        {
            ("G", "Enemy_Grunt"), ("W", "Enemy_Weaver"), ("R", "Enemy_Ringer"), ("S", "Enemy_Spiraler"), ("B", "Enemy_BossPlaceholder"),
        };

        [MenuItem("BulletHell/M5b/Setup Waves And Coins")]
        public static void Run()
        {
            CreateFolder("Assets/Data", "Waves");
            CreateFolder(WaveDir, "Rounds");

            CreatePatternAndEnemies();
            CreateWavesAndRounds();
            CreateTuningAssets();
            RenameEnemyPrefab();
            CreateCoinPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ConfigureGame();
            SetupScene();
            AssetDatabase.SaveAssets();
            Debug.Log("M5b setup complete.");
        }

        // ---------------------------------------------------------------- Enemies

        private static void CreatePatternAndEnemies()
        {
            AttackPattern burst = GetOrCreate<AttackPattern>($"{PatternDir}/Pattern_BossBurst.asset");
            var burstSo = new SerializedObject(burst);
            burstSo.FindProperty("shape").enumValueIndex = (int)AttackShape.Spread;
            burstSo.FindProperty("bulletCount").intValue = 9;
            burstSo.FindProperty("spreadAngle").floatValue = 80f;
            burstSo.FindProperty("fireInterval").floatValue = 1.8f;
            burstSo.FindProperty("bulletSpeed").floatValue = 4.5f;
            burstSo.FindProperty("bulletSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Circle.png");
            burstSo.ApplyModifiedPropertiesWithoutUndo();

            CreateEnemy("Enemy_Grunt", "Grunt", new Color(0.9f, 0.35f, 0.35f), 0.9f, 10f, 1.2f, 1.5f, Vector2.right, 5, "Pattern_Aimed");
            CreateEnemy("Enemy_Weaver", "Weaver", new Color(0.95f, 0.65f, 0.15f), 0.9f, 16f, 2.6f, 3f, Vector2.right, 8, "Pattern_Spread");
            CreateEnemy("Enemy_Ringer", "Ringer", new Color(0.3f, 0.85f, 0.95f), 1.3f, 30f, 0f, 0f, Vector2.right, 12, "Pattern_Ring");
            CreateEnemy("Enemy_Spiraler", "Spiraler", new Color(0.7f, 0.4f, 0.95f), 1.1f, 26f, 1f, 2f, Vector2.up, 12, "Pattern_Spiral");
            CreateEnemy("Enemy_BossPlaceholder", "Warden (boss placeholder)", new Color(0.95f, 0.25f, 0.5f), 2.4f, 260f, 0.8f, 4f,
                        Vector2.right, 80, "Pattern_Ring", "Pattern_Aimed", "Pattern_BossBurst");
        }

        private static void CreateEnemy(string assetName, string displayName, Color color, float size, float health,
                                        float speed, float range, Vector2 axis, int coinValue, params string[] patterns)
        {
            EnemyData enemy = GetOrCreate<EnemyData>($"{EnemyDir}/{assetName}.asset");
            var so = new SerializedObject(enemy);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("color").colorValue = color;
            so.FindProperty("size").floatValue = size;
            so.FindProperty("maxHealth").floatValue = health;
            so.FindProperty("moveSpeed").floatValue = speed;
            so.FindProperty("moveRange").floatValue = range;
            so.FindProperty("moveAxis").vector2Value = axis;
            so.FindProperty("coinValue").intValue = coinValue;
            SerializedProperty attacks = so.FindProperty("attacks");
            attacks.arraySize = patterns.Length;
            for (int i = 0; i < patterns.Length; i++)
                attacks.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<AttackPattern>($"{PatternDir}/{patterns[i]}.asset");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
        }

        // ---------------------------------------------------------------- Waves and rounds

        // Each group: enemy key, count, pattern, delay, interval. Boss rounds (3, 5, 7) are tougher placeholder rounds
        // built from the four enemy types plus the boss placeholder; real bosses replace them in M7.
        // Round 1 grunts only; round 2 adds Weavers; round 4 Ringers; round 6 Spiralers.
        private static readonly string[][][] Rounds =
        {
            new[] { new[] { "G:3" }, new[] { "G:5" } },                                                               // 1
            new[] { new[] { "G:3" }, new[] { "W:2", "G:2" }, new[] { "W:3", "G:3" } },                                // 2
            new[] { new[] { "G:6:Row" }, new[] { "W:3", "G:2:Ring" }, new[] { "B:1:Row", "G:2:Scatter:2" } },         // 3 boss
            new[] { new[] { "G:3", "W:2" }, new[] { "R:2:Ring", "G:2" }, new[] { "R:2", "W:3" } },                    // 4
            new[] { new[] { "R:2", "G:4" }, new[] { "W:4", "R:2" }, new[] { "B:1:Row", "R:2:Ring:1" } },              // 5 boss
            new[] { new[] { "G:3", "S:1" }, new[] { "W:2", "R:2", "S:1" }, new[] { "S:3", "W:2" }, new[] { "S:2", "R:2", "G:3" } }, // 6
            new[] { new[] { "S:2", "R:2", "G:4" }, new[] { "W:4", "S:3" }, new[] { "R:2", "S:3", "G:3" }, new[] { "B:1:Row", "S:2", "R:2:Ring:1" } }, // 7 boss
        };

        private static readonly int[] BossRounds = { 3, 5, 7 };

        private static void CreateWavesAndRounds()
        {
            for (int r = 0; r < Rounds.Length; r++)
            {
                var waves = new List<WaveData>();
                for (int w = 0; w < Rounds[r].Length; w++)
                {
                    WaveData wave = GetOrCreate<WaveData>($"{WaveDir}/Wave_R{r + 1}_{w + 1}.asset");
                    wave.Set(Rounds[r][w].Select(ParseGroup).ToArray());
                    waves.Add(wave);
                }

                RoundData round = GetOrCreate<RoundData>($"{RoundDir}/Round_{r + 1}.asset");
                round.Set(waves.ToArray(), BossRounds.Contains(r + 1), BossRounds.Contains(r + 1) ? 3f : 2.5f);
            }
        }

        private static SpawnGroup ParseGroup(string spec)
        {
            string[] part = spec.Split(':');
            string asset = Roster.First(e => e.key == part[0]).asset;
            int count = int.Parse(part[1]);
            SpawnPattern pattern = part.Length > 2 ? (SpawnPattern)System.Enum.Parse(typeof(SpawnPattern), part[2]) : SpawnPattern.Scatter;
            float delay = part.Length > 3 ? float.Parse(part[3]) : 0f;
            return new SpawnGroup
            {
                Enemy = AssetDatabase.LoadAssetAtPath<EnemyData>($"{EnemyDir}/{asset}.asset"),
                Count = count,
                Pattern = pattern,
                Delay = delay,
                Interval = count > 1 ? 0.4f : 0f,
            };
        }

        // ---------------------------------------------------------------- Tuning

        private static void CreateTuningAssets()
        {
            var curve = GetOrCreate<DifficultyCurve>($"{WaveDir}/DifficultyCurve.asset");
            curve.Set(
                Axis(1f, 1.6f, 0.10f, 0.5f, 3f),   // enemy count
                Axis(1f, 2.4f, 0.25f, 0.5f, 8f),   // enemy health
                Axis(1f, 1.5f, 0.05f, 0.5f, 2.5f), // fire rate
                Axis(1f, 1.35f, 0.03f, 0.5f, 1.8f)); // bullet speed

            GetOrCreate<CombatTuning>($"{WaveDir}/CombatTuning.asset");
            GetOrCreate<CoinTuning>("Assets/Data/Pickups/CoinTuning.asset");
        }

        private static DifficultyCurve.Axis Axis(float round1, float round7, float beyondPerRound, float min, float max) =>
            new DifficultyCurve.Axis
            {
                Curve = AnimationCurve.Linear(1f, round1, 7f, round7),
                BeyondLastKeyPerRound = beyondPerRound,
                Min = min,
                Max = max,
            };

        // ---------------------------------------------------------------- Prefabs

        private static void RenameEnemyPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath) != null)
                return;
            string error = AssetDatabase.MoveAsset("Assets/Prefabs/TestEnemy.prefab", EnemyPrefabPath);
            if (!string.IsNullOrEmpty(error))
                Debug.LogWarning($"Could not rename the enemy prefab: {error}");
        }

        private static void CreateCoinPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath) != null)
                return;

            var go = new GameObject("Coin", typeof(SpriteRenderer), typeof(CoinPickup));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Circle.png");
            sr.color = new Color(1f, 0.85f, 0.2f);
            sr.sortingOrder = 4;
            PrefabUtility.SaveAsPrefabAsset(go, CoinPrefabPath);
            Object.DestroyImmediate(go);
        }

        // ---------------------------------------------------------------- Config

        private static void ConfigureGame()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var so = new SerializedObject(config);
            SerializedProperty rounds = so.FindProperty("rounds");
            rounds.arraySize = Rounds.Length;
            for (int r = 0; r < Rounds.Length; r++)
                rounds.GetArrayElementAtIndex(r).objectReferenceValue = AssetDatabase.LoadAssetAtPath<RoundData>($"{RoundDir}/Round_{r + 1}.asset");
            so.FindProperty("endlessLoopStartRound").intValue = 4;
            so.FindProperty("difficulty").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DifficultyCurve>($"{WaveDir}/DifficultyCurve.asset");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        // ---------------------------------------------------------------- Scene

        private static void SetupScene()
        {
            var scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<WaveSpawner>(FindObjectsInactive.Include) != null)
            {
                Debug.Log("Game scene already has the wave spawner; left unchanged.");
                return;
            }

            // The stub round is gone: remove its enemies and the missing CombatController component.
            GameObject stubEnemies = GameObject.Find("TestEnemies");
            if (stubEnemies != null)
                Object.DestroyImmediate(stubEnemies);
            var flow = Object.FindFirstObjectByType<GameFlowUI>();
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(flow.gameObject);

            var player = Object.FindFirstObjectByType<PlayerHealth>();
            var projectiles = Object.FindFirstObjectByType<ProjectilePool>();
            var camera = Camera.main;
            var combatTuning = AssetDatabase.LoadAssetAtPath<CombatTuning>($"{WaveDir}/CombatTuning.asset");
            var coinTuning = AssetDatabase.LoadAssetAtPath<CoinTuning>("Assets/Data/Pickups/CoinTuning.asset");

            var enemyPool = new GameObject("EnemyPool").AddComponent<EnemyPool>();
            SetRef(enemyPool, "prefab", AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath).GetComponent<Enemy>());
            SetRef(enemyPool, "tuning", combatTuning);

            var coins = new GameObject("CoinField").AddComponent<CoinField>();
            SetRef(coins, "tuning", coinTuning);
            SetRef(coins, "prefab", AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath).GetComponent<CoinPickup>());
            SetRef(coins, "player", player);

            Transform safe = FindCanvas("FlowUI").Find("SafeArea");
            var banner = CreateBanner(safe);

            var spawner = new GameObject("WaveSpawner").AddComponent<WaveSpawner>();
            SetRef(spawner, "tuning", combatTuning);
            SetRef(spawner, "enemyPool", enemyPool);
            SetRef(spawner, "projectiles", projectiles);
            SetRef(spawner, "coins", coins);
            SetRef(spawner, "player", player);
            SetRef(spawner, "banner", banner);
            SetRef(spawner, "viewCamera", camera);

            SetRef(Object.FindFirstObjectByType<DebugOverlay>(), "waveSpawner", spawner);
            AddDebugRoundPicker(safe.Find("PausePanel"));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static WaveBanner CreateBanner(Transform safe)
        {
            Text text = UiBuilder.CreateText("WaveBanner", safe, "", 96, TextAnchor.MiddleCenter);
            text.fontStyle = FontStyle.Bold;
            text.supportRichText = true;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(3f, -3f);

            var rect = (RectTransform)text.transform;
            rect.anchorMin = new Vector2(0f, 0.62f);
            rect.anchorMax = new Vector2(1f, 0.85f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetSiblingIndex(1); // behind the panels, above the HUD text

            var banner = text.gameObject.AddComponent<WaveBanner>();
            SetRef(banner, "label", text);
            return banner;
        }

        private static void AddDebugRoundPicker(Transform pausePanel)
        {
            Transform box = pausePanel.Find("Box");
            RectTransform row = UiBuilder.CreateRect("DebugRoundRow", box);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().minHeight = 70f;

            Text label = UiBuilder.CreateText("Label", row, "", 30, TextAnchor.MiddleLeft);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            Button lower = UiBuilder.CreateButton("Lower", row, "-", 70f);
            Button raise = UiBuilder.CreateButton("Raise", row, "+", 70f);
            Button go = UiBuilder.CreateButton("Go", row, "Go", 70f);
            foreach (Button b in new[] { lower, raise, go })
                b.gameObject.GetComponent<LayoutElement>().preferredWidth = b == go ? 150f : 90f;

            // Sits just above the Continue/Resume button.
            row.SetSiblingIndex(box.Find("Continue").GetSiblingIndex());

            var picker = row.gameObject.AddComponent<DebugRoundPicker>();
            SetRef(picker, "label", label);
            SetRef(picker, "lowerButton", lower);
            SetRef(picker, "raiseButton", raise);
            SetRef(picker, "goButton", go);
        }

        // ---------------------------------------------------------------- Helpers

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

        private static void CreateFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
                AssetDatabase.CreateFolder(parent, name);
        }

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
