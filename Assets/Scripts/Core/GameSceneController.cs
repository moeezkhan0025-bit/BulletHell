using BulletHell.Input;
using UnityEngine;

namespace BulletHell.Core
{
    /// <summary>
    /// Root of the Game scene. Makes sure the services and a run exist (so pressing Play directly in this scene works,
    /// as a fresh run), enters the run's start state once every scene object is ready, and lets the game run only during
    /// Combat: outside it (results, shop, armory, pause, game over) input is off and time is frozen.
    /// </summary>
    [DefaultExecutionOrder(-100)] // before the player components read the RunState in their Awake
    public sealed class GameSceneController : MonoBehaviour
    {
        [SerializeField] private GameplayInputReader input;

        private RunManager run;
        private int stateChangeFrame = -1;

        private void Awake()
        {
            run = GameServices.Ensure().Run;
            run.EnsureRun();
        }

        private void OnEnable()
        {
            run.Machine.StateChanged += OnStateChanged;
            input.PausePressed += OnPausePressed;
        }

        private void OnDisable()
        {
            run.Machine.StateChanged -= OnStateChanged;
            input.PausePressed -= OnPausePressed;
        }

        private void OnDestroy() => Time.timeScale = 1f;

        private void Start() => run.BeginGame();

        private void OnStateChanged(GameState from, GameState to)
        {
            stateChangeFrame = Time.frameCount;
            bool combat = to == GameState.Combat;
            input.enabled = combat;
            Time.timeScale = combat ? 1f : 0f;
        }

        // The Start press that just left a menu screen (or paused) must not also pause the round it started.
        private void OnPausePressed()
        {
            if (stateChangeFrame != Time.frameCount)
                run.SetPaused(true);
        }

        /// <summary>Back to the Main Menu. The save stays; Continue there resumes the run at its last Shop.</summary>
        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            GameServices services = GameServices.Ensure();
            services.Run.AbandonRun();
            services.Scenes.Load(SceneLoader.MainMenu);
        }
    }
}
