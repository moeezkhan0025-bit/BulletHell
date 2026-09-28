using System;
using BulletHell.Core;
using BulletHell.Input;
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
        [SerializeField] private ArmVisual armPrefab;
        [SerializeField] private Transform armParent;

        private readonly ArmVisual[] arms = new ArmVisual[ArmLoadout.SlotCount];
        private ArmSelector selector;
        private int shownArm = ArmSelector.None;

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

        private void Awake()
        {
            run = GameServices.Ensure().Run;
            run.EnsureRun();
            BuildArms();
        }

        private void OnEnable()
        {
            input.LockTogglePressed += OnLockTogglePressed;
            run.Machine.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            input.LockTogglePressed -= OnLockTogglePressed;
            run.Machine.StateChanged -= OnStateChanged;
        }

        // The Armory is the only place the loadout changes: respawn the arms when the next round starts.
        private void OnStateChanged(GameState from, GameState to)
        {
            if (from == GameState.Armory && to == GameState.Combat)
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
            for (int i = 0; i < ArmLoadout.SlotCount; i++)
            {
                selector.SetOwned(i, loadout[i] != null);
                if (loadout[i] == null)
                    continue;
                arms[i] = Instantiate(armPrefab, armParent);
                arms[i].name = $"Arm_{i}_{loadout[i].Data.name}";
                arms[i].Setup(loadout[i]);
                PlaceArm(i, ArmSelector.HomeAngle(i));
            }
        }

        private void Update()
        {
            AimStick = input.Aim;
            int previous = selector.Selected;
            if (!selector.Update(AimStick))
                return;

            RefreshVisuals();
            if (selector.Selected != previous)
                SelectionChanged?.Invoke(selector.Selected);
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

        /// <summary>Puts an arm's attach point on the ring around the player, pointing outward along a compass angle.</summary>
        private void PlaceArm(int index, float compassDegrees)
        {
            float radians = compassDegrees * Mathf.Deg2Rad;
            var arm = arms[index].transform;
            arm.localPosition = new Vector3(Mathf.Sin(radians), Mathf.Cos(radians), 0f) * playerData.ArmRingRadius;
            // Arm space fires along +X; compass 0 (N) is +Y, increasing clockwise.
            arm.localRotation = Quaternion.Euler(0f, 0f, 90f - compassDegrees);
        }

        private void RefreshVisuals()
        {
            int selected = selector.Selected;
            if (shownArm != ArmSelector.None && shownArm != selected)
            {
                PlaceArm(shownArm, ArmSelector.HomeAngle(shownArm));
                arms[shownArm].SetState(ArmVisual.State.Hidden);
            }

            shownArm = selected;
            if (selected == ArmSelector.None)
                return;

            PlaceArm(selected, selector.AimAngle);
            arms[selected].SetState(selector.Locked ? ArmVisual.State.Locked : ArmVisual.State.Selected);
        }
    }
}
