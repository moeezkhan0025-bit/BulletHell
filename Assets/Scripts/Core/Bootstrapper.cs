using UnityEngine;

namespace BulletHell.Core
{
    /// <summary>The only object in the Boot scene: creates the persistent services, then loads the Main Menu.</summary>
    public sealed class Bootstrapper : MonoBehaviour
    {
        private void Start()
        {
            GameServices services = GameServices.Ensure();
            if (Perf.PerfArgs.Has("-perfstress"))
            {
                // Development tools: the stress test skips the menus and starts a new run at the first boss round.
                services.Run.StartNewRun();
                services.Run.State.Round = 3;
                services.Scenes.Load(SceneLoader.Game);
                return;
            }
            services.Scenes.Load(SceneLoader.MainMenu);
        }
    }
}
