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

            ammoActions[0] = gameplay.EquipAmmo1;
            ammoActions[1] = gameplay.EquipAmmo2;
            ammoActions[2] = gameplay.EquipAmmo3;
            ammoActions[3] = gameplay.EquipAmmo4;
            for (int i = 0; i < AmmoButtonCount; i++)
            {
                int slot = i;
                ammoHandlers[i] = _ => AmmoPressed?.Invoke(slot);
                ammoActions[i].performed += ammoHandlers[i];
            }
        }

        private void OnEnable() => input.Gameplay.Enable();

        private void OnDisable() => input.Gameplay.Disable();

        private void OnDestroy()
        {
            lockToggleAction.performed -= OnLockToggle;
            for (int i = 0; i < AmmoButtonCount; i++)
                ammoActions[i].performed -= ammoHandlers[i];
            input.Dispose();
        }

        private void OnLockToggle(InputAction.CallbackContext _) => LockTogglePressed?.Invoke();
    }
}
