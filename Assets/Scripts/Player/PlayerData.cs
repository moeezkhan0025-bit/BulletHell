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

        [Header("Health")]
        [Tooltip("Hits the player can take per round (each enemy bullet does its own damage in hits). Full heal every round.")]
        [SerializeField, Min(1f)] private float maxHealth = 5f;
        [Tooltip("Radius of the part of the player that enemy bullets can hit. Smaller than the sprite, bullet-hell style.")]
        [SerializeField, Min(0.02f)] private float hitboxRadius = 0.18f;
        [Tooltip("Seconds after a hit during which enemy bullets pass through the player.")]
        [SerializeField, Min(0f)] private float invulnerabilitySeconds = 1f;
        [Tooltip("Seconds per blink of the body sprite while invulnerable.")]
        [SerializeField, Min(0.02f)] private float blinkInterval = 0.1f;

        public float MoveSpeed => moveSpeed;
        public float ArmRingRadius => armRingRadius;
        public float ScreenEdgePadding => screenEdgePadding;
        public float MaxHealth => maxHealth;
        public float HitboxRadius => hitboxRadius;
        public float InvulnerabilitySeconds => invulnerabilitySeconds;
        public float BlinkInterval => blinkInterval;
    }
}
