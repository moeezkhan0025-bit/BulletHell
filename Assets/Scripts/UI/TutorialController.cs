using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Input;
using BulletHell.Pickups;
using BulletHell.Platform;
using BulletHell.Player;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>
    /// The round 1 onboarding. On the first combat of a profile that has not finished it, it walks the player through the
    /// controls one at a time (move, select an arm, fire, lock, jump, swap ammo). Each step is finished by doing the thing.
    /// Enemies wait (the wave spawner is held) until the basics are done. Select / Tab skips the lot. Finishing or skipping,
    /// or clearing round 1, saves "done" in the profile; dying or quitting does not. Not used on touch (its controls come
    /// with M12).
    /// </summary>
    public sealed class TutorialController : MonoBehaviour
    {
        [SerializeField] private TutorialData data;
        [SerializeField] private TutorialPanel panel;
        [SerializeField] private GameplayInputReader input;
        [SerializeField] private ArmSelectionController arms;
        [SerializeField] private JumpController jump;
        [SerializeField] private AmmoSlots ammo;
        [SerializeField] private WaveSpawner spawner;

        private const string KeyColor = "#A7781A";   // the theme's gold_dark: readable on marble
        private const float SelectHoldSeconds = 0.15f;

        private RunManager run;
        private GameServices services;
        private bool active;
        private int index;
        private float progress;
        private float gapLeft;
        private bool stepDone;
        private bool eventSeen;
        private bool ammoReady;
        private int ammoStart;

        public bool IsActive => active;
        public int StepIndex => index;

        private void Awake()
        {
            services = GameServices.Ensure();
            run = services.Run;
        }

        private void OnEnable()
        {
            run.RoundStarted += OnRoundStarted;
            run.Machine.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            run.RoundStarted -= OnRoundStarted;
            run.Machine.StateChanged -= OnStateChanged;
            if (active)
                Stop(false);
        }

        private bool Eligible(int round)
        {
            return data != null && data.Steps.Length > 0 && round == data.Round && !run.IsLayoutPreview
                   && !services.Profile.TutorialDone && GlyphFamilyDetector.Current() != GlyphFamily.Touch;
        }

        private void OnRoundStarted(int round)
        {
            if (active)
                Stop(false);
            if (!Eligible(round))
                return;

            active = true;
            index = -1;
            input.SkipTutorialPressed += Skip;
            arms.SelectionChanged += OnSelectionChanged;
            arms.LockChanged += OnLockChanged;
            jump.Jumped += OnJumped;
            ammo.Changed += OnAmmoChanged;
            InputDeviceWatcher.Changed += OnDeviceChanged;
            spawner.HoldSpawns = data.EnemiesWaitForSteps > 0;
            panel.SetVisible(true);
            NextStep();
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            panel.SetVisible(to == GameState.Combat);
            if (!active)
                return;
            if (to == GameState.RoundResults)
                Stop(true);          // round 1 was cleared: the lessons are over, done or not
            else if (to != GameState.Combat && to != GameState.Pause && to != GameState.RoundIntro)
                Stop(false);         // game over, back to the menu: show it again next time
        }

        private void Skip() => Stop(true);

        private void Stop(bool completed)
        {
            active = false;
            input.SkipTutorialPressed -= Skip;
            arms.SelectionChanged -= OnSelectionChanged;
            arms.LockChanged -= OnLockChanged;
            jump.Jumped -= OnJumped;
            ammo.Changed -= OnAmmoChanged;
            InputDeviceWatcher.Changed -= OnDeviceChanged;
            spawner.HoldSpawns = false;
            panel.Hide();
            if (completed)
                services.Profile.SetTutorialDone(true);
        }

        // ------------------------------------------------------------------ steps

        private void NextStep()
        {
            index++;
            // Swapping ammo needs a second ammo type; with none in the world there is nothing to teach.
            while (index < data.Steps.Length && data.Steps[index].Kind == TutorialStepKind.SwapAmmo && !AmmoAvailable())
                index++;
            if (index >= data.Steps.Length)
            {
                Stop(true);
                return;
            }
            if (index >= data.EnemiesWaitForSteps)
                spawner.HoldSpawns = false;

            progress = 0f;
            stepDone = false;
            eventSeen = false;
            ammoReady = FilledAmmo() >= 2;
            ammoStart = ammo.ActiveIndex;
            Render(true);
        }

        private bool AmmoAvailable() => FilledAmmo() >= 2 || AmmoPickup.Count > 0;

        private int FilledAmmo()
        {
            int count = 0;
            for (int i = 0; i < AmmoSlotSet.Count; i++)
                if (ammo.Get(i) != null)
                    count++;
            return count;
        }

        private void Render(bool newStep)
        {
            TutorialData.Step step = data.Steps[index];
            ButtonGlyphLibrary library = services.Config.ButtonGlyphs;
            GlyphFamily family = GlyphFamilyDetector.Current();
            string template = step.Kind == TutorialStepKind.SwapAmmo && !ammoReady && !string.IsNullOrEmpty(step.WaitingText) ? step.WaitingText : step.Text;
            string text = TutorialText.Format(library, family, template, step.Keys, KeyColor, OwnLabel);
            if (newStep)
            {
                string counter = string.Format(data.StepCounterFormat, index + 1, data.Steps.Length);
                if (!string.IsNullOrEmpty(step.Title))
                    counter += "  -  " + step.Title;
                string skip = TutorialText.Format(library, family, data.SkipText, new[] { UiAction.SkipTutorial }, KeyColor, OwnLabel);
                panel.ShowStep(counter, text, skip, index, data.Steps.Length);
            }
            else
            {
                panel.SetInstruction(text, false);
            }
        }

        // The prompt names the button the player has really bound (Settings > Controls), not the default.
        private string OwnLabel(UiAction action)
        {
            if (!BulletHell.Input.InputBindingService.TryFromUiAction(action, out BulletHell.Input.RebindAction rebind))
                return null;
            string label = services.Bindings.Label(rebind, GlyphFamilyDetector.Current());
            return string.IsNullOrEmpty(label) ? null : label;
        }

        private void OnDeviceChanged(GlyphFamily _)
        {
            if (active && !stepDone && index >= 0 && index < data.Steps.Length)
                Render(true);   // other button names: redraw the whole card (counter and skip line too)
        }

        private void Update()
        {
            if (!active)
                return;
            if (stepDone)
            {
                gapLeft -= Time.unscaledDeltaTime;
                if (gapLeft <= 0f)
                    NextStep();
                return;
            }
            if (run.Machine.Current != GameState.Combat)
                return;

            TutorialData.Step step = data.Steps[index];
            switch (step.Kind)
            {
                case TutorialStepKind.Move:
                    if (input.Move.sqrMagnitude > 0.25f)
                        progress += Time.deltaTime;
                    if (progress >= data.MoveSeconds)
                        Complete();
                    break;

                case TutorialStepKind.SelectArm:
                    // State, not just the event: a stick already held toward an arm when the step opens still counts.
                    if (arms.SelectedArm != ArmSelector.None)
                        progress += Time.deltaTime;
                    if (progress >= SelectHoldSeconds || eventSeen)
                        Complete();
                    break;

                case TutorialStepKind.Fire:
                    if (input.FireHeld && arms.SelectedArm != ArmSelector.None)
                        progress += Time.deltaTime;
                    if (progress >= data.FireSeconds)
                        Complete();
                    break;

                case TutorialStepKind.SwapAmmo:
                    if (!ammoReady && FilledAmmo() >= 2)
                    {
                        ammoReady = true;
                        ammoStart = ammo.ActiveIndex;
                        Render(false);
                    }
                    break;

                default:   // Lock and Jump finish from their events
                    if (eventSeen)
                        Complete();
                    break;
            }
        }

        private void Complete()
        {
            stepDone = true;
            gapLeft = data.DoneSeconds;
            panel.ShowDone(data.DoneText, index);
        }

        private bool Waiting(TutorialStepKind kind) => active && !stepDone && index >= 0 && index < data.Steps.Length && data.Steps[index].Kind == kind;

        private void OnSelectionChanged(int slot)
        {
            if (Waiting(TutorialStepKind.SelectArm) && slot != ArmSelector.None)
                eventSeen = true;
        }

        private void OnLockChanged(bool locked)
        {
            if (Waiting(TutorialStepKind.Lock) && locked)
                eventSeen = true;
        }

        private void OnJumped()
        {
            if (Waiting(TutorialStepKind.Jump))
                eventSeen = true;
        }

        private void OnAmmoChanged()
        {
            if (Waiting(TutorialStepKind.SwapAmmo) && ammoReady && ammo.ActiveIndex != ammoStart)
                Complete();
        }
    }
}
