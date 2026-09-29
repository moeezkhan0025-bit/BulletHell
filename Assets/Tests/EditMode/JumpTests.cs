using BulletHell.Player;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    /// <summary>M7.6: jump timing, height arc, landing spot search.</summary>
    public class JumpTests
    {
        private const float Airtime = 0.5f;
        private const float Cooldown = 0.2f;

        [Test]
        public void AJumpLastsTheAirtimeAndLandsOnce()
        {
            var timeline = new JumpTimeline();
            Assert.IsTrue(timeline.TryStart());
            Assert.IsTrue(timeline.IsAirborne);

            int landings = 0;
            for (int i = 0; i < 20; i++)      // 20 x 0.05 = 1 second, the jump is over after 0.5
                if (timeline.Tick(0.05f, Airtime, Cooldown))
                    landings++;

            Assert.AreEqual(1, landings);
            Assert.IsFalse(timeline.IsAirborne);
        }

        [Test]
        public void ProgressRunsFromZeroToOneOverTheAirtime()
        {
            var timeline = new JumpTimeline();
            timeline.TryStart();
            timeline.Tick(0.25f, Airtime, Cooldown);
            Assert.AreEqual(0.5f, timeline.Progress01, 0.0001f);
            Assert.IsTrue(timeline.Tick(0.3f, Airtime, Cooldown));
            Assert.AreEqual(0f, timeline.Progress01);
        }

        [Test]
        public void NoDoubleJumpAndNoJumpDuringTheCooldown()
        {
            var timeline = new JumpTimeline();
            Assert.IsTrue(timeline.TryStart());
            Assert.IsFalse(timeline.TryStart(), "no double jump");

            timeline.Tick(0.6f, Airtime, Cooldown);   // lands
            Assert.IsFalse(timeline.TryStart(), "cooling down");
            timeline.Tick(0.1f, Airtime, Cooldown);
            Assert.IsFalse(timeline.TryStart());
            timeline.Tick(0.15f, Airtime, Cooldown);
            Assert.IsTrue(timeline.TryStart(), "cooldown over");
        }

        [Test]
        public void CancelPutsThePlayerBackOnTheGroundWithNoCooldown()
        {
            var timeline = new JumpTimeline();
            timeline.TryStart();
            timeline.Tick(0.1f, Airtime, Cooldown);
            timeline.Cancel();
            Assert.IsFalse(timeline.IsAirborne);
            Assert.IsTrue(timeline.CanStart);
        }

        [Test]
        public void TheDefaultArcStartsAndEndsOnTheGroundAndPeaksInTheMiddle()
        {
            var tuning = ScriptableObject.CreateInstance<JumpTuning>();
            Assert.AreEqual(0f, tuning.HeightAt(0f), 0.001f);
            Assert.AreEqual(1f, tuning.HeightAt(0.5f), 0.01f);
            Assert.AreEqual(0f, tuning.HeightAt(1f), 0.001f);
            Assert.Greater(tuning.HeightAt(0.25f), 0.5f);
            Assert.AreEqual(tuning.HeightAt(0.25f), tuning.HeightAt(0.75f), 0.05f);
            Assert.IsFalse(tuning.JumpDodgesBullets, "still a bullet hell by default");
        }

        [Test]
        public void ALandingSpotThatIsFreeIsKept()
        {
            Vector2 spot = LandingResolver.Resolve(new Vector2(1f, 2f), _ => false, 0.25f, 5f);
            Assert.AreEqual(new Vector2(1f, 2f), spot);
        }

        [Test]
        public void ALandingInsideSomethingMovesToTheNearestFreeSpot()
        {
            // A blocked disc of radius 1 around the origin.
            Vector2 spot = LandingResolver.Resolve(new Vector2(0.2f, 0f), p => p.magnitude < 1f, 0.25f, 5f);
            Assert.GreaterOrEqual(spot.magnitude, 1f);
            Assert.Less(spot.magnitude, 1.6f, "the closest way out, not far away");
        }

        [Test]
        public void ALandingWithNoWayOutStaysPut()
        {
            Vector2 spot = LandingResolver.Resolve(Vector2.zero, _ => true, 0.5f, 2f);
            Assert.AreEqual(Vector2.zero, spot);
        }
    }
}
