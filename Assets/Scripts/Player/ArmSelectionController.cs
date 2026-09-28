using System;
using BulletHell.Input;
using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>Feeds the left stick into ArmSelector, handles R3 lock, and drives the arm visuals.</summary>
    public sealed class ArmSelectionController : MonoBehaviour
    {
        [SerializeField] private GameplayInputReader input;
        [SerializeField] private InputTuning tuning;
        [SerializeField] private PlayerData playerData;
        [Tooltip("One per direction, index 0 = N, clockwise.")]
        [SerializeField] private ArmVisual[] arms = new ArmVisual[ArmSelector.ArmCount];

        private ArmSelector selector;

        public int SelectedArm => selector.Selected;
        public bool IsLocked => selector.Locked;
        /// <summary>The raw aim stick this frame, for the debug overlay.</summary>
        public Vector2 AimStick { get; private set; }

        public event Action<int> SelectionChanged;
        public event Action<bool> LockChanged;

        private void Awake()
        {
            selector = new ArmSelector(tuning);
            LayoutArms();
        }

        private void OnEnable() => input.LockTogglePressed += OnLockTogglePressed;

        private void OnDisable() => input.LockTogglePressed -= OnLockTogglePressed;

        private void Update()
        {
            AimStick = input.Aim;
            if (!selector.Update(AimStick))
                return;

            RefreshVisuals();
            SelectionChanged?.Invoke(selector.Selected);
        }

        private void OnLockTogglePressed()
        {
            if (!selector.ToggleLock())
                return;

            RefreshVisuals();
            LockChanged?.Invoke(selector.Locked);
        }

        private void LayoutArms()
        {
            for (int i = 0; i < arms.Length; i++)
            {
                float degrees = i * ArmSelector.SliceDegrees;
                float radians = degrees * Mathf.Deg2Rad;
                var arm = arms[i].transform;
                arm.localPosition = new Vector3(Mathf.Sin(radians), Mathf.Cos(radians), 0f) * playerData.ArmRingRadius;
                arm.localRotation = Quaternion.Euler(0f, 0f, -degrees);
            }
        }

        private void RefreshVisuals()
        {
            for (int i = 0; i < arms.Length; i++)
            {
                var state = ArmVisual.State.Idle;
                if (i == selector.Selected)
                    state = selector.Locked ? ArmVisual.State.Locked : ArmVisual.State.Selected;
                arms[i].SetState(state);
            }
        }
    }
}
