using System.Collections.Generic;
using System.IO;
using BulletHell.Core;
using BulletHell.Save;
using BulletHell.Telemetry;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    /// <summary>D3 playtest telemetry: the CSV format, the summary maths, and the service that turns a run into a row.</summary>
    public class TelemetryTests
    {
        private sealed class FakeSave : ISaveSystem
        {
            public bool HasSave => true;
            public bool TryLoad(out SaveData data) { data = null; return false; }
            public void Save(SaveData data) { }
            public void Delete() { }
        }

        private static RunRecord Record(string result, int round, string cause = "", bool debug = false, params float[] seconds)
        {
            var r = new RunRecord { RunId = "r" + round, Result = result, CauseOfDeath = cause, RoundReached = round, DebugUsed = debug };
            r.RoundSeconds.AddRange(seconds);
            foreach (float s in seconds)
                r.RoundDamage.Add(s > 30f ? 2f : 1f);
            return r;
        }

        // ---- CSV

        [Test]
        public void ARowSurvivesAWriteAndAReadIncludingCommasAndQuotes()
        {
            var record = new RunRecord
            {
                RunId = "20260930-120000-abcd", StartedUtc = "2026-09-30T12:00:00Z", EndedUtc = "2026-09-30T12:09:00Z", Build = "editor",
                Version = "1.0", Continued = true, DebugUsed = false, Result = "died", CauseOfDeath = "Tomato, \"Shielded\" (contact)",
                RoundReached = 4, RoundsCleared = 3, CurrencyEarned = 410, CurrencySpent = 350, CurrencyEnd = 60,
                ArmsBought = 1, ArmamentsBought = 2, Rerolls = 3, CratesOpened = 1, ArmamentsEquipped = 2,
                Loadout = "Red[Homing+Pierce]|Blue[]", BossReached = "Pumpking", BossPhaseMax = 2, BossDefeated = false,
            };
            record.RoundSeconds.AddRange(new[] { 31.5f, 40.25f, 52f, 12f });
            record.RoundDamage.AddRange(new[] { 0f, 1f, 2f, 3f });
            record.Items.AddRange(new[] { "Homing", "Pierce, Mk2", "Red Arm" });

            string text = TelemetryCsv.Header + "\n" + TelemetryCsv.ToLine(record) + "\n";
            List<RunRecord> read = TelemetryCsv.Parse(text);

            Assert.AreEqual(1, read.Count);
            RunRecord back = read[0];
            Assert.AreEqual(record.CauseOfDeath, back.CauseOfDeath);
            Assert.AreEqual(4, back.RoundReached);
            Assert.AreEqual(3, back.RoundsCleared);
            Assert.AreEqual(new[] { 31.5f, 40.3f, 52f, 12f }.Length, back.RoundSeconds.Count);
            Assert.AreEqual(31.5f, back.RoundSeconds[0], 0.01f);
            Assert.AreEqual(3f, back.RoundDamage[3], 0.01f);
            Assert.AreEqual(410, back.CurrencyEarned);
            Assert.AreEqual(350, back.CurrencySpent);
            Assert.AreEqual(3, back.ItemsBought);
            Assert.AreEqual("Pierce, Mk2", back.Items[1]);
            Assert.AreEqual("Red[Homing+Pierce]|Blue[]", back.Loadout);
            Assert.AreEqual("Pumpking", back.BossReached);
            Assert.AreEqual(2, back.BossPhaseMax);
            Assert.IsTrue(back.Continued);
            Assert.IsFalse(back.DebugUsed);
        }

        [Test]
        public void TheHeaderNamesEveryColumnTheRowFills()
        {
            string line = TelemetryCsv.ToLine(new RunRecord());
            Assert.AreEqual(TelemetryCsv.Columns.Length, TelemetryCsv.Split(line)[0].Count);
            StringAssert.Contains("cause_of_death", TelemetryCsv.Header);
            StringAssert.Contains("round_seconds", TelemetryCsv.Header);
            StringAssert.Contains("round_damage", TelemetryCsv.Header);
            StringAssert.Contains("currency_earned", TelemetryCsv.Header);
            StringAssert.Contains("currency_spent", TelemetryCsv.Header);
            StringAssert.Contains("items_bought", TelemetryCsv.Header);
            StringAssert.Contains("armaments_equipped", TelemetryCsv.Header);
            StringAssert.Contains("boss_phase_max", TelemetryCsv.Header);
        }

        [Test]
        public void AFileFromBeforeAColumnWasAddedStillReads()
        {
            const string text = "run_id,result,round_reached\nabc,died,5\n";
            List<RunRecord> read = TelemetryCsv.Parse(text);
            Assert.AreEqual(1, read.Count);
            Assert.AreEqual(5, read[0].RoundReached);
            Assert.AreEqual(0, read[0].CurrencySpent);
        }

        // ---- summary

        [Test]
        public void TheSummaryAveragesAndCountsTheDistribution()
        {
            var records = new List<RunRecord>
            {
                Record("died", 2, "Tomato (contact)", false, 20f, 10f),
                Record("died", 3, "Tomato (contact)", false, 20f, 20f, 20f),
                Record("died", 3, "Pumpking smash", false, 20f, 20f, 40f),
                Record("quit", 4, "quit", false, 20f, 20f, 20f, 20f),
                Record("died", 7, "Enemy bullet", true, 99f),   // used a debug tool: left out
            };

            TelemetrySummary s = TelemetrySummary.Compute(records);

            Assert.AreEqual(5, s.Logged);
            Assert.AreEqual(4, s.Counted);
            Assert.AreEqual(1, s.DebugExcluded);
            Assert.AreEqual(3, s.Died);
            Assert.AreEqual(1, s.Quit);
            Assert.AreEqual(3f, s.RoundReached.Mean, 0.001f);       // (2 + 3 + 3 + 4) / 4
            Assert.AreEqual(3f, s.RoundReached.Median, 0.001f);
            Assert.AreEqual(4f, s.RoundReached.Max, 0.001f);
            Assert.AreEqual(2, s.RoundHistogram[3]);
            Assert.AreEqual("Tomato (contact)", s.Causes[0].Key);   // most common first; the quit is not a cause
            Assert.AreEqual(2, s.Causes[0].Value);
            Assert.AreEqual(2, s.Causes.Count);
            // round 1: 20, 20, 20, 20 over 4 runs; round 4 only one run played it
            Assert.AreEqual(4, s.PerRound[0].Runs);
            Assert.AreEqual(20f, s.PerRound[0].Seconds, 0.001f);
            Assert.AreEqual(1, s.PerRound[3].Runs);
            Assert.AreEqual(20f, s.PerRound[3].Seconds, 0.001f);

            Assert.AreEqual(5, TelemetrySummary.Compute(records, includeDebug: true).Counted);
        }

        [Test]
        public void AnEmptyLogSummarisesToZeros()
        {
            TelemetrySummary s = TelemetrySummary.Compute(new List<RunRecord>());
            Assert.AreEqual(0, s.Counted);
            Assert.AreEqual(0f, s.RoundReached.Mean);
            Assert.IsEmpty(s.Causes);
            Assert.IsEmpty(s.PerRound);
        }

        // ---- service

        private static string TempCsv() => Path.Combine(Path.GetTempPath(), "telemetry_" + System.Guid.NewGuid().ToString("N") + ".csv");

        private static RunManager NewRun()
        {
            var run = new RunManager(ScriptableObject.CreateInstance<GameConfig>(), new FakeSave());
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no New Run Loadout"));
            return run;
        }

        [Test]
        public void ADeathWritesOneRowWithTheRunsNumbers()
        {
            string path = TempCsv();
            RunManager run = NewRun();
            var service = new TelemetryService(run, path);
            try
            {
                run.StartNewRun();
                run.BeginGame();
                run.BeginCombat();
                service.Tick(12.5f);
                TelemetryEvents.RaisePlayerDamaged(1f, "Tomato (contact)");
                TelemetryEvents.RaisePlayerDamaged(1f, "Enemy bullet");
                TelemetryEvents.RaiseSpent(40, SpendKind.Item);
                TelemetryEvents.RaiseItemAcquired(ItemKind.Armament, "Homing");
                TelemetryEvents.RaiseSpent(15, SpendKind.Reroll);

                run.GameOver();

                Assert.IsTrue(File.Exists(path));
                string[] lines = File.ReadAllLines(path);
                Assert.AreEqual(2, lines.Length);   // header + one run
                RunRecord row = TelemetryCsv.Parse(File.ReadAllText(path))[0];
                Assert.AreEqual("died", row.Result);
                Assert.AreEqual("Enemy bullet", row.CauseOfDeath);   // the last thing that hit
                Assert.AreEqual(1, row.RoundReached);
                Assert.AreEqual(12.5f, row.CombatSeconds, 0.1f);
                Assert.AreEqual(2f, row.DamageTotal, 0.001f);
                Assert.AreEqual(55, row.CurrencySpent);
                Assert.AreEqual(1, row.ItemsBought);
                Assert.AreEqual(1, row.Rerolls);
                Assert.AreEqual("editor", row.Build);

                run.AbandonRun();   // leaving after game over must not log the run again
                Assert.AreEqual(2, File.ReadAllLines(path).Length);
            }
            finally
            {
                service.Dispose();
                File.Delete(path);
            }
        }

        [Test]
        public void LeavingARunMidwayLogsItAsQuitButAnEmptyOneIsNotLogged()
        {
            string path = TempCsv();
            RunManager run = NewRun();
            var service = new TelemetryService(run, path);
            try
            {
                run.StartNewRun();
                run.BeginGame();
                run.AbandonRun();   // straight back to the menu: nothing was played
                Assert.IsFalse(File.Exists(path));

                UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no New Run Loadout"));
                run.StartNewRun();
                run.BeginGame();
                run.BeginCombat();
                service.Tick(30f);
                run.AbandonRun();

                RunRecord row = TelemetryCsv.Parse(File.ReadAllText(path))[0];
                Assert.AreEqual("quit", row.Result);
                Assert.AreEqual("quit", row.CauseOfDeath);
                Assert.AreEqual(30f, row.CombatSeconds, 0.1f);
            }
            finally
            {
                service.Dispose();
                File.Delete(path);
            }
        }

        [Test]
        public void PausedTimeAndDebugRunsAreKeptApart()
        {
            string path = TempCsv();
            RunManager run = NewRun();
            var service = new TelemetryService(run, path);
            try
            {
                run.StartNewRun();
                run.BeginGame();
                run.BeginCombat();
                service.Tick(10f);
                run.SetPaused(true);
                service.Tick(99f);          // the pause screen: not combat time
                run.SetPaused(false);
                run.MarkDebug();
                run.GameOver();

                RunRecord row = TelemetryCsv.Parse(File.ReadAllText(path))[0];
                Assert.AreEqual(10f, row.CombatSeconds, 0.1f);
                Assert.IsTrue(row.DebugUsed);
                Assert.AreEqual(0, TelemetrySummary.Compute(new List<RunRecord> { row }).Counted);
            }
            finally
            {
                service.Dispose();
                File.Delete(path);
            }
        }
    }
}
