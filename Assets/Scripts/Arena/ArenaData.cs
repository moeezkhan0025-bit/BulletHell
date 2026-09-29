using UnityEngine;

namespace BulletHell.Arena
{
    /// <summary>
    /// The colosseum shell: the walls (a rectangle centred on the origin) and the placeholder colours. What stands
    /// inside it - player spawn, gates, obstacles and traps - is an <see cref="ArenaLayoutData"/>, so one shell can
    /// host many layouts.
    /// </summary>
    [CreateAssetMenu(fileName = "Arena_", menuName = "BulletHell/Arena Data")]
    public sealed class ArenaData : ScriptableObject
    {
        [Header("Shell")]
        [Tooltip("Playable width x height in world units, centred on the origin. The camera fits to this.")]
        [SerializeField] private Vector2 size = new Vector2(16f, 9f);

        [Header("Placeholder look")]
        [SerializeField] private Color floorColor = new Color(0.86f, 0.78f, 0.55f);
        [SerializeField] private Color wallColor = new Color(0.45f, 0.3f, 0.2f);
        [Tooltip("The crowd beyond the walls (also the camera background).")]
        [SerializeField] private Color standsColor = new Color(0.25f, 0.5f, 0.25f);

        public Vector2 Size => size;
        public Rect Bounds => new Rect(-size.x * 0.5f, -size.y * 0.5f, size.x, size.y);
        public Color FloorColor => floorColor;
        public Color WallColor => wallColor;
        public Color StandsColor => standsColor;

#if UNITY_EDITOR
        /// <summary>Editor-only: used by the setup script and tests.</summary>
        public void Configure(Vector2 newSize)
        {
            size = newSize;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
