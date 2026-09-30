using BulletHell.Core;
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
    /// One-shot M3b setup: creates/updates the effect assets and test armaments, adds PlayerInventory and
    /// DebugArmamentControls to the Player prefab, StatusEffects to the test enemy prefab, and wires the debug overlay
    /// in the Game scene. Safe to run again.
    /// </summary>
    public static class M3bSetup
    {
        private const string ArmamentDir = "Assets/Data/Armaments";
        private const string EffectDir = "Assets/Data/Effects";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string EnemyPrefabPath = "Assets/Prefabs/TestEnemy.prefab";
        private const string ScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("BulletHell/M3b/Create Test Armaments And Wire Scene")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(EffectDir))
                AssetDatabase.CreateFolder("Assets/Data", "Effects");

            var pierce = CreateAsset<PierceEffect>("Effect_Pierce");
            pierce.Set(2);
            pierce.SetDisplayName("Pierce");
            var burn = CreateAsset<BurnEffect>("Effect_Burn");
            burn.Set(2f, 3f, 0.5f);
            burn.SetDisplayName("Burn");
            var stun = CreateAsset<StunEffect>("Effect_Stun");
            stun.Set(0.3f, 0.8f);
            stun.SetDisplayName("Stun");
            var ricochet = CreateAsset<RicochetEffect>("Effect_Ricochet");
            ricochet.Set(3, 1);
            ricochet.SetDisplayName("Ricochet");

            ArmamentData damage = CreateArmament("Armament_Damage", "+50% Damage", null, new StatModifier(StatType.Damage, ModifierMode.Percent, 50f));
            ArmamentData fireRate = CreateArmament("Armament_FireRate", "+2 Fire Rate", null, new StatModifier(StatType.FireRate, ModifierMode.Flat, 2f));
            ArmamentData bulletSpeed = CreateArmament("Armament_BulletSpeed", "+30% Bullet Speed", null, new StatModifier(StatType.ProjectileSpeed, ModifierMode.Percent, 30f));
            ArmamentData extraProjectile = CreateArmament("Armament_ExtraProjectile", "+1 Projectile", null, new StatModifier(StatType.ProjectilesPerShot, ModifierMode.Flat, 1f));
            ArmamentData pierceArmament = CreateArmament("Armament_Pierce", "Piercing Rounds", pierce);
            ArmamentData burnArmament = CreateArmament("Armament_Burn", "Incendiary Rounds", burn);
            ArmamentData stunArmament = CreateArmament("Armament_Stun", "Shock Rounds", stun);
            ArmamentData ricochetArmament = CreateArmament("Armament_Ricochet", "Ricochet Rounds", ricochet);

            // Effect armaments twice, so two of the same effect (stacking) can be tested.
            ArmamentData[] startingArmaments =
            {
                damage, fireRate, bulletSpeed, extraProjectile,
                pierceArmament, pierceArmament, burnArmament, burnArmament,
                stunArmament, stunArmament, ricochetArmament, ricochetArmament,
            };
            WeaponArmData[] spareArms =
            {
                AssetDatabase.LoadAssetAtPath<WeaponArmData>("Assets/Data/Arms/Arm_Red.asset"),
                AssetDatabase.LoadAssetAtPath<WeaponArmData>("Assets/Data/Arms/Arm_Blue.asset"),
            };

            SetupPlayerPrefab(startingArmaments, spareArms);
            SetupEnemyPrefab();
            SetupScene();
            AssetDatabase.SaveAssets();
            Debug.Log("M3b setup complete.");
        }

        private static T CreateAsset<T>(string assetName) where T : ScriptableObject
        {
            string path = $"{EffectDir}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static ArmamentData CreateArmament(string assetName, string displayName, ArmEffect effect,
                                                   params StatModifier[] modifiers)
        {
            string path = $"{ArmamentDir}/{assetName}.asset";
            var armament = AssetDatabase.LoadAssetAtPath<ArmamentData>(path);
            if (armament == null)
            {
                armament = ScriptableObject.CreateInstance<ArmamentData>();
                AssetDatabase.CreateAsset(armament, path);
            }
            armament.Set(displayName, modifiers);
            armament.SetEffects(effect != null ? new[] { effect } : new ArmEffect[0]);
            return armament;
        }

        private static void SetupPlayerPrefab(ArmamentData[] armaments, WeaponArmData[] spareArms)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var inventory = root.GetComponent<PlayerInventory>() ?? root.AddComponent<PlayerInventory>();
                var inventorySo = new SerializedObject(inventory);
                FillList(inventorySo.FindProperty("startingArmaments"), armaments);
                FillList(inventorySo.FindProperty("startingSpareArms"), spareArms);
                inventorySo.ApplyModifiedPropertiesWithoutUndo();

                var controls = root.GetComponent<DebugArmamentControls>() ?? root.AddComponent<DebugArmamentControls>();
                var so = new SerializedObject(controls);
                so.FindProperty("input").objectReferenceValue = root.GetComponentInChildren<GameplayInputReader>();
                so.FindProperty("arms").objectReferenceValue = root.GetComponentInChildren<ArmSelectionController>();
                so.FindProperty("inventory").objectReferenceValue = inventory;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void FillList(SerializedProperty list, Object[] items)
        {
            list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        private static void SetupEnemyPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
            try
            {
                if (root.GetComponent<StatusEffects>() == null)
                    root.AddComponent<StatusEffects>();
                PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
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
            so.FindProperty("armamentControls").objectReferenceValue = Object.FindFirstObjectByType<DebugArmamentControls>();
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
