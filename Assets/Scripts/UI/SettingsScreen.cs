using System;
using System.Collections.Generic;
using BulletHell.Core;
using BulletHell.Cosmetics;
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
        private InputBindingService bindings;
        private readonly Dictionary<RebindAction, SettingRow> remapRows = new Dictionary<RebindAction, SettingRow>();
        private RebindAction? rebinding;
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
            bindings?.CancelRebind();
            rebinding = null;
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

            ProfileService profile = GameServices.Ensure().Profile;
            settings = GameServices.Ensure().Settings;
            SettingsDefaults defaults = settings.Defaults;

            // ---- Audio
            AddRow(AudioTab, "Master Volume", () => Percent(settings.Current.masterVolume), SettingKind.Slider, () => settings.Current.masterVolume,
                dir => Step(settings.Current.masterVolume, dir, defaults.VolumeStep, 0f, 1f, v => settings.Current.masterVolume = v),
                sub: null, setFraction: f => SetByFraction(f, defaults.VolumeStep, 0f, 1f, v => settings.Current.masterVolume = v));
            AddRow(AudioTab, "Music", () => Percent(settings.Current.musicVolume), SettingKind.Slider, () => settings.Current.musicVolume,
                dir => Step(settings.Current.musicVolume, dir, defaults.VolumeStep, 0f, 1f, v => settings.Current.musicVolume = v),
                sub: null, setFraction: f => SetByFraction(f, defaults.VolumeStep, 0f, 1f, v => settings.Current.musicVolume = v));
            AddRow(AudioTab, "Sound Effects", () => Percent(settings.Current.sfxVolume), SettingKind.Slider, () => settings.Current.sfxVolume,
                dir => Step(settings.Current.sfxVolume, dir, defaults.VolumeStep, 0f, 1f, v => settings.Current.sfxVolume = v),
                sub: null, setFraction: f => SetByFraction(f, defaults.VolumeStep, 0f, 1f, v => settings.Current.sfxVolume = v));

            // ---- Video (PC: a phone or console has no window to size)
            if (PlatformCapabilities.SupportsDisplaySettings)
            {
                BuildResolutionList();
                AddRow(VideoTab, "Fullscreen", () => OnOff(settings.Current.fullscreen), SettingKind.Toggle, null,
                    _ => Toggle(v => settings.Current.fullscreen = v, settings.Current.fullscreen), "Off = windowed");
                AddRow(VideoTab, "Resolution", ResolutionText, SettingKind.Choice, null, AdjustResolution);
                AddRow(VideoTab, "VSync", () => OnOff(settings.Current.vsync), SettingKind.Toggle, null,
                    _ => Toggle(v => settings.Current.vsync = v, settings.Current.vsync), "Matches the screen refresh");
            }

            // ---- Controls
            AddRow(ControlsTab, "Arm Sensitivity", () => settings.Current.aimSensitivity.ToString("0.0") + "x", SettingKind.Slider,
                () => Mathf.InverseLerp(defaults.MinAimSensitivity, defaults.MaxAimSensitivity, settings.Current.aimSensitivity),
                dir => Step(settings.Current.aimSensitivity, dir, defaults.AimSensitivityStep, defaults.MinAimSensitivity, defaults.MaxAimSensitivity,
                            v => settings.Current.aimSensitivity = v),
                sub: null, setFraction: f => SetByFraction(f, defaults.AimSensitivityStep, defaults.MinAimSensitivity, defaults.MaxAimSensitivity,
                                                           v => settings.Current.aimSensitivity = v));
            AddRow(ControlsTab, "Controller Vibration", () => OnOff(settings.Current.vibration), SettingKind.Toggle, null,
                _ => Toggle(v => settings.Current.vibration = v, settings.Current.vibration), "DualSense / Xbox");

            bindings = GameServices.Ensure().Bindings;
            AddSection(ControlsTab, "BUTTONS");
            foreach (RebindAction action in InputBindingService.All)
            {
                RebindAction captured = action;
                SettingRow row = AddRow(ControlsTab, InputBindingService.DisplayName(action), () => RemapText(captured), SettingKind.Action, null,
                                        _ => BeginRebind(captured));
                remapRows[action] = row;
            }
            AddRow(ControlsTab, "Reset Buttons", () => "Reset", SettingKind.Action, null, _ => ResetBindings(), "Back to the default layout");

            // ---- Gameplay
            AddRow(GameplayTab, "Screen Shake", () => Percent(settings.Current.shakeIntensity) + "%", SettingKind.Slider, () => settings.Current.shakeIntensity,
                dir => Step(settings.Current.shakeIntensity, dir, defaults.ShakeStep, 0f, 1f, SetShake),
                sub: null, setFraction: f => SetByFraction(f, defaults.ShakeStep, 0f, 1f, SetShake));
            AddRow(GameplayTab, "Bullet Outline", () => OutlineText(settings.Current.bulletOutlineScale, defaults), SettingKind.Slider,
                () => Mathf.InverseLerp(defaults.MinBulletOutlineScale, defaults.MaxBulletOutlineScale, settings.Current.bulletOutlineScale),
                dir => Step(settings.Current.bulletOutlineScale, dir, defaults.BulletOutlineStep, defaults.MinBulletOutlineScale, defaults.MaxBulletOutlineScale,
                            v => settings.Current.bulletOutlineScale = v),
                sub: null, setFraction: f => SetByFraction(f, defaults.BulletOutlineStep, defaults.MinBulletOutlineScale, defaults.MaxBulletOutlineScale,
                                                           v => settings.Current.bulletOutlineScale = v));
            AddRow(GameplayTab, "High Contrast", () => OnOff(settings.Current.highContrastBullets), SettingKind.Toggle, null,
                _ => Toggle(v => settings.Current.highContrastBullets = v, settings.Current.highContrastBullets), "White halo on enemy bullets");
            AddRow(GameplayTab, "HUD Scale", () => Percent(settings.Current.hudScale) + "%", SettingKind.Slider,
                () => Mathf.InverseLerp(defaults.MinHudScale, defaults.MaxHudScale, settings.Current.hudScale),
                dir => Step(settings.Current.hudScale, dir, defaults.HudScaleStep, defaults.MinHudScale, defaults.MaxHudScale, v => settings.Current.hudScale = v),
                sub: null, setFraction: f => SetByFraction(f, defaults.HudScaleStep, defaults.MinHudScale, defaults.MaxHudScale, v => settings.Current.hudScale = v));
            AddRow(GameplayTab, "Replay Tutorial", () => OnOff(!profile.TutorialDone), SettingKind.Toggle, null,
                _ => profile.SetTutorialDone(!profile.TutorialDone), "Plays in round 1 of a new run");

            if (Debug.isDebugBuild)
            {
                AddSection(GameplayTab, "DEVELOPMENT");
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

        private void PreviousTab()
        {
            if (!rebinding.HasValue)
                StepTab(-1);
        }

        private void NextTab()
        {
            if (!rebinding.HasValue)
                StepTab(1);
        }

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
            if (rowParent is RectTransform list)
                list.anchoredPosition = Vector2.zero;   // each tab opens scrolled to its top
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

        private SettingRow AddRow(int tab, string labelText, Func<string> value, SettingKind kind, Func<float> fraction, Action<int> adjust, string sub = null, Action<float> setFraction = null)
        {
            SettingRow row = Instantiate(rowPrefab, rowParent);
            row.Bind(labelText, value, adjust, kind, fraction, sub != null ? (Func<string>)(() => sub) : null, setFraction);
            row.Cancelled += OnRowCancelled;
            entries.Add(new Entry { Tab = tab, Object = row.gameObject, Row = row });
            return row;
        }

        // Circle can be the very button being chosen in a rebind: while one runs, it must not close the screen.
        private void OnRowCancelled()
        {
            if (rebinding.HasValue)
                return;
            Close();
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

        // ---- button remapping

        // The list names the gamepad buttons; with the keyboard or touch in use it still shows the PlayStation names.
        private static GlyphFamily RemapFamily()
        {
            GlyphFamily family = GlyphFamilyDetector.Current();
            return family == GlyphFamily.Keyboard || family == GlyphFamily.Touch ? GlyphFamily.PlayStation : family;
        }

        private string RemapText(RebindAction action)
        {
            if (rebinding.HasValue && rebinding.Value == action)
                return "Press a button...";
            return bindings.Label(action, RemapFamily());
        }

        private void BeginRebind(RebindAction action)
        {
            if (rebinding.HasValue)
                return;
            rebinding = action;
            RefreshRemapRows();
            StartCoroutine(StartRebindAfterRelease(action));
        }

        // Wait a moment so the press that opened the row is not the one that gets bound.
        private System.Collections.IEnumerator StartRebindAfterRelease(RebindAction action)
        {
            yield return new WaitForSecondsRealtime(0.3f);
            if (!rebinding.HasValue || !IsOpen || !bindings.StartRebind(action, OnRebound))
            {
                rebinding = null;
                RefreshRemapRows();
            }
        }

        private void OnRebound(RebindOutcome outcome)
        {
            rebinding = null;
            RefreshRemapRows();
            UiSound.Play(outcome == RebindOutcome.Cancelled ? UiSoundKind.Back : UiSoundKind.Equip);
        }

        private void ResetBindings()
        {
            if (rebinding.HasValue)
                return;
            bindings.ResetToDefaults();
            RefreshRemapRows();
            UiSound.Play(UiSoundKind.Equip);
        }

        private void RefreshRemapRows()
        {
            foreach (var pair in remapRows)
                if (pair.Value != null)
                    pair.Value.Refresh();
        }

        private void SetShake(float value)
        {
            settings.Current.shakeIntensity = value;
            settings.Current.screenShake = value > 0.001f;   // the old on / off switch follows
        }

        private static string OutlineText(float scale, SettingsDefaults defaults) => (scale * 2f).ToString("0.#") + " px";

        // A slider pressed or dragged by pointer: 0..1 along the track, snapped to the setting's step.
        private void SetByFraction(float fraction, float step, float min, float max, Action<float> set)
        {
            float value = Mathf.Clamp(Mathf.Round(Mathf.Lerp(min, max, fraction) / step) * step, min, max);
            set(value);
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
