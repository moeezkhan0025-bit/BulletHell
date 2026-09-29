using System;

namespace BulletHell.Core
{
    public enum GameState { None, RoundIntro, Combat, RoundResults, Shop, Armory, Pause, GameOver }

    /// <summary>
    /// The single state machine for the run loop: RoundIntro -> Combat -> RoundResults -> Shop -> Armory -> RoundIntro (next round).
    /// Pause and GameOver can only be entered from Combat (the intro is short and has no enemies). Pause returns to Combat; GameOver ends the run (the
    /// RunManager resets the machine). Illegal transitions are refused, never silently accepted.
    /// </summary>
    public sealed class GameStateMachine
    {
        public GameState Current { get; private set; } = GameState.None;
        public GameState Previous { get; private set; } = GameState.None;

        /// <summary>Raised after the state changed, with (from, to).</summary>
        public event Action<GameState, GameState> StateChanged;

        public static bool IsAllowed(GameState from, GameState to)
        {
            switch (from)
            {
                case GameState.None: return to == GameState.RoundIntro || to == GameState.Shop;
                case GameState.RoundIntro: return to == GameState.Combat;
                case GameState.Combat: return to == GameState.RoundResults || to == GameState.Pause || to == GameState.GameOver;
                case GameState.RoundResults: return to == GameState.Shop;
                case GameState.Shop: return to == GameState.Armory;
                case GameState.Armory: return to == GameState.RoundIntro;
                case GameState.Pause: return to == GameState.Combat || to == GameState.RoundIntro; // RoundIntro: the debug round skip
                default: return false;
            }
        }

        public bool TryEnter(GameState next)
        {
            if (!IsAllowed(Current, next))
                return false;

            GameState from = Current;
            Previous = from;
            Current = next;
            StateChanged?.Invoke(from, next);
            return true;
        }

        /// <summary>Back to None without raising StateChanged (the run is over or the scene is going away).</summary>
        public void Reset()
        {
            Previous = GameState.None;
            Current = GameState.None;
        }
    }
}
