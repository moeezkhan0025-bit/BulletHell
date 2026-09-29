using System.Collections.Generic;
using System.Linq;
using BulletHell.AI;
using BulletHell.Arena;
using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// One-shot M8 setup: the enemy AI tuning asset, the new attack patterns, the enemy roster converted to the new
    /// behaviours (Grunt = Chaser, Weaver = Skirmisher, Ringer / Spiraler = Sentries, plus the new Charger and Sniper),
    /// waves and rounds 1-7 retuned for the colosseum, the move speed difficulty axis, the brain on the Enemy prefab and
    /// the NavigationService + AI debug view in the Game scene. Safe to run again.
    /// </summary>
    public static class M8Setup
    {
        private const string EnemyDir = "Assets/Data/Enemies";
        private const string PatternDir = "Assets/Data/Enemies/Patterns";
        private const string WaveDir = "Assets/Data/Waves";
        private const string RoundDir = "Assets/Data/Waves/Rounds";
        private const string TuningPath = "Assets/Data/Enemies/EnemyAiTuning.asset";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        private const string GamePath = "Assets/Scenes/Game.unity";
        private const string CirclePath = "Assets/Art/Placeholder/Circle.png";

        private static readonly (string key, string asset)[] Roster =
        {
            ("G", "Enemy_Grunt"), ("W", "Enemy_Weaver"), ("R", "Enemy_Ringer"), ("S", "Enemy_Spiraler"),
            ("C", "Enemy_Charger"), ("N", "Enemy_Sniper"), ("B", "Enemy_BossPlaceholder"),
        };

        // Each group: enemy key, count, spawn pattern, delay. Boss rounds (3, 5, 7) stay tougher placeholder rounds.
        // Round 1 chasers with one shooter; Skirmishers from round 2; Chargers from 3, Sentries from 4, Snipers from 5.
        private static readonly string[][][] Rounds =
        {
            new[] { new[] { "G:3:Gates" }, new[] { "G:3:Gates", "W:1:Scatter:1.5" } },                                          // 1
            new[] { new[] { "G:4:Gates" }, new[] { "W:2:Gates", "G:2:Scatter" }, new[] { "W:2:Gates", "G:3:Scatter" } },         // 2
            new[] { new[] { "G:4:Gates", "W:1:Scatter:2" }, new[] { "W:2:Gates", "C:1:Gates:1" }, new[] { "B:1:Gates", "G:2:Scatter:2" } }, // 3 boss
            new[] { new[] { "G:3:Gates", "W:2:Scatter" }, new[] { "R:1:Gates", "G:3:Scatter:1" }, new[] { "C:2:Gates", "W:2:Scatter" } },   // 4
            new[] { new[] { "R:1:Gates", "C:1:Gates:1", "G:3:Scatter" }, new[] { "N:1:Gates", "W:3:Scatter" }, new[] { "B:1:Gates", "R:1:Ring:1" } }, // 5 boss
            new[] { new[] { "G:4:Gates", "N:1:Scatter:1" }, new[] { "S:1:Gates", "W:2:Scatter", "C:1:Gates:1.5" },
                    new[] { "R:1:Gates", "N:2:Scatter", "G:3:Scatter:1" }, new[] { "S:1:Gates", "C:2:Gates", "W:2:Scatter" } },      // 6
            new[] { new[] { "S:1:Gates", "R:1:Gates", "C:1:Gates:1", "G:3:Scatter" }, new[] { "N:2:Scatter", "W:3:Gates", "C:1:Gates:1.5" },
                    new[] { "R:1:Gates", "S:1:Gates", "C:2:Gates", "N:1:Scatter" }, new[] { "B:1:Gates", "S:1:Gates", "R:1:Ring:1" } }, // 7 boss
        };

        private static readonly int[] BossRounds = { 3, 5, 7 };

        [MenuItem("BulletHell/M8/Setup Everything")]
        public static void Run()
        {
            EnemyAiTuning tuning = GetOrCreate<EnemyAiTuning>(TuningPath);
            var tuningSo = new SerializedObject(tuning);
            tuningSo.FindProperty("navRadius").floatValue = 0.45f;   // clears the biggest walking enemy (Ringer, 0.39)
            tuningSo.ApplyModifiedPropertiesWithoutUndo();
            CreatePatterns();
            CreateEnemies();
            CreateWavesAndRounds();
            CreateDifficulty();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SetupEnemyPrefab();
            SetupGameScene(tuning);
            AssetDatabase.SaveAssets();
            Debug.Log("M8 setup complete.");
        }

        // ---------------------------------------------------------------- Patterns and enemies

        private static void CreatePatterns()
        {
            Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);

            AttackPattern rapid = GetOrCreate<AttackPattern>($"{PatternDir}/Pattern_RingRapid.asset");
            var so = new SerializedObject(rapid);
            so.FindProperty("shape").enumValueIndex = (int)AttackShape.Ring;
            so.FindProperty("bulletCount").intValue = 12;
            so.FindProperty("fireInterval").floatValue = 1f;
            so.FindProperty("initialDelay").floatValue = 0.3f;
            so.FindProperty("bulletSpeed").floatValue = 3.6f;
            so.FindProperty("bulletSize").floatValue = 0.3f;
            so.FindProperty("bulletSprite").objectReferenceValue = circle;
            so.ApplyModifiedPropertiesWithoutUndo();

            AttackPattern snipe = GetOrCreate<AttackPattern>($"{PatternDir}/Pattern_SniperShot.asset");
            so = new SerializedObject(snipe);
            so.FindProperty("shape").enumValueIndex = (int)AttackShape.Aimed;
            so.FindProperty("bulletCount").intValue = 1;
            so.FindProperty("bulletSpeed").floatValue = 13f;
            so.FindProperty("bulletSize").floatValue = 0.34f;
            so.FindProperty("damage").floatValue = 1f;
            so.FindProperty("bulletColor").colorValue = new Color(1f, 0.95f, 0.4f);
            so.FindProperty("bulletSprite").objectReferenceValue = circle;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateEnemies()
        {
            // Grunt -> Chaser (contact damage, no gun).
            SetEnemy("Enemy_Grunt", "Grunt", new Color(0.9f, 0.35f, 0.35f), 0.9f, 10f, 5, EnemyBehavior.Chaser, 2.7f, null, null,
                     ("acceleration", 16f), ("brake", 22f), ("turnRate", 420f), ("contactDamage", 1f));

            // Weaver -> Skirmisher (spread shots from a distance).
            SetEnemy("Enemy_Weaver", "Weaver", new Color(0.95f, 0.65f, 0.15f), 0.9f, 16f, 8, EnemyBehavior.Skirmisher, 2.6f,
                     new[] { "Pattern_Spread" }, null,
                     ("acceleration", 12f), ("brake", 18f), ("turnRate", 300f),
                     ("preferredDistance", 5.5f), ("distanceTolerance", 0.9f), ("strafeSpeedFraction", 0.6f));

            // Ringer -> Sentry: plants and blasts rings until it overheats.
            SetEnemy("Enemy_Ringer", "Ringer", new Color(0.3f, 0.85f, 0.95f), 1.3f, 30f, 12, EnemyBehavior.Sentry, 1.8f,
                     new[] { "Pattern_RingRapid" }, null,
                     ("acceleration", 8f), ("brake", 14f), ("turnRate", 200f),
                     ("heatPerShot", 0.25f), ("coolPerSecond", 0.22f), ("restartHeat", 0.3f), ("overheatedSpeedFraction", 0.3f),
                     SentryRange(3.5f, 7.5f));

            // Spiraler -> Sentry: a spiral stream, overheats after a few seconds.
            SetEnemy("Enemy_Spiraler", "Spiraler", new Color(0.7f, 0.4f, 0.95f), 1.1f, 26f, 12, EnemyBehavior.Sentry, 2f,
                     new[] { "Pattern_Spiral" }, null,
                     ("acceleration", 10f), ("brake", 16f), ("turnRate", 240f),
                     ("heatPerShot", 0.045f), ("coolPerSecond", 0.2f), ("restartHeat", 0.3f), ("overheatedSpeedFraction", 0.4f),
                     SentryRange(3f, 7f));

            // New: Charger telegraphs and dashes; jump over it.
            SetEnemy("Enemy_Charger", "Charger", new Color(0.75f, 0.35f, 0.2f), 1.1f, 24f, 10, EnemyBehavior.Charger, 3f, null, null,
                     ("acceleration", 10f), ("brake", 18f), ("turnRate", 240f), ("contactDamage", 1f),
                     ("chargeTriggerRange", 6.5f), ("chargeMinRange", 2.5f), ("telegraphSeconds", 0.9f), ("telegraphLockSeconds", 0.3f),
                     ("dashSpeed", 11f), ("dashDistance", 7.5f), ("recoverSeconds", 1f), ("chargeCooldown", 1.2f));

            // New: Sniper keeps far away and fires one fast, telegraphed shot.
            SetEnemy("Enemy_Sniper", "Sniper", new Color(0.4f, 0.9f, 0.5f), 0.9f, 12f, 10, EnemyBehavior.Sniper, 2.2f, null, "Pattern_SniperShot",
                     ("acceleration", 12f), ("brake", 18f), ("turnRate", 300f),
                     ("preferredDistance", 8f), ("distanceTolerance", 1.2f), ("strafeSpeedFraction", 0.5f),
                     ("aimSeconds", 1.3f), ("aimLockSeconds", 0.4f), ("sniperCooldown", 2.6f));
        }

        private static (string, object) SentryRange(float near, float far) => ("sentryRange", new Vector2(near, far));

        // Extra properties are (name, float) or (name, Vector2).
        private static void SetEnemy(string assetName, string displayName, Color color, float size, float health, int coinValue,
                                     EnemyBehavior behavior, float speed, string[] patterns, string sniperShot,
                                     params (string name, object value)[] extra)
        {
            EnemyData enemy = GetOrCreate<EnemyData>($"{EnemyDir}/{assetName}.asset");
            var so = new SerializedObject(enemy);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("color").colorValue = color;
            so.FindProperty("size").floatValue = size;
            so.FindProperty("maxHealth").floatValue = health;
            so.FindProperty("coinValue").intValue = coinValue;
            so.FindProperty("behavior").enumValueIndex = (int)behavior;
            so.FindProperty("moveSpeed").floatValue = speed;

            patterns = patterns ?? new string[0];
            SerializedProperty attacks = so.FindProperty("attacks");
            attacks.arraySize = patterns.Length;
            for (int i = 0; i < patterns.Length; i++)
                attacks.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<AttackPattern>($"{PatternDir}/{patterns[i]}.asset");

            so.FindProperty("sniperShot").objectReferenceValue = sniperShot == null
                ? null
                : AssetDatabase.LoadAssetAtPath<AttackPattern>($"{PatternDir}/{sniperShot}.asset");

            foreach ((string name, object value) in extra)
            {
                SerializedProperty property = so.FindProperty(name);
                if (property == null)
                {
                    Debug.LogWarning($"EnemyData has no field '{name}'.");
                    continue;
                }
                if (value is float f)
                    property.floatValue = f;
                else if (value is Vector2 v)
                    property.vector2Value = v;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
        }

        // ---------------------------------------------------------------- Waves, rounds, difficulty

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
            return new SpawnGroup
            {
                Enemy = AssetDatabase.LoadAssetAtPath<EnemyData>($"{EnemyDir}/{asset}.asset"),
                Count = count,
                Pattern = (SpawnPattern)System.Enum.Parse(typeof(SpawnPattern), part[2]),
                Delay = part.Length > 3 ? float.Parse(part[3], System.Globalization.CultureInfo.InvariantCulture) : 0f,
                Interval = count > 1 ? 0.4f : 0f,
            };
        }

        private static void CreateDifficulty()
        {
            var curve = AssetDatabase.LoadAssetAtPath<DifficultyCurve>($"{WaveDir}/DifficultyCurve.asset");
            curve.SetMoveSpeed(new DifficultyCurve.Axis
            {
                Curve = AnimationCurve.Linear(1f, 1f, 7f, 1.25f),
                BeyondLastKeyPerRound = 0.02f,
                Min = 0.8f,
                Max = 1.8f,
            });
        }

        // ---------------------------------------------------------------- Prefab and scene

        private static void SetupEnemyPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
            try
            {
                var brain = root.GetComponent<EnemyBrain>();
                if (brain == null)
                    brain = root.AddComponent<EnemyBrain>();
                var so = new SerializedObject(root.GetComponent<Enemy>());
                so.FindProperty("brain").objectReferenceValue = brain;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupGameScene(EnemyAiTuning tuning)
        {
            var scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);

            var navigation = Object.FindFirstObjectByType<NavigationService>(FindObjectsInactive.Include);
            if (navigation == null)
                navigation = new GameObject("Navigation").AddComponent<NavigationService>();
            SetRef(navigation, "arena", Object.FindFirstObjectByType<ArenaController>());
            SetRef(navigation, "player", Object.FindFirstObjectByType<PlayerHealth>());
            SetRef(navigation, "tuning", tuning);

            var view = navigation.GetComponent<AiDebugView>();
            if (view == null)
                view = navigation.gameObject.AddComponent<AiDebugView>();
            SetRef(view, "navigation", navigation);

            SetRef(Object.FindFirstObjectByType<WaveSpawner>(FindObjectsInactive.Include), "navigation", navigation);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ---------------------------------------------------------------- Helpers

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
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
    }
}
