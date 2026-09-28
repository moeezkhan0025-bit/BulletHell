using UnityEngine;

namespace BulletHell.Input
{
    /// <summary>Tuning for left-stick arm selection. Defaults match CLAUDE.md.</summary>
    [CreateAssetMenu(fileName = "InputTuning", menuName = "BulletHell/Input Tuning")]
    public sealed class InputTuning : ScriptableObject
    {
        [Tooltip("Stick magnitude needed to select an arm (outer edge).")]
        [SerializeField, Range(0f, 1f)] private float selectThreshold = 0.85f;

        [Tooltip("A selected arm deselects when stick magnitude drops below this.")]
        [SerializeField, Range(0f, 1f)] private float deselectThreshold = 0.65f;

        [Tooltip("Degrees the stick must go past a slice boundary before switching arms.")]
        [SerializeField, Range(0f, 22.5f)] private float angleHysteresisDegrees = 8f;

        [Tooltip("While locked, the arm only follows the stick at or above this magnitude; below it the last aim is kept.")]
        [SerializeField, Range(0f, 1f)] private float lockedAimDeadzone = 0.5f;

        [Header("Debug")]
        [Tooltip("Equip all 8 placeholder arms instead of the single starting arm.")]
        [SerializeField] private bool debugEquipAllArms = true;

        public float SelectThreshold => selectThreshold;
        public float DeselectThreshold => deselectThreshold;
        public float AngleHysteresisDegrees => angleHysteresisDegrees;
        public float LockedAimDeadzone => lockedAimDeadzone;
        public bool DebugEquipAllArms => debugEquipAllArms;

        private void OnValidate()
        {
            if (deselectThreshold > selectThreshold)
                deselectThreshold = selectThreshold;
        }
    }
}
