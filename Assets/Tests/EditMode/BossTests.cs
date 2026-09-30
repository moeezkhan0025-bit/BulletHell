using BulletHell.AI;
using BulletHell.Bosses;
using BulletHell.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BulletHell.Tests
{
    public class BossTests
    {
        [Test]
        public void BossBehaviourIsAppendedToTheEnum()
        {
            // Serialized as an int on every EnemyData asset: it must stay last.
            Assert.AreEqual(6, (int)EnemyBehavior.Boss);
        }

        [Test]
        public void PhaseIndexFollowsTheThresholds()
        {
            var boss = ScriptableObject.CreateInstance<BossData>();
            boss.EditorConfigure("Test", 100f, 1f, new Vector2(3f, 5f), new[]
            {
                new BossPhase { EnterBelowHp01 = 1f },
                new BossPhase { EnterBelowHp01 = 0.5f },
            });

            Assert.AreEqual(0, boss.PhaseIndexFor(1f));
            Assert.AreEqual(0, boss.PhaseIndexFor(0.51f));
            Assert.AreEqual(1, boss.PhaseIndexFor(0.5f));
            Assert.AreEqual(1, boss.PhaseIndexFor(0.05f));
            Object.DestroyImmediate(boss);
        }

        [Test]
        public void AttackPickerNeverRepeatsAKindWhenAnotherIsAvailable()
        {
            var pattern = ScriptableObject.CreateInstance<AttackPattern>();
            BossAttack[] attacks =
            {
                new BossAttack { Kind = BossAttackKind.CircleSpread, Pattern = pattern, Weight = 3f },
                new BossAttack { Kind = BossAttackKind.FastShot, Pattern = pattern, Weight = 2f },
                new BossAttack { Kind = BossAttackKind.JumpSmash, Pattern = pattern, Weight = 2f },
            };

            for (int last = 0; last < 3; last++)
                for (int i = 0; i <= 100; i++)
                {
                    int picked = BossAttackPicker.Pick(attacks, last, i / 100f);
                    Assert.GreaterOrEqual(picked, 0);
                    Assert.AreNotEqual(last, (int)attacks[picked].Kind, $"repeat with last={last}, roll={i / 100f}");
                }

            // Only one kind left: repeating it is allowed rather than never attacking.
            BossAttack[] single = { new BossAttack { Kind = BossAttackKind.FastShot, Pattern = pattern, Weight = 1f } };
            Assert.AreEqual(0, BossAttackPicker.Pick(single, (int)BossAttackKind.FastShot, 0.5f));
            Assert.AreEqual(-1, BossAttackPicker.Pick(new BossAttack[0], -1, 0.5f));
            Object.DestroyImmediate(pattern);
        }

        [Test]
        public void Round3FightsThePumpking()
        {
            var round = AssetDatabase.LoadAssetAtPath<RoundData>("Assets/Data/Waves/Rounds/Round_3.asset");
            Assert.IsNotNull(round);
            Assert.IsTrue(round.IsBossRound);
            BossData boss = round.FindBoss();
            Assert.IsNotNull(boss, "round 3 has no wave group with a BossData (run BulletHell/M10/Setup Everything)");
            Assert.AreEqual("Pumpking", boss.DisplayName);
            Assert.Greater(boss.Phases.Length, 1);
        }
    }
}
