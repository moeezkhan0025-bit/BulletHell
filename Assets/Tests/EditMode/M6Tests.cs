using System.Collections.Generic;
using System.IO;
using BulletHell.Core;
using BulletHell.Cosmetics;
using BulletHell.Input;
using BulletHell.Player;
using BulletHell.Save;
using BulletHell.Settings;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    public class M6Tests
    {
        private static string TempPath() => Path.Combine(Path.GetTempPath(), "bullethell_m6_" + System.Guid.NewGuid() + ".json");

        private static T Make<T>() where T : ScriptableObject => ScriptableObject.CreateInstance<T>();

        // ---- round intro flow

        private sealed class FakeSave : ISaveSystem
        {
            public bool HasSave => false;
            public bool TryLoad(out SaveData data) { data = null; return false; }
            public void Save(SaveData data) { }
            public void Delete() { }
        }

        private static RunManager NewRun()
        {
            var run = new RunManager(Make<GameConfig>(), new FakeSave());
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no New Run Loadout"));
            run.StartNewRun();
            return run;
        }

        [Test]
        public void IntroLeadsOnlyToCombat_AndCannotBePausedOrLost()
        {
            var machine = new GameStateMachine();
            machine.TryEnter(GameState.RoundIntro);
            Assert.IsFalse(machine.TryEnter(GameState.Pause));
            Assert.IsFalse(machine.TryEnter(GameState.GameOver));
            Assert.IsFalse(machine.TryEnter(GameState.Shop));
            Assert.IsTrue(machine.TryEnter(GameState.Combat));
        }

        [Test]
        public void TheArmoryLeadsToTheIntroNotStraightToCombat()
        {
            Assert.IsFalse(GameStateMachine.IsAllowed(GameState.Armory, GameState.Combat));
            Assert.IsTrue(GameStateMachine.IsAllowed(GameState.Armory, GameState.RoundIntro));
        }

        [Test]
        public void NewRunStartsWithTheIntro_WavesStartOnlyWhenCombatBegins()
        {
            RunManager run = NewRun();
            var intros = new List<int>();
            var starts = new List<int>();
            run.RoundIntroStarted += intros.Add;
            run.RoundStarted += starts.Add;

            run.BeginGame();
            Assert.AreEqual(GameState.RoundIntro, run.Machine.Current);
            Assert.AreEqual(new[] { 1 }, intros);
            Assert.IsEmpty(starts);

            Assert.IsTrue(run.BeginCombat());
            Assert.AreEqual(GameState.Combat, run.Machine.Current);
            Assert.AreEqual(new[] { 1 }, starts);
            Assert.IsFalse(run.BeginCombat());   // not in the intro any more
        }

        [Test]
        public void EveryRoundGetsAnIntro()
        {
            RunManager run = NewRun();
            var intros = new List<int>();
            run.RoundIntroStarted += intros.Add;
            run.BeginGame();
            run.BeginCombat();
            run.CombatCleared();
            run.Advance();   // -> shop
            run.Advance();   // -> armory
            run.Advance();   // -> round 2 intro
            Assert.AreEqual(GameState.RoundIntro, run.Machine.Current);
            Assert.AreEqual(new[] { 1, 2 }, intros);
        }

        // ---- aim sensitivity

        [Test]
        public void SensitivityOneReturnsTheTuningValuesExactly()
        {
            var tuning = Make<InputTuning>();
            Assert.AreEqual(tuning.SelectThreshold, AimThresholds.Select(tuning, 1f));
            Assert.AreEqual(tuning.DeselectThreshold, AimThresholds.Deselect(tuning, 1f));
            Assert.AreEqual(tuning.LockedAimDeadzone, AimThresholds.LockedDeadzone(tuning, 1f));
        }

        [Test]
        public void SensitivityStaysInSafeLimitsAndKeepsTheHysteresisBand()
        {
            var tuning = Make<InputTuning>();
            foreach (float sensitivity in new[] { 0.1f, 0.6f, 0.8f, 1.2f, 1.5f, 5f })
            {
                float select = AimThresholds.Select(tuning, sensitivity);
                float deselect = AimThresholds.Deselect(tuning, sensitivity);
                Assert.That(select, Is.InRange(0.5f, 0.95f), $"select at {sensitivity}");
                Assert.LessOrEqual(deselect, select, $"deselect at {sensitivity}");
                Assert.That(AimThresholds.LockedDeadzone(tuning, sensitivity), Is.InRange(0.15f, 0.8f));
            }
            Assert.Less(AimThresholds.Select(tuning, 1.5f), AimThresholds.Select(tuning, 1f));
            Assert.Greater(AimThresholds.Select(tuning, 0.6f), AimThresholds.Select(tuning, 1f));
        }

        [Test]
        public void HigherSensitivitySelectsAnArmWithLessStickTravel()
        {
            var tuning = Make<InputTuning>();
            var selector = new ArmSelector(tuning);
            selector.SetOwned(0, true);

            selector.Update(new Vector2(0f, 0.7f));
            Assert.AreEqual(ArmSelector.None, selector.Selected);   // 0.7 < 0.85

            selector.Sensitivity = 1.4f;                            // 0.85 / 1.4 = 0.61
            selector.Update(new Vector2(0f, 0.7f));
            Assert.AreEqual(0, selector.Selected);
        }

        // ---- settings

        [Test]
        public void SettingsAreClampedIntoTheirSafeRange()
        {
            var defaults = Make<SettingsDefaults>();
            var data = new SettingsData { masterVolume = 3f, musicVolume = -1f, aimSensitivity = 99f, resolutionWidth = 1920, resolutionHeight = 0 };
            defaults.Sanitize(data);
            Assert.AreEqual(1f, data.masterVolume);
            Assert.AreEqual(0f, data.musicVolume);
            Assert.AreEqual(defaults.MaxAimSensitivity, data.aimSensitivity);
            Assert.AreEqual(0, data.resolutionWidth);   // half a resolution is no resolution
        }

        [Test]
        public void SettingsRoundTripThroughTheFile_AndBadFilesGiveDefaults()
        {
            string path = TempPath();
            var defaults = Make<SettingsDefaults>();
            var audio = new AudioService();
            float listener = AudioListener.volume;
            try
            {
                var first = new SettingsService(defaults, new JsonFileStore<SettingsData>(path), audio);
                first.Current.musicVolume = 0.3f;
                first.Current.screenShake = false;
                first.Current.aimSensitivity = 1.2f;
                first.Commit();
                Assert.AreEqual(0.3f, audio.MusicVolume);          // applied immediately
                Assert.IsTrue(first.Dirty);
                first.Save();
                Assert.IsFalse(first.Dirty);

                var second = new SettingsService(defaults, new JsonFileStore<SettingsData>(path), new AudioService());
                Assert.AreEqual(0.3f, second.Current.musicVolume, 0.0001f);
                Assert.IsFalse(second.Current.screenShake);
                Assert.AreEqual(1.2f, second.Current.aimSensitivity, 0.0001f);

                File.WriteAllText(path, "{ not json");
                UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("could not be read"));
                var third = new SettingsService(defaults, new JsonFileStore<SettingsData>(path), new AudioService());
                Assert.AreEqual(defaults.CreateData().musicVolume, third.Current.musicVolume);
                Assert.IsTrue(third.Current.screenShake);
            }
            finally
            {
                AudioListener.volume = listener;
                new JsonFileStore<SettingsData>(path).Delete();
            }
        }

        [Test]
        public void SavingSettingsDoesNotWriteWhenNothingChanged()
        {
            string path = TempPath();
            float listener = AudioListener.volume;
            try
            {
                var service = new SettingsService(Make<SettingsDefaults>(), new JsonFileStore<SettingsData>(path), new AudioService());
                service.Save();
                Assert.IsFalse(File.Exists(path));
            }
            finally
            {
                AudioListener.volume = listener;
            }
        }

        // ---- cosmetics and profile

        private static CosmeticData Item(string id, CosmeticSlot slot)
        {
            var item = Make<CosmeticData>();
            item.Configure(id, slot, id, Color.white, null);
            return item;
        }

        private static AssetRegistry Registry()
        {
            var registry = Make<AssetRegistry>();
            registry.SetCosmetics(new[]
            {
                Item("coat_a", CosmeticSlot.CandyCoating), Item("coat_b", CosmeticSlot.CandyCoating), Item("coat_c", CosmeticSlot.CandyCoating),
                Item("hat_none", CosmeticSlot.Headgear), Item("hat_gold", CosmeticSlot.Headgear),
                Item("cape_none", CosmeticSlot.Cape),
                Item("arm_none", CosmeticSlot.ArmTint), Item("arm_gold", CosmeticSlot.ArmTint),
            });
            return registry;
        }

        [Test]
        public void MissingOrUnknownCosmeticFallsBackToTheSlotDefault()
        {
            var profile = new ProfileService(Registry(), new JsonFileStore<ProfileData>(TempPath()));
            Assert.AreEqual("coat_a", profile.Get(CosmeticSlot.CandyCoating).Id);
            Assert.AreEqual("hat_none", profile.Get(CosmeticSlot.Headgear).Id);

            profile.Data.cosmetics[(int)CosmeticSlot.Headgear] = "deleted_hat";
            Assert.AreEqual("hat_none", profile.Get(CosmeticSlot.Headgear).Id);
        }

        [Test]
        public void CyclingWrapsBothWays()
        {
            var profile = new ProfileService(Registry(), new JsonFileStore<ProfileData>(TempPath()));
            profile.Cycle(CosmeticSlot.CandyCoating, -1);
            Assert.AreEqual("coat_c", profile.Get(CosmeticSlot.CandyCoating).Id);
            profile.Cycle(CosmeticSlot.CandyCoating, 1);
            Assert.AreEqual("coat_a", profile.Get(CosmeticSlot.CandyCoating).Id);
            profile.Cycle(CosmeticSlot.Cape, 1);   // one item only
            Assert.AreEqual("cape_none", profile.Get(CosmeticSlot.Cape).Id);
        }

        [Test]
        public void RandomizePicksAValidItemInEverySlot()
        {
            var profile = new ProfileService(Registry(), new JsonFileStore<ProfileData>(TempPath()));
            for (int seed = 0; seed < 20; seed++)
            {
                var random = new System.Random(seed);
                profile.Randomize(count => random.Next(count));
                for (int slot = 0; slot < CosmeticSlots.Count; slot++)
                {
                    CosmeticData chosen = profile.Get((CosmeticSlot)slot);
                    Assert.IsNotNull(chosen);
                    Assert.AreEqual((CosmeticSlot)slot, chosen.Slot);
                    Assert.AreEqual(chosen.Id, profile.Data.cosmetics[slot]);
                }
            }
        }

        [Test]
        public void ProfilePersistsOnlyOnSave_AndRevertDropsEdits()
        {
            string path = TempPath();
            AssetRegistry registry = Registry();
            try
            {
                var profile = new ProfileService(registry, new JsonFileStore<ProfileData>(path));
                profile.Cycle(CosmeticSlot.CandyCoating, 1);
                profile.Revert();
                Assert.AreEqual("coat_a", profile.Get(CosmeticSlot.CandyCoating).Id);   // edit dropped, nothing was saved
                Assert.IsFalse(File.Exists(path));

                profile.Cycle(CosmeticSlot.CandyCoating, 1);
                profile.Cycle(CosmeticSlot.ArmTint, 1);
                profile.Save();

                var reloaded = new ProfileService(registry, new JsonFileStore<ProfileData>(path));
                Assert.AreEqual("coat_b", reloaded.Get(CosmeticSlot.CandyCoating).Id);
                Assert.AreEqual("arm_gold", reloaded.Get(CosmeticSlot.ArmTint).Id);
            }
            finally
            {
                new JsonFileStore<ProfileData>(path).Delete();
            }
        }

        [Test]
        public void ChangedIsRaisedByEditsAndRandomize()
        {
            var profile = new ProfileService(Registry(), new JsonFileStore<ProfileData>(TempPath()));
            int raised = 0;
            profile.Changed += () => raised++;
            profile.Cycle(CosmeticSlot.CandyCoating, 1);
            profile.Randomize(_ => 0);
            Assert.AreEqual(2, raised);
        }
    }
}
