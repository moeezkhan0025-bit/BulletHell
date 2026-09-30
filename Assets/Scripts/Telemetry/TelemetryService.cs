using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BulletHell.Bosses;
using BulletHell.Core;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Telemetry
{
    /// <summary>
    /// Local-only playtest telemetry. Watches the run (rounds, combat time, damage, shop spending, boss phases) and, when a run ends
    /// (the player dies, or leaves it / closes the game), appends one row to a CSV in the save folder. Nothing is sent anywhere.
    /// A failure to write is a warning, never an error in the game. Combat seconds are counted by <see cref="Tick"/>, called once
    /// per frame by <see cref="GameServices"/>.
    /// </summary>
    public sealed class TelemetryService : IDisposable
    {
        private readonly RunManager run;
        private readonly string path;
        private RunRecord current;
        private string lastDamageSource;
        private readonly StringBuilder lineBuffer = new StringBuilder(256);

        /// <summary>The CSV file this service writes (it may not exist until the first run ends).</summary>
        public string CsvPath => path;

        public bool RunInProgress => current != null;

        public TelemetryService(RunManager runManager, string csvPath)
        {
            run = runManager;
            path = csvPath;
            run.RunStarted += OnRunStarted;
            run.RunEnded += OnRunEnded;
            run.RoundStarted += OnRoundStarted;
            run.Machine.StateChanged += OnStateChanged;
            TelemetryEvents.PlayerDamaged += OnDamaged;
            TelemetryEvents.Spent += OnSpent;
            TelemetryEvents.ItemAcquired += OnItem;
            BossEvents.Spawned += OnBossSpawned;
            BossEvents.PhaseChanged += OnBossPhase;
            BossEvents.Defeated += OnBossDefeated;
        }

        public void Dispose()
        {
            run.RunStarted -= OnRunStarted;
            run.RunEnded -= OnRunEnded;
            run.RoundStarted -= OnRoundStarted;
            run.Machine.StateChanged -= OnStateChanged;
            TelemetryEvents.PlayerDamaged -= OnDamaged;
            TelemetryEvents.Spent -= OnSpent;
            TelemetryEvents.ItemAcquired -= OnItem;
            BossEvents.Spawned -= OnBossSpawned;
            BossEvents.PhaseChanged -= OnBossPhase;
            BossEvents.Defeated -= OnBossDefeated;
        }

        /// <summary>Adds the frame to the round's combat time while the fight is on (not paused, not a layout preview).</summary>
        public void Tick(float deltaTime)
        {
            if (current == null || run.State == null || run.IsLayoutPreview || run.Machine.Current != GameState.Combat)
                return;
            RunRecord.Add(current.RoundSeconds, run.State.Round, deltaTime);
        }

        /// <summary>The game is closing: a run still in progress is logged as quit.</summary>
        public void FlushOnQuit()
        {
            if (current != null && run.State != null)
                Finish(run.State, RunEndReason.Quit);
        }

        // ------------------------------------------------------------------ run lifecycle

        private void OnRunStarted(bool continued)
        {
            if (current != null && run.State != null)
                Finish(run.State, RunEndReason.Quit);   // a new run began without the old one being closed
            lastDamageSource = null;
            current = new RunRecord
            {
                RunId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + UnityEngine.Random.Range(0x1000, 0xFFFF).ToString("x"),
                StartedUtc = DateTime.UtcNow.ToString("o"),
                Build = Application.isEditor ? "editor" : Debug.isDebugBuild ? "dev" : "release",
                Version = Application.version,
                Continued = continued,
                RoundReached = run.State != null ? run.State.Round : 1,
            };
        }

        private void OnRunEnded(RunState state, RunEndReason reason)
        {
            if (current != null)
                Finish(state, reason);
        }

        private void OnRoundStarted(int round)
        {
            if (current != null)
                current.RoundReached = Math.Max(current.RoundReached, round);
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            if (current != null && to == GameState.RoundResults)
            {
                current.RoundsCleared++;
                current.CurrencyEarned += run.LastReward;
            }
        }

        private void OnDamaged(float amount, string source)
        {
            if (current == null || run.State == null)
                return;
            RunRecord.Add(current.RoundDamage, run.State.Round, amount);
            lastDamageSource = string.IsNullOrEmpty(source) ? "Unknown" : source;
        }

        private void OnSpent(int amount, SpendKind kind)
        {
            if (current == null)
                return;
            current.CurrencySpent += amount;
            if (kind == SpendKind.Reroll)
                current.Rerolls++;
            else if (kind == SpendKind.Crate)
                current.CratesOpened++;
        }

        private void OnItem(ItemKind kind, string name)
        {
            if (current == null)
                return;
            if (kind == ItemKind.Arm)
                current.ArmsBought++;
            else
                current.ArmamentsBought++;
            current.Items.Add(name);
        }

        private void OnBossSpawned(BossController boss)
        {
            if (current == null)
                return;
            current.BossReached = boss.Data.DisplayName;
            current.BossPhaseMax = Math.Max(current.BossPhaseMax, boss.PhaseIndex + 1);
        }

        private void OnBossPhase(BossController boss)
        {
            if (current != null)
                current.BossPhaseMax = Math.Max(current.BossPhaseMax, boss.PhaseIndex + 1);
        }

        private void OnBossDefeated(BossController boss)
        {
            if (current != null)
                current.BossDefeated = true;
        }

        // ------------------------------------------------------------------ writing

        private void Finish(RunState state, RunEndReason reason)
        {
            RunRecord record = current;
            current = null;
            if (record == null)
                return;
            // A run quit before any fight was played (Main Menu straight after New Game) says nothing.
            if (reason == RunEndReason.Quit && record.CombatSeconds < 1f && record.RoundsCleared == 0)
                return;

            record.EndedUtc = DateTime.UtcNow.ToString("o");
            record.Result = reason == RunEndReason.Died ? "died" : "quit";
            record.CauseOfDeath = reason == RunEndReason.Died ? (lastDamageSource ?? "Unknown") : "quit";
            record.DebugUsed = run.UsedDebug;
            record.RoundReached = Math.Max(record.RoundReached, state.Round);
            record.CurrencyEnd = state.Currency;
            FillLoadout(record, state);
            Write(record);
        }

        private static void FillLoadout(RunRecord record, RunState state)
        {
            var parts = new List<string>();
            int equipped = 0;
            for (int slot = 0; slot < state.Loadout.Length; slot++)
            {
                ArmInstance arm = state.Loadout[slot];
                if (arm == null)
                    continue;
                var names = new List<string>();
                for (int i = 0; i < arm.SlotCount; i++)
                {
                    ArmamentData armament = arm.GetArmament(i);
                    if (armament != null)
                        names.Add(armament.DisplayName);
                }
                equipped += names.Count;
                parts.Add(arm.Data.DisplayName + "[" + string.Join("+", names) + "]");
            }
            record.ArmamentsEquipped = equipped;
            record.Loadout = string.Join("|", parts);
        }

        private void Write(RunRecord record)
        {
            try
            {
                lineBuffer.Clear();
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                    lineBuffer.Append(TelemetryCsv.Header).Append('\n');
                lineBuffer.Append(TelemetryCsv.ToLine(record)).Append('\n');
                File.AppendAllText(path, lineBuffer.ToString(), new UTF8Encoding(false));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Playtest telemetry could not write " + path + ": " + exception.Message);
            }
        }

        /// <summary>Every logged run (empty when the file is missing or unreadable).</summary>
        public List<RunRecord> ReadAll()
        {
            try
            {
                return File.Exists(path) ? TelemetryCsv.Parse(File.ReadAllText(path, Encoding.UTF8)) : new List<RunRecord>();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Playtest telemetry could not read " + path + ": " + exception.Message);
                return new List<RunRecord>();
            }
        }
    }
}
