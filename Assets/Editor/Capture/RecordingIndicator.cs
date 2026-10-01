using System;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace BulletHell.EditorTools.GameplayCapture
{
    /// <summary>
    /// R1 portfolio capture kit - the "REC" indicator. EDITOR ONLY, on purpose: nothing is drawn in the Game view or by any runtime
    /// script, so it cannot be in a video or a PNG (the Recorder and ScreenCapture read the Game view's own render texture, not the
    /// desktop and not Editor windows). Two forms: a small borderless popup window at the top right of the Unity window (shown while
    /// recording, never takes focus: the Game view gets it back at once so F11 / F12 keep working), and a Scene view overlay.
    /// </summary>
    public static class RecordingIndicator
    {
        private static RecordingIndicatorWindow window;
        private static double nextRepaint;

        public static void Show()
        {
            if (window != null)
                return;
            window = ScriptableObject.CreateInstance<RecordingIndicatorWindow>();
            window.titleContent = new GUIContent("REC");
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            window.position = new Rect(main.xMax - 220f, main.y + 6f, 200f, 26f);
            window.ShowPopup();
            Type gameView = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameView != null)
                EditorWindow.FocusWindowIfItsOpen(gameView);
        }

        public static void Hide()
        {
            if (window != null)
            {
                window.Close();
                window = null;
            }
        }

        public static void Tick()
        {
            if (window == null || EditorApplication.timeSinceStartup < nextRepaint)
                return;
            nextRepaint = EditorApplication.timeSinceStartup + 0.5;
            window.Repaint();
        }

        public static string Label()
        {
            double seconds = RecordingService.Seconds;
            return string.Format("REC  {0:00}:{1:00}", (int)(seconds / 60.0), (int)(seconds % 60.0));
        }
    }

    internal sealed class RecordingIndicatorWindow : EditorWindow
    {
        private void OnGUI()
        {
            var area = new Rect(0f, 0f, position.width, position.height);
            EditorGUI.DrawRect(area, new Color(0.08f, 0.08f, 0.08f, 0.95f));
            bool blink = ((int)(EditorApplication.timeSinceStartup * 2.0)) % 2 == 0;
            EditorGUI.DrawRect(new Rect(8f, 7f, 12f, 12f), blink ? new Color(0.95f, 0.1f, 0.1f) : new Color(0.55f, 0.05f, 0.05f));
            var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft };
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(26f, 0f, position.width - 30f, position.height), RecordingIndicator.Label(), style);
        }
    }

    /// <summary>The Scene view copy of the indicator: a small "REC mm:ss" box while recording, empty otherwise.</summary>
    [Overlay(typeof(SceneView), "vv-capture-rec", "Capture REC", defaultDisplay = true)]
    internal sealed class RecordingSceneOverlay : Overlay
    {
        public override VisualElement CreatePanelContent()
        {
            var label = new Label("not recording");
            label.style.paddingLeft = label.style.paddingRight = 6;
            label.schedule.Execute(() =>
            {
                bool recording = RecordingService.IsRecording;
                label.text = recording ? RecordingIndicator.Label() : "not recording";
                label.style.color = recording ? new Color(1f, 0.25f, 0.25f) : new Color(0.7f, 0.7f, 0.7f);
            }).Every(500);
            return label;
        }
    }
}
