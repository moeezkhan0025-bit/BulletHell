using System;
using System.Collections.Generic;
using BulletHell.Core;
using BulletHell.Input;
using BulletHell.Platform;
using BulletHell.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// The Settings screen: master / music / SFX volume, screen shake, controller vibration, aim sensitivity, debug overlay,
    /// and on PC fullscreen + resolution, spread over tabs (Audio, Video, Controls, Gameplay; L1 / R1 switch). Every change
    /// applies immediately and is saved on close. Rows only read and write through <see cref="SettingsService"/>; the look is
    /// the VoxVegetallis row (slider, toggle or arrows). Opened from the Main Menu and from Pause; Back returns there.
    /// </summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        private const int AudioTab = 0;
        private const int VideoTab = 1;
        private const int ControlsTab = 2;
        private const int GameplayTab = 3;
        private static readonly string[] TabNames = { "Audio", "Video", "Controls", "Gameplay" };

        [SerializeField] private SettingRow rowPrefab;
        [SerializeField] private Transform rowParent;
        [SerializeField] private Button backButton;
        [Tooltip("Optional: the button prompt line.")]
        [SerializeField] private TMP_Text hintLabel;
        [Tooltip("Small spaced heading above a group of rows (FEEL).")]
        [SerializeField] private TMP_Text sectionPrefab;
        [Tooltip("L1 / R1 switch tabs while this screen is open.")]
        [SerializeField] private MenuInputReader menuInput;
        [SerializeField] private Button[] tabButtons = new Button[0];
        [SerializeField] private TMP_Text[] tabLabels = new TMP_Text[0];

        private sealed class Entry
        {
            public int Tab;
            public GameObject Object;
            public SettingRow Row;
        }

        private readonly List<Entry> entries = new List<Entry>();
        private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
        private SettingsService settings;
        private Action onClosed;
        private bool built;
        private int activeTab;

        public bool IsOpen => gameObject.activeSelf;

        public void Open(Action closed)
        {
            ScreenTransition.In(gameObject);
            PromptHint.Show(hintLabel, PromptHint.P(UiAction.Confirm, "Select"), PromptHint.P(UiAction.TabPrev, "Prev tab"),
                            PromptHint.P(UiAction.TabNext, "Next tab"), PromptHint.P(UiAction.Back, "Back"));
            EnsureBuilt();
            onClosed = closed;
            foreach (Entry entry in entries)
                if (entry.Row != null)
                    entry.Row.Refresh();
            if (menuInput != null)
            {
                menuInput.TabPrevPressed += PreviousTab;
                menuInput.TabNextPressed += NextTab;
            }
            ShowTab(activeTab);
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            settings.Save();
            if (menuInput != null)
            {
                menuInput.TabPrevPressed -= PreviousTab;
                menuInput.TabNextPressed -= NextTab;
            }
            gameObject.SetActive(false);
            Action callback = onClosed;
            onClosed = null;
            callback?.Invoke();
        }

        private void OnDisable()
        {
            if (menuInput != null)
            {
                menuInput.TabPrevPressed -= PreviousTab;
                menuInput.TabNextPressed -= NextTab;
            }
            settings?.Save();
        }

        private void EnsureBuilt()
        {
            if (built)
                return;
            built = true;

            settings = GameServices.Ensure().Settings;
            SettingsDefaults defaults = settings.Defaults;

            AddRow(AudioTab, "Master Volume", () => Percent(settings.Current.masterVolume), SettingKind.Slider, () => settings.Current.masterVolume,
                dir => Step(settings.Current.masterVolume, dir, defaults.VolumeStep, 0f, 1f, v => settings.Current.masterVolume = v));
            AddRow(AudioTab, "Music", () => Percent(settings.Current.musicVolume), SettingKind.Slider, () => settings.Current.musicVolume,
                dir => Step(settings.Current.musicVolume, dir, defaults.VolumeStep, 0f, 1f, v => settings.Current.musicVolume = v));
            AddRow(AudioTab, "Sound Effects", () => Percent(settings.Current.sfxVolume), SettingKind.Slider, () => settings.Current.sfxVolume,
                dir => Step(settings.Current.sfxVolume, dir, defaults.VolumeStep, 0f, 1f, v => settings.Current.sfxVolume = v));
            AddSection(AudioTab, "FEEL");
            AddRow(AudioTab, "Screen Shake", () => OnOff(settings.Current.screenShake), SettingKind.Toggle, null,
                _ => Toggle(v => settings.Current.screenShake = v, settings.Current.screenShake));
            AddRow(AudioTab, "Controller Vibration", () => OnOff(settings.Current.vibration), SettingKind.Toggle, null,
                _ => Toggle(v => settings.Current.vibration = v, settings.Current.vibration), "DualSense / Xbox");

            if (PlatformCapabilities.SupportsDisplaySettings)
            {
                BuildResolutionList();
                AddRow(VideoTab, "Fullscreen", () => OnOff(settings.Current.fullscreen), SettingKind.Toggle, null,
                    _ => Toggle(v => settings.Current.fullscreen = v, settings.Current.fullscreen));
                AddRow(VideoTab, "Resolution", ResolutionText, SettingKind.Choice, null, AdjustResolution);
            }

            AddRow(ControlsTab, "Aim Sensitivity", () => settings.Current.aimSensitivity.ToString("0.0") + "x", SettingKind.Slider,
                () => Mathf.InverseLerp(defaults.MinAimSensitivity, defaults.MaxAimSensitivity, settings.Current.aimSensitivity),
                dir => Step(settings.Current.aimSensitivity, dir, defaults.AimSensitivityStep, defaults.MinAimSensitivity, defaults.MaxAimSensitivity,
                            v => settings.Current.aimSensitivity = v));

            if (Debug.isDebugBuild)
            {
                AddRow(GameplayTab, "Show Debug Overlay", () => OnOff(settings.Current.showDebugOverlay), SettingKind.Toggle, null,
                    _ => Toggle(v => settings.Current.showDebugOverlay = v, settings.Current.showDebugOverlay));
                AddRow(GameplayTab, "Show AI Debug", () => OnOff(settings.Current.showAiDebug), SettingKind.Toggle, null,
                    _ => Toggle(v => settings.Current.showAiDebug = v, settings.Current.showAiDebug));
            }

            for (int i = 0; i < tabButtons.Length && i < TabNames.Length; i++)
            {
                int tab = i;
                tabLabels[i].text = TabNames[i];
                tabButtons[i].onClick.AddListener(() => ShowTab(tab));
                tabButtons[i].gameObject.SetActive(HasRows(tab));
            }

            backButton.onClick.AddListener(Close);
            if (backButton.TryGetComponent(out CancelRelay relay))
                relay.Cancelled += Close;
            backButton.transform.SetAsLastSibling();
        }

        private bool HasRows(int tab)
        {
            foreach (Entry entry in entries)
                if (entry.Tab == tab && entry.Row != null)
                    return true;
            return false;
        }

        private void PreviousTab() => StepTab(-1);

        private void NextTab() => StepTab(1);

        private void StepTab(int direction)
        {
            for (int i = 1; i <= TabNames.Length; i++)
            {
                int tab = ((activeTab + direction * i) % TabNames.Length + TabNames.Length) % TabNames.Length;
                if (HasRows(tab))
                {
                    ShowTab(tab);
                    return;
                }
            }
        }

        private void ShowTab(int tab)
        {
            if (!HasRows(tab))
                tab = AudioTab;
            activeTab = tab;
            SettingRow first = null;
            foreach (Entry entry in entries)
            {
                bool show = entry.Tab == tab;
                entry.Object.SetActive(show);
                if (show && first == null && entry.Row != null)
                    first = entry.Row;
            }

            UITheme theme = UITheme.Current;
            for (int i = 0; i < tabButtons.Length && i < TabNames.Length; i++)
            {
                bool selected = i == tab;
                if (theme != null && tabButtons[i].targetGraphic is Image image)
                {
                    Sprite sprite = theme.GetSprite(selected ? ThemeRole.TabSelected : ThemeRole.Tab);
                    if (sprite != null)
                    {
                        image.sprite = sprite;
                        image.type = Image.Type.Sliced;
                        image.pixelsPerUnitMultiplier = theme.BorderMultiplier;
                    }
                    tabLabels[i].color = selected ? theme.TextOnPanel : theme.FaintText;
                }
            }
            if (first != null)
                UIFocusGuard.Focus(first.gameObject);
        }

        private void AddRow(int tab, string labelText, Func<string> value, SettingKind kind, Func<float> fraction, Action<int> adjust, string sub = null)
        {
            SettingRow row = Instantiate(rowPrefab, rowParent);
            row.Bind(labelText, value, adjust, kind, fraction, sub != null ? (Func<string>)(() => sub) : null);
            row.Cancelled += Close;
            entries.Add(new Entry { Tab = tab, Object = row.gameObject, Row = row });
        }

        private void AddSection(int tab, string text)
        {
            if (sectionPrefab == null)
                return;
            TMP_Text section = Instantiate(sectionPrefab, rowParent);
            section.text = text;
            section.gameObject.SetActive(true);
            entries.Add(new Entry { Tab = tab, Object = section.gameObject });
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

        private static string Percent(float value) => Mathf.RoundToInt(value * 100f).ToString();

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
