using System;
using BulletHell.Platform;
using BulletHell.Settings;
using BulletHell.UI;
using UnityEngine.InputSystem;

namespace BulletHell.Input
{
    /// <summary>The gameplay buttons the player can remap (sticks stay where they are).</summary>
    public enum RebindAction { Fire, Lock, Jump, Ammo1, Ammo2, Ammo3, Ammo4, Pause }

    public enum RebindOutcome { Changed, Swapped, Cancelled }

    /// <summary>
    /// Button remapping with the Input System's rebinding API. Keeps one never-enabled <see cref="GameInput"/> (the "template") that the
    /// Settings screen rebinds and reads names from; every change is saved as a JSON of binding overrides in the settings file
    /// (<see cref="SettingsData.bindingOverrides"/>) and applied to the live input readers through <see cref="SettingsService.Changed"/>.
    /// Only the gamepad binding of each action is rebound (the keyboard fallback keeps its keys). Choosing a button another action uses
    /// swaps the two, so no button is ever left bound twice.
    /// </summary>
    public sealed class InputBindingService : IDisposable
    {
        public const float RebindTimeoutSeconds = 6f;

        public static readonly RebindAction[] All =
        {
            RebindAction.Fire, RebindAction.Lock, RebindAction.Jump, RebindAction.Ammo1, RebindAction.Ammo2, RebindAction.Ammo3, RebindAction.Ammo4, RebindAction.Pause,
        };

        private readonly SettingsService settings;
        private readonly GameInput template = new GameInput();
        private InputActionRebindingExtensions.RebindingOperation operation;
        private string appliedJson = "";

        /// <summary>Raised when the bindings changed (a rebind, a reset, or new overrides loaded).</summary>
        public event Action Changed;

        /// <summary>Goes up with every change, so a screen can tell whether its button pictures are stale.</summary>
        public int Version { get; private set; }

        public bool IsRebinding => operation != null;

        public InputBindingService(SettingsService settingsService)
        {
            settings = settingsService;
            Apply(settings.Current.bindingOverrides);
            settings.Changed += OnSettingsChanged;
        }

        public void Dispose()
        {
            operation?.Dispose();
            operation = null;
            settings.Changed -= OnSettingsChanged;
            if (UnityEngine.Application.isPlaying)
                template.Dispose();
            else
                UnityEngine.Object.DestroyImmediate(template.asset);   // GameInput.Dispose uses Destroy, which edit-mode tests cannot call
        }

        // ------------------------------------------------------------------ names

        private static string ActionName(RebindAction action)
        {
            switch (action)
            {
                case RebindAction.Fire: return "Fire";
                case RebindAction.Lock: return "LockToggle";
                case RebindAction.Jump: return "Jump";
                case RebindAction.Ammo1: return "EquipAmmo1";
                case RebindAction.Ammo2: return "EquipAmmo2";
                case RebindAction.Ammo3: return "EquipAmmo3";
                case RebindAction.Ammo4: return "EquipAmmo4";
                default: return "Pause";
            }
        }

        /// <summary>What the Settings list calls each remappable action.</summary>
        public static string DisplayName(RebindAction action)
        {
            switch (action)
            {
                case RebindAction.Fire: return "Fire";
                case RebindAction.Lock: return "Lock Arm";
                case RebindAction.Jump: return "Jump";
                case RebindAction.Ammo1: return "Ammo Slot 1";
                case RebindAction.Ammo2: return "Ammo Slot 2";
                case RebindAction.Ammo3: return "Ammo Slot 3";
                case RebindAction.Ammo4: return "Ammo Slot 4";
                default: return "Pause";
            }
        }

        /// <summary>The remappable action a prompt action stands for, if any (Fire, Lock, Jump, Ammo1-4).</summary>
        public static bool TryFromUiAction(UiAction action, out RebindAction result)
        {
            switch (action)
            {
                case UiAction.Fire: result = RebindAction.Fire; return true;
                case UiAction.Lock: result = RebindAction.Lock; return true;
                case UiAction.Jump: result = RebindAction.Jump; return true;
                case UiAction.Ammo1: result = RebindAction.Ammo1; return true;
                case UiAction.Ammo2: result = RebindAction.Ammo2; return true;
                case UiAction.Ammo3: result = RebindAction.Ammo3; return true;
                case UiAction.Ammo4: result = RebindAction.Ammo4; return true;
                default: result = RebindAction.Fire; return false;
            }
        }

        private InputAction Get(RebindAction action) => template.asset.FindAction("Gameplay/" + ActionName(action), true);

        // The binding that belongs to the gamepad (the keyboard fallback is left alone).
        private static int GamepadIndex(InputAction action)
        {
            for (int i = 0; i < action.bindings.Count; i++)
                if (!action.bindings[i].isComposite && action.bindings[i].path != null && action.bindings[i].path.StartsWith("<Gamepad>"))
                    return i;
            return -1;
        }

