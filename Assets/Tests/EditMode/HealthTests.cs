using BulletHell.Core;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    public sealed class HealthTests
    {
        private GameObject go;
        private Health health;
        private int diedCount;
        private float damagedTotal;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("HealthTest");
            health = go.AddComponent<Health>();
            health.Initialize(10f);
            diedCount = 0;
            damagedTotal = 0f;
            health.Died += () => diedCount++;
            health.Damaged += amount => damagedTotal += amount;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(go);

        [Test]
        public void TakeDamage_ReducesCurrent()
        {
            health.TakeDamage(3f);
            Assert.AreEqual(7f, health.Current, 0.001f);
            Assert.AreEqual(0.7f, health.Fraction, 0.001f);
            Assert.IsTrue(health.IsAlive);
        }

        [Test]
        public void TakeDamage_ClampsAtZeroAndReportsAppliedDamage()
        {
            health.TakeDamage(25f);
            Assert.AreEqual(0f, health.Current, 0.001f);
            Assert.IsFalse(health.IsAlive);
            Assert.AreEqual(10f, damagedTotal, 0.001f);
        }

        [Test]
        public void Died_RaisedOnlyOnce()
        {
            health.TakeDamage(10f);
            health.TakeDamage(5f);
            Assert.AreEqual(1, diedCount);
        }

        [Test]
        public void TakeDamage_IgnoresNonPositiveAmounts()
        {
            health.TakeDamage(0f);
            health.TakeDamage(-4f);
            Assert.AreEqual(10f, health.Current, 0.001f);
        }

        [Test]
        public void Revive_RestoresFullHealthAndAllowsDyingAgain()
        {
            health.TakeDamage(10f);
            health.Revive();
            Assert.AreEqual(10f, health.Current, 0.001f);
            health.TakeDamage(10f);
            Assert.AreEqual(2, diedCount);
        }
    }
}
