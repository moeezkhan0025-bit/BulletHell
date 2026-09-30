using UnityEngine;

namespace BulletHell.Settings
{
    /// <summary>Default values, steps and safe limits for the player-facing settings.</summary>
    [CreateAssetMenu(fileName = "SettingsDefaults", menuName = "BulletHell/Settings Defaults")]
    public sealed class SettingsDefaults : ScriptableObject
    {
        [Header("Defaults")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;
        [SerializeField] private bool screenShake = true;
        [SerializeField] private bool vibration = true;
        [SerializeField] private bool showDebugOverlay = false;
        [SerializeField] private bool showAiDebug = false;
        [SerializeField] private bool fullscreen = true;
        [SerializeField] private bool vsync = true;
        [SerializeField, Range(0f, 1f)] private float shakeIntensity = 1f;
        [SerializeField, Range(0.5f, 3f)] private float bulletOutlineScale = 1f;
        [SerializeField] private bool highContrastBullets = false;
        [SerializeField, Range(0.5f, 2f)] private float hudScale = 1f;

        [Header("Aim sensitivity")]
        [Tooltip("Scales the InputTuning thresholds: higher = the arm selects with less stick travel.")]
        [SerializeField, Min(0.1f)] private float aimSensitivity = 1f;
        [Tooltip("Safe limits so no setting can make arm selection unusable.")]
        [SerializeField, Min(0.1f)] private float minAimSensitivity = 0.6f;
        [SerializeField, Min(0.1f)] private float maxAimSensitivity = 1.5f;
        [SerializeField, Min(0.01f)] private float aimSensitivityStep = 0.1f;

        [Header("Limits and steps")]
        [SerializeField, Range(0.05f, 0.5f)] private float shakeStep = 0.1f;
        [SerializeField, Range(0.5f, 1f)] private float minBulletOutlineScale = 0.5f;
        [SerializeField, Range(1f, 4f)] private float maxBulletOutlineScale = 3f;
        [SerializeField, Range(0.05f, 0.5f)] private float bulletOutlineStep = 0.25f;
        [Tooltip("The HUD stays inside the safe area at every allowed scale; beyond about 1.3 it starts to cover the arena.")]
        [SerializeField, Range(0.5f, 1f)] private float minHudScale = 0.8f;
        [SerializeField, Range(1f, 2f)] private float maxHudScale = 1.3f;
        [SerializeField, Range(0.01f, 0.25f)] private float hudScaleStep = 0.05f;

        [Header("Steps")]
        [SerializeField, Range(0.01f, 0.5f)] private float volumeStep = 0.1f;

        public float MinAimSensitivity => minAimSensitivity;
        public float MaxAimSensitivity => Mathf.Max(minAimSensitivity, maxAimSensitivity);
        public float AimSensitivityStep => aimSensitivityStep;
        public float VolumeStep => volumeStep;
        public float ShakeStep => shakeStep;
        public float MinBulletOutlineScale => minBulletOutlineScale;
        public float MaxBulletOutlineScale => Mathf.Max(minBulletOutlineScale, maxBulletOutlineScale);
        public float BulletOutlineStep => bulletOutlineStep;
        public float MinHudScale => minHudScale;
        public float MaxHudScale => Mathf.Max(minHudScale, maxHudScale);
        public float HudScaleStep => hudScaleStep;

        public SettingsData CreateData() => new SettingsData
        {
            masterVolume = masterVolume,
            musicVolume = musicVolume,
            sfxVolume = sfxVolume,
            screenShake = screenShake,
            vibration = vibration,
            aimSensitivity = aimSensitivity,
            showDebugOverlay = showDebugOverlay,
            showAiDebug = showAiDebug,
            fullscreen = fullscreen,
            vsync = vsync,
            shakeIntensity = shakeIntensity,
            bulletOutlineScale = bulletOutlineScale,
            highContrastBullets = highContrastBullets,
            hudScale = hudScale,
            bindingOverrides = "",
        };

        /// <summary>Forces every value into its allowed range (a hand-edited or old file can't break the game).</summary>
        public void Sanitize(SettingsData data)
        {
            data.masterVolume = Mathf.Clamp01(data.masterVolume);
            data.musicVolume = Mathf.Clamp01(data.musicVolume);
            data.sfxVolume = Mathf.Clamp01(data.sfxVolume);
            data.aimSensitivity = Mathf.Clamp(data.aimSensitivity, MinAimSensitivity, MaxAimSensitivity);
            if (data.shakeIntensity < 0f)
                data.shakeIntensity = data.screenShake ? 1f : 0f;   // a file from before D5 only had the on / off switch
            data.shakeIntensity = Mathf.Clamp01(data.shakeIntensity);
            data.bulletOutlineScale = Mathf.Clamp(data.bulletOutlineScale, MinBulletOutlineScale, MaxBulletOutlineScale);
            data.hudScale = Mathf.Clamp(data.hudScale, MinHudScale, MaxHudScale);
            data.bindingOverrides ??= "";
            data.resolutionWidth = Mathf.Max(0, data.resolutionWidth);
            data.resolutionHeight = Mathf.Max(0, data.resolutionHeight);
            if (data.resolutionWidth == 0 || data.resolutionHeight == 0)
                data.resolutionWidth = data.resolutionHeight = 0;
        }
    }
}
