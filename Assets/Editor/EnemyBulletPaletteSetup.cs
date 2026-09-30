using System.Collections.Generic;
using System.Text;
using BulletHell.Core;
using BulletHell.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// Creates the EnemyBulletPalette asset, puts it on GameConfig, marks boss / special patterns as Special (Hot Magenta),
    /// and moves the few colours that clashed with the two bullet hues (arm ID colours, the Epic rarity, a test ammo tint).
    /// "Audit Reserved Hues" lists every colour in data, prefabs and scenes that still lands in a bullet hue band.
    /// </summary>
    public static class EnemyBulletPaletteSetup
    {
        private const string PalettePath = "Assets/Data/Enemies/EnemyBulletPalette.asset";
        private const string ConfigPath = "Assets/Resources/GameConfig.asset";

        [MenuItem("BulletHell/Enemy Bullets/Setup Palette")]
        public static void Run()
        {
            var palette = AssetDatabase.LoadAssetAtPath<EnemyBulletPalette>(PalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<EnemyBulletPalette>();
                AssetDatabase.CreateAsset(palette, PalettePath);
            }

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var configSo = new SerializedObject(config);
            configSo.FindProperty("enemyBulletPalette").objectReferenceValue = palette;
            configSo.ApplyModifiedPropertiesWithoutUndo();

            // Boss and special shots: Hot Magenta.
            foreach (string name in new[] { "Pattern_BossBurst", "Pattern_SniperShot" })
                SetEnum("Assets/Data/Enemies/Patterns/" + name + ".asset", "bulletStyle", (int)BulletStyle.Special);
            foreach (string name in new[] { "Pattern_Aimed", "Pattern_Ring", "Pattern_RingRapid", "Pattern_Spiral", "Pattern_Spread" })
                SetEnum("Assets/Data/Enemies/Patterns/" + name + ".asset", "bulletStyle", (int)BulletStyle.Standard);

            // Colours that clashed with the bullet hues move to free hues.
            SetColor("Assets/Data/Arms/Arm_Purple.asset", "idColor", new Color(1f, 0.82f, 0.25f));    // gold
            SetColor("Assets/Data/Arms/Arm_Piercer.asset", "idColor", new Color(0.55f, 1f, 0.85f));   // mint
            SetColor("Assets/Data/Ammo/Ammo_TestSpare.asset", "tint", new Color(1f, 0.6f, 0.2f));     // orange
            SetEpicRarity(new Color(0.25f, 0.85f, 0.5f));                                              // emerald

            AssetDatabase.SaveAssets();
            Debug.Log("Enemy bullet palette set up.");
        }

        private static void SetEnum(string path, string field, int index)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset == null)
                return;
            var so = new SerializedObject(asset);
            so.FindProperty(field).enumValueIndex = index;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void SetColor(string path, string field, Color color)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset == null)
                return;
            var so = new SerializedObject(asset);
            so.FindProperty(field).colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void SetEpicRarity(Color color)
        {
            var table = AssetDatabase.LoadAssetAtPath<Object>("Assets/Data/Shop/RarityTable.asset");
            var so = new SerializedObject(table);
            SerializedProperty entries = so.FindProperty("entries");
            for (int i = 0; entries != null && i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                SerializedProperty rarity = entry.FindPropertyRelative("Rarity");
                if (rarity != null && rarity.enumNames[rarity.enumValueIndex] == "Epic")
                    entry.FindPropertyRelative("Color").colorValue = color;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
        }

        // ---------------------------------------------------------------- audit

        [MenuItem("BulletHell/Enemy Bullets/Audit Reserved Hues")]
        public static void Audit()
        {
            var palette = AssetDatabase.LoadAssetAtPath<EnemyBulletPalette>(PalettePath);
            var hits = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Data", "Assets/Resources" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == PalettePath)
                    continue;
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset != null)
                    ScanObject(palette, asset, path, hits);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                    ScanGameObject(palette, prefab, path, hits);
            }
            foreach (string scenePath in new[] { "Assets/Scenes/Game.unity", "Assets/Scenes/MainMenu.unity" })
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                    ScanGameObject(palette, root, scenePath, hits);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);

            var report = new StringBuilder("Reserved-hue audit: ").Append(hits.Count).Append(" hit(s)\n");
            foreach (string hit in hits)
                report.Append("  ").Append(hit).Append('\n');
            Debug.Log(report.ToString());
        }

        private static void ScanObject(EnemyBulletPalette palette, Object asset, string where, List<string> hits)
        {
            var so = new SerializedObject(asset);
            SerializedProperty it = so.GetIterator();
            while (it.NextVisible(true))
                if (it.propertyType == SerializedPropertyType.Color && palette.IsReservedHue(it.colorValue))
                    hits.Add(where + "  " + it.propertyPath + "  " + ColorUtility.ToHtmlStringRGB(it.colorValue));
        }

        private static void ScanGameObject(EnemyBulletPalette palette, GameObject root, string where, List<string> hits)
        {
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
                if (graphic.color.a > 0.2f && palette.IsReservedHue(graphic.color))
                    hits.Add(where + "  " + PathOf(graphic.transform) + " (Graphic)  " + ColorUtility.ToHtmlStringRGB(graphic.color));
            foreach (SpriteRenderer sprite in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (sprite.color.a > 0.2f && palette.IsReservedHue(sprite.color))
                    hits.Add(where + "  " + PathOf(sprite.transform) + " (SpriteRenderer)  " + ColorUtility.ToHtmlStringRGB(sprite.color));
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
