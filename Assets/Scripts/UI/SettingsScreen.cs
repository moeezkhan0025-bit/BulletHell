using System;
using System.Collections.Generic;
using BulletHell.Core;
using BulletHell.Platform;
using BulletHell.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// The Settings screen, an overlay that opens from the Main Menu and from Pause. Every change is applied at once
    /// (SettingsService.Commit); the file is written when the screen closes. Back (button, or Cancel on any row) returns
    /// to whoever opened it. Left/right adjusts a row, up/down moves between rows.
    /// </summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        [SerializeField] private SettingRow rowPrefab;
        [SerializeField] private Transform rowParent;
        [SerializeField] private Button backButton;
        [Tooltip("Optional: the button prompt line.")]
        [SerializeField] private Text hintLabel;

        private readonly List<SettingRow> rows = new List<SettingRow>();
        private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
        private SettingsService settings;
        private Action onClosed;
        private bool built;

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>Shows the screen. onClosed is called after Back.</summary>
        public void Open(Action closed)
        {
            ScreenTransition.In(gameObject);
            PromptHint.Show(hintLabel, PromptHint.P(UiAction.Confirm, "Select"), PromptHint.P(UiAction.Back, "Back"));
            EnsureBuilt();
            onClosed = closed;
            foreach (SettingRow row in rows)
                row.Refresh();
            UIFocusGuard.Focus(rows[0].gameObject);
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            settings.Save();
            gameObject.SetActive(false);
            Action callback = onClosed;
            onClosed = null;
            callback?.Invoke();
        }

        private void OnDisable() => settings?.Save();

        private void EnsureBuilt()
        {
            if (built)
                return;
            built = true;

            settings = GameServices.Ensure().Settings;
            SettingsDefaults defaults = settings.Defaults;

            AddRow("Master volume", () => Percent(settings.Current.masterVolume),
                dir => Step(settings.Current.masterVolume, dir, defaults.VolumeStep, 0f, 1f, v => settings.Current.masterVolume = v));
            AddRow("Music volume", () => Percent(settings.Current.musicVolume),
                dir => Step(settings.Current.musicVolume, dir, defaults.VolumeStep, 0f, 1f, v => settings.Current.musicVolume = v));
            AddRow("SFX volume", () => Percent(settings.Current.sfxVolume),
                dir => Step(settings.Current.sfxVolume, dir, defaults.VolumeStep, 0f, 1f, v => settings.Current.sfxVolume = v));
            AddRow("Screen shake", () => OnOff(settings.Current.screenShake), _ => Toggle(v => settings.Current.screenShake = v, settings.Current.screenShake));
            AddRow("Controller vibration", () => OnOff(settings.Current.vibration), _ => Toggle(v => settings.Current.vibration = v, settings.Current.vibration));
            AddRow("Aim sensitivity", () => settings.Current.aimSensitivity.ToString("0.0") + "x",
                dir => Step(settings.Current.aimSensitivity, dir, defaults.AimSensitivityStep, defaults.MinAimSensitivity, defaults.MaxAimSensitivity,
                            v => settings.Current.aimSensitivity = v));

            if (Debug.isDebugBuild)
            {
                AddRow("Show debug overlay", () => OnOff(settings.Current.showDebugOverlay),
                    _ => Toggle(v => settings.Current.showDebugOverlay = v, settings.Current.showDebugOverlay));
                AddRow("Show AI debug", () => OnOff(settings.Current.showAiDebug),
                    _ => Toggle(v => settings.Current.showAiDebug = v, settings.Current.showAiDebug));
            }

            if (PlatformCapabilities.SupportsDisplaySettings)
            {
                BuildResolutionList();
                AddRow("Fullscreen", () => OnOff(settings.Current.fullscreen), _ => Toggle(v => settings.Current.fullscreen = v, settings.Current.fullscreen));
                AddRow("Resolution", ResolutionText, AdjustResolution);
            }

            backButton.onClick.AddListener(Close);
            if (backButton.TryGetComponent(out CancelRelay relay))
                relay.Cancelled += Close;
            backButton.transform.SetAsLastSibling();
        }

        private void AddRow(string labelText, Func<string> value, Action<int> adjust)
        {
            SettingRow row = Instantiate(rowPrefab, rowParent);
            row.Bind(labelText, value, adjust);
            row.Cancelled += Close;
            rows.Add(row);
        }

        private void Step(float current, int direction, float step, float min, float max, Action<float> set)
        {
            float next = Mathf.Clamp(Mathf.Round((current + direction * step) / step) * step, min, max);
            set(next);
            settings.Commit();
        }

        private void Toggle(Action<bool> set, bool current)
        {
            set(!current);
            settings.Commit();
        }

        private static string Percent(float value) => Mathf.RoundToInt(value * 100f) + "%";

        private static string OnOff(bool value) => value ? "On" : "Off";

        // ---- resolution: "Native" (leave as is) followed by every distinct screen size the display supports

        private void BuildResolutionList()
        {
            resolutions.Clear();
            resolutions.Add(Vector2Int.zero);
            foreach (Resolution r in Screen.resolutions)
            {
                var size = new Vector2Int(r.width, r.height);
                if (!resolutions.Contains(size))
                    resolutions.Add(size);
            }
        }

        private string ResolutionText()
        {
            var current = new Vector2Int(settings.Current.resolutionWidth, settings.Current.resolutionHeight);
            return current == Vector2Int.zero ? "Native" : current.x + " x " + current.y;
        }

        private void AdjustResolution(int direction)
        {
            var current = new Vector2Int(settings.Current.resolutionWidth, settings.Current.resolutionHeight);
            int index = Mathf.Max(0, resolutions.IndexOf(current));
            Vector2Int next = resolutions[((index + direction) % resolutions.Count + resolutions.Count) % resolutions.Count];
            settings.Current.resolutionWidth = next.x;
            settings.Current.resolutionHeight = next.y;
            settings.Commit();
        }
    }
}
