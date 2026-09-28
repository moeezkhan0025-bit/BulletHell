using UnityEngine;

namespace BulletHell.Core
{
    /// <summary>The only object in the Boot scene: creates the persistent services, then loads the Main Menu.</summary>
    public sealed class Bootstrapper : MonoBehaviour
    {
        private void Start()
        {
            GameServices services = GameServices.Ensure();
            services.Scenes.Load(SceneLoader.MainMenu);
        }
    }
}