        /// <summary>The gamepad control an action is on now, as an Input System path ("&lt;Gamepad&gt;/rightShoulder").</summary>
        public string PathOf(RebindAction action)
        {
            InputAction input = Get(action);
            int index = GamepadIndex(input);
            return index >= 0 ? input.bindings[index].effectivePath : "";
        }

        /// <summary>The button's name on a controller family ("R1", "RB", "R"); empty for families with no gamepad names (touch).</summary>
        public string Label(RebindAction action, GlyphFamily family)
        {
            if (family == GlyphFamily.Keyboard)
            {
                InputAction input = Get(action);
                for (int i = 0; i < input.bindings.Count; i++)
                    if (input.bindings[i].path != null && input.bindings[i].path.StartsWith("<Keyboard>"))
                        return InputControlPath.ToHumanReadableString(input.bindings[i].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
                return "";
            }
            return ControlLabels.Name(family, PathOf(action));
        }

        // ------------------------------------------------------------------ rebinding

        /// <summary>
        /// Waits for the next gamepad button press and binds the action to it. Escape or <see cref="RebindTimeoutSeconds"/> without a
        /// press cancels. Returns false when a rebind is already running.
        /// </summary>
        public bool StartRebind(RebindAction action, Action<RebindOutcome> done)
        {
            if (operation != null)
                return false;

            InputAction input = Get(action);
            int index = GamepadIndex(input);
            if (index < 0)
                return false;
            string previousPath = input.bindings[index].effectivePath;

            operation = input.PerformInteractiveRebinding(index)
                .WithControlsHavingToMatchPath("<Gamepad>/<Button>")   // buttons and triggers; sticks and the d-pad are not offered
                .WithCancelingThrough("<Keyboard>/escape")
                .WithTimeout(RebindTimeoutSeconds)
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(op =>
                {
                    op.Dispose();
                    operation = null;
                    RebindOutcome outcome = ResolveConflict(action, input.bindings[index].effectivePath, previousPath);
                    Save();
                    done?.Invoke(outcome);
                })
                .OnCancel(op =>
                {
                    op.Dispose();
                    operation = null;
                    done?.Invoke(RebindOutcome.Cancelled);
                })
                .Start();
            return true;
        }

        public void CancelRebind() => operation?.Cancel();

        /// <summary>Binds an action to a control path directly (the same path a rebind ends with); for tests and tools.</summary>
        public RebindOutcome SetBinding(RebindAction action, string gamepadPath)
        {
            InputAction input = Get(action);
            int index = GamepadIndex(input);
            string previousPath = input.bindings[index].effectivePath;
            input.ApplyBindingOverride(index, gamepadPath);
            RebindOutcome outcome = ResolveConflict(action, gamepadPath, previousPath);
            Save();
            return outcome;
        }

        // Another action already on the chosen button takes the button the rebound action just gave up.
        private RebindOutcome ResolveConflict(RebindAction action, string chosenPath, string previousPath)
        {
            if (chosenPath == previousPath)
                return RebindOutcome.Changed;
            foreach (RebindAction other in All)
            {
                if (other == action)
                    continue;
                InputAction otherInput = Get(other);
                int otherIndex = GamepadIndex(otherInput);
                if (otherIndex >= 0 && otherInput.bindings[otherIndex].effectivePath == chosenPath)
                {
                    otherInput.ApplyBindingOverride(otherIndex, previousPath);
                    return RebindOutcome.Swapped;
                }
            }
            return RebindOutcome.Changed;
        }

        /// <summary>Puts every gamepad button back to its default.</summary>
        public void ResetToDefaults()
        {
            template.asset.RemoveAllBindingOverrides();
            Save();
        }

        // ------------------------------------------------------------------ saving and applying

        private void Save()
        {
            string json = Normalize(template.asset.SaveBindingOverridesAsJson());
            appliedJson = json;
            Version++;
            settings.Current.bindingOverrides = json;
            settings.Commit();      // applies to the live readers through SettingsService.Changed, and marks the file for saving
            Changed?.Invoke();
        }

        private void OnSettingsChanged()
        {
            string json = Normalize(settings.Current.bindingOverrides);
            if (json == appliedJson)
                return;
            Apply(json);
            Version++;
            Changed?.Invoke();
        }

        private void Apply(string json)
        {
            template.asset.RemoveAllBindingOverrides();
            if (!string.IsNullOrEmpty(json))
                template.asset.LoadBindingOverridesFromJson(json);
            appliedJson = Normalize(template.asset.SaveBindingOverridesAsJson());
        }

        /// <summary>No overrides at all (the saved JSON of an untouched set) is stored as an empty string, so a fresh settings file needs no write.</summary>
        public static string Normalize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return "";
            string compact = json.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "");
            return compact == "{\"bindings\":[]}" ? "" : json;
        }

        /// <summary>Applies the saved overrides to a live input set (the gameplay reader calls this when it wakes and when settings change).</summary>
        public static void ApplyTo(GameInput input, string json)
        {
            input.asset.RemoveAllBindingOverrides();
            if (!string.IsNullOrEmpty(json))
                input.asset.LoadBindingOverridesFromJson(json);
        }
    }
}
