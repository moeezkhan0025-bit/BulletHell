using System;
using BulletHell.Input;
using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>Feeds the left stick into ArmSelector, handles L3 lock, and drives the arm visuals.</summary>
    public sealed class ArmSelectionController : MonoBehaviour
    {
        [SerializeField] private GameplayInputReader input;
        [SerializeField] private InputTuning tuning;
        [SerializeField] private PlayerData playerData;
        [Tooltip("One per direction, index 0 = N, clockwise.")]
        [SerializeField] private ArmVisual[] arms = new ArmVisual[ArmSelector.ArmCount];

        private ArmSelector selector;
        private int shownArm = ArmSelector.None;

        public int SelectedArm => selector.Selected;
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
            for (int i = 0; i < ArmSelector.ArmCount; i++)
                selector.SetOwned(i, tuning.DebugEquipAllArms || i == playerData.StartingArmSlot);

            for (int i = 0; i < arms.Length; i++)
                PlaceArm(i, ArmSelector.HomeAngle(i));
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

        /// <summary>Puts an arm on the ring around the player, pointing outward along a compass angle.</summary>
        private void PlaceArm(int index, float compassDegrees)
        {
            float radians = compassDegrees * Mathf.Deg2Rad;
            var arm = arms[index].transform;
            arm.localPosition = new Vector3(Mathf.Sin(radians), Mathf.Cos(radians), 0f) * playerData.ArmRingRadius;
            arm.localRotation = Quaternion.Euler(0f, 0f, -compassDegrees);
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
