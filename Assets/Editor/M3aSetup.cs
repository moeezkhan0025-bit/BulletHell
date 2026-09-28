using System;
using BulletHell.Input;
using BulletHell.Pickups;
using BulletHell.Player;
using BulletHell.Projectiles;
using BulletHell.UI;
using BulletHell.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// One-shot M3a setup: creates/updates the ammo and pickup-tuning assets, the pickup prefab, adds the new
    /// components to the Player prefab, and wires the Game scene (fire controller, overlay, test pickups).
    /// Safe to run again: it updates existing assets and only spawns test pickups that are missing.
    /// </summary>
    public static class M3aSetup
    {
        private const string AmmoDir = "Assets/Data/Ammo";
        private const string PickupDir = "Assets/Data/Pickups";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string PickupPrefabPath = "Assets/Prefabs/AmmoPickup.prefab";
        private const string ScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("BulletHell/M3a/Create Ammo Assets And Wire Scene")]
        public static void Run()
        {
            Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Circle.png");
            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Square.png");

            AmmoTypeData basic = CreateAmmo("Ammo_Basic", "Basic", keepSprite: true, circle, Color.white, so =>
            {
                SetFloat(so, "coolPerSecond", 0.35f);
            });
            AmmoTypeData shotgun = CreateAmmo("Ammo_Shotgun", "Shotgun", false, circle, new Color(1f, 0.6f, 0.2f), so =>
            {
                SetInt(so, "extraProjectiles", 5);
                SetFloat(so, "addedSpread", 30f);
                SetFloat(so, "damageMultiplier", 0.5f);
                SetFloat(so, "fireRateMultiplier", 0.5f);
                SetFloat(so, "projectileSpeedMultiplier", 0.9f);
                SetFloat(so, "projectileSizeMultiplier", 0.7f);
            });
            AmmoTypeData laser = CreateAmmo("Ammo_Laser", "Laser", false, circle, new Color(0.3f, 0.9f, 1f), so =>
            {
                SetEnum(so, "behavior", (int)AmmoBehavior.Beam);
                SetFloat(so, "damageMultiplier", 0.6f);
                SetFloat(so, "beamRange", 30f);
                SetFloat(so, "beamWidth", 0.12f);
                SetBool(so, "usesHeat", true);
                SetFloat(so, "heatPerSecond", 0.35f);
                SetFloat(so, "coolPerSecond", 0.3f);
                SetFloat(so, "restartThreshold", 0.3f);
            });
            AmmoTypeData gatling = CreateAmmo("Ammo_Gatling", "Gatling", false, circle, new Color(1f, 0.9f, 0.3f), so =>
            {
                SetFloat(so, "damageMultiplier", 0.5f);
                SetFloat(so, "projectileSizeMultiplier", 0.6f);
                SetFloat(so, "addedSpread", 4f);
                SetBool(so, "usesHeat", true);
                SetFloat(so, "heatPerShot", 0.02f);
                SetFloat(so, "coolPerSecond", 0.25f);
                SetFloat(so, "restartThreshold", 0.3f);
                SetFloat(so, "spinUpTime", 1.5f);
                SetFloat(so, "spinDownTime", 1f);
                SetFloat(so, "minRateMultiplier", 0.3f);
                SetFloat(so, "maxRateMultiplier", 2.5f);
            });
            // Test-only 5th type: with 4 slots and 4 starter types you could never test hold-to-replace otherwise.
            AmmoTypeData spare = CreateAmmo("Ammo_TestSpare", "Spare", false, square, new Color(1f, 0.3f, 0.8f), so => { });

            PickupTuning tuning = CreateAsset<PickupTuning>($"{PickupDir}/PickupTuning.asset");
            AmmoPickup pickupPrefab = CreatePickupPrefab(tuning);

            SetupPlayerPrefab(basic, tuning, pickupPrefab);
            SetupScene(tuning, pickupPrefab, shotgun, laser, gatling, basic, spare);

            AssetDatabase.SaveAssets();
            Debug.Log("M3a setup complete.");
        }

        private static AmmoTypeData CreateAmmo(string assetName, string displayName, bool keepSprite, Sprite sprite,
                                               Color tint, Action<SerializedObject> configure)
        {
            var ammo = CreateAsset<AmmoTypeData>($"{AmmoDir}/{assetName}.asset");
            var so = new SerializedObject(ammo);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("tint").colorValue = tint;
            if (!keepSprite || so.FindProperty("projectileSprite").objectReferenceValue == null)
                so.FindProperty("projectileSprite").objectReferenceValue = sprite;
            configure(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ammo);
            return ammo;
        }

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static AmmoPickup CreatePickupPrefab(PickupTuning tuning)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AmmoPickup>(PickupPrefabPath);
            if (existing != null)
                return existing;

            var go = new GameObject("AmmoPickup");
            go.AddComponent<SpriteRenderer>();
            var pickup = go.AddComponent<AmmoPickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("tuning").objectReferenceValue = tuning;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(go, PickupPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<AmmoPickup>(PickupPrefabPath);
        }

        private static void SetupPlayerPrefab(AmmoTypeData startingAmmo, PickupTuning tuning, AmmoPickup pickupPrefab)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var reader = root.GetComponentInChildren<GameplayInputReader>();
                var slots = root.GetComponent<AmmoSlots>() ?? root.AddComponent<AmmoSlots>();
                var collector = root.GetComponent<AmmoPickupCollector>() ?? root.AddComponent<AmmoPickupCollector>();

                var slotsSo = new SerializedObject(slots);
                slotsSo.FindProperty("input").objectReferenceValue = reader;
                slotsSo.FindProperty("startingAmmo").objectReferenceValue = startingAmmo;
                slotsSo.ApplyModifiedPropertiesWithoutUndo();

                var collectorSo = new SerializedObject(collector);
                collectorSo.FindProperty("input").objectReferenceValue = reader;
                collectorSo.FindProperty("slots").objectReferenceValue = slots;
                collectorSo.FindProperty("tuning").objectReferenceValue = tuning;
                collectorSo.FindProperty("pickupPrefab").objectReferenceValue = pickupPrefab;
                collectorSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupScene(PickupTuning tuning, AmmoPickup pickupPrefab, AmmoTypeData shotgun,
                                       AmmoTypeData laser, AmmoTypeData gatling, AmmoTypeData basic, AmmoTypeData spare)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var fire = UnityEngine.Object.FindFirstObjectByType<ArmFireController>();
            var slots = UnityEngine.Object.FindFirstObjectByType<AmmoSlots>();
            var collector = UnityEngine.Object.FindFirstObjectByType<AmmoPickupCollector>();
            var overlay = UnityEngine.Object.FindFirstObjectByType<DebugOverlay>();

            var fireSo = new SerializedObject(fire);
            fireSo.FindProperty("ammoSlots").objectReferenceValue = slots;
            fireSo.FindProperty("beamMaterial").objectReferenceValue = FindBeamMaterial();
            fireSo.ApplyModifiedPropertiesWithoutUndo();

            var overlaySo = new SerializedObject(overlay);
            overlaySo.FindProperty("ammoSlots").objectReferenceValue = slots;
            overlaySo.FindProperty("fireController").objectReferenceValue = fire;
            overlaySo.FindProperty("pickupCollector").objectReferenceValue = collector;
            overlaySo.ApplyModifiedPropertiesWithoutUndo();

            GameObject holder = GameObject.Find("TestPickups") ?? new GameObject("TestPickups");
            PlacePickup(holder.transform, pickupPrefab, tuning, shotgun, new Vector2(-4f, -2.5f));
            PlacePickup(holder.transform, pickupPrefab, tuning, laser, new Vector2(0f, -3f));
            PlacePickup(holder.transform, pickupPrefab, tuning, gatling, new Vector2(4f, -2.5f));
            PlacePickup(holder.transform, pickupPrefab, tuning, basic, new Vector2(-2f, 3f));
            PlacePickup(holder.transform, pickupPrefab, tuning, spare, new Vector2(2f, 3f));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void PlacePickup(Transform parent, AmmoPickup prefab, PickupTuning tuning, AmmoTypeData ammo, Vector2 position)
        {
            string objectName = $"Pickup_{ammo.name.Replace("Ammo_", "")}";
            if (parent.Find(objectName) != null)
                return;

            var instance = (AmmoPickup)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = objectName;
            instance.transform.position = position;
            var so = new SerializedObject(instance);
            so.FindProperty("ammo").objectReferenceValue = ammo;
            so.FindProperty("tuning").objectReferenceValue = tuning;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Show the ammo's look in the editor too.
            var renderer = instance.GetComponent<SpriteRenderer>();
            renderer.sprite = ammo.ProjectileSprite;
            renderer.color = ammo.Tint;
            instance.transform.localScale = Vector3.one * tuning.VisualSize;
        }

        private static Material FindBeamMaterial()
        {
            var urp = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            return urp != null ? urp : AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        }

        private static void SetFloat(SerializedObject so, string name, float value) => so.FindProperty(name).floatValue = value;
        private static void SetInt(SerializedObject so, string name, int value) => so.FindProperty(name).intValue = value;
        private static void SetBool(SerializedObject so, string name, bool value) => so.FindProperty(name).boolValue = value;
        private static void SetEnum(SerializedObject so, string name, int value) => so.FindProperty(name).enumValueIndex = value;
    }
}
