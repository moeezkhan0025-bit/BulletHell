using BulletHell.Input;
using BulletHell.Player;
using BulletHell.UI;
using BulletHell.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// One-shot M3b setup: creates/updates the 4 test upgrades, adds DebugUpgradeControls to the Player prefab and
    /// wires it into the debug overlay in the Game scene. Safe to run again.
    /// </summary>
    public static class M3bSetup
    {
        private const string UpgradeDir = "Assets/Data/Upgrades";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string ScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("BulletHell/M3b/Create Test Upgrades And Wire Scene")]
        public static void Run()
        {
            UpgradeData[] upgrades =
            {
                CreateUpgrade("Upg_Damage", "+50% Damage", new StatModifier(StatType.Damage, ModifierMode.Percent, 50f)),
                CreateUpgrade("Upg_FireRate", "+2 Fire Rate", new StatModifier(StatType.FireRate, ModifierMode.Flat, 2f)),
                CreateUpgrade("Upg_BulletSpeed", "+30% Bullet Speed", new StatModifier(StatType.ProjectileSpeed, ModifierMode.Percent, 30f)),
                CreateUpgrade("Upg_ExtraProjectile", "+1 Projectile", new StatModifier(StatType.ProjectilesPerShot, ModifierMode.Flat, 1f)),
            };

            SetupPlayerPrefab(upgrades);
            SetupScene();
            AssetDatabase.SaveAssets();
            Debug.Log("M3b setup complete.");
        }

        private static UpgradeData CreateUpgrade(string assetName, string displayName, params StatModifier[] modifiers)
        {
            string path = $"{UpgradeDir}/{assetName}.asset";
            var upgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
            if (upgrade == null)
            {
                upgrade = ScriptableObject.CreateInstance<UpgradeData>();
                AssetDatabase.CreateAsset(upgrade, path);
            }
            upgrade.Set(displayName, modifiers);
            return upgrade;
        }

        private static void SetupPlayerPrefab(UpgradeData[] upgrades)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var controls = root.GetComponent<DebugUpgradeControls>() ?? root.AddComponent<DebugUpgradeControls>();
                var so = new SerializedObject(controls);
                so.FindProperty("input").objectReferenceValue = root.GetComponentInChildren<GameplayInputReader>();
                so.FindProperty("arms").objectReferenceValue = root.GetComponentInChildren<ArmSelectionController>();
                SerializedProperty list = so.FindProperty("testUpgrades");
                list.arraySize = upgrades.Length;
                for (int i = 0; i < upgrades.Length; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = upgrades[i];
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var overlay = Object.FindFirstObjectByType<DebugOverlay>();
            var so = new SerializedObject(overlay);
            so.FindProperty("upgradeControls").objectReferenceValue = Object.FindFirstObjectByType<DebugUpgradeControls>();
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
