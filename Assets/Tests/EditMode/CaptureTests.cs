using System;
using BulletHell.Capture;
using BulletHell.Core;
using BulletHell.Player;
using BulletHell.Weapons;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BulletHell.Tests
{
    /// <summary>R1 capture kit: file names, scenario data, the scenario-to-run mapping and the autopilot maths.</summary>
    public class CaptureTests
    {
        // ------------------------------------------------------------------ file names

        [Test]
        public void FileNameIsScenarioDateTime()
        {
            var when = new DateTime(2026, 10, 1, 14, 30, 5);
            Assert.AreEqual("hero-loop_2026-10-01_143005.mp4", CaptureNaming.FileName("hero-loop", when, "mp4"));
            Assert.AreEqual("hero-loop_2026-10-01_143005.png", CaptureNaming.FileName("hero-loop", when, ".png"));
            Assert.AreEqual("hero-loop_2026-10-01_143005", CaptureNaming.BaseName("hero-loop", when));
        }

        [Test]
        public void PrefixIsMadeFileSafe()
        {
            Assert.AreEqual("two-enemy-types", CaptureNaming.Sanitize("Two Enemy Types"));
            Assert.AreEqual("round-1", CaptureNaming.Sanitize("  Round 1 "));
            Assert.AreEqual("a-b", CaptureNaming.Sanitize("a / b"));
            Assert.AreEqual(CaptureNaming.DefaultPrefix, CaptureNaming.Sanitize(""));
            Assert.AreEqual(CaptureNaming.DefaultPrefix, CaptureNaming.Sanitize("***"));
        }

        [Test]
        public void OutputFolderIsUnderCapturesGameplay()
        {
            StringAssert.EndsWith("Captures/Gameplay", CaptureNaming.OutputFolder());
        }

        // ------------------------------------------------------------------ scenario data

        private static readonly string[] ScenarioPaths =
        {
            "Assets/Data/Capture/Scenario_HeroLoop.asset",
            "Assets/Data/Capture/Scenario_Round1.asset",
            "Assets/Data/Capture/Scenario_TwoEnemyTypes.asset",
            "Assets/Data/Capture/Scenario_Pumpking.asset",
        };

        [Test]
        public void ShippedScenariosExistAndAreValid([ValueSource(nameof(ScenarioPaths))] string path)
        {
            var data = AssetDatabase.LoadAssetAtPath<CaptureScenarioData>(path);
            Assert.IsNotNull(data, path + " is missing (BulletHell/Capture/Setup/Create or repair capture assets)");
            Assert.IsEmpty(data.Validate(), string.Join("; ", data.Validate()));
            Assert.IsTrue(data.Autopilot);
            Assert.IsNotNull(data.Tuning);
        }

        [Test]
        public void ScenariosCoverTheRequestedRounds()
        {
            Assert.AreEqual(2, Load(ScenarioPaths[0]).Round);   // hero loop
            Assert.AreEqual(1, Load(ScenarioPaths[1]).Round);
            Assert.AreEqual(2, Load(ScenarioPaths[2]).Round);
            Assert.AreEqual(3, Load(ScenarioPaths[3]).Round);   // Pumpking
            CaptureScenarioData hero = Load(ScenarioPaths[0]);
            Assert.Greater(hero.ExtraSpawns.Count, 0, "the hero loop adds extra enemies");
            Assert.GreaterOrEqual(hero.ExtraSpawnSeconds, hero.CaptureSeconds, "extras last for the whole capture window");
        }

        private static CaptureScenarioData Load(string path) => AssetDatabase.LoadAssetAtPath<CaptureScenarioData>(path);

        [Test]
        public void InvalidScenarioIsReported()
        {
            var data = ScriptableObject.CreateInstance<CaptureScenarioData>();
            data.EditorConfigure("Bad", "Bad Name", 0, 0f, true, new[] { new CaptureArmSetup { slot = 9 } }, new AmmoTypeData[4], null, 5f, null);
            var problems = data.Validate();
            Assert.Greater(problems.Count, 0);
            StringAssert.Contains("file-safe", string.Join(";", problems));
            StringAssert.Contains("autopilot", string.Join(";", problems));
            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void ApplyToRunBuildsFreshInstancesAndLeavesAssetsAlone()
        {
            var hero = Load(ScenarioPaths[0]);
            var state = new RunState();
            CaptureDirector.ApplyToRun(hero, state);

            Assert.AreEqual(hero.Round, state.Round);
            int arms = 0;
            foreach (ArmInstance arm in state.Loadout)
                if (arm != null)
                    arms++;
            Assert.AreEqual(hero.Arms.Count, arms);

            // Each arm is a new instance carrying its armaments; the asset stays as it was.
            CaptureArmSetup first = hero.Arms[0];
            ArmInstance instance = state.Loadout[first.slot];
            Assert.IsNotNull(instance);
            Assert.AreSame(first.arm, instance.Data);
            Assert.Greater(instance.ArmamentCount, 0);

            var second = new RunState();
            CaptureDirector.ApplyToRun(hero, second);
            Assert.AreNotSame(instance, second.Loadout[first.slot]);

            Assert.IsNotNull(state.Ammo.Active);
        }

        // ------------------------------------------------------------------ autopilot maths

        [Test]
        public void BulletComingStraightAtThePlayerIsAThreatWithASideStep()
        {
            bool threat = CaptureAutopilotMath.BulletThreat(new Vector2(5f, 0f), new Vector2(-6f, 0f), Vector2.zero, 0.6f, 1.2f, 1f,
                                                            out float urgency, out Vector2 dodge);
            Assert.IsTrue(threat);
            Assert.Greater(urgency, 0f);
            Assert.AreEqual(0f, dodge.x, 0.001f, "the dodge is across the bullet's path");
            Assert.AreEqual(1f, Mathf.Abs(dodge.y), 0.001f);
        }

        [Test]
        public void TieBreakPicksTheSideForAHeadOnBullet()
        {
            CaptureAutopilotMath.BulletThreat(new Vector2(5f, 0f), new Vector2(-6f, 0f), Vector2.zero, 0.6f, 1.2f, 1f, out _, out Vector2 a);
            CaptureAutopilotMath.BulletThreat(new Vector2(5f, 0f), new Vector2(-6f, 0f), Vector2.zero, 0.6f, 1.2f, -1f, out _, out Vector2 b);
            Assert.Less(Vector2.Dot(a, b), 0f);
        }

        [Test]
        public void BulletThatMissesOrFliesAwayIsNoThreat()
        {
            Assert.IsFalse(CaptureAutopilotMath.BulletThreat(new Vector2(5f, 3f), new Vector2(-6f, 0f), Vector2.zero, 0.6f, 1.2f, 1f, out _, out _), "passes 3 units off");
            Assert.IsFalse(CaptureAutopilotMath.BulletThreat(new Vector2(-5f, 0f), new Vector2(-6f, 0f), Vector2.zero, 0.6f, 1.2f, 1f, out _, out _), "moving away");
            Assert.IsFalse(CaptureAutopilotMath.BulletThreat(new Vector2(30f, 0f), new Vector2(-6f, 0f), Vector2.zero, 0.6f, 1.2f, 1f, out _, out _), "too far for the horizon");
            Assert.IsFalse(CaptureAutopilotMath.BulletThreat(new Vector2(5f, 0f), Vector2.zero, Vector2.zero, 0.6f, 1.2f, 1f, out _, out _), "standing still");
        }

        [Test]
        public void DodgeMovesAwayFromTheSideTheBulletPassesOn()
        {
            // Bullet flying left along y = 0.2: it passes above the player, so the dodge goes down.
            Assert.IsTrue(CaptureAutopilotMath.BulletThreat(new Vector2(5f, 0.2f), new Vector2(-6f, 0f), Vector2.zero, 0.6f, 1.2f, 1f, out _, out Vector2 dodge));
            Assert.Less(dodge.y, 0f);
        }

        [Test]
        public void KeepDistanceBacksOffWhenCloseAndClosesInWhenFar()
        {
            Vector2 player = Vector2.zero;
            Vector2 enemy = new Vector2(3f, 0f);
            Assert.Less(CaptureAutopilotMath.KeepDistance(player, enemy, 5f).x, 0f, "too close: move away (negative x)");
            Assert.Greater(CaptureAutopilotMath.KeepDistance(player, enemy, 2f).x, 0f, "too far: move closer");
            Assert.AreEqual(0f, CaptureAutopilotMath.KeepDistance(player, enemy, 3f).magnitude, 0.001f);
            Assert.LessOrEqual(CaptureAutopilotMath.KeepDistance(player, enemy, 50f).magnitude, 1f);
        }

        [Test]
        public void StrafeIsPerpendicularToTheEnemyDirection()
        {
            Vector2 s = CaptureAutopilotMath.Strafe(Vector2.zero, new Vector2(4f, 0f), 1f);
            Assert.AreEqual(0f, s.x, 0.001f);
            Assert.AreEqual(1f, s.magnitude, 0.001f);
            Assert.AreEqual(-s.y, CaptureAutopilotMath.Strafe(Vector2.zero, new Vector2(4f, 0f), -1f).y, 0.001f);
        }

        [Test]
        public void WallPushPointsInwardAndIsZeroInTheOpen()
        {
            var bounds = new Rect(-8f, -4f, 16f, 8f);
            Assert.AreEqual(Vector2.zero, CaptureAutopilotMath.WallPush(Vector2.zero, bounds, 2f));
            Assert.Greater(CaptureAutopilotMath.WallPush(new Vector2(-7.5f, 0f), bounds, 2f).x, 0f);
            Assert.Less(CaptureAutopilotMath.WallPush(new Vector2(7.5f, 0f), bounds, 2f).x, 0f);
            Assert.Greater(CaptureAutopilotMath.WallPush(new Vector2(0f, -3.9f), bounds, 2f).y, 0f);
            Assert.Less(CaptureAutopilotMath.WallPush(new Vector2(0f, 3.9f), bounds, 2f).y, 0f);
        }

        [Test]
        public void TargetSwitchesOnlyWhenClearlyNearer()
        {
            Assert.IsTrue(CaptureAutopilotMath.ShouldSwitchTarget(-1f, 5f, 0.25f), "no target yet");
            Assert.IsFalse(CaptureAutopilotMath.ShouldSwitchTarget(5f, 4.5f, 0.25f), "only 10% nearer");
            Assert.IsTrue(CaptureAutopilotMath.ShouldSwitchTarget(5f, 3f, 0.25f), "40% nearer");
            Assert.IsFalse(CaptureAutopilotMath.ShouldSwitchTarget(5f, -1f, 0.25f), "no candidate");
        }

        [Test]
        public void LeadPointLooksAheadOfAMovingTarget()
        {
            Vector2 lead = CaptureAutopilotMath.LeadPoint(Vector2.zero, new Vector2(10f, 0f), new Vector2(0f, 4f), 20f);
            Assert.AreEqual(10f, lead.x, 0.001f);
            Assert.AreEqual(2f, lead.y, 0.001f);   // 0.5 s of flight x 4 units/s
            Assert.AreEqual(new Vector2(10f, 0f), CaptureAutopilotMath.LeadPoint(Vector2.zero, new Vector2(10f, 0f), new Vector2(0f, 4f), 0f));
        }

        [Test]
        public void CompassStickMatchesTheArmSelectorConvention()
        {
            foreach (float angle in new[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f })
                Assert.AreEqual(angle, ArmSelector.CompassAngle(CaptureAutopilotMath.StickFromCompass(angle)), 0.01f, "angle " + angle);
            Assert.AreEqual(10f, CaptureAutopilotMath.AngleBetween(355f, 5f), 0.001f);
        }

        [Test]
        public void SmashJumpIsTimedBeforeTheLanding()
        {
            Assert.IsTrue(CaptureAutopilotMath.ShouldJumpForSmash(0.2f, 0.22f, true));
            Assert.IsFalse(CaptureAutopilotMath.ShouldJumpForSmash(0.5f, 0.22f, true), "too early");
            Assert.IsFalse(CaptureAutopilotMath.ShouldJumpForSmash(0.2f, 0.22f, false), "outside the ring");
            Assert.IsFalse(CaptureAutopilotMath.ShouldJumpForSmash(0f, 0.22f, true), "already landed");
        }

        [Test]
        public void DelayLineReturnsTheValueFromBeforeTheDelay()
        {
            var line = new CaptureAutopilotMath.DelayLine(8);
            Assert.AreEqual(Vector2.zero, line.Sample(0f));
            line.Push(0.0f, new Vector2(1f, 0f));
            line.Push(0.1f, new Vector2(2f, 0f));
            line.Push(0.2f, new Vector2(3f, 0f));
            Assert.AreEqual(2f, line.Sample(0.15f).x);
            Assert.AreEqual(3f, line.Sample(5f).x);
            Assert.AreEqual(1f, line.Sample(-1f).x, "older than anything stored: the oldest value");

            for (int i = 0; i < 20; i++)
                line.Push(1f + i * 0.1f, new Vector2(10f + i, 0f));   // wraps the ring
            Assert.AreEqual(29f, line.Sample(100f).x);
            Assert.AreEqual(8, line.Count);
        }
    }
}
