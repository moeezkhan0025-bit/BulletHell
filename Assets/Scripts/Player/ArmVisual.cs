using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>Placeholder look for one arm: hidden, soft-selected, or locked.</summary>
    public sealed class ArmVisual : MonoBehaviour
    {
        public enum State { Hidden, Selected, Locked }

        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer lockOutline;
        [SerializeField] private Color selectedColor = new Color(0.3f, 0.9f, 1f);
        [SerializeField] private Color lockedColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField, Min(0f)] private float selectedScale = 1.25f;

        private void Awake() => SetState(State.Hidden);

        public void SetState(State state)
        {
            body.enabled = state != State.Hidden;
            body.color = state == State.Locked ? lockedColor : selectedColor;
            lockOutline.enabled = state == State.Locked;
            transform.localScale = Vector3.one * (state == State.Hidden ? 1f : selectedScale);
        }
    }
}
