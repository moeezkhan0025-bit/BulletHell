using System.IO;
using BulletHell.Save;
using UnityEngine;

namespace BulletHell.Core
{
    /// <summary>
    /// The persistent services (save system, run manager, scene loader, audio stub), created once on a
    /// DontDestroyOnLoad object. The Boot scene's Bootstrapper calls Ensure(); every other scene that needs the
    /// services calls it too, so pressing Play in any scene works without going through Boot.
    /// </summary>
    public sealed class GameServices : MonoBehaviour
    {
        private static GameServices instance;

        public GameConfig Config { get; private set; }
        public ISaveSystem Save { get; private set; }
        public RunManager Run { get; private set; }
        public SceneLoader Scenes { get; private set; }
        public AudioService Audio { get; private set; }

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
            Save = new LocalFileSaveSystem(Path.Combine(Application.persistentDataPath, config.SaveFileName));
            Run = new RunManager(config, Save);
            Scenes = new SceneLoader();
            Audio = new AudioService();
        }
    }
}
