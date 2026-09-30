using System.Text;
using TMPro;
using BulletHell.Core;
using BulletHell.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// A line of button prompts ("[Cross] Buy   [Triangle] Reroll") that follows the device in use: it is rebuilt from the
    /// ButtonGlyphLibrary whenever the player switches between PlayStation / Xbox / Nintendo / keyboard. Prompts for
    /// actions the current device has no button for are left out. Put on a TMP_Text; screens call <see cref="Show"/>.
    /// </summary>
    public sealed class PromptHint : MonoBehaviour
    {
        public readonly struct Prompt
        {
            public readonly UiAction Action;
            public readonly string Label;

            public Prompt(UiAction action, string label)
            {
                Action = action;
                Label = label;
            }
        }

        private static readonly StringBuilder Builder = new StringBuilder(160);

        private TMP_Text text;
        private Prompt[] prompts = new Prompt[0];

        /// <summary>Sets the prompts on a TMP_Text (adding the PromptHint component on first use) and renders them.</summary>
        public static void Show(TMP_Text target, params Prompt[] prompts)
        {
            if (target == null)
                return;
            if (!target.TryGetComponent(out PromptHint hint))
                hint = target.gameObject.AddComponent<PromptHint>();
            hint.prompts = prompts;
            hint.Render();
        }

        public static Prompt P(UiAction action, string label) => new Prompt(action, label);

        /// <summary>The line for a family. Static so tests can check it without a scene.</summary>
        public static string Build(ButtonGlyphLibrary library, GlyphFamily family, Prompt[] prompts)
        {
            Builder.Clear();
            foreach (Prompt prompt in prompts)
            {
                string key = library != null ? library.LabelFor(family, prompt.Action) : "";
                if (string.IsNullOrEmpty(key))
                    continue;
                if (Builder.Length > 0)
                    Builder.Append("     ");
                Builder.Append('[').Append(key).Append("] ").Append(prompt.Label);
            }
            return Builder.ToString();
        }

        private void Awake() => text = GetComponent<TMP_Text>();

        private void OnEnable()
        {
            InputDeviceWatcher.Changed += OnDeviceChanged;
            Render();
        }

        private void OnDisable() => InputDeviceWatcher.Changed -= OnDeviceChanged;

        private void OnDeviceChanged(GlyphFamily _) => Render();

        private void Render()
        {
            if (text == null)
                text = GetComponent<TMP_Text>();
            var config = Resources.Load<GameConfig>(GameConfig.ResourcePath);
            text.text = Build(config != null ? config.ButtonGlyphs : null, GlyphFamilyDetector.Current(), prompts);
        }
    }
}
