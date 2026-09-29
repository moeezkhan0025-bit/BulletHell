using UnityEngine;

namespace BulletHell.Player
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "BulletHell/Player Data")]
    public sealed class PlayerData : ScriptableObject
    {
        [Tooltip("World units per second at full stick.")]
        [SerializeField, Min(0f)] private float moveSpeed = 6f;

        [Tooltip("Unused since M7.7: the arm ring now lives in ArmRingTuning.")]
        [SerializeField, Min(0f)] private float armRingRadius = 0.45f;

        [Tooltip("Radius of the player's body against the arena walls and obstacles (not the bullet hitbox).")]
        [SerializeField, Min(0.05f)] private float bodyRadius = 0.35f;

        [Tooltip("Height of the body's centre above the feet (the player's root). The visuals, arms ring and damage core sit here.")]
        [SerializeField, Min(0f)] private float bodyCenterHeight = 0.38f;

        [Tooltip("How far inside the camera edges the player's center is kept.")]
        [SerializeField, Min(0f)] private float screenEdgePadding = 1.2f;

        [Header("Health")]
        [Tooltip("Hits the player can take per round (each enemy bullet does its own damage in hits). Full heal every round.")]
        [SerializeField, Min(1f)] private float maxHealth = 5f;
        [Tooltip("Radius of the part of the player that enemy bullets can hit. Smaller than the sprite, bullet-hell style.")]
        [SerializeField, Min(0.02f)] private float hitboxRadius = 0.18f;
        [Tooltip("How far above the feet the damage core sits. It stays on the ground plane (it does not rise when jumping), where bullets actually hit.")]
        [SerializeField, Min(0f)] private float coreFootOffset = 0.12f;
        [Tooltip("Seconds after a hit during which enemy bullets pass through the player.")]
        [SerializeField, Min(0f)] private float invulnerabilitySeconds = 1f;
        [Tooltip("Seconds per blink of the body sprite while invulnerable.")]
        [SerializeField, Min(0.02f)] private float blinkInterval = 0.1f;

        public float MoveSpeed => moveSpeed;
        public float ArmRingRadius => armRingRadius;
        /// <summary>Radius of the flat movement footprint at the feet.</summary>
        public float BodyRadius => bodyRadius;
        public float BodyCenterHeight => bodyCenterHeight;
        public float ScreenEdgePadding => screenEdgePadding;
        public float MaxHealth => maxHealth;
        public float HitboxRadius => hitboxRadius;
        public float CoreFootOffset => coreFootOffset;
        public float InvulnerabilitySeconds => invulnerabilitySeconds;
        public float BlinkInterval => blinkInterval;
    }
}
