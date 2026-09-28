using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>Moves a kinematic body back and forth along an axis at constant speed.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PatrolMover : MonoBehaviour
    {
        private Rigidbody2D body;
        private Vector2 origin;
        private Vector2 axis = Vector2.right;
        private float speed;
        private float range;
        private float elapsed;

        public void Configure(Vector2 startPosition, float moveSpeed, float moveRange, Vector2 moveAxis)
        {
            body = GetComponent<Rigidbody2D>();
            origin = startPosition;
            speed = moveSpeed;
            range = moveRange;
            axis = moveAxis.sqrMagnitude > 0f ? moveAxis.normalized : Vector2.right;
            Restart();
        }

        /// <summary>Back to the start position and phase. Patrolling is enabled only when speed and range are above zero.</summary>
        public void Restart()
        {
            elapsed = 0f;
            body.position = origin;
            transform.position = origin;
            enabled = speed > 0f && range > 0f;
        }

        private void FixedUpdate()
        {
            elapsed += Time.fixedDeltaTime;
            float offset = Mathf.PingPong(elapsed * speed + range, range * 2f) - range;
            body.MovePosition(origin + axis * offset);
        }
    }
}
