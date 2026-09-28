using BulletHell.Input;
using BulletHell.Player;
using BulletHell.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    public class ArmSelectorTests
    {
        private const int N = 0, NE = 1, E = 2, S = 4, W = 6;

        private InputTuning tuning;
        private ArmSelector selector;

        [SetUp]
        public void SetUp()
        {
            tuning = ScriptableObject.CreateInstance<InputTuning>(); // defaults: 0.85 / 0.65 / 8 deg / 0.5 aim deadzone
            selector = new ArmSelector(tuning);
            OwnOnly(0, 1, 2, 3, 4, 5, 6, 7);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(tuning);

        private void OwnOnly(params int[] arms)
        {
            for (int i = 0; i < ArmSelector.ArmCount; i++)
                selector.SetOwned(i, false);
            foreach (int arm in arms)
                selector.SetOwned(arm, true);
        }

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
            Assert.AreEqual(ArmSelectionState.None, selector.State);
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
            Assert.AreEqual(ArmSelectionState.Soft, selector.State);
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
        public void RotatingWhileHeld_Switches()
        {
            selector.Update(Stick(0f, 0.9f));
            selector.Update(Stick(90f, 0.75f));
            Assert.AreEqual(E, selector.Selected);
        }

        [Test]
        public void SoftSelected_AimsAtHomeSlot()
        {
            selector.Update(Stick(80f, 0.9f));
            Assert.AreEqual(E, selector.Selected);
            Assert.AreEqual(90f, selector.AimAngle, 0.01f);
        }

        [Test]
        public void LockWithNothingSelected_DoesNothing()
        {
            Assert.IsFalse(selector.ToggleLock(Vector2.zero));
            Assert.IsFalse(selector.Locked);
        }

        [Test]
        public void Lock_SnapsAimToStick()
        {
            selector.Update(Stick(80f, 0.9f));
            selector.ToggleLock(Stick(80f, 0.9f));
            Assert.AreEqual(ArmSelectionState.Locked, selector.State);
            Assert.AreEqual(80f, selector.AimAngle, 0.01f);
        }

        [Test]
        public void Locked_AimFollowsStick()
        {
            selector.Update(Stick(90f, 0.9f));
            selector.ToggleLock(Stick(90f, 0.9f));
            Assert.IsTrue(selector.Update(Stick(200f, 0.8f)));
            Assert.AreEqual(200f, selector.AimAngle, 0.01f);
            Assert.AreEqual(E, selector.Selected);
        }

        [Test]
        public void Locked_BelowAimDeadzone_KeepsLastAim()
        {
            selector.Update(Stick(90f, 0.9f));
            selector.ToggleLock(Stick(90f, 0.9f));
            selector.Update(Stick(200f, 0.8f));
            Assert.IsFalse(selector.Update(Stick(10f, 0.3f)));
            selector.Update(Vector2.zero);
            Assert.AreEqual(200f, selector.AimAngle, 0.01f);
        }

        [Test]
        public void Locked_StaysSelectedWhenStickReleased()
        {
            selector.Update(Stick(90f, 0.9f));
            selector.ToggleLock(Stick(90f, 0.9f));
            selector.Update(Vector2.zero);
            Assert.AreEqual(E, selector.Selected);
            Assert.IsTrue(selector.Locked);
        }

        [Test]
        public void Locked_IgnoresPushTowardAnotherArm()
        {
            selector.Update(Stick(90f, 0.9f));
            selector.ToggleLock(Stick(90f, 0.9f));
            selector.Update(Stick(180f, 1f));
            Assert.AreEqual(E, selector.Selected);
        }

        [Test]
        public void Unlock_WithStickReleased_Deselects()
        {
            selector.Update(Stick(90f, 0.9f));
            selector.ToggleLock(Stick(90f, 0.9f));
            selector.Update(Vector2.zero);
            selector.ToggleLock(Vector2.zero);
            selector.Update(Vector2.zero);
            Assert.AreEqual(ArmSelector.None, selector.Selected);
            Assert.IsFalse(selector.Locked);
        }

        [Test]
        public void Unlock_ReturnsHomeAndSoftSelectsFromStick()
        {
            selector.Update(Stick(90f, 0.9f));
            selector.ToggleLock(Stick(90f, 0.9f));
            selector.Update(Stick(270f, 1f));
            selector.ToggleLock(Stick(270f, 1f));
            Assert.AreEqual(W, selector.Selected);
            Assert.AreEqual(ArmSelectionState.Soft, selector.State);
            Assert.AreEqual(270f, selector.AimAngle, 0.01f);
        }

        [Test]
        public void NoArmsOwned_SelectsNothing()
        {
            OwnOnly();
            selector.Update(Stick(90f, 1f));
            Assert.AreEqual(ArmSelector.None, selector.Selected);
        }

        [TestCase(0f)]
        [TestCase(90f)]
        [TestCase(180f)]
        [TestCase(300f)]
        public void OneArmOwned_AnyDirectionSelectsIt(float angle)
        {
            OwnOnly(N);
            selector.Update(Stick(angle, 0.9f));
            Assert.AreEqual(N, selector.Selected);
            Assert.AreEqual(0f, selector.AimAngle, 0.01f); // fires from its home slot while soft
        }

        [Test]
        public void OneArmOwned_RotatingNeverDeselects()
        {
            OwnOnly(N);
            for (float angle = 0f; angle < 360f; angle += 15f)
            {
                selector.Update(Stick(angle, 0.9f));
                Assert.AreEqual(N, selector.Selected);
            }
        }

        [TestCase(30f, N)]
        [TestCase(60f, E)]
        [TestCase(200f, E)]
        [TestCase(250f, N)]
        public void TwoArmsOwned_SplitCircleHalfwayBetween(float angle, int expected)
        {
            OwnOnly(N, E); // boundaries at 45 and 225
            selector.Update(Stick(angle, 0.9f));
            Assert.AreEqual(expected, selector.Selected);
        }

        [Test]
        public void TwoArmsOwned_BoundaryUsesHysteresis()
        {
            OwnOnly(N, E);
            selector.Update(Stick(0f, 0.9f));
            selector.Update(Stick(45f + 7f, 0.9f));
            Assert.AreEqual(N, selector.Selected);
            selector.Update(Stick(45f + 9f, 0.9f));
            Assert.AreEqual(E, selector.Selected);
        }

        [Test]
        public void UnownedDirection_SelectsNearestOwnedArm()
        {
            selector.SetOwned(E, false);
            selector.Update(Stick(100f, 0.9f)); // E is gone; SE (135) is nearer than NE (45)
            Assert.AreEqual(3, selector.Selected);
        }

        [Test]
        public void Loadout_OwnsExactlyFilledSlots()
        {
            var arm = ScriptableObject.CreateInstance<WeaponArmData>();
            var loadout = ScriptableObject.CreateInstance<ArmLoadout>();
            loadout.SetSlot(N, arm);
            loadout.SetSlot(S, arm); // same arm type in two slots

            selector.SetOwnedFromLoadout(loadout);

            for (int i = 0; i < ArmSelector.ArmCount; i++)
                Assert.AreEqual(i == N || i == S, selector.IsOwned(i), $"slot {i}");

            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(arm);
        }
    }
}
