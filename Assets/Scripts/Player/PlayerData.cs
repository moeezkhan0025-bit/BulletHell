using UnityEngine;

namespace BulletHell.Player
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "BulletHell/Player Data")]
    public sealed class PlayerData : ScriptableObject
    {
        [Tooltip("World units per second at full stick.")]
        [SerializeField, Min(0f)] private float moveSpeed = 6f;

        [Tooltip("Distance from the player's center to each arm's attach point.")]
        [SerializeField, Min(0f)] private float armRingRadius = 0.45f;

        [Tooltip("How far inside the camera edges the player's center is kept.")]
        [SerializeField, Min(0f)] private float screenEdgePadding = 1.2f;

        public float MoveSpeed => moveSpeed;
        public float ArmRingRadius => armRingRadius;
        public float ScreenEdgePadding => screenEdgePadding;
    }
}
