using BulletHell.Input;
using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>Moves the player with the right stick, kept inside the camera view.</summary>
    public sealed class PlayerMover : MonoBehaviour
    {
        [SerializeField] private GameplayInputReader input;
        [SerializeField] private PlayerData playerData;
        [SerializeField] private Camera viewCamera;

        private void Update()
        {
            Vector2 move = Vector2.ClampMagnitude(input.Move, 1f);
            Vector3 position = transform.position + (Vector3)(move * (playerData.MoveSpeed * Time.deltaTime));

            float halfHeight = viewCamera.orthographicSize - playerData.ScreenEdgePadding;
            float halfWidth = viewCamera.orthographicSize * viewCamera.aspect - playerData.ScreenEdgePadding;
            Vector3 center = viewCamera.transform.position;
            position.x = Mathf.Clamp(position.x, center.x - halfWidth, center.x + halfWidth);
            position.y = Mathf.Clamp(position.y, center.y - halfHeight, center.y + halfHeight);

            transform.position = position;
        }
    }
}
