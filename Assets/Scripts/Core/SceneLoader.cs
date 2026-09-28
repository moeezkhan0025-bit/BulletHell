using UnityEngine.SceneManagement;

namespace BulletHell.Core
{
    /// <summary>Loads scenes by name. Kept behind one class so async loading and transitions can be added later.</summary>
    public sealed class SceneLoader
    {
        public const string Boot = "Boot";
        public const string MainMenu = "MainMenu";
        public const string Game = "Game";

        public void Load(string sceneName) => SceneManager.LoadScene(sceneName);
    }
}
