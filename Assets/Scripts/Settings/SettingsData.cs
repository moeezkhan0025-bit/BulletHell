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
        public bool screenShake = true;
        public bool vibration = true;
        public float aimSensitivity = 1f;
        public bool showDebugOverlay = true;
        public bool fullscreen = true;
        /// <summary>Window/screen size on PC. 0 x 0 = leave it as the platform has it.</summary>
        public int resolutionWidth;
        public int resolutionHeight;
    }
}
