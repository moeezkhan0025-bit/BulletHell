using System;
using TMPro;
using BulletHell.Bosses;
using BulletHell.Enemies;
using BulletHell.Feedback;
using BulletHell.Player;
using BulletHell.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// M10: the round 3 boss, the Pumpking. Imports its painted art, creates its patterns, feedback, jump and BossData,
    /// its EnemyData, rewires round 3 to the boss alone, builds the Boss prefab variant, the BossPool, the boss health bar
    /// and the debug buttons in the Game scene, and regenerates the shaders (glow). Safe to run again.
    /// </summary>
    public static class M10Setup
    {
        private const string ArtPath = "Assets/Art/Bosses/Pumpking/boss_pumpking_idle.png";
        private const string BossDir = "Assets/Data/Bosses";
        private const string EnemyDir = "Assets/Data/Enemies";
        private const string PatternDir = "Assets/Data/Enemies/Patterns";
        private const string FeedbackDir = "Assets/Data/Feedback";
        private const string WaveDir = "Assets/Data/Waves";
        private const string RoundDir = "Assets/Data/Waves/Rounds";
        private const string CombatTuningPath = "Assets/Data/Waves/CombatTuning.asset";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        private const string BossPrefabPath = "Assets/Prefabs/Boss.prefab";
        private const string GamePath = "Assets/Scenes/Game.unity";
        private const string CirclePath = "Assets/Art/Placeholder/Circle.png";
        private const string SquarePath = "Assets/Art/Placeholder/Square.png";

        // Painted characters import at the locked 1.15x scale; the pivot sits at the art's real base (ART_SPEC section 4).
        private const float PixelsPerUnit = 220f / 1.15f;
        private static readonly Vector2 Pivot = new Vector2(1366f / 2560f, 1f - 2016f / 2560f);

        [MenuItem("BulletHell/M10/Setup Everything")]
        public static void Run()
        {
            ImportArt();
            CreatePatterns();
            CreateFeedback();
            BossData boss = CreateBossData();
            EnemyData pumpking = CreateEnemy(boss);
            WireRound3(pumpking);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SetupBossPrefab();
            SetupGameScene();
            EditorApplication.ExecuteMenuItem("BulletHell/M8.6/Generate Shaders");
            AssetDatabase.SaveAssets();
            Debug.Log("M10 setup complete.");
        }

        // ---------------------------------------------------------------- Art

        private static void ImportArt()
        {
            var importer = AssetImporter.GetAtPath(ArtPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Missing {ArtPath}: run Tools/export_art.ps1 Bosses first.");
                return;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 4096;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = Pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        // ---------------------------------------------------------------- Data

        private static void CreatePatterns()
        {
            Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
            Pattern("Pattern_PumpkingRing", AttackShape.Ring, 16, 4f, 0.34f, circle);
            Pattern("Pattern_PumpkingRingDense", AttackShape.Ring, 24, 4.4f, 0.34f, circle);
            Pattern("Pattern_PumpkingFastShot", AttackShape.Aimed, 1, 11f, 0.36f, circle);
        }

        // Boss bullets are all Hot Magenta (BulletStyle.Special). The timers are unused: the boss fires them itself.
        private static void Pattern(string name, AttackShape shape, int count, float speed, float size, Sprite sprite)
        {
            var pattern = GetOrCreate<AttackPattern>($"{PatternDir}/{name}.asset");
            Set(pattern, so =>
            {
                so.FindProperty("shape").enumValueIndex = (int)shape;
                so.FindProperty("bulletCount").intValue = count;
                so.FindProperty("fireInterval").floatValue = 2f;
                so.FindProperty("initialDelay").floatValue = 0.75f;
                so.FindProperty("bulletSpeed").floatValue = speed;
                so.FindProperty("bulletSize").floatValue = size;
                so.FindProperty("damage").floatValue = 1f;
                so.FindProperty("bulletStyle").enumValueIndex = (int)BulletStyle.Special;
                so.FindProperty("bulletSprite").objectReferenceValue = sprite;
                so.FindProperty("bulletLifetime").floatValue = 10f;
            });
        }

        private static void CreateFeedback()
        {
            var motion = GetOrCreate<MotionTuning>($"{FeedbackDir}/Motion_Pumpking.asset");
            Set(motion, so =>
            {
                so.FindProperty("breathAmount").floatValue = 0.03f;
                so.FindProperty("breathSpeed").floatValue = 1.4f;
                so.FindProperty("fullSpeed").floatValue = 2.2f;
                so.FindProperty("moveThreshold").floatValue = 0.2f;
                so.FindProperty("hopHeight").floatValue = 0.16f;
                so.FindProperty("hopsPerSecond").floatValue = 1.4f;
                so.FindProperty("tiltDegrees").floatValue = 3f;
                so.FindProperty("footDust").boolValue = true;
                so.FindProperty("leanDegrees").floatValue = 4f;
                so.FindProperty("leanFollow").floatValue = 8f;
                so.FindProperty("startStretch").floatValue = 0.06f;
                so.FindProperty("stopSquash").floatValue = 0.12f;
                so.FindProperty("springFrequency").floatValue = 2.4f;
                so.FindProperty("springDamping").floatValue = 0.4f;
                so.FindProperty("flipToFace").boolValue = false;
                so.FindProperty("windupInflate").floatValue = 0.22f;
                so.FindProperty("windupTremble").floatValue = 0.06f;
                so.FindProperty("trembleSpeed").floatValue = 40f;
                so.FindProperty("pulseSpeed").floatValue = 20f;
            });

            var hit = GetOrCreate<HitFeedbackTuning>($"{FeedbackDir}/Hit_Pumpking.asset");
            Set(hit, so =>
            {
                so.FindProperty("flashSeconds").floatValue = 0.06f;
                so.FindProperty("knockbackDistance").floatValue = 0.03f;
                so.FindProperty("scalePunch").floatValue = 0.04f;
                so.FindProperty("hitstopSeconds").floatValue = 0f;
                so.FindProperty("shake").floatValue = 0.03f;
                so.FindProperty("sparks").intValue = 3;
            });

            var lifeCycle = GetOrCreate<LifeCycleTuning>($"{FeedbackDir}/LifeCycle_Pumpking.asset");
            Set(lifeCycle, so =>
            {
                so.FindProperty("spawnSeconds").floatValue = 0.6f;
                so.FindProperty("splatParticles").intValue = 30;
            });

            Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
            var jump = GetOrCreate<JumpTuning>($"{BossDir}/Jump_Pumpking.asset");
            Set(jump, so =>
            {
                so.FindProperty("airtime").floatValue = 0.9f;
                so.FindProperty("maxHeight").floatValue = 2.4f;
                so.FindProperty("cooldownAfterLanding").floatValue = 0f;
                so.FindProperty("apexScale").floatValue = 1f;
                so.FindProperty("takeoffSquash").vector2Value = new Vector2(1.25f, 0.8f);
                so.FindProperty("takeoffSquashSeconds").floatValue = 0.12f;
                so.FindProperty("landingSquash").vector2Value = new Vector2(1.4f, 0.65f);
                so.FindProperty("landingSquashSeconds").floatValue = 0.18f;
                so.FindProperty("shadowScaleAtApex").floatValue = 0.5f;
                so.FindProperty("shadowAlphaAtApex").floatValue = 0.35f;
                so.FindProperty("airborneSortHeight").floatValue = 0.08f;
                so.FindProperty("dustSprite").objectReferenceValue = circle;
                so.FindProperty("dustCount").intValue = 12;
                so.FindProperty("dustSeconds").floatValue = 0.5f;
                so.FindProperty("dustStartSize").floatValue = 0.25f;
                so.FindProperty("dustEndSize").floatValue = 0.8f;
                so.FindProperty("dustSpread").floatValue = 1.4f;
            });
        }

        private static BossData CreateBossData()
        {
            AttackPattern ring = Load<AttackPattern>($"{PatternDir}/Pattern_PumpkingRing.asset");
            AttackPattern dense = Load<AttackPattern>($"{PatternDir}/Pattern_PumpkingRingDense.asset");
            AttackPattern fast = Load<AttackPattern>($"{PatternDir}/Pattern_PumpkingFastShot.asset");
            var jump = Load<JumpTuning>($"{BossDir}/Jump_Pumpking.asset");
            Sprite ringSprite = M75Art.Load("Ring");

            var phase1 = new BossPhase
            {
                EnterBelowHp01 = 1f,
                MoveSpeedMultiplier = 1f,
                GlowAmount = 0f,
                Attacks = new[]
                {
                    Attack(BossAttackKind.CircleSpread, ring, 0.9f, 2, 0.45f, 11.25f, 1.6f, 3f),
                    Attack(BossAttackKind.FastShot, fast, 1f, 1, 0.18f, 0f, 1.4f, 2f),
                    Attack(BossAttackKind.JumpSmash, ring, 0.8f, 1, 0f, 0f, 2.2f, 2f),
                },
            };
            var phase2 = new BossPhase
            {
                EnterBelowHp01 = 0.5f,
                MoveSpeedMultiplier = 1.35f,
                GlowColor = new Color(1.2f, 0.6f, 0.12f),
                GlowAmount = 0.15f,   // additive over the whole body: keep it a warm glow, not a wash
                GlowPulseSpeed = 6f,
                Attacks = new[]
                {
                    Attack(BossAttackKind.CircleSpread, dense, 0.75f, 3, 0.4f, 5f, 1.3f, 3f),
                    Attack(BossAttackKind.FastShot, fast, 0.9f, 3, 0.18f, 0f, 1.4f, 2f),
                    Attack(BossAttackKind.JumpSmash, dense, 0.7f, 2, 0f, 0f, 2f, 2f),
                },
            };
            var smash = new SmashSettings
            {
                Radius = 1.9f, DamageToPlayer = 1f, DamageToEnemies = 6f,
                TelegraphColor = new Color(1f, 0.302f, 0.2f), RingSprite = ringSprite,
                PostSpreads = 3, PostSpreadInterval = 0.25f, PostSpreadAngleStepDeg = 7.5f,
                SecondJumpWindup = 0.35f, CrouchSquash = 0.3f, Shake = 0.5f, HitstopSeconds = 0.06f,
                DustCount = 14, DebrisCount = 8, MaxJumpDistance = 7f, HittableInAir = true,
            };
            var transition = new TransitionSettings { Seconds = 1.6f, Invulnerable = true, RoarInflate = 0.35f, FlashCycles = 3, Shake = 0.7f, HitstopSeconds = 0.1f };
            var death = new DeathSettings
            {
                StaggerSeconds = 0.9f, SquashSeconds = 0.3f, DissolveSeconds = 0.9f, FlashCycles = 5,
                DebrisBursts = 6, DebrisPerBurst = 8, CoinBurst = 24, ShakePerStage = 0.35f, HitstopSeconds = 0.12f,
            };

            var boss = GetOrCreate<BossData>($"{BossDir}/Boss_Pumpking.asset");
            boss.EditorConfigure("Pumpking", 420f, 1f, new Vector2(3f, 5f), new[] { phase1, phase2 }, jump, smash, transition, death);
            return boss;
        }

        private static BossAttack Attack(BossAttackKind kind, AttackPattern pattern, float windup, int volleys, float interval, float angleStep, float cooldown, float weight) =>
            new BossAttack { Kind = kind, Pattern = pattern, WindupSeconds = windup, Volleys = volleys, VolleyInterval = interval, AngleOffsetStepDeg = angleStep, Cooldown = cooldown, Weight = weight };

        private static EnemyData CreateEnemy(BossData boss)
        {
            var enemy = GetOrCreate<EnemyData>($"{EnemyDir}/Enemy_Pumpking.asset");
            Set(enemy, so =>
            {
                so.FindProperty("displayName").stringValue = "Pumpking";
                so.FindProperty("color").colorValue = new Color(0.95f, 0.55f, 0.15f);
                so.FindProperty("size").floatValue = 2.76f;
                so.FindProperty("paintedSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
                so.FindProperty("paintedHeight").floatValue = 3.55f;
                so.FindProperty("paintedFootprintRadius").floatValue = 0.7f;
                so.FindProperty("paintedHurtboxSize").vector2Value = new Vector2(3f, 2.6f);
                so.FindProperty("paintedHurtboxOffsetX").floatValue = 0f;
                so.FindProperty("maxHealth").floatValue = 420f;
                so.FindProperty("hitFlashDuration").floatValue = 0.06f;
                so.FindProperty("motionOverride").objectReferenceValue = Load<MotionTuning>($"{FeedbackDir}/Motion_Pumpking.asset");
                so.FindProperty("hitOverride").objectReferenceValue = Load<HitFeedbackTuning>($"{FeedbackDir}/Hit_Pumpking.asset");
                so.FindProperty("lifeCycleOverride").objectReferenceValue = Load<LifeCycleTuning>($"{FeedbackDir}/LifeCycle_Pumpking.asset");
                so.FindProperty("coinValue").intValue = 120;
                so.FindProperty("attacks").arraySize = 0;
                so.FindProperty("behavior").enumValueIndex = (int)EnemyBehavior.Boss;
                so.FindProperty("moveSpeed").floatValue = 1.6f;
                so.FindProperty("acceleration").floatValue = 6f;
                so.FindProperty("brake").floatValue = 10f;
                so.FindProperty("turnRate").floatValue = 180f;
                so.FindProperty("boss").objectReferenceValue = boss;
            });
            return enemy;
        }

        // Round 3 is the Pumpking alone (its layout, hazard budget and boss flag stay as M8.5 set them).
        private static void WireRound3(EnemyData pumpking)
        {
            var wave = GetOrCreate<WaveData>($"{WaveDir}/Wave_R3_1.asset");
            wave.Set(new SpawnGroup { Enemy = pumpking, Count = 1, Pattern = SpawnPattern.Row, Delay = 0f, Interval = 0f });
            var round = GetOrCreate<RoundData>($"{RoundDir}/Round_3.asset");
            round.Set(new[] { wave }, true, round.WaveBreatherSeconds);
        }

        // ---------------------------------------------------------------- Prefab

        // Boss.prefab is a VARIANT of Enemy.prefab: Enemy changes keep flowing to it; it adds the boss layer and hides the world health bar.
        private static void SetupBossPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath) == null)
            {
                var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
                var preview = EditorSceneManager.NewPreviewScene();
                try
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, preview);
                    instance.name = "Boss";
                    PrefabUtility.SaveAsPrefabAsset(instance, BossPrefabPath);
                }
                finally
                {
                    EditorSceneManager.ClosePreviewScene(preview);
                }
            }

            GameObject root = PrefabUtility.LoadPrefabContents(BossPrefabPath);
            try
            {
                Transform rig = root.transform.Find("Rig");
                Transform shadow = root.transform.Find("Shadow");
                var jump = GetOrAdd<BossJump>(root);
                Set(jump, so =>
                {
                    so.FindProperty("rig").objectReferenceValue = rig;
                    so.FindProperty("shadow").objectReferenceValue = shadow != null ? shadow.GetComponent<SpriteRenderer>() : null;
                    so.FindProperty("sortingGroup").objectReferenceValue = root.GetComponent<SortingGroup>();
                    so.FindProperty("hitbox").objectReferenceValue = root.GetComponent<CapsuleCollider2D>();
                    so.FindProperty("motion").objectReferenceValue = root.GetComponent<ProceduralMotion>();
                });
                var controller = GetOrAdd<BossController>(root);
                Set(controller, so =>
                {
                    so.FindProperty("health").objectReferenceValue = root.GetComponent<Core.Health>();
                    so.FindProperty("enemy").objectReferenceValue = root.GetComponent<Enemy>();
                    so.FindProperty("fx").objectReferenceValue = root.GetComponent<SpriteFx>();
                    so.FindProperty("motion").objectReferenceValue = root.GetComponent<ProceduralMotion>();
                    so.FindProperty("telegraph").objectReferenceValue = root.GetComponent<TelegraphFx>();
                    so.FindProperty("jump").objectReferenceValue = jump;
                    so.FindProperty("hitbox").objectReferenceValue = root.GetComponent<CapsuleCollider2D>();
                });
                Transform bar = rig != null ? rig.Find("HealthBar") : null;
                if (bar != null)
                    bar.gameObject.SetActive(false);   // the screen-space boss bar replaces it
                PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------------------------------------------------------------- Scene

        private static void SetupGameScene()
        {
            var scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);

            // The boss pool: a second EnemyPool next to the main one, holding the variant.
            EnemyPool mainPool = null;
            EnemyPool bossPool = null;
            foreach (EnemyPool pool in Object.FindObjectsByType<EnemyPool>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (pool.name == "BossPool")
                    bossPool = pool;
                else
                    mainPool = pool;
            }
            if (bossPool == null)
            {
                var go = new GameObject("BossPool");
                go.transform.SetParent(mainPool != null ? mainPool.transform.parent : null, false);
                if (mainPool != null)
                    go.transform.SetSiblingIndex(mainPool.transform.GetSiblingIndex() + 1);
                bossPool = go.AddComponent<EnemyPool>();
            }
            Set(bossPool, so =>
            {
                so.FindProperty("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath).GetComponent<Enemy>();
                so.FindProperty("tuning").objectReferenceValue = Load<CombatTuning>(CombatTuningPath);
                so.FindProperty("bossPool").boolValue = true;
            });

            var spawner = Object.FindFirstObjectByType<WaveSpawner>(FindObjectsInactive.Include);
            SetRef(spawner, "bossPool", bossPool);

            BuildBossHud();
            AddDebugButtons();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildBossHud()
        {
            Transform safeArea = GameObject.Find("FlowUI").transform.Find("SafeArea");
            Transform existing = safeArea.Find("BossHud");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            Sprite roundRect = M75Art.Load("UIRoundRect");
            Sprite roundOutline = M75Art.Load("UIRoundRectOutline");
            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
            var gold = new Color(0.95f, 0.78f, 0.3f);

            RectTransform hud = UiBuilder.CreateRect("BossHud", safeArea);
            hud.SetSiblingIndex(1);   // over the combat HUD, under the banners and panels
            hud.anchorMin = hud.anchorMax = hud.pivot = new Vector2(0.5f, 1f);
            hud.anchoredPosition = new Vector2(0f, -28f);
            hud.sizeDelta = new Vector2(820f, 96f);
            var group = hud.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            TMP_Text name = UiBuilder.CreateText("NamePlate", hud, "PUMPKING", 34, TextAnchor.MiddleCenter);
            name.fontStyle = FontStyles.Bold;
            name.color = gold;
            var outline = name.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            var nameRect = (RectTransform)name.transform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = Vector2.zero;
            nameRect.sizeDelta = new Vector2(0f, 44f);

            RectTransform bar = UiBuilder.CreateRect("Bar", hud);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0f, 6f);
            bar.sizeDelta = new Vector2(0f, 36f);

            Pic(bar, "Back", roundRect, new Color(0.08f, 0.07f, 0.1f, 0.9f), Vector2.zero, Vector2.zero, Image.Type.Sliced);
            Image trail = Pic(bar, "Trail", square, new Color(1f, 1f, 1f, 0.35f), new Vector2(5f, 5f), new Vector2(-5f, -5f), Image.Type.Filled);
            trail.fillMethod = Image.FillMethod.Horizontal;
            trail.fillOrigin = 0;
            Image fill = Pic(bar, "Fill", square, new Color(0.96f, 0.45f, 0.12f), new Vector2(5f, 5f), new Vector2(-5f, -5f), Image.Type.Filled);
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            RectTransform tick = UiBuilder.CreateRect("PhaseTick", bar);
            tick.anchorMin = new Vector2(0.5f, 0f);
            tick.anchorMax = new Vector2(0.5f, 1f);
            tick.pivot = new Vector2(0.5f, 0.5f);
            tick.anchoredPosition = Vector2.zero;
            tick.sizeDelta = new Vector2(5f, 0f);
            var tickImage = tick.gameObject.AddComponent<Image>();
            tickImage.sprite = square;
            tickImage.color = gold;
            tickImage.raycastTarget = false;
            Pic(bar, "Frame", roundOutline, gold, Vector2.zero, Vector2.zero, Image.Type.Sliced);

            var bossBar = hud.gameObject.AddComponent<BossHealthBar>();
            Set(bossBar, so =>
            {
                so.FindProperty("group").objectReferenceValue = group;
                so.FindProperty("namePlate").objectReferenceValue = name;
                so.FindProperty("fill").objectReferenceValue = fill;
                so.FindProperty("trail").objectReferenceValue = trail;
                so.FindProperty("phaseTick").objectReferenceValue = tick;
            });
        }

        // Pause screen debug row: Boss (skip to the boss round), HP- / HP+ (the living boss's health).
        private static void AddDebugButtons()
        {
            var picker = Object.FindFirstObjectByType<DebugRoundPicker>(FindObjectsInactive.Include);
            if (picker == null)
            {
                Debug.LogWarning("No DebugRoundPicker in the Game scene (run M5b setup): the boss debug buttons were not added.");
                return;
            }
            Transform row = picker.transform;
            Button boss = row.Find("Boss") != null ? row.Find("Boss").GetComponent<Button>() : UiBuilder.CreateButton("Boss", row, "Boss", 70f);
            Button down = row.Find("HpDown") != null ? row.Find("HpDown").GetComponent<Button>() : UiBuilder.CreateButton("HpDown", row, "HP-", 70f);
            Button up = row.Find("HpUp") != null ? row.Find("HpUp").GetComponent<Button>() : UiBuilder.CreateButton("HpUp", row, "HP+", 70f);
            boss.GetComponent<LayoutElement>().preferredWidth = 120f;
            down.GetComponent<LayoutElement>().preferredWidth = 100f;
            up.GetComponent<LayoutElement>().preferredWidth = 100f;
            SetRef(picker, "bossButton", boss);
            SetRef(picker, "healthDownButton", down);
            SetRef(picker, "healthUpButton", up);
        }

        // ---------------------------------------------------------------- Helpers

        private static Image Pic(Transform parent, string name, Sprite sprite, Color color, Vector2 offsetMin, Vector2 offsetMax, Image.Type type)
        {
            RectTransform rect = UiBuilder.CreateRect(name, parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = type;
            image.raycastTarget = false;
            return image;
        }

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static void Set(Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetRef(Object target, string field, Object value)
        {
            if (target == null)
                return;
            Set(target, so => so.FindProperty(field).objectReferenceValue = value);
        }
    }
}
