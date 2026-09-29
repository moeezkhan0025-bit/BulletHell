using System;
using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Arena
{
    /// <summary>
    /// A placed obstacle. It blocks movement (through the arena grid) and every bullet: player bullets and beams hit its
    /// collider (layer Obstacle), enemy bullets hit its grid cells. Solid ones are indestructible. Breakable ones are
    /// damaged by any bullet, tint through damage stages, and on breaking turn into non-blocking debris.
    /// The collider is on this object; the art is on the Art child so it can be scaled freely.
    /// </summary>
    public sealed class Obstacle : MonoBehaviour, IDamageable
    {
        private const int ObstacleSortingOrder = 1;
        private const int DebrisSortingOrder = -8;

        [SerializeField] private SpriteRenderer art;
        [SerializeField] private BoxCollider2D boxCollider;
        [SerializeField] private CircleCollider2D circleCollider;

        private ObstacleData data;
        private Sprite sprite;
        private Vector2 size;
        private ArenaGrid grid;
        private float health;
        private int stage;

        public int Id { get; private set; }
        public bool IsBroken { get; private set; }
        public ObstacleData Data => data;
        public float Health => health;
        /// <summary>The current damage stage (0 = undamaged).</summary>
        public int Stage => stage;

        /// <summary>Only a standing breakable is a living target; solids report false, which makes bullets stop on them without damage.</summary>
        public bool IsAlive => data != null && data.IsBreakable && !IsBroken;

        /// <summary>Raised once when a breakable breaks.</summary>
        public event Action<Obstacle> Broken;

        /// <summary>Fills this (pooled) object in for a placement. Call Register to put it into the grid.</summary>
        public void Setup(ObstacleData obstacleData, Vector2 position, Vector2 obstacleSize, int id, Sprite squareSprite, Sprite circleSprite)
        {
            data = obstacleData;
            Id = id;
            size = obstacleSize;
            sprite = data.Sprite != null ? data.Sprite : (data.Shape == ObstacleShape.Circle ? circleSprite : squareSprite);
            transform.position = position;
            transform.rotation = Quaternion.identity;

            bool circle = data.Shape == ObstacleShape.Circle;
            boxCollider.size = size;
            boxCollider.offset = Vector2.zero;
            circleCollider.radius = size.x * 0.5f;
            circleCollider.offset = Vector2.zero;
            boxCollider.enabled = !circle;
            circleCollider.enabled = circle;
            Restore();
        }

        public void Register(ArenaGrid arenaGrid)
        {
            grid = arenaGrid;
            if (IsBroken)
                return;
            if (data.Shape == ObstacleShape.Circle)
                grid.AddCircle(transform.position, size.x * 0.5f, Id);
            else
                grid.AddBox(transform.position, size, Id);
        }

        /// <summary>Back to standing and undamaged (the start of a round). The caller re-registers it in a cleared grid.</summary>
        public void Restore()
        {
            IsBroken = false;
            health = data.MaxHealth;
            stage = 0;
            SetCollidersEnabled(true);
            ApplyLook();
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            health = Mathf.Max(0f, health - amount);
            if (health <= 0f)
            {
                Break();
                return;
            }

            int newStage = ObstacleHealthStages.Stage(health / data.MaxHealth, data.StageTints.Length);
            if (newStage != stage)
            {
                stage = newStage;
                ApplyLook();
            }
        }

        private void Break()
        {
            IsBroken = true;
            SetCollidersEnabled(false);
            grid?.ClearOwner(Id);
            ApplyLook();
            Broken?.Invoke(this);
        }

        private void SetCollidersEnabled(bool enabled)
        {
            bool circle = data.Shape == ObstacleShape.Circle;
            boxCollider.enabled = enabled && !circle;
            circleCollider.enabled = enabled && circle;
        }

        private void ApplyLook()
        {
            art.sprite = sprite;
            Vector2 spriteSize = sprite != null ? (Vector2)sprite.bounds.size : Vector2.one;
            float fraction = IsBroken ? data.DebrisScale : 1f;
            art.transform.localScale = new Vector3(size.x / spriteSize.x * fraction, size.y / spriteSize.y * fraction, 1f);
            art.sortingOrder = IsBroken ? DebrisSortingOrder : ObstacleSortingOrder;

            Color color = data.Color;
            if (IsBroken)
            {
                color = data.DebrisColor;
            }
            else if (data.IsBreakable && stage < data.StageTints.Length)
            {
                Color tint = data.StageTints[stage];
                color = new Color(color.r * tint.r, color.g * tint.g, color.b * tint.b, color.a * tint.a);
            }
            art.color = color;
        }
    }
}
