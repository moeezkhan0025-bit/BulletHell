using BulletHell.Save;

namespace BulletHell.Core
{
    /// <summary>
    /// Owns the current run: its RunState, the GameStateMachine, autosave and the round rewards. Scene objects read
    /// State and react to Machine.StateChanged (and must unsubscribe when they are disabled).
    ///
    /// Save timing: the save always describes "the Shop of round R". It is written when entering the Shop and again
    /// when leaving the Armory (before the round number goes up), so Continue always resumes at the Shop.
    /// </summary>
    public sealed class RunManager
    {
        private readonly GameConfig config;
        private readonly ISaveSystem saveSystem;

        public RunState State { get; private set; }
        public GameStateMachine Machine { get; } = new GameStateMachine();
        /// <summary>State the Game scene starts in: Combat for a new run, Shop for a continued one.</summary>
        public GameState PendingStart { get; private set; } = GameState.Combat;
        /// <summary>Currency granted by the round that just ended (for the Round Results screen).</summary>
        public int LastReward { get; private set; }

        public bool HasRun => State != null;

        public RunManager(GameConfig gameConfig, ISaveSystem save)
        {
            config = gameConfig;
            saveSystem = save;
        }

        /// <summary>Starts a fresh run from the game config. Does not touch the save file.</summary>
        public void StartNewRun()
        {
            Machine.Reset();
            State = RunState.NewRun(config);
            PendingStart = GameState.Combat;
            LastReward = 0;
        }

        /// <summary>Loads the save and prepares to resume at its Shop. Returns false when there is no usable save.</summary>
        public bool ContinueRun()
        {
            if (!saveSystem.TryLoad(out SaveData data) ||
                !RunSaveMapper.TryFromSave(data, config.Registry, out RunState loaded))
                return false;

            Machine.Reset();
            State = loaded;
            PendingStart = GameState.Shop;
            LastReward = 0;
            return true;
        }

        /// <summary>Pressing Play in the Game scene with no run in progress: start a new one.</summary>
        public void EnsureRun()
        {
            if (State == null)
                StartNewRun();
        }

        /// <summary>Called by the Game scene once its objects are ready: enters the pending start state.</summary>
        public void BeginGame()
        {
            Machine.Reset();
            Machine.TryEnter(PendingStart);
        }

        /// <summary>Combat is over (stub: all test enemies dead). Banks the reward and shows Round Results.</summary>
        public bool CombatCleared()
        {
            if (Machine.Current != GameState.Combat)
                return false;
            LastReward = config.RoundReward(State.Round);
            State.Currency += LastReward;
            return Machine.TryEnter(GameState.RoundResults);
        }

        /// <summary>The Continue button of the current between-rounds screen.</summary>
        public void Advance()
        {
            switch (Machine.Current)
            {
                case GameState.RoundResults:
                    SaveRun();
                    Machine.TryEnter(GameState.Shop);
                    break;
                case GameState.Shop:
                    Machine.TryEnter(GameState.Armory);
                    break;
                case GameState.Armory:
                    SaveRun();
                    State.Round++;
                    Machine.TryEnter(GameState.Combat);
                    break;
            }
        }

        public bool SetPaused(bool paused)
        {
            return paused ? Machine.TryEnter(GameState.Pause) : Machine.Current == GameState.Pause && Machine.TryEnter(GameState.Combat);
        }

        /// <summary>The run was lost: roguelike rules, the save is deleted.</summary>
        public void GameOver()
        {
            Machine.TryEnter(GameState.GameOver);
            saveSystem.Delete();
            AbandonRun();
        }

        /// <summary>Leaves the run without touching the save (back to the menu).</summary>
        public void AbandonRun()
        {
            Machine.Reset();
            State = null;
        }

        public void SaveRun()
        {
            if (State != null)
                saveSystem.Save(RunSaveMapper.ToSave(State));
        }
    }
}
