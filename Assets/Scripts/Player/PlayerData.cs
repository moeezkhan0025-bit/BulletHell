using UnityEngine;

namespace BulletHell.Player
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "BulletHell/Player Data")]
    public sealed class PlayerData : ScriptableObject
    {
        [Tooltip("World units per second at full stick.")]
        [SerializeField, Min(0f)] private float moveSpeed = 6f;

        [Tooltip("Distance from the player's center to each arm's center.")]
        [SerializeField, Min(0f)] private float armRingRadius = 0.85f;

        [Tooltip("How far inside the camera edges the player's center is kept.")]
        [SerializeField, Min(0f)] private float screenEdgePadding = 1.2f;

        [Tooltip("Home slot of the single starting arm (0 = N, clockwise). Ignored when InputTuning equips all arms.")]
        [SerializeField, Range(0, 7)] private int startingArmSlot = 0;

        public float MoveSpeed => moveSpeed;
        public int StartingArmSlot => startingArmSlot;
        public float ArmRingRadius => armRingRadius;
        public float ScreenEdgePadding => screenEdgePadding;
    }
}
