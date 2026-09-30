using System.Reflection;
using BulletHell.Platform;
using BulletHell.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.Tests
{
    public class M9dTests
    {
        private static ButtonGlyphLibrary Library()
        {
            var library = ScriptableObject.CreateInstance<ButtonGlyphLibrary>();
            library.SetSets(new[]
            {
                new ButtonGlyphLibrary.FamilySet { Family = GlyphFamily.PlayStation, ActionLabels = new[] { "Cross", "Circle", "Square", "Square", "Triangle", "Triangle", "L1", "R1", "Options" } },
                new ButtonGlyphLibrary.FamilySet { Family = GlyphFamily.Xbox, ActionLabels = new[] { "A", "B", "X", "X", "Y", "Y", "LB", "RB", "Menu" } },
                new ButtonGlyphLibrary.FamilySet { Family = GlyphFamily.Keyboard, ActionLabels = new[] { "Enter", "Esc", "R", "Q", "T", "X", "[", "]", "" } },
            });
            return library;
        }

        [Test]
        public void PromptsUseTheLabelsOfTheDeviceFamily()
        {
            ButtonGlyphLibrary library = Library();
            var prompts = new[] { PromptHint.P(UiAction.Confirm, "Buy"), PromptHint.P(UiAction.Back, "Leave") };

            Assert.AreEqual("[Cross] Buy     [Circle] Leave", PromptHint.Build(library, GlyphFamily.PlayStation, prompts));
            Assert.AreEqual("[A] Buy     [B] Leave", PromptHint.Build(library, GlyphFamily.Xbox, prompts));
            Assert.AreEqual("[Enter] Buy     [Esc] Leave", PromptHint.Build(library, GlyphFamily.Keyboard, prompts));
        }

        [Test]
        public void APromptWithNoButtonOnTheDeviceIsLeftOut()
        {
            ButtonGlyphLibrary library = Library();
            var prompts = new[] { PromptHint.P(UiAction.Confirm, "Select"), PromptHint.P(UiAction.Start, "Fight!") };

            Assert.AreEqual("[Options] Fight!", PromptHint.Build(library, GlyphFamily.PlayStation, new[] { prompts[1] }));
            Assert.AreEqual("[Enter] Select", PromptHint.Build(library, GlyphFamily.Keyboard, prompts));
            Assert.AreEqual("", PromptHint.Build(null, GlyphFamily.Keyboard, prompts));
        }

        [Test]
        public void SwitchingDeviceRaisesChangedOnceAndUpdatesTheFamily()
        {
            InputDeviceWatcher.Set(GlyphFamily.Keyboard);
            int changes = 0;
            GlyphFamily last = GlyphFamily.PlayStation;
            System.Action<GlyphFamily> handler = family => { changes++; last = family; };
            InputDeviceWatcher.Changed += handler;
            try
            {
                InputDeviceWatcher.Set(GlyphFamily.Xbox);
                InputDeviceWatcher.Set(GlyphFamily.Xbox);   // same device again: no event
                InputDeviceWatcher.Set(GlyphFamily.PlayStation);

                Assert.AreEqual(2, changes);
                Assert.AreEqual(GlyphFamily.PlayStation, last);
                Assert.AreEqual(GlyphFamily.PlayStation, GlyphFamilyDetector.Current());
            }
            finally
            {
                InputDeviceWatcher.Changed -= handler;
            }
        }

        [Test]
        public void UiSoundHooksCountEvenWithoutClips()
        {
            int before = UiSound.RequestCount;
            UiSound.Play(UiSoundKind.Buy);
            UiSound.Play(UiSoundKind.Error);

            Assert.AreEqual(before + 2, UiSound.RequestCount);
            Assert.AreEqual(UiSoundKind.Error, UiSound.Last);
        }

        [Test]
        public void ConfirmDialogOpensOnNoAndBlocksStartUntilAnswered()
        {
            var root = new GameObject("dialog", typeof(RectTransform));
            try
            {
                var dialog = root.AddComponent<ConfirmDialog>();
                Button yes = Child<Button>(root, "yes");
                Button no = Child<Button>(root, "no");
                Set(dialog, "titleLabel", Child<Text>(root, "t"));
                Set(dialog, "messageLabel", Child<Text>(root, "m"));
                Set(dialog, "yesLabel", Child<Text>(root, "yl"));
                Set(dialog, "noLabel", Child<Text>(root, "nl"));
                Set(dialog, "yesButton", yes);
                Set(dialog, "noButton", no);
                typeof(ConfirmDialog).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(dialog, null);

                int confirmed = 0, declined = 0;
                root.SetActive(false);
                dialog.Ask("Q", "M", "Yes", "No", () => confirmed++, () => declined++);
                Assert.IsTrue(ConfirmDialog.AnyOpen);

                no.onClick.Invoke();
                Assert.IsFalse(ConfirmDialog.AnyOpen);
                Assert.AreEqual(0, confirmed);
                Assert.AreEqual(1, declined);

                dialog.Ask("Q", "M", "Yes", "No", () => confirmed++);
                yes.onClick.Invoke();
                Assert.IsFalse(ConfirmDialog.AnyOpen);
                Assert.AreEqual(1, confirmed);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static T Child<T>(GameObject parent, string name) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go.AddComponent<T>();
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }
}
