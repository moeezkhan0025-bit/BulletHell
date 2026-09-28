using System.Linq;
using BulletHell.Armory;
using BulletHell.Core;
using BulletHell.Shop;
using BulletHell.UI;
using BulletHell.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// One-shot M4 part 2 setup: two test arm types with effects, the ShopPool, GameConfig changes (starting currency,
    /// the real one-arm starting loadout, no free test stock), the MenuRow prefab, and the Shop / Armory screens in
    /// the Game scene (replacing the placeholder panels). Safe to run again.
    /// </summary>
    public static class M4bSetup
    {
        private const string ShopDir = "Assets/Data/Shop";
        private const string PoolPath = ShopDir + "/ShopPool.asset";
        private const string RowPrefabPath = "Assets/Prefabs/UI/MenuRow.prefab";
        private const string ConfigPath = "Assets/Resources/GameConfig.asset";
        private const string GamePath = "Assets/Scenes/Game.unity";

        [MenuItem("BulletHell/M4/Setup Shop And Armory")]
        public static void Run()
        {
            WeaponArmData ember = CreateArmVariant("Arm_Green", "Arm_Ember", "Ember Arm", "Effect_Burn");
            WeaponArmData piercer = CreateArmVariant("Arm_Purple", "Arm_Piercer", "Piercer Arm", "Effect_Pierce");
            M4Setup.CollectRegistry(); // gives the new arms their save IDs and adds them to the registry

            ShopPool pool = CreatePool(ember, piercer);
            ConfigureGame(pool);
            MenuRow rowPrefab = CreateRowPrefab();
            SetupScene(rowPrefab);
            AssetDatabase.SaveAssets();
            Debug.Log("M4 part 2 setup complete.");
        }

        /// <summary>
        /// A new arm type made from an existing one (same sprite, muzzle and stats) plus an effect. Placeholder until there
        /// is art for real shop arms; the original arm assets and sprites are not touched.
        /// </summary>
        private static WeaponArmData CreateArmVariant(string sourceName, string newName, string displayName, string effectName)
        {
            string path = $"Assets/Data/Arms/{newName}.asset";
            if (AssetDatabase.LoadAssetAtPath<WeaponArmData>(path) == null)
                AssetDatabase.CopyAsset($"Assets/Data/Arms/{sourceName}.asset", path);

            var arm = AssetDatabase.LoadAssetAtPath<WeaponArmData>(path);
            var so = new SerializedObject(arm);
            so.FindProperty("displayName").stringValue = displayName;
            SerializedProperty effects = so.FindProperty("effects");
            effects.arraySize = 1;
            effects.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<ArmEffect>($"Assets/Data/Effects/{effectName}.asset");
            so.ApplyModifiedPropertiesWithoutUndo();
            arm.SetId(newName);
            EditorUtility.SetDirty(arm);
            return arm;
        }

        private static ShopPool CreatePool(WeaponArmData ember, WeaponArmData piercer)
        {
            if (!AssetDatabase.IsValidFolder(ShopDir))
                AssetDatabase.CreateFolder("Assets/Data", "Shop");

            var pool = AssetDatabase.LoadAssetAtPath<ShopPool>(PoolPath);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<ShopPool>();
                AssetDatabase.CreateAsset(pool, PoolPath);
            }

            var so = new SerializedObject(pool);
            SerializedProperty entries = so.FindProperty("entries");
            entries.arraySize = 6;
            SetArm(entries.GetArrayElementAtIndex(0), ember, 200);
            SetArm(entries.GetArrayElementAtIndex(1), piercer, 220);
            SetArmament(entries.GetArrayElementAtIndex(2), "Damage", 60);
            SetArmament(entries.GetArrayElementAtIndex(3), "ExtraProjectile", 90);
            SetArmament(entries.GetArrayElementAtIndex(4), "Burn", 80);
            SetArmament(entries.GetArrayElementAtIndex(5), "Stun", 80);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pool);
            return pool;
        }

        private static void SetArm(SerializedProperty entry, WeaponArmData arm, int price)
        {
            entry.FindPropertyRelative("Kind").enumValueIndex = (int)ShopItemKind.Arm;
            entry.FindPropertyRelative("Arm").objectReferenceValue = arm;
            entry.FindPropertyRelative("Armament").objectReferenceValue = null;
            entry.FindPropertyRelative("Price").intValue = price;
        }

        private static void SetArmament(SerializedProperty entry, string name, int price)
        {
            entry.FindPropertyRelative("Kind").enumValueIndex = (int)ShopItemKind.Armament;
            entry.FindPropertyRelative("Arm").objectReferenceValue = null;
            entry.FindPropertyRelative("Armament").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<ArmamentData>($"Assets/Data/Armaments/Armament_{name}.asset");
            entry.FindPropertyRelative("Price").intValue = price;
        }

        /// <summary>The real starting setup: one arm (StartingLoadout), no free stock, some currency to shop with.</summary>
        private static void ConfigureGame(ShopPool pool)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var so = new SerializedObject(config);
            so.FindProperty("shopPool").objectReferenceValue = pool;
            so.FindProperty("startingCurrency").intValue = 500;
            so.FindProperty("newRunLoadout").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ArmLoadout>("Assets/Data/Loadouts/StartingLoadout.asset");
            so.FindProperty("startingArmaments").arraySize = 0;
            so.FindProperty("startingSpareArms").arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static MenuRow CreateRowPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath);
            if (existing != null)
                return existing.GetComponent<MenuRow>();

            GameObject temp = UiBuilder.CreateMenuRow();
            PrefabUtility.SaveAsPrefabAsset(temp, RowPrefabPath);
            Object.DestroyImmediate(temp);
            AssetDatabase.SaveAssets();
            // Reload from the path: a reference kept from before the temp object was destroyed ended up null in the scene.
            return AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath).GetComponent<MenuRow>();
        }

        private static void SetupScene(MenuRow rowPrefab)
        {
            var scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<ShopScreen>(FindObjectsInactive.Include) != null)
            {
                Debug.Log("Game scene already has the Shop and Armory screens; left unchanged.");
                return;
            }

            var flowUi = Object.FindFirstObjectByType<GameFlowUI>();
            Transform safe = FindDeep("FlowUI/SafeArea");

            foreach (string old in new[] { "ShopPanel", "ArmoryPanel" })
            {
                Transform panel = safe.Find(old);
                if (panel != null)
                    Object.DestroyImmediate(panel.gameObject);
            }

            UiBuilder.ListScreenParts shopParts = UiBuilder.CreateListScreen("ShopScreen", safe, rowPrefab, "Continue to Armory");
            var shop = shopParts.Root.gameObject.AddComponent<ShopScreen>();
            var shopSo = new SerializedObject(shop);
            shopSo.FindProperty("title").objectReferenceValue = shopParts.Title;
            shopSo.FindProperty("info").objectReferenceValue = shopParts.Info;
            shopSo.FindProperty("list").objectReferenceValue = shopParts.List;
            shopSo.FindProperty("continueButton").objectReferenceValue = shopParts.Continue;
            shopSo.FindProperty("menuButton").objectReferenceValue = shopParts.Menu;
            shopSo.ApplyModifiedPropertiesWithoutUndo();

            UiBuilder.ListScreenParts armoryParts = UiBuilder.CreateListScreen("ArmoryScreen", safe, rowPrefab, "Start Next Round");
            var armory = armoryParts.Root.gameObject.AddComponent<ArmoryScreen>();
            var armorySo = new SerializedObject(armory);
            armorySo.FindProperty("title").objectReferenceValue = armoryParts.Title;
            armorySo.FindProperty("info").objectReferenceValue = armoryParts.Info;
            armorySo.FindProperty("list").objectReferenceValue = armoryParts.List;
            armorySo.FindProperty("footer").objectReferenceValue = armoryParts.Footer;
            armorySo.FindProperty("continueButton").objectReferenceValue = armoryParts.Continue;
            armorySo.FindProperty("menuButton").objectReferenceValue = armoryParts.Menu;
            armorySo.ApplyModifiedPropertiesWithoutUndo();

            var uiSo = new SerializedObject(flowUi);
            uiSo.FindProperty("shop").objectReferenceValue = shop;
            uiSo.FindProperty("armory").objectReferenceValue = armory;
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            shopParts.Root.gameObject.SetActive(false);
            armoryParts.Root.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Transform FindDeep(string path)
        {
            string[] parts = path.Split('/');
            GameObject root = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Select(c => c.gameObject).First(g => g.name == parts[0]);
            Transform current = root.transform;
            for (int i = 1; i < parts.Length; i++)
                current = current.Find(parts[i]);
            return current;
        }
    }
}
