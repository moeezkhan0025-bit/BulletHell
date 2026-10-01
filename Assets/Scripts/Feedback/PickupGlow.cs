using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Feedback
{
    /// <summary>
    /// A soft glow under a coin or an ammo pickup, so loot reads on the busy floor: a procedural placeholder sprite (from FeedbackTuning)
    /// tinted in the VoxKit palette, slowly pulsing. Put on the prefab; it builds its own child sprite behind the parent's renderer.
    /// An AmmoPickup tints it with its ammo colour.
    /// </summary>
    public sealed class PickupGlow : MonoBehaviour
    {
        [SerializeField] private Color color = new Color(0.914f, 0.714f, 0.227f, 0.55f);
        [Tooltip("World size of the glow disc.")]
        [SerializeField, Min(0.1f)] private float size = 0.85f;
        [SerializeField, Min(0f)] private float pulseSpeed = 3.2f;
        [SerializeField, Range(0f, 0.6f)] private float pulseAmount = 0.25f;
        [Tooltip("Use the ammo colour of an AmmoPickup on the same object.")]
        [SerializeField] private bool tintFromAmmo;

        private SpriteRenderer glow;
        private SpriteRenderer parentRenderer;
        private float phase;

        private void Awake()
        {
            parentRenderer = GetComponentInChildren<SpriteRenderer>();
            Sprite sprite = GameServices.Ensure().Config.Feedback != null ? GameServices.Ensure().Config.Feedback.GlowSprite : null;
            if (sprite == null || parentRenderer == null)
            {
                enabled = false;
                return;
            }
            var go = new GameObject("PickupGlow");
            go.transform.SetParent(transform, false);
            glow = go.AddComponent<SpriteRenderer>();
            glow.sprite = sprite;
            glow.sortingLayerID = parentRenderer.sortingLayerID;
            glow.sortingOrder = parentRenderer.sortingOrder - 1;
            float native = Mathf.Max(0.01f, sprite.bounds.size.x);
            go.transform.localScale = Vector3.one * (size / native);
            phase = Random.value * 6.28f;
        }

        private void OnEnable()
        {
            if (tintFromAmmo && TryGetComponent(out BulletHell.Pickups.AmmoPickup pickup) && pickup.Ammo != null && glow != null)
            {
                Color tint = pickup.Ammo.Tint;
                color = new Color(tint.r, tint.g, tint.b, color.a);
            }
        }

        private void LateUpdate()
        {
            if (glow == null)
                return;
            float pulse = 1f + pulseAmount * Mathf.Sin(Time.time * pulseSpeed + phase);
            Color c = color;
            c.a *= pulse;
            glow.color = c;
            glow.transform.position = parentRenderer.transform.position;   // follows the coin while it bounces out
        }
    }
}
