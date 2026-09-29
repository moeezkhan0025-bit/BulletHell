using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Save;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    public class CombatTests
    {
        // ---- attack pattern geometry

        private static float[] Buffer() => new float[AttackPattern.MaxBulletsPerVolley];

        [Test]
        public void AimedFiresOneBulletAtTheAimAngle()
        {
            float[] angles = Buffer();
            int n = AttackPatternMath.FillAngles(AttackShape.Aimed, 5, 90f, 33f, 0f, angles);
            Assert.AreEqual(1, n);
            Assert.AreEqual(33f, angles[0]);
        }

        [Test]
        public void SpreadFansAcrossTheArcCentredOnTheAim()
        {
            float[] angles = Buffer();
            int n = AttackPatternMath.FillAngles(AttackShape.Spread, 5, 40f, 90f, 0f, angles);
            Assert.AreEqual(5, n);
            Assert.AreEqual(70f, angles[0], 0.001f);
            Assert.AreEqual(80f, angles[1], 0.001f);
            Assert.AreEqual(90f, angles[2], 0.001f);
            Assert.AreEqual(110f, angles[4], 0.001f);
        }

        [Test]
        public void SpreadOfOneBulletIsJustAimed()
        {
            float[] angles = Buffer();
            Assert.AreEqual(1, AttackPatternMath.FillAngles(AttackShape.Spread, 1, 90f, 12f, 0f, angles));
            Assert.AreEqual(12f, angles[0]);
        }

        [Test]
        public void RingIsEvenlySpacedAndIgnoresTheAim()
        {
            float[] angles = Buffer();
            int n = AttackPatternMath.FillAngles(AttackShape.Ring, 8, 0f, 123f, 0f, angles);
            Assert.AreEqual(8, n);
            for (int i = 0; i < n; i++)
                Assert.AreEqual(45f * i, angles[i], 0.001f);
        }

        [Test]
        public void SpiralIsARingTurnedByTheOffset()
        {
            float[] angles = Buffer();
            int n = AttackPatternMath.FillAngles(AttackShape.Spiral, 4, 0f, 0f, 30f, angles);
            Assert.AreEqual(4, n);
            Assert.AreEqual(30f, angles[0], 0.001f);
            Assert.AreEqual(120f, angles[1], 0.001f);
            Assert.AreEqual(300f, angles[3], 0.001f);
        }

        [Test]
        public void VolleySizeIsClampedToTheBuffer()
        {
            float[] small = new float[4];
            Assert.AreEqual(4, AttackPatternMath.FillAngles(AttackShape.Ring, 50, 0f, 0f, 0f, small));
            Assert.AreEqual(1, AttackPatternMath.FillAngles(AttackShape.Ring, 0, 0f, 0f, 0f, small));
        }

        // ---- game over

        private sealed class FakeSave : ISaveSystem
        {
            public bool Exists = true;
            public bool HasSave => Exists;
            public bool TryLoad(out SaveData data) { data = null; return false; }
            public void Save(SaveData data) => Exists = true;
            public void Delete() => Exists = false;
        }

        private static RunManager NewRun(FakeSave save)
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            var run = new RunManager(config, save);
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no New Run Loadout"));
            run.StartNewRun();
            return run;
        }

        [Test]
        public void GameOverEndsTheRunDeletesTheSaveAndKeepsTheStateForTheScreen()
        {
            var save = new FakeSave();
            RunManager run = NewRun(save);
            run.BeginGame();
            run.BeginCombat();
            Assert.AreEqual(GameState.Combat, run.Machine.Current);

            run.GameOver();

            Assert.AreEqual(GameState.GameOver, run.Machine.Current);
            Assert.IsFalse(save.Exists);
            Assert.IsTrue(run.HasRun);   // the Game Over screen still reads the round

            run.AbandonRun();
            Assert.IsFalse(run.HasRun);
        }

        [Test]
        public void GameOverOutsideCombatIsRefusedAndKeepsTheSave()
        {
            var save = new FakeSave();
            RunManager run = NewRun(save);
            run.BeginGame();
            run.BeginCombat();
            run.CombatCleared();
            Assert.AreEqual(GameState.RoundResults, run.Machine.Current);

            run.GameOver();

            Assert.AreEqual(GameState.RoundResults, run.Machine.Current);
            Assert.IsTrue(save.Exists);
        }

        [Test]
        public void PauseAndResumeReturnToCombatWithoutChangingTheRound()
        {
            RunManager run = NewRun(new FakeSave());
            run.BeginGame();
            run.BeginCombat();

            Assert.IsTrue(run.SetPaused(true));
            Assert.AreEqual(GameState.Pause, run.Machine.Current);
            Assert.IsTrue(run.SetPaused(false));
            Assert.AreEqual(GameState.Combat, run.Machine.Current);
            Assert.AreEqual(1, run.State.Round);
            Assert.IsFalse(run.SetPaused(false)); // not paused any more
        }
    }
}
