using BulletHell.Input;
using UnityEngine;

namespace BulletHell.Core
{
    /// <summary>
    /// Root of the Game scene. Makes sure the services and a run exist (so pressing Play directly in this scene works,
    /// as a fresh run), enters the run's start state once every scene object is ready, and lets the player act only
    /// during Combat.
    /// </summary>
    [DefaultExecutionOrder(-100)] // before the player components read the RunState in their Awake
    public sealed class GameSceneController : MonoBehaviour
    {
        [SerializeField] private GameplayInputReader input;

        private RunManager run;

        private void Awake()
        {
            run = GameServices.Ensure().Run;
            run.EnsureRun();
        }

        private void OnEnable() => run.Machine.StateChanged += OnStateChanged;

        private void OnDisable() => run.Machine.StateChanged -= OnStateChanged;

        private void Start() => run.BeginGame();

        private void OnStateChanged(GameState from, GameState to) => input.enabled = to == GameState.Combat;

        /// <summary>Back to the Main Menu. The save stays; Continue there resumes the run at its last Shop.</summary>
        public void QuitToMenu()
        {
            GameServices services = GameServices.Ensure();
            services.Run.AbandonRun();
            services.Scenes.Load(SceneLoader.MainMenu);
        }
    }
}
