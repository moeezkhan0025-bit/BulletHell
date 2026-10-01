using BulletHell.Capture;
using UnityEditor;
using UnityEngine;

namespace BulletHell.EditorTools.Capture
{
    /// <summary>
    /// Editor bridge of the placeholder annotation overlay: assigns the provenance delegates of the runtime class (runtime code cannot reference
    /// editor code), the VoxKit palette and fonts, and owns the menu toggle (stored in EditorPrefs).
    /// </summary>
    [InitializeOnLoad]
    public static class AnnotationMenu
    {
        private const string MenuPath = "BulletHell/Capture/Placeholder Annotation";
        private const string PrefKey = "BulletHell.Capture.PlaceholderAnnotation";
        private const string ThemePath = "Assets/Data/UI/VoxVegetallis.asset";
        private const string ChipFontPath = "Assets/Fonts/LilitaOne-Regular.ttf";
        private const string NameFontPath = "Assets/Fonts/Nunito_800ExtraBold.ttf";

        private static string audioLegendCache;

        static AnnotationMenu()
        {
            PlaceholderAnnotation.IsFinal = obj => ArtProvenance.Classify(obj).IsFinal;
            PlaceholderAnnotation.AudioLegend = () => audioLegendCache ?? (audioLegendCache = ArtProvenance.AudioLegendText());
            PlaceholderAnnotation.Enabled = EditorPrefs.GetBool(PrefKey, false);
            EditorApplication.delayCall += LoadLooks;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                audioLegendCache = null;
                PlaceholderAnnotation.Enabled = EditorPrefs.GetBool(PrefKey, false);
            }
        }

        private static void LoadLooks()
        {
            var theme = AssetDatabase.LoadAssetAtPath<BulletHell.UI.UITheme>(ThemePath);
            if (theme != null)
            {
                PlaceholderAnnotation.InkSoil = theme.InkSoil;
                PlaceholderAnnotation.Marble = theme.Marble;
                PlaceholderAnnotation.Leaf = theme.Leaf;
                PlaceholderAnnotation.Carrot = theme.Carrot;
                PlaceholderAnnotation.Gold = theme.Gold;
            }
            PlaceholderAnnotation.ChipFont = AssetDatabase.LoadAssetAtPath<Font>(ChipFontPath);
            PlaceholderAnnotation.NameFont = AssetDatabase.LoadAssetAtPath<Font>(NameFontPath);
        }

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            bool on = !EditorPrefs.GetBool(PrefKey, false);
            EditorPrefs.SetBool(PrefKey, on);
            PlaceholderAnnotation.Enabled = on;
            audioLegendCache = null;
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(PrefKey, false));
            return true;
        }
    }
}
