using BulletHell.Arena;
using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>
    /// Moves a kinematic body back and forth along an axis at constant speed. With an arena it avoids obstacles simply:
    /// when the next step would be blocked (an obstacle or a wall) it turns around instead of walking into it.
    /// (Real navigation arrives with the M8 AI rework.)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PatrolMover : MonoBehaviour
    {
        private Rigidbody2D body;
        private Vector2 origin;
        private Vector2 axis = Vector2.right;
        private float speed;
        private float range;
        private float offset;
        private float direction = 1f;
        private StatusEffects status;
        private ArenaController arena;
        private float radius;

        /// <param name="avoidArena">Obstacles to turn around at; null = walk the whole range regardless.</param>
        /// <param name="bodyRadius">Radius of the enemy's body for the obstacle test.</param>
        public void Configure(Vector2 startPosition, float moveSpeed, float moveRange, Vector2 moveAxis,
                              ArenaController avoidArena = null, float bodyRadius = 0f)
        {
            body = GetComponent<Rigidbody2D>();
            TryGetComponent(out status);
            origin = startPosition;
            speed = moveSpeed;
            range = moveRange;
            axis = moveAxis.sqrMagnitude > 0f ? moveAxis.normalized : Vector2.right;
            arena = avoidArena;
            radius = bodyRadius;
            Restart();
        }

        /// <summary>Back to the start position and phase. Patrolling is enabled only when speed and range are above zero.</summary>
        public void Restart()
        {
            offset = 0f;
            direction = 1f;
            body.position = origin;
            transform.position = origin;
            enabled = speed > 0f && range > 0f;
        }

        private void FixedUpdate()
        {
            if (status != null && status.IsStunned)
                return; // stunned: hold position, and the patrol phase pauses too

            float next = offset + direction * speed * Time.fixedDeltaTime;
            if (next > range || next < -range)
            {
                next = Mathf.Clamp(next, -range, range);
                direction = -direction;
            }

            Vector2 target = origin + axis * next;
            if (arena != null && arena.IsBuilt && arena.Grid.CircleBlocked(target, radius))
            {
                direction = -direction;   // something is in the way: turn around and try the other way next step
                return;
            }

            offset = next;
            body.MovePosition(target);
        }
    }
}
