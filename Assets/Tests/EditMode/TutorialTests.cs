using System.IO;
using BulletHell.Cosmetics;
using BulletHell.Platform;
using BulletHell.Save;
using BulletHell.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BulletHell.Tests
{
    /// <summary>D2 onboarding: the text built from the device's button names, the glyph asset's gameplay labels, the tutorial data, and the profile flag.</summary>
    public class TutorialTests
    {
        private static ButtonGlyphLibrary Library()
        {
            var library = ScriptableObject.CreateInstance<ButtonGlyphLibrary>();
            string[] basics = { "", "", "", "", "", "", "", "", "" };
            library.SetSets(new[]
            {
                new ButtonGlyphLibrary.FamilySet { Family = GlyphFamily.PlayStation, ActionLabels = Join(basics, "Right Stick", "Left Stick", "R1", "L3", "R2", "Cross", "Circle", "Square", "Triangle", "Create") },
                new ButtonGlyphLibrary.FamilySet { Family = GlyphFamily.Keyboard, ActionLabels = Join(basics, "WASD", "Mouse", "Click", "L", "Space", "1", "2", "3", "4", "Tab") },
                new ButtonGlyphLibrary.FamilySet { Family = GlyphFamily.Touch, ActionLabels = basics },
            });
            return library;
        }

        private static string[] Join(string[] first, params string[] more)
        {
            var all = new string[first.Length + more.Length];
            first.CopyTo(all, 0);
            more.CopyTo(all, first.Length);
            return all;
        }

        [Test]
        public void TextNamesTheButtonsOfTheDeviceInUse()
        {
            ButtonGlyphLibrary library = Library();
            var keys = new[] { UiAction.Lock, UiAction.Aim };
            const string template = "Press {0} to lock, then aim with {1}.";

            Assert.AreEqual("Press [L3] to lock, then aim with [Left Stick].", TutorialText.Format(library, GlyphFamily.PlayStation, template, keys));
            Assert.AreEqual("Press [L] to lock, then aim with [Mouse].", TutorialText.Format(library, GlyphFamily.Keyboard, template, keys));
        }

        [Test]
        public void KeysCanBeColouredAndAMissingButtonIsLeftOut()
        {
            ButtonGlyphLibrary library = Library();
            Assert.AreEqual("Hold <color=#A7781A>[R1]</color> to fire.",
                TutorialText.Format(library, GlyphFamily.PlayStation, "Hold {0} to fire.", new[] { UiAction.Fire }, "#A7781A"));
            // Touch has no name for the control: the brackets disappear and the spaces close up.
            Assert.AreEqual("Hold to fire.", TutorialText.Format(library, GlyphFamily.Touch, "Hold {0} to fire.", new[] { UiAction.Fire }));
            Assert.AreEqual("", TutorialText.Format(library, GlyphFamily.PlayStation, "", new UiAction[0]));
        }

        [Test]
        public void TheGlyphAssetNamesEveryGameplayControlOnEveryPadAndKeyboard()
        {
            var library = AssetDatabase.LoadAssetAtPath<ButtonGlyphLibrary>("Assets/Data/UI/ButtonGlyphs.asset");
            Assert.IsNotNull(library);
            foreach (GlyphFamily family in new[] { GlyphFamily.PlayStation, GlyphFamily.Xbox, GlyphFamily.Nintendo, GlyphFamily.Keyboard })
                for (UiAction action = UiAction.Move; action <= UiAction.SkipTutorial; action++)
                    Assert.IsNotEmpty(library.LabelFor(family, action), family + " has no label for " + action);

            // The menu prompts that were there before are untouched.
            Assert.AreEqual("Cross", library.LabelFor(GlyphFamily.PlayStation, UiAction.Confirm));
            Assert.AreEqual("R2", library.LabelFor(GlyphFamily.PlayStation, UiAction.Jump));
            Assert.AreEqual("ZR", library.LabelFor(GlyphFamily.Nintendo, UiAction.Jump));
            Assert.AreEqual("RT", library.LabelFor(GlyphFamily.Xbox, UiAction.Jump));
        }

        [Test]
        public void TheTutorialTeachesTheSixControlsInOrderAndEnemiesWaitForTheBasics()
        {
            var data = AssetDatabase.LoadAssetAtPath<TutorialData>("Assets/Data/UI/Tutorial.asset");
            Assert.IsNotNull(data);
            var kinds = new[] { TutorialStepKind.Move, TutorialStepKind.SelectArm, TutorialStepKind.Fire, TutorialStepKind.Lock, TutorialStepKind.Jump, TutorialStepKind.SwapAmmo };
            Assert.AreEqual(kinds.Length, data.Steps.Length);
            for (int i = 0; i < kinds.Length; i++)
            {
                Assert.AreEqual(kinds[i], data.Steps[i].Kind);
                Assert.IsNotEmpty(data.Steps[i].Text);
                Assert.IsNotEmpty(data.Steps[i].Keys, "step " + i + " names a control");
            }
            Assert.AreEqual(1, data.Round);
            Assert.AreEqual(3, data.EnemiesWaitForSteps);   // move, select, fire
        }

        [Test]
        public void TheTutorialIsNotDoneOnAFreshProfileAndTheFlagIsSaved()
        {
            string path = Path.Combine(Path.GetTempPath(), "tutorial_profile_" + System.Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var store = new JsonFileStore<ProfileData>(path);
                var profile = new ProfileService(null, store);
                Assert.IsFalse(profile.TutorialDone);

                profile.SetTutorialDone(true);
                var reloaded = new ProfileService(null, new JsonFileStore<ProfileData>(path));
                Assert.IsTrue(reloaded.TutorialDone);

                reloaded.SetTutorialDone(false);   // Settings > Replay Tutorial
                Assert.IsFalse(new ProfileService(null, new JsonFileStore<ProfileData>(path)).TutorialDone);
            }
            finally
            {
                new JsonFileStore<ProfileData>(path).Delete();
            }
        }

        [Test]
        public void AProfileWrittenBeforeTheTutorialExistedCountsAsNotDone()
        {
            var old = JsonUtility.FromJson<ProfileData>("{\"version\":2,\"cosmetics\":[\"a\",\"b\",\"c\",\"d\",\"e\"]}");
            Assert.IsFalse(old.tutorialDone);
        }
    }
}
