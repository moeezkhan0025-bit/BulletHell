using System.Collections.Generic;
using System.IO;
using BulletHell.Core;
using BulletHell.Cosmetics;
using BulletHell.Enemies;
using BulletHell.Feedback;
using BulletHell.Save;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BulletHell.Tests
{
    /// <summary>S1 demo scope: the DemoConfig rules, the shipped round data, the locked profile, and the procedural effects.</summary>
    public class DemoTests
    {
        private static DemoConfig Shipped() => AssetDatabase.LoadAssetAtPath<DemoConfig>("Assets/Data/DemoConfig.asset");

        private static GameConfig Config() => Resources.Load<GameConfig>(GameConfig.ResourcePath);

        private static EnemyData Enemy(string name) => AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/Enemies/" + name + ".asset");

        private static DemoConfig Demo(bool on)
        {
            DemoConfig demo = Object.Instantiate(Shipped());   // a copy: the asset on disk is never touched
            demo.SetDemo(on);
            return demo;
        }

        // ---- rules

        [Test]
        public void TheShippedFlagIsOffSoTheEditorPlaysTheFullGame()
        {
            Assert.IsNotNull(Shipped());
            Assert.IsFalse(Shipped().IsDemo);
            Assert.AreSame(Shipped(), Config().Demo, "GameConfig points at the DemoConfig");
        }

        [Test]
        public void InTheDemoSentriesAndTrapsWaitForRoundFourAndTheFullGameHasNoLimits()
        {
            DemoConfig demo = Demo(true);
            for (int round = 1; round <= 3; round++)
            {
                Assert.IsFalse(demo.AllowsSentry(round), "sentry in round " + round);
                Assert.IsFalse(demo.AllowsTraps(round), "traps in round " + round);
            }
            Assert.IsTrue(demo.AllowsSentry(4));
            Assert.IsTrue(demo.AllowsTraps(4));

            DemoConfig full = Demo(false);
            Assert.IsTrue(full.AllowsSentry(1));
            Assert.IsTrue(full.AllowsTraps(1));
            Assert.IsFalse(full.CharacterCreationLocked);
            Assert.IsTrue(demo.CharacterCreationLocked);
        }

        [Test]
        public void ACutSentryIsSwappedForTheChaserSoTheWaveKeepsItsSize()
        {
            DemoConfig demo = Demo(true);
            Assert.AreEqual(EnemyBehavior.Chaser, demo.SentryStandIn.Behavior);
            Assert.AreSame(demo.SentryStandIn, demo.Filter(2, Enemy("Enemy_Ringer")));
            Assert.AreSame(demo.SentryStandIn, demo.Filter(3, Enemy("Enemy_Spiraler")));
            Assert.AreSame(Enemy("Enemy_Ringer"), demo.Filter(4, Enemy("Enemy_Ringer")), "a Sentry is fine from round 4");
            Assert.AreSame(Enemy("Enemy_Weaver"), demo.Filter(1, Enemy("Enemy_Weaver")), "a Skirmisher is never touched");
            Assert.AreSame(Enemy("Enemy_Ringer"), Demo(false).Filter(1, Enemy("Enemy_Ringer")), "the full game is unchanged");
            Assert.IsNull(demo.Filter(1, null));
        }

        // ---- the shipped rounds

        [Test]
        public void RoundsOneToThreeUseOnlyChasersSkirmishersAndThePumpkingEvenBeforeTheFilter()
        {
            GameConfig config = Config();
            for (int round = 1; round <= 3; round++)
            {
                foreach (WaveData wave in config.GetRound(round).Waves)
                    foreach (SpawnGroup group in wave.Groups)
                    {
                        EnemyBehavior behavior = group.Enemy.Behavior;
                        Assert.IsTrue(behavior == EnemyBehavior.Chaser || behavior == EnemyBehavior.Skirmisher || behavior == EnemyBehavior.Boss,
                                      $"round {round}: {group.Enemy.name} is a {behavior}");
                    }
            }
        }

        [Test]
        public void TheDemoRoundsStillHoldTheirEnemiesAfterTheFilter()
        {
            DemoConfig demo = Demo(true);
            GameConfig config = Config();
            int round1 = 0, round2 = 0;
            foreach (WaveData wave in config.GetRound(1).Waves)
                foreach (SpawnGroup group in wave.Groups)
                    round1 += group.Count;
            foreach (WaveData wave in config.GetRound(2).Waves)
                foreach (SpawnGroup group in wave.Groups)
                    round2 += group.Count;
            Assert.GreaterOrEqual(round1, 6, "round 1 keeps its size");
            Assert.GreaterOrEqual(round2, 10, "round 2 keeps its size");
            Assert.AreEqual(Enemy("Enemy_Pumpking"), demo.Filter(3, Enemy("Enemy_Pumpking")));
        }

        [Test]
        public void ObstaclesStayInTheDemoAndTheBossLayoutKeepsItsTrapDataForTheFullGame()
        {
            GameConfig config = Config();
            for (int round = 1; round <= 3; round++)
                Assert.Greater(config.GetLayout(round).Obstacles.Length, 0, "round " + round + " keeps its obstacles");
            Assert.Greater(config.GetLayout(3).Traps.Length, 0, "nothing was deleted: the demo only filters the traps out at run time");
            Assert.AreEqual(0, config.GetLayout(1).Traps.Length);
            Assert.AreEqual(0, config.GetLayout(2).Traps.Length);
        }

        // ---- Character Creation

        [Test]
        public void InTheDemoTheProfileShowsTheDefaultLookRefusesEditsAndSavesTheOldLookUntouched()
        {
            string path = Path.Combine(Path.GetTempPath(), "demo_profile_" + System.Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var store = new JsonFileStore<ProfileData>(path);
                var existing = new ProfileData { cosmetics = new[] { "head_crown", "body_b", "armor_c", "acc1_d", "acc2_e" } };
                store.Save(existing);

                var profile = new ProfileService(null, new JsonFileStore<ProfileData>(path));
                profile.LockCosmetics(true);
                foreach (string id in profile.Data.cosmetics)
                    Assert.AreEqual("", id, "the default look is shown");

                profile.Randomize();
                profile.Set(CosmeticSlot.Head, null);
                foreach (string id in profile.Data.cosmetics)
                    Assert.AreEqual("", id, "edits are refused");

                profile.SetTutorialDone(true);   // a save that has nothing to do with looks
                ProfileData onDisk = JsonUtility.FromJson<ProfileData>(File.ReadAllText(path));
                Assert.IsTrue(onDisk.tutorialDone);
                CollectionAssert.AreEqual(existing.cosmetics, onDisk.cosmetics, "the full game look survives a demo save");
            }
            finally
            {
                new JsonFileStore<ProfileData>(path).Delete();
            }
        }

        [Test]
        public void InTheDemoNoPartOverlaysAreDrawnButTheFullGameKeepsThePartSystem()
        {
            Assert.IsFalse(Demo(true).PartOverlaysShown);
            Assert.IsTrue(Demo(false).PartOverlaysShown);
            Assert.IsTrue(Config().CreationShowsParts, "the shipped asset is the full game");
        }

        [Test]
        public void TheMenuLineCarriesTheVersionAndTheDemoNoteOnlyInADemo()
        {
            StringAssert.StartsWith("v" + BuildInfo.Version, BuildInfo.MenuLine);
            Assert.AreEqual("", BuildInfo.DemoNote, "the shipped asset has the demo flag off");
        }

        // ---- procedural effects

        [Test]
        public void EveryEffectPresetAndPlaceholderSpriteIsAssigned()
        {
            FeedbackTuning tuning = Config().Feedback;
            foreach (VfxKind kind in System.Enum.GetValues(typeof(VfxKind)))
                Assert.IsNotNull(tuning.Prefab(kind), kind + " has no particle preset");
            Assert.IsNotNull(tuning.GlowSprite);
            Assert.IsNotNull(tuning.ShockwaveSprite);
            Assert.Greater(tuning.ShockwaveSeconds, 0f);
        }

        [Test]
        public void TheEffectColoursAvoidTheReservedBulletHues()
        {
            EnemyBulletPalette palette = Config().EnemyBulletPalette;
            var colors = new List<Color>();
            foreach (VfxKind kind in System.Enum.GetValues(typeof(VfxKind)))
            {
                ParticleSystem.MinMaxGradient start = Config().Feedback.Prefab(kind).main.startColor;
                // colorMin/colorMax only mean something in the constant and two-colour modes; gradient modes are read by their keys.
                if (start.mode == ParticleSystemGradientMode.Color)
                    colors.Add(start.color);
                else if (start.mode == ParticleSystemGradientMode.TwoColors)
                {
                    colors.Add(start.colorMin);
                    colors.Add(start.colorMax);
                }
                else if (start.gradient != null)
                    foreach (GradientColorKey key in start.gradient.colorKeys)
                        colors.Add(key.color);
                else if (start.gradientMin != null && start.gradientMax != null)
                    foreach (Gradient gradient in new[] { start.gradientMin, start.gradientMax })
                        foreach (GradientColorKey key in gradient.colorKeys)
                            colors.Add(key.color);
            }
            foreach (Color color in colors)
                Assert.IsFalse(palette.IsReservedHue(color), "effect colour " + ColorUtility.ToHtmlStringRGB(color) + " is in an enemy bullet hue band");
        }

        [Test]
        public void CoinsAndAmmoPickupsCarryAGlow()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Coin.prefab").GetComponent<PickupGlow>());
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AmmoPickup.prefab").GetComponent<PickupGlow>());
        }
    }
}
