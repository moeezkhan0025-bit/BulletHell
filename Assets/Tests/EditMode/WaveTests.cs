using System.Collections.Generic;
using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Save;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    public class WaveTests
    {
        private static EnemyData Enemy() => ScriptableObject.CreateInstance<EnemyData>();

        private static WaveData Wave(params SpawnGroup[] groups)
        {
            var wave = ScriptableObject.CreateInstance<WaveData>();
            wave.Set(groups);
            return wave;
        }

        private static SpawnGroup Group(EnemyData enemy, int count, float delay = 0f, float interval = 0f,
                                        SpawnPattern pattern = SpawnPattern.Scatter) =>
            new SpawnGroup { Enemy = enemy, Count = count, Delay = delay, Interval = interval, Pattern = pattern };

        // ---- scheduler

        [Test]
        public void SchedulerSpawnsEveryEnemyOnItsTimeAndThenFinishes()
        {
            EnemyData grunt = Enemy(), weaver = Enemy();
            var scheduler = new SpawnScheduler();
            scheduler.Begin(Wave(Group(grunt, 3, 0f, 1f), Group(weaver, 2, 1.5f, 0f)), 1f);
            Assert.AreEqual(5, scheduler.TotalCount);

            var due = new List<SpawnRequest>();
            scheduler.Tick(0.1f, due);                       // t=0.1  -> first grunt
            Assert.AreEqual(1, due.Count);
            scheduler.Tick(1.0f, due);                       // t=1.1  -> second grunt
            Assert.AreEqual(2, due.Count);
            scheduler.Tick(0.5f, due);                       // t=1.6  -> both weavers (delay 1.5)
            Assert.AreEqual(4, due.Count);
            Assert.IsFalse(scheduler.Finished);
            scheduler.Tick(1.0f, due);                       // t=2.6  -> third grunt (t=2.0)
            Assert.AreEqual(5, due.Count);
            Assert.IsTrue(scheduler.Finished);
        }

        [Test]
        public void SchedulerScalesGroupSizesByTheCountMultiplierWithAMinimumOfOne()
        {
            Assert.AreEqual(1, SpawnScheduler.ScaledCount(1, 0.4f));
            Assert.AreEqual(3, SpawnScheduler.ScaledCount(2, 1.5f));
            Assert.AreEqual(10, SpawnScheduler.ScaledCount(5, 2f));

            var scheduler = new SpawnScheduler();
            scheduler.Begin(Wave(Group(Enemy(), 4)), 1.5f);
            Assert.AreEqual(6, scheduler.TotalCount);
        }

        [Test]
        public void SchedulerReportsIndexAndScaledGroupSizeForPlacement()
        {
            var scheduler = new SpawnScheduler();
            scheduler.Begin(Wave(Group(Enemy(), 3, 0f, 0f, SpawnPattern.Row)), 1f);
            var due = new List<SpawnRequest>();
            scheduler.Tick(0.01f, due);

            Assert.AreEqual(3, due.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(i, due[i].Index);
                Assert.AreEqual(3, due[i].Count);
                Assert.AreEqual(SpawnPattern.Row, due[i].Pattern);
            }
        }

        [Test]
        public void SchedulerSkipsGroupsWithNoEnemyType()
        {
            var scheduler = new SpawnScheduler();
            scheduler.Begin(Wave(Group(null, 5), Group(Enemy(), 2)), 1f);
            Assert.AreEqual(2, scheduler.TotalCount);
        }

        // ---- placement

        private static readonly Rect Arena = new Rect(-8f, -4f, 16f, 8f);

        // Rect.Contains excludes the max edge; spawns may sit exactly on it.
        private static bool Inside(Vector2 p) => p.x >= Arena.xMin && p.x <= Arena.xMax && p.y >= Arena.yMin && p.y <= Arena.yMax;

        [Test]
        public void RowSpansTheTopEvenlyAndCentresASingleEnemy()
        {
            Vector2 first = SpawnPlacement.Position(SpawnPattern.Row, 0, 3, Arena, Vector2.zero, 3f, 4f, 0.5f);
            Vector2 mid = SpawnPlacement.Position(SpawnPattern.Row, 1, 3, Arena, Vector2.zero, 3f, 4f, 0.5f);
            Vector2 last = SpawnPlacement.Position(SpawnPattern.Row, 2, 3, Arena, Vector2.zero, 3f, 4f, 0.5f);
            Assert.AreEqual(-4f, first.x, 0.001f);
            Assert.AreEqual(0f, mid.x, 0.001f);
            Assert.AreEqual(4f, last.x, 0.001f);
            Assert.AreEqual(Arena.yMax, first.y);

            Vector2 single = SpawnPlacement.Position(SpawnPattern.Row, 0, 1, Arena, Vector2.zero, 3f, 4f, 0.5f);
            Assert.AreEqual(0f, single.x, 0.001f);
        }

        [Test]
        public void RingIsEvenlySpacedAndAlwaysInsideTheArena()
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 p = SpawnPlacement.Position(SpawnPattern.Ring, i, 8, Arena, Vector2.zero, 3f, 30f, 0.5f);
                Assert.IsTrue(Inside(p), $"ring point {i} {p} left the arena");
            }

            Vector2 top = SpawnPlacement.Position(SpawnPattern.Ring, 0, 4, Arena, Vector2.zero, 3f, 3f, 0.5f);
            Assert.AreEqual(0f, top.x, 0.001f);
            Assert.AreEqual(3f, top.y, 0.001f);
        }

        [Test]
        public void ScatterStaysInsideTheArenaAndAwayFromThePlayer()
        {
            var player = new Vector2(0f, 0f);
            for (int i = 0; i < 200; i++)
            {
                Vector2 p = SpawnPlacement.Position(SpawnPattern.Scatter, 0, 1, Arena, player, 3.5f, 4f, 0.5f);
                Assert.IsTrue(Inside(p));
                Assert.GreaterOrEqual((p - player).magnitude, 3.5f - 0.001f);
            }
        }

        // ---- difficulty

        private static DifficultyCurve.Axis Axis(float at1, float at7, float perRoundBeyond, float min, float max) =>
            new DifficultyCurve.Axis
            {
                Curve = AnimationCurve.Linear(1f, at1, 7f, at7),
                BeyondLastKeyPerRound = perRoundBeyond,
                Min = min,
                Max = max,
            };

        private static DifficultyCurve Curve()
        {
            var curve = ScriptableObject.CreateInstance<DifficultyCurve>();
            curve.Set(Axis(1f, 2f, 0.1f, 0.5f, 3f), Axis(1f, 4f, 0.5f, 0.5f, 6f), Axis(1f, 1.5f, 0.05f, 0.5f, 2f), Axis(1f, 1.3f, 0.02f, 0.5f, 1.5f));
            return curve;
        }

        [Test]
        public void DifficultyRisesAcrossTheAuthoredRounds()
        {
            DifficultyCurve curve = Curve();
            RoundDifficulty r1 = curve.Evaluate(1), r4 = curve.Evaluate(4), r7 = curve.Evaluate(7);
            Assert.AreEqual(1f, r1.CountMultiplier, 0.001f);
            Assert.AreEqual(1.5f, r4.CountMultiplier, 0.001f);
            Assert.AreEqual(2f, r7.CountMultiplier, 0.001f);
            Assert.AreEqual(4f, r7.HealthMultiplier, 0.001f);
            Assert.Greater(r7.FireRateMultiplier, r1.FireRateMultiplier);
            Assert.Greater(r7.BulletSpeedMultiplier, r1.BulletSpeedMultiplier);
        }

        [Test]
        public void DifficultyKeepsGrowingPastTheLastRoundButIsCapped()
        {
            DifficultyCurve curve = Curve();
            Assert.AreEqual(2.3f, curve.Evaluate(10).CountMultiplier, 0.001f);   // 2 + 3 * 0.1
            Assert.AreEqual(5.5f, curve.Evaluate(10).HealthMultiplier, 0.001f);  // 4 + 3 * 0.5
            Assert.AreEqual(3f, curve.Evaluate(100).CountMultiplier, 0.001f);    // capped
            Assert.AreEqual(6f, curve.Evaluate(100).HealthMultiplier, 0.001f);
            Assert.AreEqual(2f, curve.Evaluate(100).FireRateMultiplier, 0.001f);
            Assert.AreEqual(1.5f, curve.Evaluate(100).BulletSpeedMultiplier, 0.001f);
        }

        [Test]
        public void RoundsBelowOneAreTreatedAsRoundOne()
        {
            Assert.AreEqual(Curve().Evaluate(1).HealthMultiplier, Curve().Evaluate(-5).HealthMultiplier);
        }

        // ---- round table

        private static RoundData Round(bool boss = false)
        {
            var round = ScriptableObject.CreateInstance<RoundData>();
            round.Set(new WaveData[0], boss, 2f);
            return round;
        }

        [Test]
        public void RoundsPlayInOrderThenLoopFromTheLoopStartRound()
        {
            var rounds = new RoundData[7];
            for (int i = 0; i < 7; i++)
                rounds[i] = Round(boss: i == 2 || i == 4 || i == 6);
            var config = ScriptableObject.CreateInstance<GameConfig>();
            config.SetRounds(rounds, 4);

            for (int r = 1; r <= 7; r++)
                Assert.AreSame(rounds[r - 1], config.GetRound(r), $"round {r}");

            Assert.AreSame(rounds[3], config.GetRound(8));    // 4 again
            Assert.AreSame(rounds[4], config.GetRound(9));    // 5 (boss)
            Assert.AreSame(rounds[5], config.GetRound(10));
            Assert.AreSame(rounds[6], config.GetRound(11));   // 7 (boss)
            Assert.AreSame(rounds[3], config.GetRound(12));
            Assert.IsTrue(config.GetRound(9).IsBossRound);    // boss every second round in endless play
        }

        [Test]
        public void EmptyRoundTableGivesNoRound()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            Assert.IsNull(config.GetRound(1));
        }

        // ---- currency banking and round starts

        private sealed class FakeSave : ISaveSystem
        {
            public bool HasSave => false;
            public bool TryLoad(out SaveData data) { data = null; return false; }
            public void Save(SaveData data) { }
            public void Delete() { }
        }

        private static RunManager NewRun(out List<int> started)
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            var run = new RunManager(config, new FakeSave());
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no New Run Loadout"));
            run.StartNewRun();
            var rounds = new List<int>();
            run.RoundStarted += rounds.Add;
            started = rounds;
            return run;
        }

        [Test]
        public void CollectedCoinsAreBankedWhenTheRoundIsClearedAndShownOnResults()
        {
            RunManager run = NewRun(out _);
            run.BeginGame();
            int before = run.State.Currency;

            run.AddEarnings(5);
            run.AddEarnings(12);
            Assert.AreEqual(17, run.RoundEarnings);
            Assert.AreEqual(before, run.State.Currency);      // not banked yet

            run.CombatCleared();

            Assert.AreEqual(before + 17, run.State.Currency);
            Assert.AreEqual(17, run.LastReward);
            Assert.AreEqual(0, run.RoundEarnings);
            Assert.AreEqual(GameState.RoundResults, run.Machine.Current);
        }

        [Test]
        public void ANewRoundStartsWithNoEarningsAndAnnouncesItself()
        {
            RunManager run = NewRun(out List<int> started);
            run.BeginGame();
            Assert.AreEqual(new[] { 1 }, started);

            run.AddEarnings(9);
            run.CombatCleared();
            run.Advance();   // results -> shop
            run.Advance();   // shop -> armory
            run.AddEarnings(4);
            run.Advance();   // armory -> round 2

            Assert.AreEqual(new[] { 1, 2 }, started);
            Assert.AreEqual(0, run.RoundEarnings);
        }

        [Test]
        public void DebugSkipRestartsCombatAtAnyRoundButOnlyFromPause()
        {
            RunManager run = NewRun(out List<int> started);
            run.BeginGame();
            Assert.IsFalse(run.DebugSkipToRound(5));            // not paused
            Assert.AreEqual(1, run.State.Round);

            run.AddEarnings(30);
            run.SetPaused(true);
            Assert.IsTrue(run.DebugSkipToRound(6));

            Assert.AreEqual(GameState.Combat, run.Machine.Current);
            Assert.AreEqual(6, run.State.Round);
            Assert.AreEqual(new[] { 1, 6 }, started);
            Assert.AreEqual(0, run.RoundEarnings);              // uncollected earnings of the abandoned round are dropped
        }

        [Test]
        public void ResumingFromPauseDoesNotAnnounceANewRound()
        {
            RunManager run = NewRun(out List<int> started);
            run.BeginGame();
            run.SetPaused(true);
            run.SetPaused(false);
            Assert.AreEqual(new[] { 1 }, started);
        }
    }
}
