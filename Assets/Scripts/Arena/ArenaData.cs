using System;
using UnityEngine;

namespace BulletHell.Arena
{
    [Serializable]
    public struct ObstaclePlacement
    {
        public ObstacleData Data;
        public Vector2 Position;
        [Tooltip("Zero = use the obstacle data's size.")]
        public Vector2 SizeOverride;

        public Vector2 Size => SizeOverride != Vector2.zero ? SizeOverride : (Data != null ? Data.Size : Vector2.one);
    }

    [Serializable]
    public struct TrapPlacement
    {
        public TrapData Data;
        public Vector2 Position;
        [Tooltip("Degrees, counter-clockwise (matters for skewer lines).")]
        public float Rotation;
        [Tooltip("Added to the trap's start delay, so traps in one arena don't all fire together.")]
        [Min(0f)] public float ExtraStartDelay;
    }

    /// <summary>
    /// A colosseum layout: the walls (a rectangle centred on the origin), where the player starts, the gates enemies
    /// come out of, and the placed obstacles and traps. Rounds reference one; a new layout is a new asset, not code.
    /// </summary>
    [CreateAssetMenu(fileName = "Arena_", menuName = "BulletHell/Arena Data")]
    public sealed class ArenaData : ScriptableObject
    {
        [Header("Layout")]
        [Tooltip("Playable width x height in world units, centred on the origin. The camera fits to this.")]
        [SerializeField] private Vector2 size = new Vector2(16f, 9f);
        [SerializeField] private Vector2 playerSpawn = new Vector2(0f, -3.2f);
        [Tooltip("Enemies with the Gates spawn pattern appear here, one gate after another.")]
        [SerializeField] private Vector2[] spawnGates = new Vector2[0];
        [SerializeField] private ObstaclePlacement[] obstacles = new ObstaclePlacement[0];
        [SerializeField] private TrapPlacement[] traps = new TrapPlacement[0];

        [Header("Placeholder look")]
        [SerializeField] private Color floorColor = new Color(0.86f, 0.78f, 0.55f);
        [SerializeField] private Color wallColor = new Color(0.45f, 0.3f, 0.2f);
        [Tooltip("The crowd beyond the walls (also the camera background).")]
        [SerializeField] private Color standsColor = new Color(0.25f, 0.5f, 0.25f);

        public Vector2 Size => size;
        public Rect Bounds => new Rect(-size.x * 0.5f, -size.y * 0.5f, size.x, size.y);
        public Vector2 PlayerSpawn => playerSpawn;
        public Vector2[] SpawnGates => spawnGates;
        public ObstaclePlacement[] Obstacles => obstacles;
        public TrapPlacement[] Traps => traps;
        public Color FloorColor => floorColor;
        public Color WallColor => wallColor;
        public Color StandsColor => standsColor;

#if UNITY_EDITOR
        /// <summary>Editor-only: used by the setup script and tests.</summary>
        public void Configure(Vector2 newSize, Vector2 spawn, Vector2[] gates, ObstaclePlacement[] newObstacles, TrapPlacement[] newTraps)
        {
            size = newSize;
            playerSpawn = spawn;
            spawnGates = gates;
            obstacles = newObstacles;
            traps = newTraps;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
