using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BulletHell.Input
{
    /// <summary>
    /// Owner of the "Menu" input map: the controller's Start / Options button as the menu's primary action, plus the
    /// extra face/shoulder buttons some screens use.
    /// Navigating and pressing buttons is the EventSystem's job (stick/D-pad + A/Cross); this adds the shortcuts.
    /// Events go out through <see cref="InputDiagnostics"/>, which logs context if a subscriber throws.
    /// </summary>
    public sealed class MenuInputReader : MonoBehaviour
    {
        private GameInput input;

        /// <summary>Start / Options pressed.</summary>
        public event Action PrimaryPressed;

        /// <summary>Square / West (or R on the keyboard) pressed: Randomize on the character creation screen.</summary>
        public event Action RandomizePressed;

        /// <summary>Triangle / North (or T) pressed: Reroll in the Shop.</summary>
        public event Action RerollPressed;

        /// <summary>Square / West (or Q) pressed: toggle detailed stats in the Shop.</summary>
        public event Action DetailsPressed;

        /// <summary>L1 / LB / L (or [) pressed: previous tab in the Armory.</summary>
        public event Action TabPrevPressed;

        /// <summary>R1 / RB / R (or ]) pressed: next tab in the Armory.</summary>
        public event Action TabNextPressed;

        /// <summary>Triangle / North (or X) pressed: remove the focused arm or armament in the Armory.</summary>
        public event Action RemovePressed;

        private void Awake()
        {
            input = new GameInput();
            input.Menu.Primary.performed += OnPrimary;
            input.Menu.Randomize.performed += OnRandomize;
            input.Menu.Reroll.performed += OnReroll;
            input.Menu.Details.performed += OnDetails;
            input.Menu.TabPrev.performed += OnTabPrev;
            input.Menu.TabNext.performed += OnTabNext;
            input.Menu.Remove.performed += OnRemove;
        }

        private void OnEnable()
        {
            try
            {
                input.Menu.Enable();
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
                input.Menu.Disable();
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

            input.Menu.Primary.performed -= OnPrimary;
            input.Menu.Randomize.performed -= OnRandomize;
            input.Menu.Reroll.performed -= OnReroll;
            input.Menu.Details.performed -= OnDetails;
            input.Menu.TabPrev.performed -= OnTabPrev;
            input.Menu.TabNext.performed -= OnTabNext;
            input.Menu.Remove.performed -= OnRemove;
            input.Dispose();
        }

        private void OnPrimary(InputAction.CallbackContext c) => InputDiagnostics.Raise(this, nameof(OnPrimary), c, PrimaryPressed);

        private void OnRandomize(InputAction.CallbackContext c) => InputDiagnostics.Raise(this, nameof(OnRandomize), c, RandomizePressed);

        private void OnReroll(InputAction.CallbackContext c) => InputDiagnostics.Raise(this, nameof(OnReroll), c, RerollPressed);

        private void OnDetails(InputAction.CallbackContext c) => InputDiagnostics.Raise(this, nameof(OnDetails), c, DetailsPressed);

        private void OnTabPrev(InputAction.CallbackContext c) => InputDiagnostics.Raise(this, nameof(OnTabPrev), c, TabPrevPressed);

        private void OnTabNext(InputAction.CallbackContext c) => InputDiagnostics.Raise(this, nameof(OnTabNext), c, TabNextPressed);

        private void OnRemove(InputAction.CallbackContext c) => InputDiagnostics.Raise(this, nameof(OnRemove), c, RemovePressed);
    }
}
