using System;
using BulletHell.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace BulletHell.Arena
{
    /// <summary>
    /// A placed obstacle. The object sits at the centre of its flat footprint, which is what blocks movement (through
    /// the arena grid). Bullets are blocked by the footprint plus a vertical reach (the body above it): enemy bullets by
    /// the bullet grid, player bullets and beams by this object's collider (layer Obstacle), which has the same shape.
    /// Solid ones are indestructible. Breakable ones are damaged by any bullet, tint through damage stages, and on
    /// breaking turn into non-blocking debris on the ground layer.
    /// The art is a taller sprite on the Art child, hanging up from the footprint; a SortingGroup on this object sorts
    /// it by the footprint (feet) position against everything else.
    /// </summary>
    public sealed class Obstacle : MonoBehaviour, IDamageable
    {
        private const int EllipseSegments = 24;

        [SerializeField] private SpriteRenderer art;
        [SerializeField] private BoxCollider2D boxCollider;
        [Tooltip("Bullet-blocking shape of ellipse footprints.")]
        [SerializeField] private PolygonCollider2D ellipseCollider;
        [SerializeField] private SortingGroup sortingGroup;

        private ObstacleData data;
        private Sprite sprite;
        private Vector2 footprint;
        private Vector2 artSize;
        private float reach;
        private ArenaGrid moveGrid;
        private ArenaGrid bulletGrid;
        private ArenaGrid tallGrid;
        private float fade = 1f;
        private float health;
        private int stage;
        private Vector2[] ellipsePath;

        public int Id { get; private set; }
        public bool IsBroken { get; private set; }
        public ObstacleData Data => data;
        public bool IsLow => data != null && data.IsLow;
        /// <summary>Drawn size of the art (width x height); the fade uses it to know what "behind" means.</summary>
        public Vector2 ArtSize => artSize;
        public SpriteRenderer Art => art;
        public float Health => health;
        /// <summary>The footprint on the floor: width x depth.</summary>
        public Vector2 Footprint => footprint;
        /// <summary>The current damage stage (0 = undamaged).</summary>
        public int Stage => stage;

        /// <summary>Only a standing breakable is a living target; solids report false, which makes bullets stop on them without damage.</summary>
        public bool IsAlive => data != null && data.IsBreakable && !IsBroken;

        /// <summary>Raised once when a breakable breaks.</summary>
        public event Action<Obstacle> Broken;

        private bool IsEllipse => data.Shape == ObstacleShape.Circle;

        /// <summary>Fills this (pooled) object in for a placement. Call Register to put it into the grids.</summary>
        /// <param name="footprintSize">Width x depth of the footprint on the floor.</param>
        /// <param name="bulletReach">How far above the footprint bullets are still blocked (PerspectiveTuning).</param>
        public void Setup(ObstacleData obstacleData, Vector2 position, Vector2 footprintSize, int id, Sprite squareSprite, Sprite circleSprite,
                          float bulletReach = 0f)
        {
            data = obstacleData;
            Id = id;
            footprint = footprintSize;
            reach = Mathf.Max(0f, bulletReach);
            artSize = data.ArtSizeFor(footprint);
            sprite = data.Sprite != null ? data.Sprite : (IsEllipse ? circleSprite : squareSprite);
            transform.position = position;
            transform.rotation = Quaternion.identity;

            // Player bullets: one collider that covers the footprint and the reach above it.
            boxCollider.size = new Vector2(footprint.x, footprint.y + reach);
            boxCollider.offset = new Vector2(0f, reach * 0.5f);
            if (IsEllipse)
                ellipseCollider.SetPath(0, BuildEllipsePath(footprint * 0.5f, reach));
            Restore();
        }

        /// <param name="movement">Grid that blocks walking (the plain footprint).</param>
        /// <param name="bullets">Grid that blocks enemy bullets (footprint plus reach). Null = none.</param>
        /// <param name="tall">Grid of what still blocks a player high enough to clear Low obstacles: only Tall ones are added. Null = none.</param>
        public void Register(ArenaGrid movement, ArenaGrid bullets = null, ArenaGrid tall = null)
        {
            moveGrid = movement;
            bulletGrid = bullets;
            tallGrid = tall;
            if (IsBroken)
                return;

            Vector2 center = transform.position;
            Vector2 half = footprint * 0.5f;
            bool inTall = tall != null && !IsLow;
            if (IsEllipse)
            {
                movement.AddEllipse(center, half, Id);
                bullets?.AddEllipseReachingUp(center, half, reach, Id);
                if (inTall)
                    tall.AddEllipse(center, half, Id);
            }
            else
            {
                movement.AddBox(center, footprint, Id);
                bullets?.AddBox(center + new Vector2(0f, reach * 0.5f), new Vector2(footprint.x, footprint.y + reach), Id);
                if (inTall)
                    tall.AddBox(center, footprint, Id);
            }
        }

        /// <summary>Back to standing and undamaged (the start of a round). The caller re-registers it in cleared grids.</summary>
        public void Restore()
        {
            IsBroken = false;
            health = data.MaxHealth;
            stage = 0;
            fade = 1f;
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
            moveGrid?.ClearOwner(Id);
            bulletGrid?.ClearOwner(Id);
            tallGrid?.ClearOwner(Id);
            ApplyLook();
            Broken?.Invoke(this);
        }

        private void SetCollidersEnabled(bool enabled)
        {
            boxCollider.enabled = enabled && !IsEllipse;
            ellipseCollider.enabled = enabled && IsEllipse;
        }

        private void ApplyLook()
        {
            art.sprite = sprite;
            Vector2 spriteSize = sprite != null ? (Vector2)sprite.bounds.size : Vector2.one;
            float fraction = IsBroken ? data.DebrisScale : 1f;
            Vector2 drawn = artSize * fraction;
            var scale = new Vector2(drawn.x / spriteSize.x, drawn.y / spriteSize.y);
            art.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            // The art's bottom edge rests on the front edge of the footprint, whatever the sprite's pivot is.
            float bottom = sprite != null ? sprite.bounds.min.y : -0.5f;
            art.transform.localPosition = new Vector3(0f, -footprint.y * 0.5f - bottom * scale.y, 0f);

            if (sortingGroup != null)
            {
                sortingGroup.sortingLayerID = SortingLayers.Id(IsBroken ? SortingLayers.Ground : SortingLayers.Characters);
                sortingGroup.sortingOrder = 0;
            }

            ApplyContactShadow();
            ApplyColor();
        }

        // A soft ellipse under the footprint so the placeholder sits on the painted floor; broken debris is flat and has none.
        private void ApplyContactShadow()
        {
            PerspectiveTuning look = GameServices.Ensure().Config.Perspective;
            SpriteRenderer shadow = PlaceholderLook.ContactShadow(art, look.ShadowSprite, Vector2.zero,
                new Vector2(footprint.x * look.ContactShadowWidth, footprint.y * 1.05f), look, -1, transform);
            if (shadow != null)
                shadow.enabled = !IsBroken;
        }

        private void ApplyColor()
        {
            Color color = GameServices.Ensure().Config.Perspective.Muted(data.Color);
            if (IsBroken)
            {
                color = data.DebrisColor;
            }
            else
            {
                if (data.IsBreakable && stage < data.StageTints.Length)
                {
                    Color tint = data.StageTints[stage];
                    color = new Color(color.r * tint.r, color.g * tint.g, color.b * tint.b, color.a * tint.a);
                }
                color.a *= fade;
            }
            art.color = color;
        }

        /// <summary>Opacity multiplier of the art (1 = opaque). A Tall obstacle fades while something stands behind it.</summary>
        public float Fade => fade;

        public void SetFade(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            if (Mathf.Approximately(alpha, fade))
                return;
            fade = alpha;
            if (data != null)
                ApplyColor();
        }

        /// <summary>
        /// Outline of an ellipse footprint that also covers the reach above it: the lower half of the ellipse at the
        /// feet, the upper half moved up by the reach. Convex, in this object's local space.
        /// </summary>
        private Vector2[] BuildEllipsePath(Vector2 radii, float upReach)
        {
            int half = EllipseSegments / 2;
            ellipsePath ??= new Vector2[EllipseSegments + 2];
            int n = 0;
            for (int i = 0; i <= half; i++)                  // upper half, moved up by the reach
            {
                float angle = i * (2f * Mathf.PI / EllipseSegments);
                ellipsePath[n++] = new Vector2(Mathf.Cos(angle) * radii.x, Mathf.Sin(angle) * radii.y + upReach);
            }
            for (int i = half; i <= EllipseSegments; i++)    // lower half, at the feet
            {
                float angle = i * (2f * Mathf.PI / EllipseSegments);
                ellipsePath[n++] = new Vector2(Mathf.Cos(angle) * radii.x, Mathf.Sin(angle) * radii.y);
            }
            return ellipsePath;
        }
    }
}
