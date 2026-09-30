using System.IO;
using BulletHell.Audio;
using BulletHell.Cosmetics;
using BulletHell.Save;
using BulletHell.Settings;
using BulletHell.Telemetry;
using UnityEngine;

namespace BulletHell.Core
{
    /// <summary>
    /// The persistent services (save system, settings, profile, run manager, scene loader, audio stub), created once on a
    /// DontDestroyOnLoad object. The Boot scene's Bootstrapper calls Ensure(); every other scene that needs the
    /// services calls it too, so pressing Play in any scene works without going through Boot.
    /// </summary>
    public sealed class GameServices : MonoBehaviour
    {
        private static GameServices instance;

        public GameConfig Config { get; private set; }
        public ISaveSystem Save { get; private set; }
        public SettingsService Settings { get; private set; }
        public ProfileService Profile { get; private set; }
        public RunManager Run { get; private set; }
        public SceneLoader Scenes { get; private set; }
        public AudioService Audio { get; private set; }
        /// <summary>Music per part of the game and the round / boss / game over stingers, driven by the run state.</summary>
        public AudioDirector Director { get; private set; }
        /// <summary>Local-only playtest log (one CSV row per finished run).</summary>
        public TelemetryService Telemetry { get; private set; }

        /// <summary>Creates the services if they don't exist yet and returns them.</summary>
        public static GameServices Ensure()
        {
            if (instance != null)
                return instance;

            var config = Resources.Load<GameConfig>(GameConfig.ResourcePath);
            if (config == null)
                throw new System.InvalidOperationException(
                    "Assets/Resources/GameConfig.asset is missing. Create it via BulletHell/Create Game Config And Registry.");

            var host = new GameObject("Services");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<GameServices>();
            instance.Init(config);
            return instance;
        }

        // With domain reload disabled, statics survive between Play sessions; forget the destroyed instance.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        private void Init(GameConfig config)
        {
            Config = config;
            string folder = Application.persistentDataPath;
            Save = new LocalFileSaveSystem(Path.Combine(folder, config.SaveFileName));
            Run = new RunManager(config, Save);
            Scenes = new SceneLoader();
            Audio = new AudioService();
            Audio.Initialize(config.AudioLibrary, gameObject);
            Director = new AudioDirector(Audio, Run, config);

            SettingsDefaults defaults = config.SettingsDefaults != null
                ? config.SettingsDefaults
                : ScriptableObject.CreateInstance<SettingsDefaults>();
            Settings = new SettingsService(defaults, new JsonFileStore<SettingsData>(Path.Combine(folder, config.SettingsFileName)), Audio);
            Profile = new ProfileService(config.Registry, new JsonFileStore<ProfileData>(Path.Combine(folder, config.ProfileFileName)));
            Telemetry = new TelemetryService(Run, Path.Combine(folder, config.TelemetryFileName));
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                Settings?.Save();
        }

        private void Update() => Telemetry?.Tick(Time.deltaTime);

        private void OnApplicationQuit()
        {
            Settings?.Save();
            Telemetry?.FlushOnQuit();
        }

        private void OnDestroy()
        {
            Telemetry?.Dispose();
            Director?.Dispose();
        }
    }
}
