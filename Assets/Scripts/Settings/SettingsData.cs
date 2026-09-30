using System;

namespace BulletHell.Settings
{
    /// <summary>What the settings file stores. Plain data; defaults and limits live on SettingsDefaults.</summary>
    [Serializable]
    public sealed class SettingsData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        /// <summary>Kept in step with <see cref="shakeIntensity"/> (intensity above zero). Older files stored only this switch.</summary>
        public bool screenShake = true;
        public bool vibration = true;
        public float aimSensitivity = 1f;
        public bool showDebugOverlay = false;
        /// <summary>Development builds: draws the enemy flow field and line-of-sight rays.</summary>
        public bool showAiDebug = false;
        public bool fullscreen = true;
        /// <summary>Window/screen size on PC. 0 x 0 = leave it as the platform has it.</summary>
        public int resolutionWidth;
        public int resolutionHeight;
        public bool vsync = true;
        /// <summary>Screen shake strength, 0..1. -1 = a file written before D5: Load turns the old on / off switch into 1 or 0.</summary>
        public float shakeIntensity = -1f;
        /// <summary>Multiplies the thickness of the enemy bullet outline (the palette's pixels at 1080p).</summary>
        public float bulletOutlineScale = 1f;
        /// <summary>Enemy bullets get a white halo and a thicker outline so they read on any floor.</summary>
        public bool highContrastBullets;
        /// <summary>Scale of the combat HUD (hearts, heat, ammo, round and currency pills, boss bar).</summary>
        public float hudScale = 1f;
        /// <summary>Input System binding overrides of the gameplay buttons (JSON from SaveBindingOverridesAsJson). Empty = the defaults.</summary>
        public string bindingOverrides = "";
    }
}
