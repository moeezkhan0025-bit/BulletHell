using UnityEngine;

namespace BulletHell.Arena
{
    public enum ObstacleKind { Solid, Breakable }

    public enum ObstacleShape { Box, Circle }

    /// <summary>
    /// One kind of obstacle. Both kinds block movement and every bullet. Solid ones are permanent and indestructible;
    /// breakable ones take damage from any bullet, look worse in stages, then break into non-blocking debris.
    /// </summary>
    [CreateAssetMenu(fileName = "Obstacle_", menuName = "BulletHell/Obstacle Data")]
    public sealed class ObstacleData : ScriptableObject
    {
        [SerializeField] private ObstacleKind kind = ObstacleKind.Solid;
        [SerializeField] private ObstacleShape shape = ObstacleShape.Box;
        [Tooltip("Box: width x height. Circle: x is the diameter. A placement can override it.")]
        [SerializeField] private Vector2 size = Vector2.one;

        [Header("Look")]
        [Tooltip("Optional art. Empty = the placeholder square / circle.")]
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color color = new Color(0.55f, 0.5f, 0.45f);

        [Header("Breakable only")]
        [SerializeField, Min(1f)] private float maxHealth = 30f;
        [Tooltip("Tint per damage stage (first = undamaged), multiplied with the colour. Its length is the number of stages.")]
        [SerializeField] private Color[] stageTints = { Color.white, new Color(0.85f, 0.75f, 0.7f), new Color(0.65f, 0.5f, 0.45f) };
        [SerializeField] private Color debrisColor = new Color(0.35f, 0.3f, 0.25f, 0.8f);
        [Tooltip("Debris size as a fraction of the obstacle's size.")]
        [SerializeField, Range(0.1f, 1f)] private float debrisScale = 0.6f;

        public ObstacleKind Kind => kind;
        public ObstacleShape Shape => shape;
        public Vector2 Size => size;
        public Sprite Sprite => sprite;
        public Color Color => color;
        public float MaxHealth => maxHealth;
        public Color[] StageTints => stageTints;
        public Color DebrisColor => debrisColor;
        public float DebrisScale => debrisScale;
        public bool IsBreakable => kind == ObstacleKind.Breakable;

#if UNITY_EDITOR
        /// <summary>Editor-only: used by the setup script and tests.</summary>
        public void Configure(ObstacleKind newKind, ObstacleShape newShape, Vector2 newSize, Color newColor, float health)
        {
            kind = newKind;
            shape = newShape;
            size = newSize;
            color = newColor;
            maxHealth = health;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
