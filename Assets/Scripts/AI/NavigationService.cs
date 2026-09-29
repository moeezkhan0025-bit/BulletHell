using System;
using System.Collections.Generic;
using BulletHell.Arena;
using BulletHell.Enemies;
using BulletHell.Player;
using UnityEngine;

namespace BulletHell.AI
{
    /// <summary>
    /// Enemy navigation for the arena: owns the flow field towards the player's FEET (so it follows the shadow during a
    /// jump), keeps it up to date (rebuilt when a breakable breaks, when the arena is rebuilt for a round, and as the
    /// player crosses into other cells), answers "which way should I walk", and keeps the registry of live enemies that
    /// separation works from.
    /// </summary>
    public sealed class NavigationService : MonoBehaviour
    {
        [SerializeField] private ArenaController arena;
        [SerializeField] private PlayerHealth player;
        [SerializeField] private EnemyAiTuning tuning;

        private readonly List<Enemy> enemies = new List<Enemy>();
        private FlowField field;
        private bool maskDirty = true;
        private float sinceRebuild = 99f;

        public EnemyAiTuning Tuning => tuning != null ? tuning : EnemyAiTuning.Fallback;
        public ArenaController Arena => arena;
        public PlayerHealth Player => player;
        public IReadOnlyList<Enemy> Enemies => enemies;
        /// <summary>The current flow field, or null while no arena is built.</summary>
        public FlowField Field => field;

        /// <summary>Where enemies aim to be: the player's feet, which stay on the ground while jumping.</summary>
        public Vector2 TargetPosition => player.FeetPosition;

        private void OnEnable()
        {
            arena.GridRebuilt += MarkDirty;
            arena.ObstacleBroken += OnObstacleBroken;
            maskDirty = true;
        }

        private void OnDisable()
        {
            arena.GridRebuilt -= MarkDirty;
            arena.ObstacleBroken -= OnObstacleBroken;
        }

        private void MarkDirty() => maskDirty = true;

        private void OnObstacleBroken(Obstacle obstacle) => maskDirty = true;

        public void Register(Enemy enemy)
        {
            if (!enemies.Contains(enemy))
                enemies.Add(enemy);
        }

        public void Unregister(Enemy enemy) => enemies.Remove(enemy);

        private void Update()
        {
            if (!arena.IsBuilt)
            {
                field = null;
                return;
            }

            sinceRebuild += Time.deltaTime;

            if (field == null || !ReferenceEquals(field.Grid, arena.Grid))
            {
                field = new FlowField(arena.Grid, Tuning.NavRadius);
                maskDirty = false;
                Rebuild();
                return;
            }

            if (maskDirty)
            {
                field.RebuildMask(Tuning.NavRadius);
                maskDirty = false;
                Rebuild();
                return;
            }

            arena.Grid.WorldToCell(TargetPosition, out int column, out int row);
            if ((column != field.TargetColumn || row != field.TargetRow) && sinceRebuild >= Tuning.MinRebuildInterval)
                Rebuild();
        }

        private void Rebuild()
        {
            field.Build(TargetPosition);
            sinceRebuild = 0f;
        }

        /// <summary>
        /// The way to walk from a position towards the player: straight at them when nothing is in the way (smooth),
        /// otherwise along the flow field around obstacles. Zero when there is no arena or no route.
        /// </summary>
        public Vector2 Direction(Vector2 position, float radius)
        {
            Vector2 target = TargetPosition;
            if (!arena.IsBuilt || field == null)
                return (target - position).normalized;

            if (!arena.Grid.SegmentBlocked(position, target, radius, out _))
                return (target - position).normalized;
            return field.Direction(position);
        }

        /// <summary>Sum of the pushes of nearby enemies on one enemy (each up to unit length, growing with overlap).</summary>
        public Vector2 Separation(Enemy self)
        {
            float padding = Tuning.SeparationPadding;
            Vector2 push = Vector2.zero;
            Vector2 position = self.Position;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy other = enemies[i];
                if (other == self || !other.IsAlive)
                    continue;
                float reach = self.FootprintRadius + other.FootprintRadius + padding;
                push += Steering.PushAway(position, other.Position, reach);
            }
            return push;
        }
    }
}
