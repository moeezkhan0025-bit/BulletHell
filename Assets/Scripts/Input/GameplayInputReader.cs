using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BulletHell.Input
{
    /// <summary>
    /// The single owner of the generated GameInput class. Exposes device-agnostic values
    /// so gameplay code never touches input devices directly.
    /// </summary>
    public sealed class GameplayInputReader : MonoBehaviour
    {
        private GameInput input;
        private InputAction moveAction;
        private InputAction aimAction;
        private InputAction lockToggleAction;
        private InputAction fireAction;
        private readonly InputAction[] ammoActions = new InputAction[AmmoButtonCount];
        private readonly Action<InputAction.CallbackContext>[] ammoHandlers = new Action<InputAction.CallbackContext>[AmmoButtonCount];

        public const int AmmoButtonCount = 4;

        /// <summary>Right stick.</summary>
        public Vector2 Move => moveAction.ReadValue<Vector2>();

        /// <summary>Left stick.</summary>
        public Vector2 Aim => aimAction.ReadValue<Vector2>();

        /// <summary>R1 held down.</summary>
        public bool FireHeld => fireAction.IsPressed();

        public event Action LockTogglePressed;

        /// <summary>R2 / Space pressed (jump).</summary>
        public event Action JumpPressed;

        /// <summary>Options / Start pressed (pause).</summary>
        public event Action PausePressed;

        // Debug map (D-pad / F1-F4): add, remove, next and previous test armament.
        public event Action DebugAddArmamentPressed;
        public event Action DebugRemoveArmamentPressed;
        public event Action DebugNextArmamentPressed;
        public event Action DebugPrevArmamentPressed;

        /// <summary>A face button (ammo slot 0-3) was pressed.</summary>
        public event Action<int> AmmoPressed;

        /// <summary>Face button for ammo slot 0-3 currently held down.</summary>
        public bool IsAmmoHeld(int slot) => ammoActions[slot].IsPressed();

        private void Awake()
        {
            input = new GameInput();
            var gameplay = input.Gameplay;
            moveAction = gameplay.Move;
            aimAction = gameplay.Aim;
            lockToggleAction = gameplay.LockToggle;
            fireAction = gameplay.Fire;
            lockToggleAction.performed += OnLockToggle;
            gameplay.Pause.performed += OnPause;
            gameplay.Jump.performed += OnJump;
            input.Debug.DebugAddArmament.performed += OnDebugAdd;
            input.Debug.DebugRemoveArmament.performed += OnDebugRemove;
            input.Debug.DebugNextArmament.performed += OnDebugNext;
            input.Debug.DebugPrevArmament.performed += OnDebugPrev;
            input.Debug.DebugToggleBulletPaths.performed += OnDebugToggleBulletPaths;
            input.Debug.DebugGiveCurrency.performed += OnDebugGiveCurrency;

            ammoActions[0] = gameplay.EquipAmmo1;
            ammoActions[1] = gameplay.EquipAmmo2;
            ammoActions[2] = gameplay.EquipAmmo3;
            ammoActions[3] = gameplay.EquipAmmo4;
            for (int i = 0; i < AmmoButtonCount; i++)
            {
                int slot = i;
                ammoHandlers[i] = c => InputDiagnostics.Raise(this, "OnAmmoPressed", c, AmmoPressed, slot);
                ammoActions[i].performed += ammoHandlers[i];
            }
        }

        private void OnEnable()
        {
            try
            {
                input.Gameplay.Enable();
                input.Debug.Enable();
            }
            catch (Exception exception)
            {
                InputDiagnostics.ReportLifecycle(this, nameof(OnEnable), exception);
                throw;
            }
        }

        private void OnDisable()
        {
            try
            {
                input.Gameplay.Disable();
                input.Debug.Disable();
            }
            catch (Exception exception)
            {
                InputDiagnostics.ReportLifecycle(this, nameof(OnDisable), exception);
                throw;
            }
        }

        private void OnDestroy()
        {
            if (input == null)
                return; // destroyed before it ever woke up (a scene closed while this was inactive)

            lockToggleAction.performed -= OnLockToggle;
            input.Gameplay.Pause.performed -= OnPause;
            input.Gameplay.Jump.performed -= OnJump;
            for (int i = 0; i < AmmoButtonCount; i++)
                ammoActions[i].performed -= ammoHandlers[i];
            input.Debug.DebugAddArmament.performed -= OnDebugAdd;
            input.Debug.DebugRemoveArmament.performed -= OnDebugRemove;
            input.Debug.DebugNextArmament.performed -= OnDebugNext;
            input.Debug.DebugPrevArmament.performed -= OnDebugPrev;
            input.Debug.DebugToggleBulletPaths.performed -= OnDebugToggleBulletPaths;
            input.Debug.DebugGiveCurrency.performed -= OnDebugGiveCurrency;
            input.Dispose();
        }

        private void OnDebugAdd(InputAction.CallbackContext c) => InputDiagnostics.Raise(this, nameof(OnDebugAdd), c, DebugAddArmamentPressed);
        private void OnDebugRemove(InputAction.CallbackContext c) => InputDiagnostics.Raise(this, nameof(OnDebugRemove), c, DebugRemoveArmamentPressed);
        private void OnDebugNext(InputAction.CallbackContext c) => InputDiagnostics.Raise(this, nameof(OnDebugNext), c, DebugNextArmamentPressed);
        private void OnDebugPrev(InputAction.CallbackContext c) => InputDiagnostics.Raise(this, nameof(OnDebugPrev), c, DebugPrevArmamentPressed);

        // F6: show / hide the bullet path visualisation (works whether or not the debug overlay is showing).
        private void OnDebugToggleBulletPaths(InputAction.CallbackContext _) => Projectiles.BulletPathDebug.Toggle();

        // F7: give yourself currency (the amount is on ShopTuning). Works in any run state; the Shop refreshes by itself.
        private void OnDebugGiveCurrency(InputAction.CallbackContext _)
        {
            Core.GameServices services = Core.GameServices.Ensure();
            if (services.Run.State != null)
                services.Run.State.Currency += services.Config.ShopTuning != null ? services.Config.ShopTuning.DebugCurrencyGrant : 100;
        }

        private void OnLockToggle(InputAction.CallbackContext c) => InputDiagnostics.Raise(this, nameof(OnLockToggle), c, LockTogglePressed);

        private void OnPause(InputAction.CallbackContext c) => InputDiagnostics.Raise(this, nameof(OnPause), c, PausePressed);

        private void OnJump(InputAction.CallbackContext c) => InputDiagnostics.Raise(this, nameof(OnJump), c, JumpPressed);
    }
}
