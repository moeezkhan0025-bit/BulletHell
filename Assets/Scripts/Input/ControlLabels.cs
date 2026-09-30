using BulletHell.Platform;
using BulletHell.UI;

namespace BulletHell.Input
{
    /// <summary>
    /// What a gamepad control is called on each controller family ("rightShoulder" is R1 on PlayStation, RB on Xbox, R on Switch). Used to
    /// show the player's own (possibly remapped) buttons in the Settings list, the tutorial and the HUD. Pure lookup, so tests need no device.
    /// </summary>
    public static class ControlLabels
    {
        private static readonly string[] Controls =
        {
            "buttonSouth", "buttonEast", "buttonWest", "buttonNorth", "leftShoulder", "rightShoulder", "leftTrigger", "rightTrigger",
            "leftStickPress", "rightStickPress", "start", "select",
        };

        private static readonly string[] PlayStation = { "Cross", "Circle", "Square", "Triangle", "L1", "R1", "L2", "R2", "L3", "R3", "Options", "Create" };
        private static readonly string[] Xbox = { "A", "B", "X", "Y", "LB", "RB", "LT", "RT", "LS Click", "RS Click", "Menu", "View" };
        private static readonly string[] Nintendo = { "B", "A", "Y", "X", "L", "R", "ZL", "ZR", "L Stick Click", "R Stick Click", "+", "-" };

        /// <summary>The last part of an Input System control path: "&lt;Gamepad&gt;/rightShoulder" gives "rightShoulder".</summary>
        public static string ControlName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "";
            int slash = path.LastIndexOf('/');
            return slash >= 0 ? path.Substring(slash + 1) : path;
        }

        /// <summary>The family's name for a gamepad control path, or an empty string when the family has none (touch) or the control is unknown.</summary>
        public static string Name(GlyphFamily family, string gamepadPath)
        {
            string[] names = family == GlyphFamily.PlayStation ? PlayStation : family == GlyphFamily.Xbox ? Xbox : family == GlyphFamily.Nintendo ? Nintendo : null;
            if (names == null)
                return "";
            string control = ControlName(gamepadPath);
            for (int i = 0; i < Controls.Length; i++)
                if (Controls[i] == control)
                    return names[i];
            return control;
        }

        /// <summary>The face button position of a control (for the HUD's round glyph pictures), or null when it is not a face button.</summary>
        public static GlyphButton? FaceButton(string gamepadPath)
        {
            switch (ControlName(gamepadPath))
            {
                case "buttonSouth": return GlyphButton.South;
                case "buttonEast": return GlyphButton.East;
                case "buttonWest": return GlyphButton.West;
                case "buttonNorth": return GlyphButton.North;
                default: return null;
            }
        }
    }
}
