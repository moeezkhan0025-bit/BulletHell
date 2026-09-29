using BulletHell.Core;
using BulletHell.Cosmetics;
using BulletHell.Player;
using BulletHell.Projectiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// One-shot M7.7 setup: the ArmRingTuning asset, the Player prefab's ArmRing anchor (at the feet, outside the lifted
    /// Visuals) and its damage Core + marker (on the ground plane, low), the lifted Body + ground Shadow children of
    /// the Projectile prefab, and the customization preview's arms on the ellipse. Safe to run again.
    /// </summary>
    public static class M77Setup
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string ProjectilePrefabPath = "Assets/Prefabs/Projectile.prefab";
        private const string TuningPath = "Assets/Data/Player/ArmRingTuning.asset";
        private const string CirclePath = "Assets/Art/Placeholder/Circle.png";
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

        [MenuItem("BulletHell/M7.7/Setup Everything")]
        public static void Run()
        {
            ArmRingTuning tuning = GetOrCreateTuning();
            SetupPlayerPrefab(tuning);
            SetupProjectilePrefab();
            SetupMenuPreview();
            AssetDatabase.SaveAssets();
            Debug.Log("M7.7 setup complete.");
        }

        /// <summary>Places one arm of a static preview (root origin = body centre) on the arm ellipse around the feet.</summary>
        public static void PlacePreviewArm(Transform arm, SpriteRenderer renderer, float compass, float artRotation, float bodyCenterHeight)
        {
            ArmRingTuning ring = AssetDatabase.LoadAssetAtPath<ArmRingTuning>(TuningPath);
            if (ring == null)
                ring = ArmRingTuning.Fallback;

            Vector2 position = ring.PositionAt(compass);
            position.y -= bodyCenterHeight;
            arm.localPosition = position;
            arm.localRotation = Quaternion.Euler(0f, 0f, 90f - compass + artRotation);
            arm.localScale = Vector3.one * ring.ScaleAt(ArmRingMath.Depth01(compass));
            renderer.sortingOrder = ArmRingMath.IsBack(compass, ring.BackDeadzone) ? ring.BackArtOrder : ring.FrontArtOrder;
        }

        private static ArmRingTuning GetOrCreateTuning()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<ArmRingTuning>(TuningPath);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<ArmRingTuning>();
                AssetDatabase.CreateAsset(tuning, TuningPath);
            }
            return tuning;
        }

        // ---------------------------------------------------------------- Player prefab

        private static void SetupPlayerPrefab(ArmRingTuning tuning)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Transform visuals = root.transform.Find("Visuals");

                // The arm ring is anchored at the feet: not under Visuals, which sits at body height and squashes.
                Transform armRing = root.transform.Find("ArmRing");
                if (armRing == null)
                {
                    armRing = visuals.Find("Arms");
                    armRing.name = "ArmRing";
                    armRing.SetParent(root.transform, false);
                }
                armRing.localPosition = Vector3.zero;
                armRing.localScale = Vector3.one;

                // The damage core is on the ground plane near the feet, where bullets really hit.
                Transform core = root.transform.Find("Core");
                if (core == null)
                {
                    core = visuals.Find("Core");
                    core.SetParent(root.transform, false);
                }

                SpriteRenderer marker = EnsureMarker(core);

                var rigSo = new SerializedObject(root.GetComponent<PlayerVisualRig>());
                rigSo.FindProperty("core").objectReferenceValue = core;
                rigSo.FindProperty("coreMarker").objectReferenceValue = marker;
                rigSo.ApplyModifiedPropertiesWithoutUndo();

                var healthSo = new SerializedObject(root.GetComponent<PlayerHealth>());
                healthSo.FindProperty("core").objectReferenceValue = core;
                healthSo.ApplyModifiedPropertiesWithoutUndo();

                var armsSo = new SerializedObject(root.GetComponent<ArmSelectionController>());
                armsSo.FindProperty("ring").objectReferenceValue = tuning;
                armsSo.FindProperty("jump").objectReferenceValue = root.GetComponent<JumpController>();
                armsSo.FindProperty("armParent").objectReferenceValue = armRing;
                armsSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // A bright dot with a dark outline, above the body, so the hitbox reads against any cosmetics.
        private static SpriteRenderer EnsureMarker(Transform core)
        {
            Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
            Transform markerT = core.Find("Marker");
            if (markerT == null)
            {
                markerT = new GameObject("Marker").transform;
                markerT.SetParent(core, false);
            }
            SpriteRenderer marker = GetOrAdd<SpriteRenderer>(markerT.gameObject);
            marker.sprite = circle;
            marker.color = new Color(1f, 0.95f, 0.45f, 1f);
            marker.sortingLayerID = 0;
            marker.sortingOrder = 10;
            marker.spriteSortPoint = SpriteSortPoint.Pivot;

            Transform outlineT = markerT.Find("Outline");
            if (outlineT == null)
            {
                outlineT = new GameObject("Outline").transform;
                outlineT.SetParent(markerT, false);
            }
            outlineT.localScale = Vector3.one * 1.5f;
            SpriteRenderer outline = GetOrAdd<SpriteRenderer>(outlineT.gameObject);
            outline.sprite = circle;
            outline.color = new Color(0.1f, 0.05f, 0.15f, 0.9f);
            outline.sortingLayerID = 0;
            outline.sortingOrder = 9;
            outline.spriteSortPoint = SpriteSortPoint.Pivot;
            return marker;
        }

        // ---------------------------------------------------------------- Projectile prefab

        // Root = the ground point (collision). Body = the sprite lifted above it; Shadow = flat and dark on the ground.
        private static void SetupProjectilePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ProjectilePrefabPath);
            try
            {
                Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
                int layerId = SortingLayers.Id(SortingLayers.Bullets);

                var rootRenderer = root.GetComponent<SpriteRenderer>();
                Material material = rootRenderer != null ? rootRenderer.sharedMaterial : null;
                Sprite sprite = rootRenderer != null && rootRenderer.sprite != null ? rootRenderer.sprite : circle;

                Transform bodyT = root.transform.Find("Body");
                if (bodyT == null)
                {
                    bodyT = new GameObject("Body").transform;
                    bodyT.SetParent(root.transform, false);
                }
                SpriteRenderer body = GetOrAdd<SpriteRenderer>(bodyT.gameObject);
                body.sprite = sprite;
                if (material != null)
                    body.sharedMaterial = material;
                body.sortingLayerID = layerId;
                body.sortingOrder = 5;
                body.spriteSortPoint = SpriteSortPoint.Pivot;

                Transform shadowT = root.transform.Find("Shadow");
                if (shadowT == null)
                {
                    shadowT = new GameObject("Shadow").transform;
                    shadowT.SetParent(root.transform, false);
                }
                SpriteRenderer shadow = GetOrAdd<SpriteRenderer>(shadowT.gameObject);
                shadow.sprite = circle;
                if (material != null)
                    shadow.sharedMaterial = material;
                shadow.color = new Color(0f, 0f, 0f, 0.3f);
                shadow.sortingLayerID = layerId;
                shadow.sortingOrder = 4;
                shadow.spriteSortPoint = SpriteSortPoint.Pivot;

                if (rootRenderer != null)
                    Object.DestroyImmediate(rootRenderer);

                var so = new SerializedObject(root.GetComponent<Projectile>());
                so.FindProperty("body").objectReferenceValue = body;
                so.FindProperty("shadow").objectReferenceValue = shadow;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------------------------------------------------------------- Customization preview (Main Menu scene)

        private static void SetupMenuPreview()
        {
            var scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            var data = AssetDatabase.LoadAssetAtPath<PlayerData>("Assets/Data/Player/PlayerData.asset");
            float bodyCenterHeight = data != null ? data.BodyCenterHeight : 0.38f;

            foreach (GladiatorCosmetics cosmetics in Object.FindObjectsByType<GladiatorCosmetics>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                for (int i = 0; i < 4; i++)
                {
                    Transform arm = cosmetics.transform.Find("Arm" + i);
                    if (arm == null)
                        continue;

                    float compass = 45f + i * 90f;
                    float artRotation = Mathf.DeltaAngle(90f - compass, arm.localEulerAngles.z);
                    PlacePreviewArm(arm, arm.GetComponent<SpriteRenderer>(), compass, artRotation, bodyCenterHeight);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }
    }
}
