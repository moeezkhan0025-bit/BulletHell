using System;
using BulletHell.Input;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>
    /// Spawns the arms from the loadout, feeds the left stick into ArmSelector, handles L3 lock,
    /// and drives the arm visuals.
    /// </summary>
    public sealed class ArmSelectionController : MonoBehaviour
    {
        [SerializeField] private GameplayInputReader input;
        [SerializeField] private InputTuning tuning;
        [SerializeField] private PlayerData playerData;
        [Tooltip("Arms equipped at start. StartingLoadout for real runs, DebugLoadout for testing.")]
        [SerializeField] private ArmLoadout loadout;
        [SerializeField] private ArmVisual armPrefab;
        [SerializeField] private Transform armParent;

        private readonly ArmVisual[] arms = new ArmVisual[ArmLoadout.SlotCount];
        private ArmSelector selector;
        private int shownArm = ArmSelector.None;

        public int SelectedArm => selector.Selected;
        /// <summary>Data of the selected arm, or null when nothing is selected.</summary>
        public WeaponArmData SelectedArmData => selector.Selected == ArmSelector.None ? null : arms[selector.Selected].Data;
        /// <summary>The spawned arm at a slot, or null if the slot is empty.</summary>
        public ArmVisual GetArm(int slot) => arms[slot];
        public ArmSelectionState State => selector.State;
        /// <summary>Compass direction the selected arm points (slot direction when soft, aim when locked).</summary>
        public float AimAngle => selector.AimAngle;
        /// <summary>The raw aim stick this frame, for the debug overlay.</summary>
        public Vector2 AimStick { get; private set; }

        public event Action<int> SelectionChanged;
        public event Action<bool> LockChanged;

        private void Awake()
        {
            selector = new ArmSelector(tuning);
            selector.SetOwnedFromLoadout(loadout);

            for (int i = 0; i < ArmLoadout.SlotCount; i++)
            {
                if (!loadout.IsFilled(i))
                    continue;
                arms[i] = Instantiate(armPrefab, armParent);
                arms[i].name = $"Arm_{i}_{loadout.GetSlot(i).name}";
                arms[i].Setup(loadout.GetSlot(i));
                PlaceArm(i, ArmSelector.HomeAngle(i));
            }
        }

        private void OnEnable() => input.LockTogglePressed += OnLockTogglePressed;

        private void OnDisable() => input.LockTogglePressed -= OnLockTogglePressed;

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
