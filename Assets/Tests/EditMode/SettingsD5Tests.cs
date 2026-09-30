using System.IO;
using BulletHell.Core;
using BulletHell.Input;
using BulletHell.Platform;
using BulletHell.Save;
using BulletHell.Settings;
using BulletHell.UI;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    /// <summary>D5 settings: the new values and their limits, old settings files, gamepad button remapping and how it reaches the prompts.</summary>
    public class SettingsD5Tests
    {
        private static SettingsDefaults Defaults() => ScriptableObject.CreateInstance<SettingsDefaults>();

        private static SettingsService NewService(string path, out JsonFileStore<SettingsData> store)
        {
            store = new JsonFileStore<SettingsData>(path);
            return new SettingsService(Defaults(), store, new AudioService());
        }

        private static string TempPath() => Path.Combine(Path.GetTempPath(), "settings_d5_" + System.Guid.NewGuid().ToString("N") + ".json");

        // ---- values and limits

        [Test]
        public void TheNewSettingsHaveSensibleDefaults()
        {
            SettingsData data = Defaults().CreateData();
            Assert.IsTrue(data.vsync);
            Assert.AreEqual(1f, data.shakeIntensity);
            Assert.AreEqual(1f, data.bulletOutlineScale);
            Assert.IsFalse(data.highContrastBullets);
            Assert.AreEqual(1f, data.hudScale);
            Assert.AreEqual("", data.bindingOverrides);
        }

        [Test]
        public void EveryNewValueIsKeptInsideItsSafeRange()
        {
            SettingsDefaults defaults = Defaults();
            var data = defaults.CreateData();
            data.shakeIntensity = 7f;
            data.bulletOutlineScale = 0f;
            data.hudScale = 5f;
            data.bindingOverrides = null;
            defaults.Sanitize(data);
            Assert.AreEqual(1f, data.shakeIntensity);
            Assert.AreEqual(defaults.MinBulletOutlineScale, data.bulletOutlineScale);
            Assert.AreEqual(defaults.MaxHudScale, data.hudScale);
            Assert.AreEqual("", data.bindingOverrides);

            data.hudScale = 0.1f;
            data.bulletOutlineScale = 99f;
            defaults.Sanitize(data);
            Assert.AreEqual(defaults.MinHudScale, data.hudScale);
            Assert.AreEqual(defaults.MaxBulletOutlineScale, data.bulletOutlineScale);
        }

        [Test]
        public void AnOldFileWithTheShakeSwitchOffStaysOffAndOneWithItOnStaysOn()
        {
            SettingsDefaults defaults = Defaults();
            // A file written before D5 has no shakeIntensity: JsonUtility leaves the -1 marker.
            var off = JsonUtility.FromJson<SettingsData>("{\"version\":1,\"screenShake\":false}");
            var on = JsonUtility.FromJson<SettingsData>("{\"version\":1,\"screenShake\":true}");
            Assert.AreEqual(-1f, off.shakeIntensity);
            defaults.Sanitize(off);
            defaults.Sanitize(on);
            Assert.AreEqual(0f, off.shakeIntensity);
            Assert.AreEqual(1f, on.shakeIntensity);
        }

        [Test]
        public void TheNewSettingsSurviveASaveAndALoad()
        {
            string path = TempPath();
            try
            {
                SettingsService first = NewService(path, out _);
                first.Current.vsync = false;
                first.Current.shakeIntensity = 0.4f;
                first.Current.bulletOutlineScale = 2f;
                first.Current.highContrastBullets = true;
                first.Current.hudScale = 1.2f;
                first.Commit();
                first.Save();

                SettingsService second = NewService(path, out _);
                Assert.IsFalse(second.Current.vsync);
                Assert.AreEqual(0.4f, second.Current.shakeIntensity, 0.0001f);
                Assert.AreEqual(2f, second.Current.bulletOutlineScale, 0.0001f);
                Assert.IsTrue(second.Current.highContrastBullets);
                Assert.AreEqual(1.2f, second.Current.hudScale, 0.0001f);
            }
            finally
            {
                new JsonFileStore<SettingsData>(path).Delete();
            }
        }

        [Test]
        public void VSyncFollowsTheSetting()
        {
            int before = QualitySettings.vSyncCount;
            string path = TempPath();
            try
            {
                SettingsService service = NewService(path, out _);
                service.Current.vsync = false;
                service.Commit();
                Assert.AreEqual(0, QualitySettings.vSyncCount);
                service.Current.vsync = true;
                service.Commit();
                Assert.AreEqual(1, QualitySettings.vSyncCount);
            }
            finally
            {
                QualitySettings.vSyncCount = before;
                new JsonFileStore<SettingsData>(path).Delete();
            }
        }

        // ---- names of controls

        [Test]
        public void EachControllerFamilyNamesTheSameControlItsOwnWay()
        {
            Assert.AreEqual("R1", ControlLabels.Name(GlyphFamily.PlayStation, "<Gamepad>/rightShoulder"));
            Assert.AreEqual("RB", ControlLabels.Name(GlyphFamily.Xbox, "<Gamepad>/rightShoulder"));
            Assert.AreEqual("R", ControlLabels.Name(GlyphFamily.Nintendo, "<Gamepad>/rightShoulder"));
            Assert.AreEqual("Cross", ControlLabels.Name(GlyphFamily.PlayStation, "<Gamepad>/buttonSouth"));
            Assert.AreEqual("B", ControlLabels.Name(GlyphFamily.Nintendo, "<Gamepad>/buttonSouth"));   // Switch swaps the face labels
            Assert.AreEqual("L3", ControlLabels.Name(GlyphFamily.PlayStation, "<Gamepad>/leftStickPress"));
            Assert.AreEqual("", ControlLabels.Name(GlyphFamily.Touch, "<Gamepad>/buttonSouth"));
            Assert.AreEqual(GlyphButton.North, ControlLabels.FaceButton("<Gamepad>/buttonNorth"));
            Assert.IsNull(ControlLabels.FaceButton("<Gamepad>/rightTrigger"));
        }

        // ---- remapping

        [Test]
        public void TheDefaultButtonsAreTheDocumentedOnes()
        {
            string path = TempPath();
            try
            {
                var bindings = new InputBindingService(NewService(path, out _));
                Assert.AreEqual("<Gamepad>/rightShoulder", bindings.PathOf(RebindAction.Fire));
                Assert.AreEqual("<Gamepad>/leftStickPress", bindings.PathOf(RebindAction.Lock));
                Assert.AreEqual("<Gamepad>/rightTrigger", bindings.PathOf(RebindAction.Jump));
                Assert.AreEqual("<Gamepad>/buttonEast", bindings.PathOf(RebindAction.Ammo2));
                Assert.AreEqual("<Gamepad>/start", bindings.PathOf(RebindAction.Pause));
                Assert.AreEqual("R1", bindings.Label(RebindAction.Fire, GlyphFamily.PlayStation));
                Assert.AreEqual("RT", bindings.Label(RebindAction.Jump, GlyphFamily.Xbox));
                bindings.Dispose();
            }
            finally
            {
                new JsonFileStore<SettingsData>(path).Delete();
            }
        }

        [Test]
        public void RebindingChangesTheButtonAndIsSavedInTheSettings()
        {
            string path = TempPath();
            try
            {
                SettingsService settings = NewService(path, out _);
                var bindings = new InputBindingService(settings);
                Assert.AreEqual(RebindOutcome.Changed, bindings.SetBinding(RebindAction.Fire, "<Gamepad>/leftShoulder"));
                Assert.AreEqual("L1", bindings.Label(RebindAction.Fire, GlyphFamily.PlayStation));
                StringAssert.Contains("leftShoulder", settings.Current.bindingOverrides);
                Assert.IsTrue(settings.Dirty);

                // A new service (the next launch) reads it back from the saved file.
                settings.Save();
                bindings.Dispose();
                var next = new InputBindingService(NewService(path, out _));
                Assert.AreEqual("<Gamepad>/leftShoulder", next.PathOf(RebindAction.Fire));
                next.Dispose();
            }
            finally
            {
                new JsonFileStore<SettingsData>(path).Delete();
            }
        }

        [Test]
        public void ChoosingAButtonAnotherActionUsesSwapsTheTwo()
        {
            string path = TempPath();
            try
            {
                var bindings = new InputBindingService(NewService(path, out _));
                // Jump is on the right trigger, Fire on the right shoulder: put Jump on R1 and Fire takes the trigger.
                Assert.AreEqual(RebindOutcome.Swapped, bindings.SetBinding(RebindAction.Jump, "<Gamepad>/rightShoulder"));
                Assert.AreEqual("<Gamepad>/rightShoulder", bindings.PathOf(RebindAction.Jump));
                Assert.AreEqual("<Gamepad>/rightTrigger", bindings.PathOf(RebindAction.Fire));

                // No button is ever left on two actions.
                var seen = new System.Collections.Generic.HashSet<string>();
                foreach (RebindAction action in InputBindingService.All)
                    Assert.IsTrue(seen.Add(bindings.PathOf(action)), action + " shares a button");
                bindings.Dispose();
            }
            finally
            {
                new JsonFileStore<SettingsData>(path).Delete();
            }
        }

        [Test]
        public void ResetPutsEveryButtonBackAndClearsTheSavedOverrides()
        {
            string path = TempPath();
            try
            {
                SettingsService settings = NewService(path, out _);
                var bindings = new InputBindingService(settings);
                bindings.SetBinding(RebindAction.Ammo1, "<Gamepad>/leftTrigger");
                bindings.SetBinding(RebindAction.Jump, "<Gamepad>/buttonNorth");
                Assert.AreNotEqual("", settings.Current.bindingOverrides);

                bindings.ResetToDefaults();

                Assert.AreEqual("<Gamepad>/buttonSouth", bindings.PathOf(RebindAction.Ammo1));
                Assert.AreEqual("<Gamepad>/rightTrigger", bindings.PathOf(RebindAction.Jump));
                Assert.AreEqual("", settings.Current.bindingOverrides);   // an untouched layout is stored as nothing
                bindings.Dispose();
            }
            finally
            {
                new JsonFileStore<SettingsData>(path).Delete();
            }
        }

        [Test]
        public void SavedOverridesReachALiveInputSet()
        {
            string path = TempPath();
            try
            {
                SettingsService settings = NewService(path, out _);
                var bindings = new InputBindingService(settings);
                bindings.SetBinding(RebindAction.Fire, "<Gamepad>/leftTrigger");

                var live = new GameInput();
                InputBindingService.ApplyTo(live, settings.Current.bindingOverrides);
                Assert.AreEqual("<Gamepad>/leftTrigger", live.Gameplay.Fire.bindings[FindGamepad(live.Gameplay.Fire)].effectivePath);

                InputBindingService.ApplyTo(live, "");   // back to the defaults
                Assert.AreEqual("<Gamepad>/rightShoulder", live.Gameplay.Fire.bindings[FindGamepad(live.Gameplay.Fire)].effectivePath);
                Object.DestroyImmediate(live.asset);
                bindings.Dispose();
            }
            finally
            {
                new JsonFileStore<SettingsData>(path).Delete();
            }
        }

        private static int FindGamepad(UnityEngine.InputSystem.InputAction action)
        {
            for (int i = 0; i < action.bindings.Count; i++)
                if (action.bindings[i].path.StartsWith("<Gamepad>"))
                    return i;
            return -1;
        }

        // ---- prompts follow the player's buttons

        [Test]
        public void TutorialPromptsCanNameTheRemappedButtonInsteadOfTheDefault()
        {
            var library = ScriptableObject.CreateInstance<ButtonGlyphLibrary>();
            library.SetSets(new[]
            {
                new ButtonGlyphLibrary.FamilySet
                {
                    Family = GlyphFamily.PlayStation,
                    ActionLabels = new[] { "", "", "", "", "", "", "", "", "", "Right Stick", "Left Stick", "R1", "L3", "R2", "Cross", "Circle", "Square", "Triangle", "Create" },
                },
            });
            var keys = new[] { UiAction.Fire, UiAction.Aim };
            string Own(UiAction action) => action == UiAction.Fire ? "L2" : null;

            Assert.AreEqual("Hold [R1] and push [Left Stick].", TutorialText.Format(library, GlyphFamily.PlayStation, "Hold {0} and push {1}.", keys));
            Assert.AreEqual("Hold [L2] and push [Left Stick].",
                TutorialText.Format(library, GlyphFamily.PlayStation, "Hold {0} and push {1}.", keys, null, Own));   // only the remappable one changes
        }

        [Test]
        public void EveryGameplayButtonPromptMapsToARemappableAction()
        {
            foreach (UiAction action in new[] { UiAction.Fire, UiAction.Lock, UiAction.Jump, UiAction.Ammo1, UiAction.Ammo2, UiAction.Ammo3, UiAction.Ammo4 })
                Assert.IsTrue(InputBindingService.TryFromUiAction(action, out _), action + " should be remappable");
            Assert.IsFalse(InputBindingService.TryFromUiAction(UiAction.Move, out _));   // sticks stay put
            Assert.IsFalse(InputBindingService.TryFromUiAction(UiAction.Aim, out _));
        }
    }
}
