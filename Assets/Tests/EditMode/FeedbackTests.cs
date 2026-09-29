using BulletHell.Core;
using BulletHell.Feedback;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BulletHell.Tests
{
    public sealed class FeedbackTests
    {
        // ---- HitstopClock: hitstop can never freeze menus or outlive a state -------------------------------

        [Test]
        public void Scale_IsZeroOutsideTheLiveStates()
        {
            var clock = new HitstopClock();
            foreach (GameState state in new[] { GameState.None, GameState.RoundResults, GameState.Shop, GameState.Armory, GameState.Pause, GameState.GameOver })
            {
                clock.SetState(state);
                Assert.AreEqual(0f, clock.Scale, state.ToString());
            }
        }

        [Test]
        public void Scale_IsOneInRoundIntroAndCombat()
        {
            var clock = new HitstopClock();
            clock.SetState(GameState.RoundIntro);
            Assert.AreEqual(1f, clock.Scale);
            clock.SetState(GameState.Combat);
            Assert.AreEqual(1f, clock.Scale);
        }

        [Test]
        public void Hitstop_FreezesCombatThenReleases()
        {
            var clock = new HitstopClock();
            clock.SetState(GameState.Combat);
            Assert.IsTrue(clock.Request(0.1f, 0f));
            Assert.AreEqual(0f, clock.Scale);
            Assert.IsTrue(clock.IsHitstopping);

            clock.Tick(0.06f);
            Assert.AreEqual(0f, clock.Scale);
            clock.Tick(0.06f);
            Assert.AreEqual(1f, clock.Scale);
            Assert.IsFalse(clock.IsHitstopping);
        }

        [Test]
        public void Hitstop_IsIgnoredOutsideCombat()
        {
            var clock = new HitstopClock();
            foreach (GameState state in new[] { GameState.RoundIntro, GameState.RoundResults, GameState.Shop, GameState.Armory, GameState.Pause, GameState.GameOver })
            {
                clock.SetState(state);
                Assert.IsFalse(clock.Request(0.5f, 0f), state.ToString());
                Assert.IsFalse(clock.IsHitstopping, state.ToString());
            }
        }

        [Test]
        public void Hitstop_IsClearedByEveryStateChange()
        {
            var clock = new HitstopClock();
            clock.SetState(GameState.Combat);
            clock.Request(1f, 0f);

            clock.SetState(GameState.Pause); // pausing mid-hitstop
            Assert.AreEqual(0f, clock.Scale);
            clock.SetState(GameState.Combat); // resuming must not bring the old hitstop back
            Assert.AreEqual(1f, clock.Scale);
            Assert.IsFalse(clock.IsHitstopping);
        }

        [Test]
        public void Hitstop_LongerRequestWinsAndScaleIsClamped()
        {
            var clock = new HitstopClock();
            clock.SetState(GameState.Combat);
            clock.Request(0.05f, 0.5f);
            clock.Request(0.2f, 3f);
            Assert.AreEqual(1f, clock.Scale, 1e-5f); // scale clamped to 0..1
            clock.Tick(0.1f);
            Assert.IsTrue(clock.IsHitstopping);      // the longer stop is still running
            clock.Request(0.01f, 0f);                // a shorter one doesn't cut it short
            clock.Tick(0.11f);
            Assert.IsFalse(clock.IsHitstopping);
        }

        [Test]
        public void Hitstop_NeverStopsAnInfiniteFreeze()
        {
            // Even repeated requests end: the timer only ever counts down.
            var clock = new HitstopClock();
            clock.SetState(GameState.Combat);
            for (int i = 0; i < 100; i++)
            {
                clock.Request(0.05f, 0f);
                clock.Tick(0.05f);
            }
            Assert.AreEqual(1f, clock.Scale);
        }

        // ---- Spring ---------------------------------------------------------------------------------------

        [Test]
        public void Spring_SettlesBackToZero()
        {
            var spring = new Spring { Value = 1f };
            for (int i = 0; i < 600; i++)
                spring.Step(1f / 60f, 3f, 0.5f);
            Assert.IsTrue(spring.IsAtRest);
        }

        [Test]
        public void Spring_ImpulseOvershootsThenReturns()
        {
            var spring = new Spring();
            spring.Velocity = 5f;
            float peak = 0f;
            for (int i = 0; i < 600; i++)
            {
                spring.Step(1f / 60f, 3f, 0.4f);
                peak = Mathf.Max(peak, spring.Value);
            }
            Assert.Greater(peak, 0.05f);
            Assert.IsTrue(spring.IsAtRest);
        }

        [Test]
        public void Spring_IsStableForAHugeTimeStep()
        {
            var spring = new Spring { Value = 1f };
            spring.Step(5f, 4f, 0.3f); // e.g. the first frame after a long stall
            Assert.IsFalse(float.IsNaN(spring.Value) || float.IsInfinity(spring.Value));
            Assert.Less(Mathf.Abs(spring.Value), 2f);
        }

        // ---- Tuning assets --------------------------------------------------------------------------------

        [Test]
        public void FeedbackTuning_HasEverythingItPromises()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<FeedbackTuning>("Assets/Data/Feedback/FeedbackTuning.asset");
            Assert.IsNotNull(tuning, "Run BulletHell/M8.6/Setup Everything.");
            Assert.IsNotNull(tuning.PlayerMotion);
            Assert.IsNotNull(tuning.PlayerHit);
            Assert.IsNotNull(tuning.EnemyMotion);
            Assert.IsNotNull(tuning.EnemyHit);
            Assert.IsNotNull(tuning.EnemyLifeCycle);
            foreach (VfxKind kind in System.Enum.GetValues(typeof(VfxKind)))
                Assert.IsNotNull(tuning.Prefab(kind), kind + " particle preset is missing");
        }

        [Test]
        public void GameConfig_PointsAtTheFeedbackTuning()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Resources/GameConfig.asset");
            Assert.IsNotNull(config.Feedback);
        }

        [Test]
        public void LifeCycle_SpawnCurveStartsAtZeroAndEndsAtOne()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<LifeCycleTuning>("Assets/Data/Feedback/LifeCycle_Enemy.asset");
            Assert.AreEqual(0f, tuning.SpawnScaleAt(0f), 1e-4f);
            Assert.AreEqual(1f, tuning.SpawnScaleAt(1f), 1e-4f);
        }

        [Test]
        public void ShaderGraphs_AllCompile()
        {
            foreach (string name in new[] { "Character", "HitFlash", "Dissolve", "Outline", "TintPalette", "PulseGlow", "UVScroll", "Wave" })
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>($"Assets/Art/Shaders/Sprite_{name}.shadergraph");
                Assert.IsNotNull(shader, name);
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader), name);
            }
        }
    }
}
