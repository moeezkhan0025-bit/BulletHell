using System;
using BulletHell.Core;
using BulletHell.Input;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// Makes the controller's Start / Options button press the main button of whichever screen is showing.
    /// Routes are checked in order and the first whose scope is active wins; a route with no button just swallows the
    /// press (used to keep Start from confirming a destructive dialog). A press on the same frame the run state changed
    /// is ignored, so the Start press that opened a screen can't also press that screen's button.
    /// </summary>
    public sealed class MenuPrimaryRouter : MonoBehaviour
    {
        [Serializable]
        public struct Route
        {
            [Tooltip("The route applies while this object is active in the scene.")]
            public GameObject Scope;
            [Tooltip("Button pressed by Start. Leave empty to ignore Start while this scope is showing.")]
            public Button Button;
        }

        [SerializeField] private MenuInputReader reader;
        [Tooltip("Checked top to bottom; put dialogs and popups first.")]
        [SerializeField] private Route[] routes = new Route[0];
        [Tooltip("Ignore Start on the frame the run state changes (Game scene only).")]
        [SerializeField] private bool guardRunStateChanges;

        private RunManager run;
        private int stateChangeFrame = -1;

        private void OnEnable()
        {
            reader.PrimaryPressed += OnPrimary;
            if (guardRunStateChanges)
            {
                run = GameServices.Ensure().Run;
                run.Machine.StateChanged += OnStateChanged;
            }
        }

        private void OnDisable()
        {
            reader.PrimaryPressed -= OnPrimary;
            if (run != null)
                run.Machine.StateChanged -= OnStateChanged;
            run = null;
        }

        private void OnStateChanged(GameState from, GameState to) => stateChangeFrame = Time.frameCount;

        private void OnPrimary()
        {
            if (ConfirmDialog.AnyOpen)
                return;   // Start must never confirm a destructive dialog
            if (stateChangeFrame == Time.frameCount)
                return;

            foreach (Route route in routes)
            {
                if (route.Scope == null || !route.Scope.activeInHierarchy)
                    continue;
                if (route.Button != null && route.Button.gameObject.activeInHierarchy && route.Button.interactable)
                    route.Button.onClick.Invoke();
                return;
            }
        }
    }
}
