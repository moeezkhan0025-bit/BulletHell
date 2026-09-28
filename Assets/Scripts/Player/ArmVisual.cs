using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>Placeholder look for one arm: idle, selected, or selected + locked.</summary>
    public sealed class ArmVisual : MonoBehaviour
    {
        public enum State { Idle, Selected, Locked }

        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer lockOutline;
        [SerializeField] private Color idleColor = new Color(0.35f, 0.4f, 0.5f);
        [SerializeField] private Color selectedColor = new Color(0.3f, 0.9f, 1f);
        [SerializeField] private Color lockedColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField, Min(0f)] private float selectedScale = 1.25f;

        private void Awake() => SetState(State.Idle);

        public void SetState(State state)
        {
            body.color = state switch
            {
                State.Selected => selectedColor,
                State.Locked => lockedColor,
                _ => idleColor,
            };
            lockOutline.enabled = state == State.Locked;
            transform.localScale = Vector3.one * (state == State.Idle ? 1f : selectedScale);
        }
    }
}
