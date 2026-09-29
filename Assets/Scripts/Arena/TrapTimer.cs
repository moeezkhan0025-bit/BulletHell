using UnityEngine;

namespace BulletHell.Arena
{
    public enum TrapPhase { Idle, Telegraph, Active, Cooldown }

    /// <summary>
    /// The phase clock of one trap: Idle (until started, then for the start delay) -> Telegraph -> Active -> Cooldown ->
    /// Telegraph -> ... Tick returns how many damage strikes fell due: one when Active begins, then one per hit interval
    /// while it lasts (interval 0 = a single strike per activation). Plain code with no engine objects, so it is tested.
    /// </summary>
    public sealed class TrapTimer
    {
        private readonly float startDelay, telegraph, active, cooldown, hitInterval;
        private float timeInPhase;
        private int strikesDone;

        public TrapPhase Phase { get; private set; } = TrapPhase.Idle;
        public bool IsRunning { get; private set; }

        public TrapTimer(float startDelaySeconds, float telegraphSeconds, float activeSeconds, float cooldownSeconds, float hitIntervalSeconds)
        {
            startDelay = Mathf.Max(0f, startDelaySeconds);
            telegraph = Mathf.Max(0f, telegraphSeconds);
            active = Mathf.Max(0f, activeSeconds);
            cooldown = Mathf.Max(0f, cooldownSeconds);
            hitInterval = Mathf.Max(0f, hitIntervalSeconds);
        }

        /// <summary>Seconds into the current phase divided by its length (0..1), for visuals.</summary>
        public float Progress
        {
            get
            {
                float length = LengthOf(Phase);
                return length > 0f ? Mathf.Clamp01(timeInPhase / length) : 1f;
            }
        }

        /// <summary>Back to Idle and stopped: a trap in the round intro (or between rounds) does nothing.</summary>
        public void Reset()
        {
            IsRunning = false;
            Phase = TrapPhase.Idle;
            timeInPhase = 0f;
            strikesDone = 0;
        }

        /// <summary>Begins the cycle: waits out the start delay, then telegraphs.</summary>
        public void Start()
        {
            Reset();
            IsRunning = true;
        }

        /// <summary>Advances the clock. Returns the strikes due during this step.</summary>
        public int Tick(float dt)
        {
            if (!IsRunning || dt <= 0f)
                return 0;

            int strikes = 0;
            timeInPhase += dt;
            for (int guard = 0; guard < 8; guard++)
            {
                float length = LengthOf(Phase);
                if (timeInPhase < length)
                    break;
                if (Phase == TrapPhase.Active)
                    strikes += DueAt(active) - strikesDone;   // strikes that fell before Active ended
                timeInPhase -= length;
                Enter(Next(Phase), ref strikes);
            }

            if (Phase == TrapPhase.Active)
            {
                int due = DueAt(timeInPhase);
                strikes += due - strikesDone;
                strikesDone = due;
            }
            return Mathf.Max(0, strikes);
        }

        private void Enter(TrapPhase next, ref int strikes)
        {
            Phase = next;
            strikesDone = 0;
            if (next == TrapPhase.Active)
            {
                strikes += 1;      // the first strike lands the moment the trap goes off
                strikesDone = 1;
            }
        }

        // Strikes due after `time` seconds of Active: one at 0, then one per interval (never at exactly the end).
        private int DueAt(float time)
        {
            if (hitInterval <= 0f)
                return 1;
            return 1 + Mathf.FloorToInt(Mathf.Max(0f, time - 0.0001f) / hitInterval);
        }

        private static TrapPhase Next(TrapPhase phase)
        {
            switch (phase)
            {
                case TrapPhase.Idle: return TrapPhase.Telegraph;
                case TrapPhase.Telegraph: return TrapPhase.Active;
                case TrapPhase.Active: return TrapPhase.Cooldown;
                default: return TrapPhase.Telegraph;
            }
        }

        private float LengthOf(TrapPhase phase)
        {
            switch (phase)
            {
                case TrapPhase.Idle: return startDelay;
                case TrapPhase.Telegraph: return telegraph;
                case TrapPhase.Active: return active;
                default: return cooldown;
            }
        }
    }
}
