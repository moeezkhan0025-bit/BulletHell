using UnityEngine;

namespace BulletHell.Arena
{
    public enum ObstacleKind { Solid, Breakable }

    /// <summary>Shape of the footprint on the floor. Circle is an ellipse when the footprint is wider than deep.</summary>
    public enum ObstacleShape { Box, Circle }

    /// <summary>
    /// One kind of obstacle. Both kinds block movement and every bullet. Solid ones are permanent and indestructible;
    /// breakable ones take damage from any bullet, look worse in stages, then break into non-blocking debris.
    /// The gameplay shape is a flat footprint at the base (width x depth); the art is a separate, taller sprite with
    /// its pivot at the base that overlaps whatever is behind it.
    /// </summary>
    [CreateAssetMenu(fileName = "Obstacle_", menuName = "BulletHell/Obstacle Data")]
    public sealed class ObstacleData : ScriptableObject
    {
        [SerializeField] private ObstacleKind kind = ObstacleKind.Solid;
        [SerializeField] private ObstacleShape shape = ObstacleShape.Box;
        [Tooltip("The footprint on the floor: width x depth (Circle = ellipse). A placement can override it.")]
        [SerializeField] private Vector2 size = Vector2.one;

        [Header("Look")]
        [Tooltip("Optional art, pivot at the base. Empty = the placeholder square / circle.")]
        [SerializeField] private Sprite sprite;
        [Tooltip("Drawn size of the art: width x height (the tall body above the footprint). Zero = same as the footprint.")]
        [SerializeField] private Vector2 artSize;
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
        /// <summary>The footprint: width x depth.</summary>
        public Vector2 Size => size;
        public Sprite Sprite => sprite;
        public Color Color => color;
        public float MaxHealth => maxHealth;
        public Color[] StageTints => stageTints;
        public Color DebrisColor => debrisColor;
        public float DebrisScale => debrisScale;
        public bool IsBreakable => kind == ObstacleKind.Breakable;

        /// <summary>Drawn size of the art for a placed footprint: width follows the footprint width, extra footprint depth adds to the height (the visible top face).</summary>
        public Vector2 ArtSizeFor(Vector2 footprint)
        {
            if (artSize == Vector2.zero || size.x <= 0f)
                return footprint;
            return new Vector2(artSize.x * footprint.x / size.x, Mathf.Max(0.05f, artSize.y + footprint.y - size.y));
        }

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

        /// <summary>Editor-only: the art and its drawn size.</summary>
        public void ConfigureArt(Sprite newSprite, Vector2 newArtSize)
        {
            sprite = newSprite;
            artSize = newArtSize;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
