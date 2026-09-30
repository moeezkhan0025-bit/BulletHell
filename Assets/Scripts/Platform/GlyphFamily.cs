using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace BulletHell.Platform
{
    /// <summary>Which set of button pictures to show: the family of the device in the player's hands.</summary>
    public enum GlyphFamily { PlayStation, Xbox, Nintendo, Touch, Keyboard }

    /// <summary>Works out the glyph family of the active device. The only place that asks which controller it is.</summary>
    public static class GlyphFamilyDetector
    {
        /// <summary>The family of the device used last (see <see cref="InputDeviceWatcher"/>).</summary>
        public static GlyphFamily Current() => InputDeviceWatcher.Family;

        /// <summary>The family of a gamepad.</summary>
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
            if (maker.IndexOf("Sony", StringComparison.OrdinalIgnoreCase) >= 0)
                return GlyphFamily.PlayStation;
            if (maker.IndexOf("Nintendo", StringComparison.OrdinalIgnoreCase) >= 0)
                return GlyphFamily.Nintendo;
            return GlyphFamily.Xbox;
        }
    }

    /// <summary>
    /// Tracks which kind of device the player pressed something on last: a gamepad (its family) or the keyboard / mouse.
    /// Prompts listen to <see cref="Changed"/> so they switch live when the player swaps devices. Before any press it is
    /// the connected gamepad's family, else Touch on phones, else Keyboard.
    /// </summary>
    public static class InputDeviceWatcher
    {
        private static bool started;
        private static bool hasPressed;
        private static GlyphFamily family;
        private static IDisposable subscription;

        public static event Action<GlyphFamily> Changed;

        public static GlyphFamily Family
        {
            get
            {
                Start();
                return hasPressed ? family : Initial();
            }
        }

        private static GlyphFamily Initial()
        {
            Gamepad pad = Gamepad.current;
            if (pad != null)
                return GlyphFamilyDetector.Of(pad);
            return Application.isMobilePlatform ? GlyphFamily.Touch : GlyphFamily.Keyboard;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            subscription?.Dispose();
            subscription = null;
            started = false;
            hasPressed = false;
            Changed = null;
        }

        private static void Start()
        {
            if (started)
                return;
            started = true;
            subscription = InputSystem.onAnyButtonPress.Call(control => Report(control.device));
        }

        /// <summary>Records that a device was just used. Public so tests and touch code can drive it.</summary>
        public static void Report(InputDevice device)
        {
            GlyphFamily next;
            if (device is Gamepad)
                next = GlyphFamilyDetector.Of(device);
            else if (device is Keyboard || device is Mouse)
                next = GlyphFamily.Keyboard;
            else
                return;
            Set(next);
        }

        /// <summary>Forces a family (tests, touch input).</summary>
        public static void Set(GlyphFamily next)
        {
            GlyphFamily before = Family;
            hasPressed = true;
            family = next;
            if (before != next)
                Changed?.Invoke(next);
        }
    }
}
