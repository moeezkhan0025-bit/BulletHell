using BulletHell.AI;
using BulletHell.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BulletHell.Perf
{
    /// <summary>
    /// Owner of the performance tools: one persistent object in development builds and the editor (never in release
    /// builds). Records a frame every Update for the overlay and the logger, toggles the overlay (F8), starts the stress
    /// test (F10 or -perfstress) and binds to the Game scene when it loads. Created by a runtime-init hook, so no scene needs it.
    /// </summary>
    public sealed class PerfHost : MonoBehaviour
    {
        private static PerfHost instance;

        public PerfStats Stats { get; private set; }
        public PerfLogger Logger { get; private set; }
        public PerfOverlay Overlay { get; private set; }
        public StressTest Stress { get; private set; }

        public static PerfHost Instance => instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            BossBehavior.DebugStress = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (!Debug.isDebugBuild || instance != null)
                return;
            var go = new GameObject("PerfTools");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<PerfHost>();
        }

        /// <summary>F8: show or hide the performance overlay.</summary>
        public static void ToggleOverlay()
        {
            if (instance != null)
                instance.Overlay.Toggle();
        }

        /// <summary>F10: start the stress test in the Game scene (or stop a running one).</summary>
        public static void ToggleStress()
        {
            if (instance == null)
                return;
            if (instance.Stress != null && instance.Stress.Running)
                Destroy(instance.Stress);   // OnDisable stops the logger and restores everything
            else
                instance.BeginStress(PerfArgs.Value("-perflabel", "run"), PerfArgs.Int("-perfseconds", 90), PerfArgs.Int("-perfenemies", 80),
                                     PerfArgs.Value("-perfvsync", "0"), false);
        }

        /// <summary>Starts the stress test with explicit settings (tools and tests). vsync: "0", "1" or "default".</summary>
        public static bool StartStress(string label, int seconds, int enemies, string vsync, bool quitWhenDone) =>
            instance != null && instance.BeginStress(label, seconds, enemies, vsync, quitWhenDone);

        private void Awake()
        {
            Stats = new PerfStats();
            Logger = new PerfLogger();
            Overlay = gameObject.AddComponent<PerfOverlay>();
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (PerfArgs.Has("-perfoverlay"))
                Overlay.SetVisible(true);
            if (PerfArgs.Has("-perfdiscover"))
                PerfLogger.DumpAvailableStats();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Stats?.Dispose();
            Logger?.Dispose();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneLoader.Game)
                return;
            Stats.Bind();
            if (PerfArgs.Has("-perfstress") && (Stress == null || !Stress.Running))
                BeginStress(PerfArgs.Value("-perflabel", "run"), PerfArgs.Int("-perfseconds", 90), PerfArgs.Int("-perfenemies", 80),
                            PerfArgs.Value("-perfvsync", "0"), !PerfArgs.Has("-perfnoquit"));
        }

        private bool BeginStress(string label, int seconds, int enemies, string vsync, bool quitWhenDone)
        {
            if (Stress == null)
                Stress = gameObject.AddComponent<StressTest>();
            return Stress.Begin(this, label, seconds, enemies, vsync, quitWhenDone);
        }

        private void Update()
        {
            using var _ = PerfMarkers.PerfTools.Auto();
            float dt = Time.unscaledDeltaTime;
            Stats.Record(dt);
            if (Logger.Running)
            {
                int state = GameServices.Ensure().Run.Machine.Current == GameState.None ? 0 : (int)GameServices.Ensure().Run.Machine.Current;
                Logger.Sample(dt, Stats, Time.timeScale > 0f ? 1 : 0, state);
            }
            Overlay.Tick(Stats);
        }
    }
}
