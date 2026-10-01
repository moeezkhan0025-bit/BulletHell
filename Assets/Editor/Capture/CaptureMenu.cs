using System.Diagnostics;
using System.IO;
using BulletHell.Capture;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace BulletHell.EditorTools.GameplayCapture
{
    /// <summary>
    /// R1 portfolio capture kit - the Capture menu for gameplay material. Output goes to Captures/Gameplay (git-ignored).
    ///
    ///   BulletHell/Capture/Scenarios/...   Hero loop, Round 1, Two enemy types, Pumpking. Each starts a FRESH run (Play mode is entered
    ///                                       when needed; the Game scene is reloaded from the run state), onboarding skipped for the
    ///                                       session only, autopilot on. Data: Assets/Data/Capture/Scenario_*.asset.
    ///   BulletHell/Capture/Gameplay/...    Start/stop recording (F11), save screenshot (F12), open the folder.
    ///   Toggles (checked = on, saved in EditorPrefs):
    ///     Clean capture            hides the debug overlays (DebugOverlay, perf F8, telemetry F9, AI view, bullet paths) and the OS cursor; keeps the HUD
    ///     Invincible for capture   the player cannot die (the existing debug god mode; the run is flagged as a debug run in the telemetry)
    ///     Auto record scenarios    recording starts with the scenario's combat and stops after its capture seconds
    ///   BulletHell/Capture/Setup/...       Create or repair the capture assets (CaptureMenuAssets).
    /// </summary>
    public static class CaptureMenu
    {
        private const string Root = "BulletHell/Capture/";
        private const string CleanPath = Root + "Gameplay/Clean capture";
        private const string InvinciblePath = Root + "Gameplay/Invincible for capture";
        private const string AutoRecordPath = Root + "Gameplay/Auto record scenarios";
        private const string RecordPath = Root + "Gameplay/Start or stop recording (F11)";
        private const string ShotPath = Root + "Gameplay/Save screenshot 1920x1080 (F12)";

        // ------------------------------------------------------------------ scenarios

        [MenuItem(Root + "Scenarios/Hero loop", false, 100)]
        private static void HeroLoop() => Launch(CaptureMenuAssets.HeroLoopPath);

        [MenuItem(Root + "Scenarios/Round 1", false, 101)]
        private static void Round1() => Launch(CaptureMenuAssets.Round1Path);

        [MenuItem(Root + "Scenarios/Two enemy types", false, 102)]
        private static void TwoEnemyTypes() => Launch(CaptureMenuAssets.TwoEnemyTypesPath);

        [MenuItem(Root + "Scenarios/Pumpking", false, 103)]
        private static void Pumpking() => Launch(CaptureMenuAssets.PumpkingPath);

        /// <summary>Starts a scenario asset: straight away in Play mode, else by entering Play mode (picked up by CaptureDirector).</summary>
        public static void Launch(string assetPath)
        {
            var data = AssetDatabase.LoadAssetAtPath<CaptureScenarioData>(assetPath);
            if (data == null)
            {
                CaptureMenuAssets.CreateAll();
                data = AssetDatabase.LoadAssetAtPath<CaptureScenarioData>(assetPath);
                if (data == null)
                {
                    Debug.LogError("Capture: scenario asset missing: " + assetPath);
                    return;
                }
            }

            CaptureMenuPrefs.Sync();
            if (EditorApplication.isPlaying)
            {
                CaptureDirector.StartScenario(data);
            }
            else
            {
                SessionState.SetString(CaptureDirector.PendingScenarioKey, assetPath);
                EditorApplication.isPlaying = true;
            }
        }

        // ------------------------------------------------------------------ recording and screenshots

        [MenuItem(RecordPath, false, 200)]
        private static void ToggleRecording() => RecordingService.Toggle();

        [MenuItem(RecordPath, true)]
        private static bool ToggleRecordingValid()
        {
            Menu.SetChecked(RecordPath, RecordingService.IsRecording);
            return EditorApplication.isPlaying;
        }

        [MenuItem(ShotPath, false, 201)]
        private static void Screenshot() => RecordingService.RequestScreenshot();

        [MenuItem(ShotPath, true)]
        private static bool ScreenshotValid() => EditorApplication.isPlaying;

        [MenuItem(Root + "Gameplay/Open gameplay captures folder", false, 202)]
        private static void OpenFolder()
        {
            string folder = CaptureNaming.OutputFolder();
            Directory.CreateDirectory(folder);
            Process.Start("explorer.exe", folder.Replace('/', '\\'));
        }

        // ------------------------------------------------------------------ toggles

        [MenuItem(CleanPath, false, 300)]
        private static void ToggleClean() => CaptureMenuPrefs.Clean = !CaptureMenuPrefs.Clean;

        [MenuItem(CleanPath, true)]
        private static bool ToggleCleanValid()
        {
            Menu.SetChecked(CleanPath, CaptureMenuPrefs.Clean);
            return true;
        }

        [MenuItem(InvinciblePath, false, 301)]
        private static void ToggleInvincible() => CaptureMenuPrefs.Invincible = !CaptureMenuPrefs.Invincible;

        [MenuItem(InvinciblePath, true)]
        private static bool ToggleInvincibleValid()
        {
            Menu.SetChecked(InvinciblePath, CaptureMenuPrefs.Invincible);
            return true;
        }

        [MenuItem(AutoRecordPath, false, 302)]
        private static void ToggleAutoRecord() => CaptureMenuPrefs.AutoRecord = !CaptureMenuPrefs.AutoRecord;

        [MenuItem(AutoRecordPath, true)]
        private static bool ToggleAutoRecordValid()
        {
            Menu.SetChecked(AutoRecordPath, CaptureMenuPrefs.AutoRecord);
            return true;
        }
    }
}
