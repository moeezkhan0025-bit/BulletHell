using UnityEngine;
using UnityEngine.InputSystem;

namespace BulletHell.Platform
{
    /// <summary>Which set of button pictures to show: the family of the controller in the player's hands.</summary>
    public enum GlyphFamily { PlayStation, Xbox, Nintendo, Touch }

    /// <summary>Works out the glyph family of the active controller. The only place that asks which controller it is.</summary>
    public static class GlyphFamilyDetector
    {
        /// <summary>The family of the last-used gamepad. Without one: Touch on phones, PlayStation otherwise (the dev pad).</summary>
        public static GlyphFamily Current()
        {
            Gamepad pad = Gamepad.current;
            if (pad == null)
                return Application.isMobilePlatform ? GlyphFamily.Touch : GlyphFamily.PlayStation;
            return Of(pad);
        }

        public static GlyphFamily Of(InputDevice device)
        {
            string layout = device.layout;
            if (InputSystem.IsFirstLayoutBasedOnSecond(layout, "DualShockGamepad"))
                return GlyphFamily.PlayStation;
            if (InputSystem.IsFirstLayoutBasedOnSecond(layout, "SwitchProControllerHID"))
                return GlyphFamily.Nintendo;
            if (InputSystem.IsFirstLayoutBasedOnSecond(layout, "XInputController"))
                return GlyphFamily.Xbox;

            string maker = device.description.manufacturer ?? "";
            if (maker.IndexOf("Sony", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return GlyphFamily.PlayStation;
            if (maker.IndexOf("Nintendo", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return GlyphFamily.Nintendo;
            return GlyphFamily.Xbox;
        }
    }
}
