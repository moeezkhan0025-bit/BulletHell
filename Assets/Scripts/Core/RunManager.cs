using BulletHell.Save;
using BulletHell.Telemetry;

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
        /// <summary>State the Game scene starts in: RoundIntro for a new run, Shop for a continued one.</summary>
        public GameState PendingStart { get; private set; } = GameState.RoundIntro;
        /// <summary>Currency banked by the round that just ended (for the Round Results screen).</summary>
        public int LastReward { get; private set; }
        /// <summary>Currency picked up so far this round. It is banked into the RunState when the round is cleared.</summary>
        public int RoundEarnings { get; private set; }

        /// <summary>
        /// Raised (with the round number) when a round's intro starts: new run, next round, or a debug skip. Listeners
        /// reset for the round (clear bullets, enemies and coins, refill health); enemies and traps stay idle.
        /// </summary>
        public event System.Action<int> RoundIntroStarted;

        /// <summary>Raised (with the round number) when the intro ends and the round's combat begins: waves start now.</summary>
        public event System.Action<int> RoundStarted;

        /// <summary>A run began (false = New Game, true = Continue). The playtest telemetry starts its record here.</summary>
        public event System.Action<bool> RunStarted;

        /// <summary>The run ended: the player died, or left it (back to the menu, or the game closed). Raised once per run.</summary>
        public event System.Action<RunState, RunEndReason> RunEnded;

        /// <summary>A debug tool touched this run (round skip or preview, currency grant, god mode, a debug start round). Telemetry flags such runs.</summary>
        public bool UsedDebug { get; private set; }

        public void MarkDebug() => UsedDebug = true;

        public bool HasRun => State != null;

        /// <summary>Debug: the current round was started as a layout preview - no waves spawn and traps stay idle.</summary>
        public bool IsLayoutPreview { get; private set; }

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
            if (UnityEngine.Debug.isDebugBuild && config.DebugStartRound > 1)
                State.Round = config.DebugStartRound;   // debug: skip ahead (e.g. straight to the boss)
            UsedDebug = UnityEngine.Debug.isDebugBuild && config.DebugStartRound > 1;
            PendingStart = GameState.RoundIntro;
            LastReward = 0;
            RoundEarnings = 0;
            RunStarted?.Invoke(false);
        }

        /// <summary>Loads the save and prepares to resume at its Shop. Returns false when there is no usable save.</summary>
        public bool ContinueRun()
        {
            if (!saveSystem.TryLoad(out SaveData data) ||
                !RunSaveMapper.TryFromSave(data, config.Registry, out RunState loaded))
                return false;

            Machine.Reset();
            State = loaded;
            if (State.Shop == null)
                State.Shop = BulletHell.Shop.ShopVisit.Create(NewShopSeed()); // an older save without a saved visit
            PendingStart = GameState.Shop;
            LastReward = 0;
            RoundEarnings = 0;
            UsedDebug = false;
            RunStarted?.Invoke(true);
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
            if (Machine.TryEnter(PendingStart) && PendingStart == GameState.RoundIntro)
                StartRoundIntro();
        }

        /// <summary>The intro's countdown is over: enter Combat and start the round's waves.</summary>
        public bool BeginCombat()
        {
            if (Machine.Current != GameState.RoundIntro || !Machine.TryEnter(GameState.Combat))
                return false;
            RoundStarted?.Invoke(State.Round);
            return true;
        }

        /// <summary>A coin was picked up: it counts toward this round's earnings.</summary>
        public void AddEarnings(int amount)
        {
            if (amount > 0)
                RoundEarnings += amount;
        }

        /// <summary>The last wave is cleared. Banks the round's earnings into the run's currency and shows Round Results.</summary>
        public bool CombatCleared()
        {
            if (Machine.Current != GameState.Combat)
                return false;
            LastReward = RoundEarnings;
            State.Currency += RoundEarnings;
            RoundEarnings = 0;
            return Machine.TryEnter(GameState.RoundResults);
        }

        /// <summary>
        /// Debug: restart combat at any round. Only allowed from Pause (it is the pause screen's debug option); the run's
        /// loadout and currency are untouched, this round's uncollected earnings are dropped.
        /// </summary>
        public bool DebugSkipToRound(int round) => DebugEnterRound(round, false);

        /// <summary>
        /// Debug: show any round's arena layout to walk and jump around in. Like <see cref="DebugSkipToRound"/> but no
        /// enemies spawn and traps stay idle, and the round never ends; pause and skip again to leave.
        /// </summary>
        public bool DebugPreviewLayout(int round) => DebugEnterRound(round, true);

        private bool DebugEnterRound(int round, bool preview)
        {
            if (Machine.Current != GameState.Pause)
                return false;
            UsedDebug = true;
            State.Round = System.Math.Max(1, round);
            if (!Machine.TryEnter(GameState.RoundIntro))
                return false;
            StartRoundIntro();
            IsLayoutPreview = preview;   // after the intro event, which resets it
            return true;
        }

        private void StartRoundIntro()
        {
            IsLayoutPreview = false;
            RoundEarnings = 0;
            RoundIntroStarted?.Invoke(State.Round);
        }

        /// <summary>The Continue button of the current between-rounds screen.</summary>
        public void Advance()
        {
            switch (Machine.Current)
            {
                case GameState.RoundResults:
                    State.Shop = BulletHell.Shop.ShopVisit.Create(NewShopSeed()); // a new visit: new stock, no rerolls yet
                    SaveRun();
                    Machine.TryEnter(GameState.Shop);
                    break;
                case GameState.Shop:
                    Machine.TryEnter(GameState.Armory);
                    break;
                case GameState.Armory:
                    SaveRun();
                    State.Round++;
                    if (Machine.TryEnter(GameState.RoundIntro))
                        StartRoundIntro();
                    break;
            }
        }

        public bool SetPaused(bool paused)
        {
            return paused ? Machine.TryEnter(GameState.Pause) : Machine.Current == GameState.Pause && Machine.TryEnter(GameState.Combat);
        }

        /// <summary>
        /// The run was lost: roguelike rules, the save is deleted. The RunState stays so the Game Over screen can show it;
        /// leaving to the menu (AbandonRun) clears it.
        /// </summary>
        public void GameOver()
        {
            if (!Machine.TryEnter(GameState.GameOver))
                return;
            saveSystem.Delete();
            RunEnded?.Invoke(State, RunEndReason.Died);
        }

        /// <summary>Leaves the run without touching the save (back to the menu).</summary>
        public void AbandonRun()
        {
            if (State != null && Machine.Current != GameState.GameOver)
                RunEnded?.Invoke(State, RunEndReason.Quit);
            Machine.Reset();
            State = null;
        }

        private static int NewShopSeed() => UnityEngine.Random.Range(1, int.MaxValue);

        public void SaveRun()
        {
            if (State != null)
                saveSystem.Save(RunSaveMapper.ToSave(State));
        }
    }
}
