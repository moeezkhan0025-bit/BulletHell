using System.Collections.Generic;
using BulletHell.AI;
using BulletHell.Arena;
using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Player;
using BulletHell.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// One-shot M8.5 setup: sets the height class of every obstacle, creates one ArenaLayoutData per round (1-7, with
    /// separate open layouts for the boss rounds 3, 5 and 7) and each round's HazardBudget, points the rounds and the
    /// GameConfig at them, retunes the waves (via M8Setup's tables), adds the Tall-obstacle fader and the debug
    /// "View" (layout preview) button to the Game scene, and validates every layout. Safe to run again.
    /// </summary>
    public static class M85Setup
    {
        private const string ArenaDir = "Assets/Data/Arenas";
        private const string LayoutDir = "Assets/Data/Arenas/Layouts";
        private const string ShellPath = "Assets/Data/Arenas/Arena_Colosseum01.asset";
        private const string RoundDir = "Assets/Data/Waves/Rounds";
        private const string ConfigPath = "Assets/Resources/GameConfig.asset";
        private const string GamePath = "Assets/Scenes/Game.unity";

        private static readonly Vector2 Spawn = new Vector2(0f, -3.2f);
        private static readonly Vector2[] Gates =
        {
            new Vector2(-5f, 3.8f), new Vector2(5f, 3.8f), new Vector2(-7.2f, 0.5f), new Vector2(7.2f, 0.5f),
        };

        private struct Obstacles
        {
            public ObstacleData Pillar, LowWall, Crate, Pumpkin, Cabbage;
        }

        private struct Traps
        {
            public TrapData Vent, Skewer, Zone;
        }

        [MenuItem("BulletHell/M8.5/Setup Everything")]
        public static void Run()
        {
            Obstacles obstacles = SetHeightClasses();
            Traps traps = LoadTraps();
            ArenaData shell = AssetDatabase.LoadAssetAtPath<ArenaData>(ShellPath);
            if (shell == null || traps.Vent == null)
            {
                Debug.LogError("M8.5: the arena shell or trap assets are missing (Assets/Data/Arenas).");
                return;
            }

            EnsureFolder(LayoutDir);
            var layouts = new ArenaLayoutData[7];
            var budgets = new HazardBudget[7];
            BuildLayouts(shell, obstacles, traps, layouts, budgets);

            M8Setup.CreateWavesAndRounds();   // waves retuned for the new layouts
            AssignToRounds(layouts, budgets);
            AssetDatabase.SaveAssets();

            if (!ValidateAll(layouts, budgets))
                return;

            SetupGameScene();
            AssetDatabase.SaveAssets();
            Debug.Log("M8.5 setup complete: 7 layouts valid.");
        }

        // ---------------------------------------------------------------- Height classes

        private static Obstacles SetHeightClasses()
        {
            var o = new Obstacles
            {
                Pillar = LoadObstacle("Pillar"), LowWall = LoadObstacle("LowWall"), Crate = LoadObstacle("Crate"),
                Pumpkin = LoadObstacle("Pumpkin"), Cabbage = LoadObstacle("Cabbage"),
            };
            o.Pillar.ConfigureHeightClass(ObstacleHeightClass.Tall);
            o.Pumpkin.ConfigureHeightClass(ObstacleHeightClass.Tall);
            o.LowWall.ConfigureHeightClass(ObstacleHeightClass.Low);
            o.Crate.ConfigureHeightClass(ObstacleHeightClass.Low);
            o.Cabbage.ConfigureHeightClass(ObstacleHeightClass.Low);

            // A big pumpkin: a wider footprint and a body well above the jump (placeholder proportions; arena objects keep the original scale, only characters are 1.5x).
            o.Pumpkin.Configure(ObstacleKind.Breakable, ObstacleShape.Circle, new Vector2(1.3f, 0.8f), Color.white, 40f);
            o.Pumpkin.ConfigureArt(o.Pumpkin.Sprite, new Vector2(1.6f, 1.6f));
            return o;
        }

        private static ObstacleData LoadObstacle(string name) =>
            AssetDatabase.LoadAssetAtPath<ObstacleData>($"{ArenaDir}/Obstacles/Obstacle_{name}.asset");

        private static Traps LoadTraps() => new Traps
        {
            Vent = AssetDatabase.LoadAssetAtPath<TrapData>($"{ArenaDir}/Traps/Trap_Vent.asset"),
            Skewer = AssetDatabase.LoadAssetAtPath<TrapData>($"{ArenaDir}/Traps/Trap_Skewer.asset"),
            Zone = AssetDatabase.LoadAssetAtPath<TrapData>($"{ArenaDir}/Traps/Trap_Zone.asset"),
        };

        // ---------------------------------------------------------------- Layouts

        // One new hazard at a time: R1 cover only, R2 breakables, R3 vents (boss, open), R4 more vents, R5 zone (boss, open),
        // R6 skewer, R7 everything with an open middle (boss).
        private static void BuildLayouts(ArenaData shell, Obstacles o, Traps t, ArenaLayoutData[] layouts, HazardBudget[] budgets)
        {
            ObstaclePlacement P(ObstacleData d, float x, float y, float w = 0f, float h = 0f) =>
                new ObstaclePlacement { Data = d, Position = new Vector2(x, y), SizeOverride = new Vector2(w, h) };
            TrapPlacement T(TrapData d, float x, float y, float rot = 0f, float delay = 0f) =>
                new TrapPlacement { Data = d, Position = new Vector2(x, y), Rotation = rot, ExtraStartDelay = delay };

            // 1: cover only, no traps or zones.
            Make(0, shell, layouts, budgets,
                new[] { P(o.Pillar, -3.5f, 0.8f), P(o.Pillar, 3.5f, 0.8f),
                        P(o.LowWall, 0f, 1.6f, 2.6f, 0.5f), P(o.LowWall, -5.5f, -1.5f, 0.5f, 1.6f), P(o.LowWall, 5.5f, -1.5f, 0.5f, 1.6f) },
                new TrapPlacement[0],
                new HazardBudget { LowObstacles = 4, TallObstacles = 4 });

            // 2: + breakables (crates, a cabbage, a big pumpkin).
            Make(1, shell, layouts, budgets,
                new[] { P(o.Pillar, -3.5f, 0.8f), P(o.Pillar, 3.5f, 0.8f), P(o.Pillar, -3.5f, -1.4f), P(o.Pillar, 3.5f, -1.4f),
                        P(o.LowWall, 0f, 1.9f, 2.6f, 0.5f), P(o.Crate, -1.8f, -0.2f), P(o.Crate, 1.8f, -0.2f),
                        P(o.Pumpkin, -5.5f, 2.2f), P(o.Cabbage, 5.5f, 2.2f) },
                new TrapPlacement[0],
                new HazardBudget { LowObstacles = 5, TallObstacles = 5, Breakables = 4 });

            // 3 (boss, open): two pillars, two crates, the first trap: a pair of vents.
            Make(2, shell, layouts, budgets,
                new[] { P(o.Pillar, -3.2f, 0.5f), P(o.Pillar, 3.2f, 0.5f), P(o.Crate, -1.4f, 1.6f), P(o.Crate, 1.4f, 1.6f) },
                new[] { T(t.Vent, -5.5f, -2.6f), T(t.Vent, 5.5f, -2.6f, 0f, 1.5f) },
                new HazardBudget { Vents = 2, LowObstacles = 2, TallObstacles = 2, Breakables = 2 });

            // 4: the round 2 cover plus three vents.
            Make(3, shell, layouts, budgets,
                new[] { P(o.Pillar, -3.5f, 0.8f), P(o.Pillar, 3.5f, 0.8f), P(o.Pillar, -3.5f, -1.4f), P(o.Pillar, 3.5f, -1.4f),
                        P(o.LowWall, 0f, 1.9f, 2.6f, 0.5f), P(o.Crate, -1.8f, -0.2f), P(o.Crate, 1.8f, -0.2f) },
                new[] { T(t.Vent, -5.5f, -2.6f), T(t.Vent, 5.5f, -2.6f, 0f, 1.5f), T(t.Vent, 0f, 0.4f, 0f, 0.75f) },
                new HazardBudget { Vents = 3, LowObstacles = 3, TallObstacles = 4, Breakables = 2 });

            // 5 (boss, open): vents plus the first hazard zone in the middle.
            Make(4, shell, layouts, budgets,
                new[] { P(o.Pillar, -3.2f, 0.6f), P(o.Pillar, 3.2f, 0.6f), P(o.LowWall, 0f, 1.7f, 2f, 0.5f) },
                new[] { T(t.Vent, -5.5f, -2.6f), T(t.Vent, 5.5f, -2.6f, 0f, 1.5f), T(t.Zone, 0f, -0.1f) },
                new HazardBudget { Vents = 2, Zones = 1, LowObstacles = 1, TallObstacles = 2 });

            // 6: the first skewer line, on top of vents, a zone and mixed cover.
            Make(5, shell, layouts, budgets,
                new[] { P(o.Pillar, -3.5f, 0.8f), P(o.Pillar, 3.5f, 0.8f), P(o.Pillar, -3.5f, -1.4f), P(o.Pillar, 3.5f, -1.4f),
                        P(o.LowWall, 0f, 1.9f, 2.6f, 0.5f), P(o.Pumpkin, -5.5f, 2.2f), P(o.Cabbage, 5.5f, 2.2f) },
                new[] { T(t.Vent, -5.5f, -2.6f), T(t.Vent, 5.5f, -2.6f, 0f, 1.5f), T(t.Zone, -5.2f, -0.3f), T(t.Skewer, 0f, 0.6f, 90f, 0.5f) },
                new HazardBudget { Vents = 2, Skewers = 1, Zones = 1, LowObstacles = 2, TallObstacles = 5, Breakables = 2 });

            // 7 (boss): every hazard, but the middle stays open.
            Make(6, shell, layouts, budgets,
                new[] { P(o.Pillar, -3.5f, 0.8f), P(o.Pillar, 3.5f, 0.8f), P(o.LowWall, -2.2f, -0.5f, 1.2f, 0.5f),
                        P(o.LowWall, 2.2f, -0.5f, 1.2f, 0.5f), P(o.Pumpkin, 5.5f, 2.2f) },
                new[] { T(t.Vent, -5.5f, -2.6f), T(t.Vent, 5.5f, -2.6f, 0f, 1.5f), T(t.Zone, -5.2f, -0.3f), T(t.Skewer, 0f, 0.6f, 90f, 0.5f) },
                new HazardBudget { Vents = 2, Skewers = 1, Zones = 1, LowObstacles = 2, TallObstacles = 3, Breakables = 1 });
        }

        private static void Make(int index, ArenaData shell, ArenaLayoutData[] layouts, HazardBudget[] budgets,
                                 ObstaclePlacement[] obstacles, TrapPlacement[] traps, HazardBudget budget)
        {
            var layout = GetOrCreate<ArenaLayoutData>($"{LayoutDir}/Layout_R{index + 1}.asset");
            layout.Configure(shell, Spawn, Gates, obstacles, traps);
            layouts[index] = layout;
            budgets[index] = budget;
        }

        private static void AssignToRounds(ArenaLayoutData[] layouts, HazardBudget[] budgets)
        {
            for (int i = 0; i < layouts.Length; i++)
            {
                var round = AssetDatabase.LoadAssetAtPath<RoundData>($"{RoundDir}/Round_{i + 1}.asset");
                round.SetLayout(layouts[i], budgets[i]);
            }
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            config.SetDefaultLayout(layouts[0]);
            EditorUtility.SetDirty(config);
        }

        private static bool ValidateAll(ArenaLayoutData[] layouts, HazardBudget[] budgets)
        {
            var problems = new List<string>();
            for (int i = 0; i < layouts.Length; i++)
            {
                var found = new List<string>();
                LayoutValidator.Validate(layouts[i], budgets[i], found);
                foreach (string p in found)
                    problems.Add($"Layout_R{i + 1}: {p}");
                if (i > 0 && budgets[i].NewTrapKindsSince(budgets[i - 1]) > 1)
                    problems.Add($"Layout_R{i + 1}: adds more than one new trap kind");
            }
            if (budgets[0].Traps != 0)
                problems.Add("Layout_R1 must have no traps");

            foreach (string p in problems)
                Debug.LogError("M8.5: " + p);
            return problems.Count == 0;
        }

        // ---------------------------------------------------------------- Scene

        private static void SetupGameScene()
        {
            var scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);

            var arena = Object.FindFirstObjectByType<ArenaController>(FindObjectsInactive.Include);
            var fader = arena.GetComponent<TallObstacleFader>();
            if (fader == null)
                fader = arena.gameObject.AddComponent<TallObstacleFader>();
            SetRef(fader, "arena", arena);
            SetRef(fader, "player", Object.FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Include));
            SetRef(fader, "navigation", Object.FindFirstObjectByType<NavigationService>(FindObjectsInactive.Include));
            SetRef(fader, "tuning", AssetDatabase.LoadAssetAtPath<PerspectiveTuning>($"{ArenaDir}/PerspectiveTuning.asset"));

            var picker = Object.FindFirstObjectByType<DebugRoundPicker>(FindObjectsInactive.Include);
            if (picker != null && picker.transform.Find("Preview") == null)
            {
                Button preview = UiBuilder.CreateButton("Preview", picker.transform, "View", 70f);
                preview.gameObject.GetComponent<LayoutElement>().preferredWidth = 150f;
                SetRef(picker, "previewButton", preview);
            }

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

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'), System.IO.Path.GetFileName(path));
        }
    }
}
