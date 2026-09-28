using BulletHell.Input;
using BulletHell.Player;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    public class ArmSelectorTests
    {
        private const int N = 0, NE = 1, E = 2, S = 4;

        private InputTuning tuning;
        private ArmSelector selector;

        [SetUp]
        public void SetUp()
        {
            tuning = ScriptableObject.CreateInstance<InputTuning>(); // defaults: 0.85 / 0.65 / 8 deg
            selector = new ArmSelector(tuning);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(tuning);

        /// <summary>Stick at a compass angle (0 = N, clockwise) and magnitude.</summary>
        private static Vector2 Stick(float compassDegrees, float magnitude)
        {
            float r = compassDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(r), Mathf.Cos(r)) * magnitude;
        }

        [Test]
        public void BelowSelectThreshold_SelectsNothing()
        {
            selector.Update(Stick(0f, 0.84f));
            Assert.AreEqual(ArmSelector.None, selector.Selected);
        }

        [TestCase(0f, N)]
        [TestCase(45f, NE)]
        [TestCase(90f, E)]
        [TestCase(180f, S)]
        [TestCase(350f, N)]
        public void AtEdge_SelectsNearestArm(float angle, int expected)
        {
            selector.Update(Stick(angle, 0.9f));
            Assert.AreEqual(expected, selector.Selected);
        }

        [Test]
        public void BetweenThresholds_KeepsSelection()
        {
            selector.Update(Stick(0f, 0.9f));
            selector.Update(Stick(0f, 0.7f));
            Assert.AreEqual(N, selector.Selected);
        }

        [Test]
        public void BelowDeselectThreshold_Deselects()
        {
            selector.Update(Stick(0f, 0.9f));
            selector.Update(Stick(0f, 0.6f));
            Assert.AreEqual(ArmSelector.None, selector.Selected);
        }

        [Test]
        public void JustPastBoundary_WithinHysteresis_KeepsArm()
        {
            selector.Update(Stick(0f, 0.9f));
            selector.Update(Stick(22.5f + 7f, 0.9f));
            Assert.AreEqual(N, selector.Selected);
        }

        [Test]
        public void PastBoundaryPlusHysteresis_SwitchesArm()
        {
            selector.Update(Stick(0f, 0.9f));
            selector.Update(Stick(22.5f + 9f, 0.9f));
            Assert.AreEqual(NE, selector.Selected);
        }

        [Test]
        public void SwitchBack_AlsoNeedsHysteresis()
        {
            selector.Update(Stick(0f, 0.9f));
            selector.Update(Stick(35f, 0.9f));
            selector.Update(Stick(20f, 0.9f)); // back across the boundary, but only 2.5 deg past it
            Assert.AreEqual(NE, selector.Selected);
        }

        [Test]
        public void RotatingBetweenThresholds_DoesNotSwitch()
        {
            selector.Update(Stick(0f, 0.9f));
            selector.Update(Stick(90f, 0.75f));
            Assert.AreEqual(N, selector.Selected);
        }

        [Test]
        public void LockWithNothingSelected_DoesNothing()
        {
            Assert.IsFalse(selector.ToggleLock());
            Assert.IsFalse(selector.Locked);
        }

        [Test]
        public void Locked_StaysSelectedWhenStickReleased()
        {
            selector.Update(Stick(90f, 0.9f));
            selector.ToggleLock();
            selector.Update(Vector2.zero);
            Assert.AreEqual(E, selector.Selected);
            Assert.IsTrue(selector.Locked);
        }

        [Test]
        public void Locked_IgnoresPushTowardAnotherArm()
        {
            selector.Update(Stick(90f, 0.9f));
            selector.ToggleLock();
            selector.Update(Stick(180f, 1f));
            Assert.AreEqual(E, selector.Selected);
        }

        [Test]
        public void Unlock_WithStickReleased_Deselects()
        {
            selector.Update(Stick(90f, 0.9f));
            selector.ToggleLock();
            selector.Update(Vector2.zero);
            selector.ToggleLock();
            selector.Update(Vector2.zero);
            Assert.AreEqual(ArmSelector.None, selector.Selected);
            Assert.IsFalse(selector.Locked);
        }

        [Test]
        public void UnownedDirection_SelectsNothing()
        {
            selector.SetOwned(E, false);
            selector.Update(Stick(90f, 0.9f));
            Assert.AreEqual(ArmSelector.None, selector.Selected);
        }
    }
}
