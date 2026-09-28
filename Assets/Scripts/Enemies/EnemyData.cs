using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>Tunable data for one enemy type. Real enemies (M4) extend this; movement fields are optional.</summary>
    [CreateAssetMenu(fileName = "Enemy_", menuName = "BulletHell/Enemy Data")]
    public sealed class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Enemy";
        [SerializeField] private Color color = new Color(0.9f, 0.3f, 0.3f);
        [Tooltip("Diameter in world units.")]
        [SerializeField, Min(0.1f)] private float size = 1f;

        [Header("Health")]
        [SerializeField, Min(0.01f)] private float maxHealth = 10f;
        [SerializeField, Min(0f)] private float respawnDelay = 3f;
        [SerializeField, Min(0f)] private float hitFlashDuration = 0.08f;

        [Header("Patrol (speed 0 = static)")]
        [SerializeField, Min(0f)] private float moveSpeed;
        [Tooltip("Distance from the start position it travels each way.")]
        [SerializeField, Min(0f)] private float moveRange = 3f;
        [SerializeField] private Vector2 moveAxis = Vector2.right;

        public string DisplayName => displayName;
        public Color Color => color;
        public float Size => size;
        public float MaxHealth => maxHealth;
        public float RespawnDelay => respawnDelay;
        public float HitFlashDuration => hitFlashDuration;
        public float MoveSpeed => moveSpeed;
        public float MoveRange => moveRange;
        public Vector2 MoveAxis => moveAxis;
    }
}
