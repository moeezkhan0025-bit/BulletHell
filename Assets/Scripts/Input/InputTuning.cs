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

        public float SelectThreshold => selectThreshold;
        public float DeselectThreshold => deselectThreshold;
        public float AngleHysteresisDegrees => angleHysteresisDegrees;

        private void OnValidate()
        {
            if (deselectThreshold > selectThreshold)
                deselectThreshold = selectThreshold;
        }
    }
}
