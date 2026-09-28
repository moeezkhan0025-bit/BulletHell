using BulletHell.Weapons;
using NUnit.Framework;

namespace BulletHell.Tests
{
    public class HeatComponentTests
    {
        // 0.1 heat per shot, 0.5/s while firing continuously, cools 0.5/s, restart at 0.4.
        private static readonly HeatSettings Settings = new HeatSettings(0.1f, 0.5f, 0.5f, 0.4f);

        [Test]
        public void ShotsAddHeat()
        {
            var heat = new HeatComponent();
            heat.AddShot(Settings);
            heat.AddShot(Settings);
            Assert.AreEqual(0.2f, heat.Heat01, 0.0001f);
            Assert.IsFalse(heat.IsOverheated);
        }

        [Test]
        public void ContinuousFiringAddsHeatOverTime()
        {
            var heat = new HeatComponent();
            heat.Tick(1f, true, Settings);
            Assert.AreEqual(0.5f, heat.Heat01, 0.0001f);
        }

        [Test]
        public void FullHeatOverheats()
        {
            var heat = new HeatComponent();
            heat.Tick(2f, true, Settings);
            Assert.AreEqual(1f, heat.Heat01, 0.0001f);
            Assert.IsTrue(heat.IsOverheated);
        }

        [Test]
        public void DoesNotCoolWhileFiring()
        {
            var heat = new HeatComponent();
            heat.AddShot(Settings);
            var noContinuous = new HeatSettings(0.1f, 0f, 0.5f, 0.4f);
            heat.Tick(1f, true, noContinuous);
            Assert.AreEqual(0.1f, heat.Heat01, 0.0001f);
        }

        [Test]
        public void OverheatClearsOnlyAtRestartThreshold()
        {
            var heat = new HeatComponent();
            heat.Tick(2f, true, Settings);

            heat.Tick(1f, false, Settings); // 1.0 -> 0.5, still above 0.4
            Assert.IsTrue(heat.IsOverheated);

            heat.Tick(0.3f, false, Settings); // 0.5 -> 0.35
            Assert.IsFalse(heat.IsOverheated);
        }

        [Test]
        public void HeatDecaysWhenIdleAndClampsAtZero()
        {
            var heat = new HeatComponent();
            heat.AddShot(Settings);
            heat.Tick(10f, false, Settings);
            Assert.AreEqual(0f, heat.Heat01, 0.0001f);
        }

        [Test]
        public void SpinUpBuildsAndWindsDown()
        {
            var spin = new SpinUp();
            var settings = new SpinSettings(1f, 2f, 0.5f, 2f);

            Assert.AreEqual(0.5f, spin.Tick(0f, true, settings), 0.0001f);
            Assert.AreEqual(1.25f, spin.Tick(0.5f, true, settings), 0.0001f);
            Assert.AreEqual(2f, spin.Tick(5f, true, settings), 0.0001f);
            Assert.AreEqual(1.25f, spin.Tick(1f, false, settings), 0.0001f);
        }

        [Test]
        public void NoSpinUpTimeMeansMaxMultiplier()
        {
            var spin = new SpinUp();
            Assert.AreEqual(1.5f, spin.Tick(0.1f, false, new SpinSettings(0f, 1f, 1f, 1.5f)), 0.0001f);
        }
    }
}
