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

        /// <summary>Right stick.</summary>
        public Vector2 Move => moveAction.ReadValue<Vector2>();

        /// <summary>Left stick.</summary>
        public Vector2 Aim => aimAction.ReadValue<Vector2>();

        public event Action LockTogglePressed;

        private void Awake()
        {
            input = new GameInput();
            var gameplay = input.Gameplay;
            moveAction = gameplay.Move;
            aimAction = gameplay.Aim;
            lockToggleAction = gameplay.LockToggle;
            lockToggleAction.performed += OnLockToggle;
        }

        private void OnEnable() => input.Gameplay.Enable();

        private void OnDisable() => input.Gameplay.Disable();

        private void OnDestroy()
        {
            lockToggleAction.performed -= OnLockToggle;
            input.Dispose();
        }

        private void OnLockToggle(InputAction.CallbackContext _) => LockTogglePressed?.Invoke();
    }
}
