using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BulletHell.Input
{
    /// <summary>
    /// Owner of the "Menu" input map: the controller's Start / Options button as the menu's primary action.
    /// Navigating and pressing buttons is the EventSystem's job (stick/D-pad + A/Cross); this adds the Start shortcut.
    /// </summary>
    public sealed class MenuInputReader : MonoBehaviour
    {
        private GameInput input;

        /// <summary>Start / Options pressed.</summary>
        public event Action PrimaryPressed;

        /// <summary>Square / West (or R on the keyboard) pressed: Randomize on the character creation screen.</summary>
        public event Action RandomizePressed;

        private void Awake()
        {
            input = new GameInput();
            input.Menu.Primary.performed += OnPrimary;
            input.Menu.Randomize.performed += OnRandomize;
        }

        private void OnEnable() => input.Menu.Enable();

        private void OnDisable() => input.Menu.Disable();

        private void OnDestroy()
        {
            if (input == null)
                return; // destroyed before it ever woke up (a scene closed while this was inactive)

            input.Menu.Primary.performed -= OnPrimary;
            input.Menu.Randomize.performed -= OnRandomize;
            input.Dispose();
        }

        private void OnPrimary(InputAction.CallbackContext _) => PrimaryPressed?.Invoke();

        private void OnRandomize(InputAction.CallbackContext _) => RandomizePressed?.Invoke();
    }
}
