using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// S1 setup: creates the DemoConfig asset (Assets/Data/DemoConfig.asset) and puts it on the GameConfig, and wires the notice dialog of the
    /// Character Creation screen in the Main Menu scene. Also the editor switch for trying the demo rules in Play mode. Safe to run again.
    /// </summary>
    public static class DemoSetup
    {
        public const string DemoConfigPath = "Assets/Data/DemoConfig.asset";
        private const string ConfigPath = "Assets/Resources/GameConfig.asset";
        private const string MenuScene = "Assets/Scenes/MainMenu.unity";

        [MenuItem("BulletHell/Demo/Set Up Demo Config")]
        public static void Build()
        {
            var demo = AssetDatabase.LoadAssetAtPath<DemoConfig>(DemoConfigPath);
            if (demo == null)
            {
                demo = ScriptableObject.CreateInstance<DemoConfig>();
                AssetDatabase.CreateAsset(demo, DemoConfigPath);
            }
            var grunt = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/Enemies/Enemy_Grunt.asset");
            var so = new SerializedObject(demo);
            so.FindProperty("sentryStandIn").objectReferenceValue = grunt;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(demo);

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var configObject = new SerializedObject(config);
            configObject.FindProperty("demo").objectReferenceValue = demo;
            configObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);

            var scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            var customization = Object.FindFirstObjectByType<CustomizationScreen>(FindObjectsInactive.Include);
            var dialog = Object.FindFirstObjectByType<ConfirmDialog>(FindObjectsInactive.Include);
            if (customization != null && dialog != null)
            {
                var screen = new SerializedObject(customization);
                screen.FindProperty("dialog").objectReferenceValue = dialog;
                screen.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            else
            {
                Debug.LogWarning("S1: the Main Menu scene has no CustomizationScreen or ConfirmDialog to wire.");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("S1: DemoConfig set up (demo flag " + demo.IsDemo + ").");
        }

        [MenuItem("BulletHell/Demo/Demo Mode On (Editor)")]
        public static void DemoOn() => SetDemo(true);

        [MenuItem("BulletHell/Demo/Demo Mode Off (Editor)")]
        public static void DemoOff() => SetDemo(false);

        public static bool SetDemo(bool on)
        {
            var demo = AssetDatabase.LoadAssetAtPath<DemoConfig>(DemoConfigPath);
            if (demo == null)
                return false;
            bool was = demo.IsDemo;
            demo.SetDemo(on);
            AssetDatabase.SaveAssets();
            Debug.Log("S1: demo mode " + (on ? "ON" : "OFF") + " in the editor asset.");
            return was;
        }
    }
}
