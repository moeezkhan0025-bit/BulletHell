using BulletHell.Core;
using NUnit.Framework;

namespace BulletHell.Tests
{
    public sealed class FireTimerTests
    {
        private static int ShotsOver(float seconds, float frameTime, float rate)
        {
            var timer = new FireTimer();
            int shots = 0;
            for (float t = 0f; t < seconds - 0.0001f; t += frameTime)
                shots += timer.Tick(frameTime, true, rate);
            return shots;
        }

        [TestCase(6f, 1f / 60f)]
        [TestCase(6f, 1f / 30f)]
        [TestCase(12f, 1f / 144f)]
        [TestCase(1.2f, 1f / 60f)]
        public void HeldFire_ProducesRateShotsPerSecond(float rate, float frameTime)
        {
            // The first shot fires immediately, so N seconds gives about rate * N + 1 shots.
            int shots = ShotsOver(5f, frameTime, rate);
            Assert.That(shots, Is.InRange(rate * 5f - 1f, rate * 5f + 2f));
        }

        [Test]
        public void NotFiring_ReturnsZero()
        {
            var timer = new FireTimer();
            Assert.AreEqual(0, timer.Tick(0.5f, false, 10f));
        }

        [Test]
        public void IdlePause_DoesNotStoreUpABurst()
        {
            var timer = new FireTimer();
            for (int i = 0; i < 120; i++)
                timer.Tick(1f / 60f, false, 10f);

            Assert.AreEqual(1, timer.Tick(1f / 60f, true, 10f));
            Assert.AreEqual(0, timer.Tick(1f / 60f, true, 10f));
        }

        [Test]
        public void ReleasingAndRepressingQuickly_CannotBeatFireRate()
        {
            var timer = new FireTimer();
            int shots = timer.Tick(1f / 60f, true, 2f);
            shots += timer.Tick(1f / 60f, false, 2f);
            shots += timer.Tick(1f / 60f, true, 2f);
            Assert.AreEqual(1, shots);
        }
    }
}
