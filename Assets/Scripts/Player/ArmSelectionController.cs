using System;
using BulletHell.Core;
using BulletHell.Cosmetics;
using BulletHell.Input;
using BulletHell.Settings;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>
    /// Spawns the arms from the run state's loadout, feeds the left stick into ArmSelector, handles L3 lock,
    /// and drives the arm visuals.
    /// </summary>
    public sealed class ArmSelectionController : MonoBehaviour
    {
        [SerializeField] private GameplayInputReader input;
        [SerializeField] private InputTuning tuning;
        [SerializeField] private PlayerData playerData;
        [Tooltip("The elliptical arm ring around the feet (radii, front/back sorting, depth cue, spin).")]
        [SerializeField] private ArmRingTuning ring;
        [Tooltip("Rises with the body during a jump, so the ring does too. Optional.")]
        [SerializeField] private JumpController jump;
        [SerializeField] private ArmVisual armPrefab;
        [Tooltip("The ArmRing anchor at the feet (a child of the player root, not of the lifted Visuals).")]
        [SerializeField] private Transform armParent;

        private readonly ArmVisual[] arms = new ArmVisual[ArmLoadout.SlotCount];
        private ArmSelector selector;
        private int shownArm = ArmSelector.None;
        private float ringAngle;   // where the shown arm is on the ring; eases towards the aim angle (the "spin")

        private ArmRingTuning Ring => ring != null ? ring : ArmRingTuning.Fallback;

        /// <summary>
        /// How far below a muzzle's world position the ground point is: the ring's lift above the feet plus the
        /// jump height. Bullets and beams start on the ground plane at muzzle - GroundOffset.
        /// </summary>
        public float GroundOffset => armParent.position.y - transform.position.y + Ring.VerticalOffset * transform.lossyScale.y;

        /// <summary>The muzzle of an arm projected onto the ground plane: where its bullets really start.</summary>
        public Vector2 GroundMuzzle(ArmVisual arm) => (Vector2)arm.Muzzle.position - Vector2.up * GroundOffset;

        public int SelectedArm => selector.Selected;
        /// <summary>Data of the selected arm, or null when nothing is selected.</summary>
        public WeaponArmData SelectedArmData => selector.Selected == ArmSelector.None ? null : arms[selector.Selected].Data;
        /// <summary>Run state (armaments, final stats) of the selected arm, or null when nothing is selected.</summary>
        public ArmInstance SelectedInstance => selector.Selected == ArmSelector.None ? null : arms[selector.Selected].Instance;
        /// <summary>The spawned arm at a slot, or null if the slot is empty.</summary>
        public ArmVisual GetArm(int slot) => arms[slot];
        public ArmSelectionState State => selector.State;
        /// <summary>Compass direction the selected arm points (slot direction when soft, aim when locked).</summary>
        public float AimAngle => selector.AimAngle;
        /// <summary>The raw aim stick this frame, for the debug overlay.</summary>
        public Vector2 AimStick { get; private set; }

        public event Action<int> SelectionChanged;
        public event Action<bool> LockChanged;

        /// <summary>Raised after the arms were respawned from the loadout (the Armory changed it).</summary>
        public event Action ArmsRebuilt;

        private RunManager run;
        private SettingsService settings;

        private void Awake()
        {
            GameServices services = GameServices.Ensure();
            run = services.Run;
            settings = services.Settings;
            run.EnsureRun();
            BuildArms();
        }

        private void OnEnable()
        {
            input.LockTogglePressed += OnLockTogglePressed;
            run.Machine.StateChanged += OnStateChanged;
            settings.Changed += ApplySensitivity;
        }

        private void OnDisable()
        {
            input.LockTogglePressed -= OnLockTogglePressed;
            run.Machine.StateChanged -= OnStateChanged;
            settings.Changed -= ApplySensitivity;
        }

        private void ApplySensitivity() => selector.Sensitivity = settings.Current.aimSensitivity;

        // The Armory is the only place the loadout changes: respawn the arms when the next round starts.
        private void OnStateChanged(GameState from, GameState to)
        {
            if (from == GameState.Armory && to == GameState.RoundIntro)
                Rebuild();
        }

        /// <summary>Destroys the spawned arms and spawns them again from the run state's loadout. Clears any selection.</summary>
        public void Rebuild()
        {
            int previous = selector.Selected;
            bool wasLocked = selector.Locked;
            for (int i = 0; i < arms.Length; i++)
            {
                if (arms[i] != null)
                    Destroy(arms[i].gameObject);
                arms[i] = null;
            }

            shownArm = ArmSelector.None;
            BuildArms();
            ArmsRebuilt?.Invoke();
            if (wasLocked)
                LockChanged?.Invoke(false);
            if (previous != ArmSelector.None)
                SelectionChanged?.Invoke(ArmSelector.None);
        }

        private void BuildArms()
        {
            ArmInstance[] loadout = run.State.Loadout;

            selector = new ArmSelector(tuning);
            ApplySensitivity();
            for (int i = 0; i < ArmLoadout.SlotCount; i++)
            {
                selector.SetOwned(i, loadout[i] != null);
                if (loadout[i] == null)
                    continue;
                arms[i] = Instantiate(armPrefab, armParent);
                arms[i].name = $"Arm_{i}_{loadout[i].Data.name}";
                arms[i].Setup(loadout[i]);
                PlaceArm(i, ArmSelector.HomeAngle(i), ArmSelector.HomeAngle(i));
            }
        }

        private void Update()
        {
            AimStick = input.Aim;
            int previous = selector.Selected;
            if (selector.Update(AimStick))
            {
                RefreshVisuals();
                if (selector.Selected != previous)
                    SelectionChanged?.Invoke(selector.Selected);
            }

            SpinRing(Time.deltaTime);
        }

        // The ring rises with the jump but is not squashed with the body, and the shadow stays on the ground.
        private void LateUpdate()
        {
            float height = jump != null ? jump.Height : 0f;
            armParent.localPosition = new Vector3(0f, height, 0f);
        }

        // The shown arm slides along the ellipse to the aim angle; its sprite already points along the true aim.
        private void SpinRing(float dt)
        {
            if (shownArm == ArmSelector.None)
                return;
            ringAngle = ArmRingMath.MoveAngle(ringAngle, selector.AimAngle, Ring.SpinDegreesPerSecond, dt);
            PlaceArm(shownArm, ringAngle, selector.AimAngle);
        }

        private void OnLockTogglePressed()
        {
            int previous = selector.Selected;
            if (!selector.ToggleLock(input.Aim))
                return;

            RefreshVisuals();
            LockChanged?.Invoke(selector.Locked);
            if (selector.Selected != previous)
                SelectionChanged?.Invoke(selector.Selected);
        }

        /// <summary>
        /// Puts an arm's attach point on the ellipse at ringDegrees (compass) and rotates it to fire along aimDegrees
        /// (compass). Position and aim are separate: the ring only decides where the arm is drawn and where its
        /// muzzle sits, never which way it shoots.
        /// </summary>
        private void PlaceArm(int index, float ringDegrees, float aimDegrees)
        {
            ArmVisual visual = arms[index];
            Transform arm = visual.transform;
            arm.localPosition = Ring.PositionAt(ringDegrees);
            // Arm space fires along +X; compass 0 (N) is +Y, increasing clockwise.
            arm.localRotation = Quaternion.Euler(0f, 0f, 90f - aimDegrees);
            visual.SetDepth(ArmRingMath.IsBack(ringDegrees, Ring.BackDeadzone), ArmRingMath.Depth01(ringDegrees), Ring);
        }

        private void RefreshVisuals()
        {
            int selected = selector.Selected;
            int previous = shownArm;
            if (previous != ArmSelector.None && previous != selected)
            {
                PlaceArm(previous, ArmSelector.HomeAngle(previous), ArmSelector.HomeAngle(previous));
                arms[previous].SetState(ArmVisual.State.Hidden);
            }

            shownArm = selected;
            if (selected == ArmSelector.None)
                return;

            // A newly shown arm appears in place; switching straight from another arm (or a lock change) keeps
            // the ring angle so the arm spins along the ellipse to its new spot.
            if (previous == ArmSelector.None)
                ringAngle = selector.AimAngle;
            PlaceArm(selected, ringAngle, selector.AimAngle);
            arms[selected].SetState(selector.Locked ? ArmVisual.State.Locked : ArmVisual.State.Selected);
        }
    }
}
