using BulletHell.Arena;
using BulletHell.Input;
using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>
    /// Moves the player with the right stick. Inside an arena the body (a circle) slides along the walls and obstacles
    /// through the arena grid; with no arena built it stays inside the camera view like before.
    /// </summary>
    public sealed class PlayerMover : MonoBehaviour
    {
        [SerializeField] private GameplayInputReader input;
        [SerializeField] private PlayerData playerData;
        [SerializeField] private Camera viewCamera;
        [Tooltip("The arena to collide with. Optional.")]
        [SerializeField] private ArenaController arena;
        [Tooltip("Scales the speed while airborne. Optional.")]
        [SerializeField] private JumpController jump;

        private void Update()
        {
            Vector2 move = Vector2.ClampMagnitude(input.Move, 1f);
            Vector2 delta = move * (playerData.MoveSpeed * (jump != null ? jump.MoveMultiplier : 1f) * Time.deltaTime);

            if (arena != null && arena.IsBuilt)
            {
                transform.position = arena.Grid.Move(transform.position, delta, playerData.BodyRadius);
                return;
            }

            Vector3 position = transform.position + (Vector3)delta;
            float halfHeight = viewCamera.orthographicSize - playerData.ScreenEdgePadding;
            float halfWidth = viewCamera.orthographicSize * viewCamera.aspect - playerData.ScreenEdgePadding;
            Vector3 center = viewCamera.transform.position;
            position.x = Mathf.Clamp(position.x, center.x - halfWidth, center.x + halfWidth);
            position.y = Mathf.Clamp(position.y, center.y - halfHeight, center.y + halfHeight);
            transform.position = position;
        }
    }
}
