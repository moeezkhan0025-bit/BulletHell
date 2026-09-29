using System;
using BulletHell.Core;
using BulletHell.Platform;
using BulletHell.Save;
using UnityEngine;

namespace BulletHell.Settings
{
    /// <summary>
    /// The player-facing settings: loaded from the settings file at startup, applied immediately whenever something
    /// changes (Commit), and written back with Save (the settings screen saves when it closes, and the app saves on
    /// quit/pause). The file is separate from the run save and is never deleted by New Game or Game Over.
    /// Screen shake and vibration are only stored and exposed here; whatever implements them reads Current.
    /// </summary>
    public sealed class SettingsService
    {
        private readonly SettingsDefaults defaults;
        private readonly JsonFileStore<SettingsData> store;
        private readonly AudioService audio;

        private bool appliedDisplay;
        private bool appliedFullscreen;
        private int appliedWidth, appliedHeight;

        public SettingsData Current { get; private set; }
        public SettingsDefaults Defaults => defaults;
        public bool Dirty { get; private set; }

        /// <summary>Raised after any value changed and was applied.</summary>
        public event Action Changed;

        public SettingsService(SettingsDefaults settingsDefaults, JsonFileStore<SettingsData> fileStore, AudioService audioService)
        {
            defaults = settingsDefaults;
            store = fileStore;
            audio = audioService;
            Load();
            Apply();
        }

        /// <summary>Reads the file. A missing, unreadable or other-version file gives the defaults.</summary>
        public void Load()
        {
            if (store.TryLoad(out SettingsData loaded) && loaded.version == SettingsData.CurrentVersion)
            {
                Current = loaded;
            }
            else
            {
                Current = defaults.CreateData();
            }
            defaults.Sanitize(Current);
            Dirty = false;
        }

        /// <summary>Call after editing Current: clamps, applies the change now and raises Changed.</summary>
        public void Commit()
        {
            defaults.Sanitize(Current);
            Apply();
            Dirty = true;
            Changed?.Invoke();
        }

        public void ResetToDefaults()
        {
            Current = defaults.CreateData();
            Commit();
        }

        public void Save()
        {
            if (!Dirty)
                return;
            store.Save(Current);
            Dirty = false;
        }

        private void Apply()
        {
            AudioListener.volume = Current.masterVolume;
            audio.SetVolumes(Current.musicVolume, Current.sfxVolume);
            ApplyDisplay();
        }

        private void ApplyDisplay()
        {
            // Never resize the Editor's Game view; on non-desktop platforms there is nothing to choose.
            if (Application.isEditor || !PlatformCapabilities.SupportsDisplaySettings)
                return;

            if (appliedDisplay && appliedFullscreen == Current.fullscreen &&
                appliedWidth == Current.resolutionWidth && appliedHeight == Current.resolutionHeight)
                return;

            appliedDisplay = true;
            appliedFullscreen = Current.fullscreen;
            appliedWidth = Current.resolutionWidth;
            appliedHeight = Current.resolutionHeight;

            FullScreenMode mode = Current.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (Current.resolutionWidth > 0)
                Screen.SetResolution(Current.resolutionWidth, Current.resolutionHeight, mode);
            else
                Screen.fullScreenMode = mode;
        }
    }
}
