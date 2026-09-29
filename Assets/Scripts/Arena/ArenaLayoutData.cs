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
    /// What stands in a colosseum for a round: where the player starts, the gates enemies come out of, and the placed
    /// obstacles and traps, on top of an <see cref="ArenaData"/> shell. Every layout must keep a clear spawn area and a
    /// walkable lane from each gate (checked by <see cref="LayoutValidator"/>). A new layout is a new asset, not code.
    /// </summary>
    [CreateAssetMenu(fileName = "Layout_", menuName = "BulletHell/Arena Layout Data")]
    public sealed class ArenaLayoutData : ScriptableObject
    {
        [SerializeField] private ArenaData arena;
        [SerializeField] private Vector2 playerSpawn = new Vector2(0f, -3.2f);
        [Tooltip("Enemies with the Gates spawn pattern appear here, one gate after another.")]
        [SerializeField] private Vector2[] spawnGates = new Vector2[0];
        [SerializeField] private ObstaclePlacement[] obstacles = new ObstaclePlacement[0];
        [SerializeField] private TrapPlacement[] traps = new TrapPlacement[0];

        public ArenaData Arena => arena;
        public Vector2 PlayerSpawn => playerSpawn;
        public Vector2[] SpawnGates => spawnGates;
        public ObstaclePlacement[] Obstacles => obstacles;
        public TrapPlacement[] Traps => traps;

#if UNITY_EDITOR
        /// <summary>Editor-only: used by the setup script and tests.</summary>
        public void Configure(ArenaData shell, Vector2 spawn, Vector2[] gates, ObstaclePlacement[] newObstacles, TrapPlacement[] newTraps)
        {
            arena = shell;
            playerSpawn = spawn;
            spawnGates = gates;
            obstacles = newObstacles;
            traps = newTraps;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
