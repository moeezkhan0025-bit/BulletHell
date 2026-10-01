#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BulletHell.EditorTools
{
    /// <summary>Small window that explains where new art goes: ArtSource (4x masters, outside Assets) to Assets/Art (2x game PNGs) via Tools/export_art.ps1.</summary>
    public class ArtDropMapWindow : EditorWindow
    {
        const string Command = "powershell -File Tools/export_art.ps1 [subfolder]";
        Vector2 scroll;

        [MenuItem("BulletHell/Capture/Editor Views/Open Art Drop Map window")]
        public static void Open()
        {
            var w = GetWindow<ArtDropMapWindow>(true, "Art Drop Map", true);
            w.minSize = new Vector2(560, 360);
            w.Show();
        }

        static string Root => Path.GetDirectoryName(Application.dataPath);

        static int CountPng(string dir)
        {
            if (!Directory.Exists(dir)) return 0;
            return Directory.GetFiles(dir, "*.png", SearchOption.AllDirectories).Length;
        }

        void OnGUI()
        {
            var title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 };
            var box = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(10, 10, 8, 8) };
            GUILayout.Space(6);
            EditorGUILayout.LabelField("New art pipeline: where a PNG drops in", title);
            GUILayout.Space(6);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(box, GUILayout.Width(210)))
                {
                    GUILayout.Label("ArtSource/<folder>", EditorStyles.boldLabel);
                    GUILayout.Label("4x master PNGs (Procreate)\nOUTSIDE Assets, Unity ignores it", EditorStyles.wordWrappedLabel);
                }
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(120)))
                {
                    GUILayout.Space(4);
                    var c = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true };
                    GUILayout.Label("Tools/export_art.ps1\n50% downscale\n=====>", c);
                }
                using (new EditorGUILayout.VerticalScope(box, GUILayout.Width(210)))
                {
                    GUILayout.Label("Assets/Art/<folder>", EditorStyles.boldLabel);
                    GUILayout.Label("2x game PNGs, same relative path\nImported by Unity (PPU set per art type)", EditorStyles.wordWrappedLabel);
                }
            }

            GUILayout.Space(8);
            EditorGUILayout.LabelField("PNG count per folder (read from disk)", EditorStyles.boldLabel);
            var src = Path.Combine(Root, "ArtSource");
            var dst = Path.Combine(Root, "Assets", "Art");
            scroll = EditorGUILayout.BeginScrollView(scroll);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Folder", EditorStyles.miniBoldLabel, GUILayout.Width(150));
                GUILayout.Label("ArtSource (4x)", EditorStyles.miniBoldLabel, GUILayout.Width(110));
                GUILayout.Label("Assets/Art (2x)", EditorStyles.miniBoldLabel, GUILayout.Width(110));
            }
            if (Directory.Exists(src))
            {
                foreach (var d in Directory.GetDirectories(src))
                {
                    var n = Path.GetFileName(d);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(n, GUILayout.Width(150));
                        GUILayout.Label(CountPng(d).ToString(), GUILayout.Width(110));
                        GUILayout.Label(CountPng(Path.Combine(dst, n)).ToString(), GUILayout.Width(110));
                    }
                }
            }
            else GUILayout.Label("ArtSource folder not found.");
            var loose = Directory.Exists(src) ? Directory.GetFiles(src, "*.png").Length : 0;
            if (loose > 0) GUILayout.Label("(+" + loose + " PNG(s) directly in ArtSource)", EditorStyles.miniLabel);
            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Run from the project root (skips files whose output is newer; -Force re-exports all):", EditorStyles.miniLabel);
            EditorGUILayout.SelectableLabel(Command, EditorStyles.textField, GUILayout.Height(20));
            if (GUILayout.Button("Reveal ArtSource in Explorer")) EditorUtility.RevealInFinder(src);
        }
    }
}
#endif
