using BulletHell.Player;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    /// <summary>M7.7: arm ring ellipse, front/back halves, spin easing.</summary>
    public class ArmRingTests
    {
        private const float Rx = 0.6f;
        private const float Ry = 0.3f;
        private const float Lift = 0.1f;

        [Test]
        public void CardinalPointsSitOnTheFlattenedEllipse()
        {
            Assert.That(ArmRingMath.Position(0f, Rx, Ry, Lift), Is.EqualTo(new Vector2(0f, Ry + Lift)).Using(Vector2EqualityComparer.Instance));
            Assert.That(ArmRingMath.Position(90f, Rx, Ry, Lift), Is.EqualTo(new Vector2(Rx, Lift)).Using(Vector2EqualityComparer.Instance));
            Assert.That(ArmRingMath.Position(180f, Rx, Ry, Lift), Is.EqualTo(new Vector2(0f, -Ry + Lift)).Using(Vector2EqualityComparer.Instance));
            Assert.That(ArmRingMath.Position(270f, Rx, Ry, Lift), Is.EqualTo(new Vector2(-Rx, Lift)).Using(Vector2EqualityComparer.Instance));
        }

        [Test]
        public void UpperHalfIsBehindTheBodyAndSidesAreInFront()
        {
            Assert.IsTrue(ArmRingMath.IsBack(0f, 0.05f));    // N
            Assert.IsTrue(ArmRingMath.IsBack(45f, 0.05f));   // NE
            Assert.IsTrue(ArmRingMath.IsBack(315f, 0.05f));  // NW
            Assert.IsFalse(ArmRingMath.IsBack(90f, 0.05f));  // E
            Assert.IsFalse(ArmRingMath.IsBack(270f, 0.05f)); // W
            Assert.IsFalse(ArmRingMath.IsBack(135f, 0.05f)); // SE
            Assert.IsFalse(ArmRingMath.IsBack(180f, 0.05f)); // S
        }

        [Test]
        public void DepthRunsFromBackToFront()
        {
            Assert.AreEqual(0f, ArmRingMath.Depth01(0f), 0.0001f);
            Assert.AreEqual(0.5f, ArmRingMath.Depth01(90f), 0.0001f);
            Assert.AreEqual(1f, ArmRingMath.Depth01(180f), 0.0001f);
        }

        [Test]
        public void SpinTakesTheShortWayAcrossNorth()
        {
            float angle = ArmRingMath.MoveAngle(350f, 10f, 100f, 0.1f);   // 10 degrees this step: 350 -> 0 (north)
            Assert.AreEqual(0f, Mathf.DeltaAngle(angle, 0f), 0.001f);
        }

        [Test]
        public void SpinStopsAtTheTargetAndZeroSpeedSnaps()
        {
            Assert.AreEqual(90f, ArmRingMath.MoveAngle(80f, 90f, 720f, 1f), 0.001f);
            Assert.AreEqual(200f, ArmRingMath.MoveAngle(0f, 200f, 0f, 0.016f), 0.001f);
        }
    }

    internal sealed class Vector2EqualityComparer : System.Collections.Generic.IEqualityComparer<Vector2>
    {
        public static readonly Vector2EqualityComparer Instance = new Vector2EqualityComparer();
        public bool Equals(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < 1e-8f;
        public int GetHashCode(Vector2 v) => v.GetHashCode();
    }
}
