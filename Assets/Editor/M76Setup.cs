using System.Collections.Generic;
using BulletHell.Arena;
using BulletHell.Core;
using BulletHell.Input;
using BulletHell.Player;
using BulletHell.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// One-shot M7.6 setup: the "Player" / "PlayerAirborne" physics layers (the airborne one does not collide with
    /// enemies), the "Airborne" sorting layer between the characters and the bullets, the JumpTuning asset, the
    /// JumpController and contact trigger on the Player prefab, and the jump line in the debug overlay.
    /// Safe to run again.
    /// </summary>
    public static class M76Setup
    {
        private const string GamePath = "Assets/Scenes/Game.unity";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string PlayerDataPath = "Assets/Data/Player/PlayerData.asset";
        private const string TuningPath = "Assets/Data/Player/JumpTuning.asset";
        private const int PlayerLayerIndex = 8;
        private const int AirborneLayerIndex = 9;
        private const int EnemyLayerIndex = 6;

        [MenuItem("BulletHell/M7.6/Setup Everything")]
        public static void Run()
        {
            EnsurePhysicsLayers();
            EnsureAirborneSortingLayer();
            JumpTuning tuning = GetOrCreateTuning();
            SetupPlayerPrefab(tuning);
            SetupGameScene();
            AssetDatabase.SaveAssets();
            Debug.Log("M7.6 setup complete.");
        }

        // ---------------------------------------------------------------- Layers

        private static void EnsurePhysicsLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            SetLayer(layers, PlayerLayerIndex, "Player");
            SetLayer(layers, AirborneLayerIndex, "PlayerAirborne");
            tagManager.ApplyModifiedPropertiesWithoutUndo();

            // A jumping player passes over enemy bodies (and, later, charger dashes): no contact with the Enemy layer.
            Physics2D.IgnoreLayerCollision(AirborneLayerIndex, EnemyLayerIndex, true);
            Physics2D.IgnoreLayerCollision(PlayerLayerIndex, PlayerLayerIndex, true);
            Physics2D.IgnoreLayerCollision(PlayerLayerIndex, AirborneLayerIndex, true);
            Physics2D.IgnoreLayerCollision(AirborneLayerIndex, AirborneLayerIndex, true);
        }

        private static void SetLayer(SerializedProperty layers, int index, string layerName)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(index);
            if (layer.stringValue == layerName)
                return;
            if (!string.IsNullOrEmpty(layer.stringValue))
                Debug.LogWarning($"Layer {index} was '{layer.stringValue}'; it is now '{layerName}'.");
            layer.stringValue = layerName;
        }

        // Between Default (characters) and Bullets: the jumping body is above the characters, below the bullets and the foreground.
        private static void EnsureAirborneSortingLayer()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("m_SortingLayers");

            var names = new List<string>();
            var ids = new List<int>();
            for (int i = 0; i < layers.arraySize; i++)
            {
                SerializedProperty element = layers.GetArrayElementAtIndex(i);
                string layerName = element.FindPropertyRelative("name").stringValue;
                if (layerName == SortingLayers.Airborne)
                    return;
                names.Add(layerName);
                ids.Add(element.FindPropertyRelative("uniqueID").intValue);
            }

            int at = names.IndexOf(SortingLayers.Characters) + 1;
            names.Insert(at, SortingLayers.Airborne);
            ids.Insert(at, 7502);

            layers.ClearArray();
            for (int i = 0; i < names.Count; i++)
            {
                layers.InsertArrayElementAtIndex(i);
                SerializedProperty element = layers.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("name").stringValue = names[i];
                element.FindPropertyRelative("uniqueID").intValue = ids[i];
                element.FindPropertyRelative("locked").boolValue = false;
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- Assets and prefab

        private static JumpTuning GetOrCreateTuning()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<JumpTuning>(TuningPath);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<JumpTuning>();
                AssetDatabase.CreateAsset(tuning, TuningPath);
            }
            if (tuning.DustSprite == null)
                tuning.SetDustSprite(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Circle.png"));
            return tuning;
        }

        private static void SetupPlayerPrefab(JumpTuning tuning)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                root.layer = LayerMask.NameToLayer("Player");

                // The body on the floor for anything that wants to touch the player (enemy contact, charger dashes in
                // M8). It is on the Player layer, which the jump swaps for PlayerAirborne.
                var contact = root.GetComponent<CircleCollider2D>();
                if (contact == null)
                    contact = root.AddComponent<CircleCollider2D>();
                contact.isTrigger = true;
                contact.radius = AssetDatabase.LoadAssetAtPath<PlayerData>(PlayerDataPath).BodyRadius;

                var jump = root.GetComponent<JumpController>();
                if (jump == null)
                    jump = root.AddComponent<JumpController>();
                var so = new SerializedObject(jump);
                so.FindProperty("input").objectReferenceValue = root.GetComponent<GameplayInputReader>();
                so.FindProperty("rig").objectReferenceValue = root.GetComponent<PlayerVisualRig>();
                so.FindProperty("health").objectReferenceValue = root.GetComponent<PlayerHealth>();
                so.FindProperty("data").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlayerData>(PlayerDataPath);
                so.FindProperty("tuning").objectReferenceValue = tuning;
                so.FindProperty("sortingGroup").objectReferenceValue = root.GetComponent<SortingGroup>();
                so.FindProperty("shadow").objectReferenceValue = root.transform.Find("Shadow").GetComponent<SpriteRenderer>();
                so.ApplyModifiedPropertiesWithoutUndo();

                var moverSo = new SerializedObject(root.GetComponent<PlayerMover>());
                moverSo.FindProperty("jump").objectReferenceValue = jump;
                moverSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------------------------------------------------------------- Game scene

        private static void SetupGameScene()
        {
            var scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);

            var jump = Object.FindFirstObjectByType<JumpController>();
            var arenaSo = new SerializedObject(jump);
            arenaSo.FindProperty("arena").objectReferenceValue = Object.FindFirstObjectByType<ArenaController>();
            arenaSo.ApplyModifiedPropertiesWithoutUndo();

            var overlay = Object.FindFirstObjectByType<BulletHell.UI.DebugOverlay>(FindObjectsInactive.Include);
            var overlaySo = new SerializedObject(overlay);
            overlaySo.FindProperty("jump").objectReferenceValue = jump;
            overlaySo.ApplyModifiedPropertiesWithoutUndo();

            // 2D lights only light the sorting layers they list.
            foreach (Light2D light in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var lightSo = new SerializedObject(light);
                SerializedProperty applied = lightSo.FindProperty("m_ApplyToSortingLayers");
                SortingLayer[] all = SortingLayer.layers;
                applied.arraySize = all.Length;
                for (int i = 0; i < all.Length; i++)
                    applied.GetArrayElementAtIndex(i).intValue = all[i].id;
                lightSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
