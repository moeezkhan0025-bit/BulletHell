using UnityEngine;

namespace BulletHell.Core
{
    /// <summary>
    /// The one owner of Time.timeScale during a run. Time runs only in the round intro and Combat (menus, pause, shop and
    /// results freeze it) and Combat can additionally be frozen for a moment by a hitstop. The hitstop timer runs on
    /// unscaled time, is ignored outside Combat, and is cleared by every state change, so it can never freeze menus,
    /// outlive a pause, or leave the game stuck slow. Input and the state machine don't depend on timeScale at all.
    /// </summary>
    public sealed class HitstopClock
    {
        private GameState state = GameState.None;
        private float remaining;
        private float stopScale;

        /// <summary>What Time.timeScale should be right now.</summary>
        public float Scale
        {
            get
            {
                if (state != GameState.Combat && state != GameState.RoundIntro)
                    return 0f;
                return remaining > 0f ? stopScale : 1f;
            }
        }

        public bool IsHitstopping => remaining > 0f && state == GameState.Combat;

        /// <summary>The run entered a new state: hitstops never carry across states.</summary>
        public void SetState(GameState next)
        {
            state = next;
            remaining = 0f;
        }

        /// <summary>Freezes gameplay to `scale` (0 = fully) for `seconds` of real time. Only Combat honours it; a longer stop wins.</summary>
        public bool Request(float seconds, float scale)
        {
            if (state != GameState.Combat || seconds <= 0f)
                return false;
            if (seconds > remaining)
            {
                remaining = seconds;
                stopScale = Mathf.Clamp01(scale);
            }
            return true;
        }

        /// <summary>Counts the hitstop down in real time.</summary>
        public void Tick(float unscaledDeltaTime)
        {
            if (remaining > 0f)
                remaining = Mathf.Max(0f, remaining - unscaledDeltaTime);
        }
    }

    /// <summary>Static access to the run's HitstopClock, and the only place that writes Time.timeScale for it.</summary>
    public static class GameClock
    {
        private static readonly HitstopClock Clock = new HitstopClock();

        public static bool IsHitstopping => Clock.IsHitstopping;

        public static void SetState(GameState state)
        {
            Clock.SetState(state);
            Apply();
        }

        /// <summary>Requests a hitstop (only honoured in Combat). Feedback code calls this, never Time.timeScale.</summary>
        public static void Hitstop(float seconds, float scale = 0f)
        {
            if (Clock.Request(seconds, scale))
                Apply();
        }

        /// <summary>Called every frame by the Game scene controller.</summary>
        public static void Tick()
        {
            if (!Clock.IsHitstopping)
                return;
            Clock.Tick(Time.unscaledDeltaTime);
            Apply();
        }

        /// <summary>Leaving the Game scene: back to normal time.</summary>
        public static void Reset()
        {
            Clock.SetState(GameState.None);
            Time.timeScale = 1f;
        }

        private static void Apply() => Time.timeScale = Clock.Scale;
    }
}
