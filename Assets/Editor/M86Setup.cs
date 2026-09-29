using System.IO;
using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Feedback;
using BulletHell.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// One-shot M8.6 setup: creates the feedback tuning assets, the character / outline materials, the particle textures
    /// and the seven particle presets, wires the Motion child + feedback components into the Player, Enemy and Arm prefabs,
    /// puts CameraShake on the Game camera and points the GameConfig at the FeedbackTuning. Run "Generate Shaders" first.
    /// Safe to run again (it reuses what exists).
    /// </summary>
    public static class M86Setup
    {
        private const string DataDir = "Assets/Data/Feedback";
        private const string MatDir = "Assets/Art/Materials";
        private const string VfxTexDir = "Assets/Art/Placeholder/Vfx";
        private const string VfxPrefabDir = "Assets/Prefabs/Vfx";
        private const string TuningPath = DataDir + "/FeedbackTuning.asset";
        private const string CharacterMatPath = MatDir + "/Mat_SpriteCharacter.mat";
        private const string OutlineMatPath = MatDir + "/Mat_SpriteOutline.mat";
        private const string PlayerPrefab = "Assets/Prefabs/Player.prefab";
        private const string EnemyPrefab = "Assets/Prefabs/Enemy.prefab";
        private const string ArmPrefab = "Assets/Prefabs/Arm.prefab";
        private const string ConfigPath = "Assets/Resources/GameConfig.asset";
        private const string GamePath = "Assets/Scenes/Game.unity";
        private const string SpriteLitGuid = "a97c105638bdf8b4a8650670310a4cd3";

        [MenuItem("BulletHell/M8.6/Setup Everything")]
        public static void Run()
        {
            EnsureFolder(DataDir);
            EnsureFolder(MatDir);
            EnsureFolder(VfxTexDir);
            EnsureFolder(VfxPrefabDir);

            Material character = CreateMaterial(CharacterMatPath, "Shader Graphs/Sprite_Character");
            Material outline = CreateMaterial(OutlineMatPath, "Shader Graphs/Sprite_Outline");
            if (character == null || outline == null)
            {
                Debug.LogError("M8.6: the shader graphs are missing. Run BulletHell/M8.6/Generate Shaders first.");
                return;
            }

            FeedbackTuning tuning = CreateTunings();
            CreateParticles(tuning);
            SetupPlayerPrefab(character);
            SetupEnemyPrefab(character);
            SetupArmPrefab(outline);
            SetArmMeshTypes();
            SetupGameScene();
            SetupConfig(tuning);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("M8.6 setup complete.");
        }

        // ------------------------------------------------------------------------------------ materials

        private static Material CreateMaterial(string path, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                return null;
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        private static Material SpriteLit() =>
            AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(SpriteLitGuid));

        // ------------------------------------------------------------------------------------ tunings

        private static FeedbackTuning CreateTunings()
        {
            var playerMotion = Ensure<MotionTuning>(DataDir + "/Motion_Player.asset", so =>
            {
                so.FindProperty("breathAmount").floatValue = 0.025f;
                so.FindProperty("hopHeight").floatValue = 0.07f;
                so.FindProperty("leanDegrees").floatValue = 7f;
                so.FindProperty("tiltDegrees").floatValue = 4f;
                so.FindProperty("footDust").boolValue = true;
            });
            var enemyMotion = Ensure<MotionTuning>(DataDir + "/Motion_Enemy.asset", so =>
            {
                so.FindProperty("breathAmount").floatValue = 0.04f;
                so.FindProperty("hopHeight").floatValue = 0.1f;
                so.FindProperty("fullSpeed").floatValue = 3.5f;
                so.FindProperty("hopsPerSecond").floatValue = 2.6f;
                so.FindProperty("tiltDegrees").floatValue = 7f;
                so.FindProperty("leanDegrees").floatValue = 10f;
            });
            var playerHit = Ensure<HitFeedbackTuning>(DataDir + "/Hit_Player.asset", so =>
            {
                so.FindProperty("flashSeconds").floatValue = 0.1f;
                so.FindProperty("knockbackDistance").floatValue = 0.2f;
                so.FindProperty("scalePunch").floatValue = 0.25f;
                so.FindProperty("hitstopSeconds").floatValue = 0.07f;
                so.FindProperty("shake").floatValue = 0.45f;
                so.FindProperty("sparks").intValue = 8;
            });
            var enemyHit = Ensure<HitFeedbackTuning>(DataDir + "/Hit_Enemy.asset", so =>
            {
                so.FindProperty("flashSeconds").floatValue = 0.08f;
                so.FindProperty("knockbackDistance").floatValue = 0.1f;
                so.FindProperty("scalePunch").floatValue = 0.12f;
                so.FindProperty("hitstopSeconds").floatValue = 0f;
                so.FindProperty("shake").floatValue = 0f;
                so.FindProperty("sparks").intValue = 3;
            });
            var lifeCycle = Ensure<LifeCycleTuning>(DataDir + "/LifeCycle_Enemy.asset", null);

            return Ensure<FeedbackTuning>(TuningPath, so =>
            {
                so.FindProperty("playerMotion").objectReferenceValue = playerMotion;
                so.FindProperty("playerHit").objectReferenceValue = playerHit;
                so.FindProperty("enemyMotion").objectReferenceValue = enemyMotion;
                so.FindProperty("enemyHit").objectReferenceValue = enemyHit;
                so.FindProperty("enemyLifeCycle").objectReferenceValue = lifeCycle;
            }, alwaysApply: true);
        }

        private static T Ensure<T>(string path, System.Action<SerializedObject> init, bool alwaysApply = false) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            bool created = asset == null;
            if (created)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            if ((created || alwaysApply) && init != null)
            {
                var so = new SerializedObject(asset);
                init(so);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
            return asset;
        }

        // ------------------------------------------------------------------------------------ particles

        private static Texture2D MakeTexture(string name, int width, int height, System.Func<float, float, float> alphaAt)
        {
            string path = $"{VfxTexDir}/{name}.png";
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width * 2f - 1f;
                    float v = (y + 0.5f) / height * 2f - 1f;
                    float a = Mathf.Clamp01(alphaAt(u, v));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material ParticleMaterial(string name, Texture2D texture)
        {
            string path = $"{MatDir}/Mat_Vfx_{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.mainTexture = texture;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateParticles(FeedbackTuning tuning)
        {
            Texture2D soft = MakeTexture("SoftDot", 64, 64, (u, v) => 1f - Mathf.Sqrt(u * u + v * v));
            Texture2D streak = MakeTexture("SparkStreak", 64, 16, (u, v) => (1f - Mathf.Abs(u)) * (1f - Mathf.Abs(v) * 1.2f));
            Texture2D puff = MakeTexture("SmokePuff", 64, 64, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                float n = Mathf.PerlinNoise(u * 2.2f + 5f, v * 2.2f + 5f);
                return Mathf.SmoothStep(0f, 1f, (1f - d) * 1.6f) * Mathf.Lerp(0.55f, 1f, n);
            });
            Texture2D shard = MakeTexture("Shard", 32, 32, (u, v) => (v > -0.9f && Mathf.Abs(u) < (0.9f - v) * 0.5f) ? 1f : 0f);

            var so = new SerializedObject(tuning);
            AssignParticle(so, "spark", Preset("Vfx_Spark", streak, 0.25f, 0.05f, 0.35f, 5f, 9f, 0f, 0.06f, 0.14f,
                new Color(1f, 0.95f, 0.5f), new Color(1f, 0.55f, 0.15f), 6, alignToVelocity: true));
            AssignParticle(so, "dust", Preset("Vfx_Dust", puff, 0.4f, 0.3f, 0.5f, 0.4f, 1.2f, 0f, 0.15f, 0.3f,
                new Color(0.85f, 0.8f, 0.7f, 0.7f), new Color(0.85f, 0.8f, 0.7f, 0.5f), 6, growth: 1.8f));
            AssignParticle(so, "smoke", Preset("Vfx_Smoke", puff, 0.9f, 0.6f, 1f, 0.2f, 0.7f, -0.05f, 0.25f, 0.45f,
                new Color(0.4f, 0.38f, 0.42f, 0.7f), new Color(0.3f, 0.28f, 0.32f, 0.5f), 6, growth: 2.2f));
            AssignParticle(so, "steam", Preset("Vfx_Steam", soft, 0.8f, 0.5f, 0.9f, 0.5f, 1.1f, -0.15f, 0.15f, 0.3f,
                new Color(1f, 1f, 1f, 0.6f), new Color(0.9f, 0.95f, 1f, 0.4f), 8, growth: 2f));
            AssignParticle(so, "debris", Preset("Vfx_Debris", shard, 0.6f, 0.35f, 0.7f, 2.5f, 5f, 1.4f, 0.08f, 0.18f,
                new Color(1f, 0.6f, 0.75f), new Color(1f, 0.85f, 0.4f), 10, spin: true));
            AssignParticle(so, "coin", Preset("Vfx_Coin", soft, 0.5f, 0.3f, 0.6f, 1.5f, 3f, 1f, 0.08f, 0.16f,
                new Color(1f, 0.85f, 0.2f), new Color(1f, 0.95f, 0.5f), 6));
            AssignParticle(so, "confetti", Preset("Vfx_Confetti", shard, 1.6f, 1.1f, 1.8f, 3f, 7f, 1f, 0.1f, 0.2f,
                new Color(1f, 0.3f, 0.5f), new Color(0.3f, 0.8f, 1f), 40, spin: true, randomColors: true));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tuning);
        }

        private static void AssignParticle(SerializedObject so, string field, ParticleSystem prefab) =>
            so.FindProperty(field).objectReferenceValue = prefab;

        // Builds (or rebuilds) one particle preset prefab. All burst-only, world space, drawn over the characters.
        private static ParticleSystem Preset(string name, Texture2D texture, float duration, float lifeMin, float lifeMax,
                                             float speedMin, float speedMax, float gravity, float sizeMin, float sizeMax,
                                             Color colorA, Color colorB, int burst, bool alignToVelocity = false,
                                             float growth = 1f, bool spin = false, bool randomColors = false)
        {
            string path = $"{VfxPrefabDir}/{name}.prefab";
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.gravityModifier = gravity;
            main.maxParticles = 64;
            main.useUnscaledTime = name == "Vfx_Confetti"; // the round-results screen freezes game time
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            if (randomColors)
            {
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(new Color(1f, 0.3f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0.33f),
                            new GradientColorKey(new Color(0.3f, 0.85f, 0.5f), 0.66f), new GradientColorKey(new Color(0.3f, 0.7f, 1f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
                main.startColor = new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };
            }
            if (spin)
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f;
            shape.arc = 360f;

            var life = ps.colorOverLifetime;
            life.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            life.color = fade;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, growth));

            if (spin)
            {
                var rotation = ps.rotationOverLifetime;
                rotation.enabled = true;
                rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = ParticleMaterial(name.Replace("Vfx_", ""), texture);
            renderer.sortingLayerName = SortingLayers.Bullets;
            renderer.sortingOrder = 6;
            renderer.renderMode = alignToVelocity ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            if (alignToVelocity)
            {
                renderer.lengthScale = 2.2f;
                renderer.velocityScale = 0.04f;
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved.GetComponent<ParticleSystem>();
        }

        // ------------------------------------------------------------------------------------ prefabs

        private static void SetupPlayerPrefab(Material character)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                Transform visuals = root.transform.Find("Visuals");
                Transform motion = visuals.Find("Motion");
                if (motion == null)
                {
                    motion = new GameObject("Motion").transform;
                    motion.SetParent(visuals, false);
                    foreach (string child in new[] { "Body", "Cape", "Headgear" })
                        visuals.Find(child).SetParent(motion, true);
                }

                SpriteRenderer body = motion.Find("Body").GetComponent<SpriteRenderer>();
                SpriteRenderer cape = motion.Find("Cape").GetComponent<SpriteRenderer>();
                SpriteRenderer head = motion.Find("Headgear").GetComponent<SpriteRenderer>();
                foreach (SpriteRenderer r in new[] { body, cape, head })
                    r.sharedMaterial = character;

                var fx = GetOrAdd<SpriteFx>(root);
                Set(fx, so => SetArray(so.FindProperty("renderers"), body, cape, head));

                var pm = GetOrAdd<ProceduralMotion>(root);
                Set(pm, so =>
                {
                    so.FindProperty("motion").objectReferenceValue = motion;
                    so.FindProperty("jump").objectReferenceValue = root.GetComponent<JumpController>();
                });

                var hit = GetOrAdd<HitFeedback>(root);
                Set(hit, so =>
                {
                    so.FindProperty("health").objectReferenceValue = root.GetComponent<Health>();
                    so.FindProperty("motion").objectReferenceValue = pm;
                    so.FindProperty("fx").objectReferenceValue = fx;
                    so.FindProperty("sparkAnchor").objectReferenceValue = root.transform.Find("Core");
                    so.FindProperty("shakesCamera").boolValue = true;
                });
                var binder = GetOrAdd<PlayerFeedbackBinder>(root);
                Set(binder, so =>
                {
                    so.FindProperty("motion").objectReferenceValue = pm;
                    so.FindProperty("hit").objectReferenceValue = hit;
                });
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupEnemyPrefab(Material character)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(EnemyPrefab);
            try
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root); // the retired HitFlash

                Transform rig = root.transform.Find("Rig");
                Transform motion = rig.Find("Motion");
                if (motion == null)
                {
                    motion = new GameObject("Motion").transform;
                    motion.SetParent(rig, false);
                    rig.Find("Body").SetParent(motion, true);
                }
                SpriteRenderer body = motion.Find("Body").GetComponent<SpriteRenderer>();
                body.sharedMaterial = character;

                var fx = GetOrAdd<SpriteFx>(root);
                Set(fx, so => SetArray(so.FindProperty("renderers"), body));

                var pm = GetOrAdd<ProceduralMotion>(root);
                Set(pm, so => so.FindProperty("motion").objectReferenceValue = motion);

                var hit = GetOrAdd<HitFeedback>(root);
                Set(hit, so =>
                {
                    so.FindProperty("health").objectReferenceValue = root.GetComponent<Health>();
                    so.FindProperty("motion").objectReferenceValue = pm;
                    so.FindProperty("fx").objectReferenceValue = fx;
                    so.FindProperty("sparkAnchor").objectReferenceValue = rig;
                    so.FindProperty("shakesCamera").boolValue = false;
                });

                var telegraph = GetOrAdd<TelegraphFx>(root);
                Set(telegraph, so =>
                {
                    so.FindProperty("motion").objectReferenceValue = pm;
                    so.FindProperty("fx").objectReferenceValue = fx;
                });

                var enemy = root.GetComponent<Enemy>();
                Set(enemy, so =>
                {
                    so.FindProperty("hitFeedback").objectReferenceValue = hit;
                    so.FindProperty("motion").objectReferenceValue = pm;
                    so.FindProperty("telegraph").objectReferenceValue = telegraph;
                    so.FindProperty("lineMaterial").objectReferenceValue = SpriteLit();
                });
                PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupArmPrefab(Material outline)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ArmPrefab);
            try
            {
                root.transform.Find("Art").GetComponent<SpriteRenderer>().sharedMaterial = outline;
                Transform halo = root.transform.Find("Halo");
                if (halo != null)
                    halo.GetComponent<SpriteRenderer>().enabled = false;
                PrefabUtility.SaveAsPrefabAsset(root, ArmPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // The outline grows past the opaque pixels, so the arm sprites need a Full Rect mesh (a tight mesh would clip it).
        private static void SetArmMeshTypes()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Arms" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteMeshType == SpriteMeshType.FullRect)
                    continue;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        // ------------------------------------------------------------------------------------ scene and config

        private static void SetupGameScene()
        {
            var scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);
            Camera camera = Camera.main;
            if (camera == null)
            {
                foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    if (c.orthographic)
                        camera = c;
            }
            if (camera != null && camera.GetComponent<CameraShake>() == null)
                camera.gameObject.AddComponent<CameraShake>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetupConfig(FeedbackTuning tuning)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var so = new SerializedObject(config);
            so.FindProperty("feedback").objectReferenceValue = tuning;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        // ------------------------------------------------------------------------------------ helpers

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static void Set(Object target, System.Action<SerializedObject> apply)
        {
            var so = new SerializedObject(target);
            apply(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(SerializedProperty array, params Object[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
