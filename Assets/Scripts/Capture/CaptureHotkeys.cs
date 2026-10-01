using System;
using UnityEngine;

namespace BulletHell.Capture
{
    /// <summary>
    /// R1 portfolio capture kit - the hooks between the gameplay input (F11 / F12) and the capture tools.
    ///
    /// KEYS (editor and development builds only; the Debug input map, like F8 / F9 / F10):
    ///   F11  start / stop the gameplay video recording (Editor only: the Unity Recorder, see Assets/Editor/Capture/GameplayRecording.cs)
    ///   F12  save a 1920x1080 PNG screenshot to Captures/Gameplay/&lt;scenario&gt;_&lt;date&gt;_&lt;time&gt;.png
    ///
    /// The runtime never references the Recorder (an editor-only package): the Editor subscribes to <see cref="RecordToggleRequested"/>.
    /// The state flags (<see cref="IsRecording"/>) are pushed in by the Editor so runtime code can tell without a reference.
    /// Release builds never raise anything.
    /// </summary>
    public static class CaptureHotkeys
    {
        /// <summary>F11 (or the Capture menu) asked to start or stop recording. Subscribed to by the Editor.</summary>
        public static event Action RecordToggleRequested;

        /// <summary>F12 (or the Capture menu) asked for a screenshot. Handled by <see cref="CaptureDirector"/>.</summary>
        public static event Action ScreenshotRequested;

        /// <summary>A screenshot was written (full path). The Editor restores the Game view size afterwards.</summary>
        public static event Action<string> ScreenshotSaved;

        /// <summary>Set by the Editor while the Recorder is running. Runtime code only reads it.</summary>
        public static bool IsRecording { get; set; }

        public static void RaiseRecordToggle()
        {
            if (Debug.isDebugBuild)
                RecordToggleRequested?.Invoke();
        }

        public static void RaiseScreenshot()
        {
            if (Debug.isDebugBuild)
                ScreenshotRequested?.Invoke();
        }

        internal static void RaiseScreenshotSaved(string path) => ScreenshotSaved?.Invoke(path);
    }

    /// <summary>
    /// Session-wide capture switches, pushed in from the Capture menu (Editor, persisted in EditorPrefs there) and read by the
    /// director. Deliberately not reset on a domain reload: the Editor sets them again before Play.
    /// </summary>
    public static class CaptureFlags
    {
        /// <summary>Hide debug overlays and the OS cursor, keep the HUD.</summary>
        public static bool CleanCapture { get; set; } = true;

        /// <summary>The player cannot die during a capture scenario (uses the existing debug god mode; the run is flagged as a debug run).</summary>
        public static bool Invincible { get; set; } = true;

        /// <summary>Start recording when a scenario's combat begins and stop when its capture window has elapsed (Editor).</summary>
        public static bool AutoRecordScenarios { get; set; }
    }

    /// <summary>Scenario lifecycle events for the Editor (auto-record, REC indicator, logging).</summary>
    public static class CaptureEvents
    {
        /// <summary>A scenario was applied and the Game scene is (re)loading.</summary>
        public static event Action<CaptureScenarioData> ScenarioStarted;

        /// <summary>The scenario's combat began (the first frame the player can be shot at).</summary>
        public static event Action<CaptureScenarioData> CombatBegan;

        /// <summary>The scenario's capture window (CaptureScenarioData.captureSeconds of combat) is over.</summary>
        public static event Action<CaptureScenarioData> WindowElapsed;

        /// <summary>The scenario ended (round over, run over, or replaced by another).</summary>
        public static event Action<CaptureScenarioData> ScenarioEnded;

        internal static void RaiseStarted(CaptureScenarioData data) => ScenarioStarted?.Invoke(data);
        internal static void RaiseCombatBegan(CaptureScenarioData data) => CombatBegan?.Invoke(data);
        internal static void RaiseWindowElapsed(CaptureScenarioData data) => WindowElapsed?.Invoke(data);
        internal static void RaiseEnded(CaptureScenarioData data) => ScenarioEnded?.Invoke(data);
    }
}
