using System;
using System.IO;
using BulletHell.Capture;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace BulletHell.EditorTools.GameplayCapture
{
    /// <summary>
    /// R1 portfolio capture kit - gameplay video and screenshots from the Editor (Recorder 5.1.2 is editor-only).
    ///
    /// KEYS in Play mode (gameplay input actions DebugToggleRecording / DebugScreenshot, like F8 / F9 / F10):
    ///   F11  start / stop an MP4 recording        F12  save a 1920x1080 PNG
    /// Both are also in the menu BulletHell/Capture/Gameplay.
    ///
    /// VIDEO: Recorder Movie recorder, H.264 MP4, Game View input at 1920x1080 (the HUD is part of the Game view), constant 60 fps,
    /// the game's audio (AudioInputSettings.PreserveAudio), named "&lt;scenario&gt;_&lt;date&gt;_&lt;time&gt;.mp4" in Captures/Gameplay/
    /// (CaptureNaming). The Game view is set to 1920x1080 for the recording and put back afterwards.
    /// SCREENSHOT: the Game view at the end of a frame (ScreenCapture), resampled to 1920x1080 if the view is another size; the Game
    /// view is sized to 1920x1080 for the shot and restored.
    ///
    /// REC INDICATOR: NOT drawn in the game. It is a small Editor popup window and a Scene view overlay (RecordingIndicator.cs). The
    /// Recorder reads the Game view's own render (ScreenCapture into a RenderTexture) - never the desktop or any Editor window - and no
    /// runtime object draws a REC mark, so the indicator cannot appear in a video or a PNG.
    ///
    /// Scenarios can start and stop the recording by themselves (menu toggle "Auto record scenarios"): it starts when the scenario's
    /// combat begins and stops when its capture window (CaptureSeconds) has elapsed or the scenario ends.
    /// </summary>
    [InitializeOnLoad]
    public static class RecordingService
    {
        public const int Width = CaptureDirector.ScreenshotWidth;
        public const int Height = CaptureDirector.ScreenshotHeight;
        public const float FrameRate = 60f;
        private const int ShotSettleFrames = 6;

        private static RecorderController controller;
        private static RecorderControllerSettings controllerSettings;
        private static MovieRecorderSettings movie;
        private static string outputPath;
        private static double startedAt;
        private static bool autoStarted;
        private static bool shotPending;
        private static bool shotSizedByUs;
        private static int shotFrames;

        public static bool IsRecording => controller != null && controller.IsRecording();
        public static double Seconds => IsRecording ? EditorApplication.timeSinceStartup - startedAt : 0.0;
        public static string OutputPath => outputPath;

        static RecordingService()
        {
            CaptureHotkeys.RecordToggleRequested -= Toggle;
            CaptureHotkeys.RecordToggleRequested += Toggle;
            CaptureHotkeys.ScreenshotRequested -= RequestScreenshot;
            CaptureHotkeys.ScreenshotRequested += RequestScreenshot;
            CaptureHotkeys.ScreenshotSaved -= OnScreenshotSaved;
            CaptureHotkeys.ScreenshotSaved += OnScreenshotSaved;
            CaptureEvents.CombatBegan -= OnCombatBegan;
            CaptureEvents.CombatBegan += OnCombatBegan;
            CaptureEvents.WindowElapsed -= OnWindowElapsed;
            CaptureEvents.WindowElapsed += OnWindowElapsed;
            CaptureEvents.ScenarioEnded -= OnScenarioEnded;
            CaptureEvents.ScenarioEnded += OnScenarioEnded;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        // ------------------------------------------------------------------ recording

        public static void Toggle()
        {
            if (IsRecording)
                StopRecording();
            else
                StartRecording(CaptureDirector.CurrentPrefix);
        }

        /// <summary>Starts an MP4 recording of the Game view. False (with a log) when it cannot.</summary>
        public static bool StartRecording(string prefix)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Capture: recording needs Play mode. Start a scenario from BulletHell/Capture/Scenarios, or press Play.");
                return false;
            }
            if (EditorApplication.isPaused)
            {
                Debug.LogWarning("Capture: the Editor is paused; unpause before recording.");
                return false;
            }
            if (IsRecording)
                return false;

            RecordingGameView.Apply(Width, Height);

            string folder = CaptureNaming.OutputFolder();
            Directory.CreateDirectory(folder);
            string baseName = CaptureNaming.BaseName(prefix, DateTime.Now);
            outputPath = folder + "/" + baseName + ".mp4";

            controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = "VV Gameplay Capture";
            movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,                       // H.264
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            };
            movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = Width, OutputHeight = Height };
            movie.CaptureAudio = true;
            movie.AudioInputSettings.PreserveAudio = true;
            movie.OutputFile = folder + "/" + baseName;   // the Recorder adds ".mp4"

            controllerSettings.AddRecorderSettings(movie);
            controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
            controllerSettings.FrameRate = FrameRate;
            controllerSettings.CapFrameRate = true;

            controller = new RecorderController(controllerSettings);
            try
            {
                controller.PrepareRecording();
                if (!controller.StartRecording())
                {
                    Debug.LogWarning("Capture: the Recorder did not start (see the console).");
                    Cleanup();
                    return false;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Capture: could not start the Recorder: " + exception.Message);
                Cleanup();
                return false;
            }

            startedAt = EditorApplication.timeSinceStartup;
            CaptureHotkeys.IsRecording = true;
            RecordingIndicator.Show();
            Debug.Log("Capture: REC started -> " + outputPath);
            return true;
        }

        public static void StopRecording()
        {
            if (controller == null)
                return;
            string path = outputPath;
            try
            {
                controller.StopRecording();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Capture: stopping the Recorder failed: " + exception.Message);
            }
            Cleanup();
            Debug.Log("Capture: REC stopped -> " + path);
        }

        private static void Cleanup()
        {
            controller = null;
            if (movie != null)
                UnityEngine.Object.DestroyImmediate(movie);
            if (controllerSettings != null)
                UnityEngine.Object.DestroyImmediate(controllerSettings);
            movie = null;
            controllerSettings = null;
            autoStarted = false;
            CaptureHotkeys.IsRecording = false;
            RecordingIndicator.Hide();
            if (!shotPending)
                RecordingGameView.Restore();
        }

        // ------------------------------------------------------------------ screenshot

        public static void RequestScreenshot()
        {
            if (!Application.isPlaying || shotPending || CaptureDirector.Instance == null)
                return;
            shotSizedByUs = !IsRecording && RecordingGameView.Apply(Width, Height);
            shotFrames = shotSizedByUs ? ShotSettleFrames : 0;   // let the Game view resize and repaint first
            shotPending = true;
        }

        private static void OnScreenshotSaved(string path)
        {
            if (shotSizedByUs && !IsRecording)
                RecordingGameView.Restore();
            shotSizedByUs = false;
        }

        // ------------------------------------------------------------------ scenario hooks

        private static void OnCombatBegan(CaptureScenarioData data)
        {
            if (!CaptureFlags.AutoRecordScenarios || IsRecording)
                return;
            autoStarted = StartRecording(data.FilePrefix);
        }

        private static void OnWindowElapsed(CaptureScenarioData data)
        {
            if (autoStarted && IsRecording)
                StopRecording();
        }

        private static void OnScenarioEnded(CaptureScenarioData data)
        {
            if (autoStarted && IsRecording)
                StopRecording();
        }

        // ------------------------------------------------------------------ editor loop

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                if (controller != null)
                    StopRecording();
                shotPending = false;
                RecordingGameView.Restore();
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                RecordingIndicator.Hide();
                CaptureHotkeys.IsRecording = false;
            }
        }

        private static void OnUpdate()
        {
            if (shotPending)
            {
                if (shotFrames > 0)
                {
                    shotFrames--;
                }
                else
                {
                    shotPending = false;
                    CaptureDirector.TakeScreenshot();
                }
            }

            if (controller != null && !controller.IsRecording())
            {
                // The Recorder stopped by itself (an error, or Play mode ended).
                Cleanup();
                return;
            }
            RecordingIndicator.Tick();
        }
    }
}
