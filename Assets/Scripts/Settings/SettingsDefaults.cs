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
        [SerializeField] private bool showDebugOverlay = true;
        [SerializeField] private bool fullscreen = true;

        [Header("Aim sensitivity")]
        [Tooltip("Scales the InputTuning thresholds: higher = the arm selects with less stick travel.")]
        [SerializeField, Min(0.1f)] private float aimSensitivity = 1f;
        [Tooltip("Safe limits so no setting can make arm selection unusable.")]
        [SerializeField, Min(0.1f)] private float minAimSensitivity = 0.6f;
        [SerializeField, Min(0.1f)] private float maxAimSensitivity = 1.5f;
        [SerializeField, Min(0.01f)] private float aimSensitivityStep = 0.1f;

        [Header("Steps")]
        [SerializeField, Range(0.01f, 0.5f)] private float volumeStep = 0.1f;

        public float MinAimSensitivity => minAimSensitivity;
        public float MaxAimSensitivity => Mathf.Max(minAimSensitivity, maxAimSensitivity);
        public float AimSensitivityStep => aimSensitivityStep;
        public float VolumeStep => volumeStep;

        public SettingsData CreateData() => new SettingsData
        {
            masterVolume = masterVolume,
            musicVolume = musicVolume,
            sfxVolume = sfxVolume,
            screenShake = screenShake,
            vibration = vibration,
            aimSensitivity = aimSensitivity,
            showDebugOverlay = showDebugOverlay,
            fullscreen = fullscreen,
        };

        /// <summary>Forces every value into its allowed range (a hand-edited or old file can't break the game).</summary>
        public void Sanitize(SettingsData data)
        {
            data.masterVolume = Mathf.Clamp01(data.masterVolume);
            data.musicVolume = Mathf.Clamp01(data.musicVolume);
            data.sfxVolume = Mathf.Clamp01(data.sfxVolume);
            data.aimSensitivity = Mathf.Clamp(data.aimSensitivity, MinAimSensitivity, MaxAimSensitivity);
            data.resolutionWidth = Mathf.Max(0, data.resolutionWidth);
            data.resolutionHeight = Mathf.Max(0, data.resolutionHeight);
            if (data.resolutionWidth == 0 || data.resolutionHeight == 0)
                data.resolutionWidth = data.resolutionHeight = 0;
        }
    }
}
