using System.Text;
using BulletHell.Platform;

namespace BulletHell.UI
{
    /// <summary>Turns a tutorial template ("Push {0} to move") into the text for the device in use. Static so tests can check it without a scene.</summary>
    public static class TutorialText
    {
        /// <summary>
        /// labelOverride gives the player's own name for a control (null = use the library).
        /// {i} becomes "[label]" of keys[i] for the family (wrapped in a colour tag when keyColorHex is given). A control the
        /// device has no button for becomes nothing. Doubled spaces left behind are closed up.
        /// </summary>
        public static string Format(ButtonGlyphLibrary library, GlyphFamily family, string template, UiAction[] keys, string keyColorHex = null,
                                    System.Func<UiAction, string> labelOverride = null)
        {
            if (string.IsNullOrEmpty(template))
                return "";
            var builder = new StringBuilder(template.Length + 24);
            for (int i = 0; i < template.Length; i++)
            {
                char c = template[i];
                if (c == '{' && i + 2 < template.Length && template[i + 2] == '}' && char.IsDigit(template[i + 1]))
                {
                    int index = template[i + 1] - '0';
                    string label = keys != null && index < keys.Length && library != null ? library.LabelFor(family, keys[index]) : "";
                    if (keys != null && index < keys.Length && labelOverride != null)
                    {
                        string own = labelOverride(keys[index]);   // the player's own (remapped) button, when the action can be remapped
                        if (own != null)
                            label = own;
                    }
                    if (!string.IsNullOrEmpty(label))
                    {
                        if (keyColorHex != null)
                            builder.Append("<color=").Append(keyColorHex).Append('>');
                        builder.Append('[').Append(label).Append(']');
                        if (keyColorHex != null)
                            builder.Append("</color>");
                    }
                    i += 2;
                    continue;
                }
                builder.Append(c);
            }
            string text = builder.ToString();
            while (text.Contains("  "))
                text = text.Replace("  ", " ");
            return text.Trim();
        }
    }
}
