using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Bosses
{
    /// <summary>
    /// The landing ring of a Jump &amp; Smash: a flat ellipse on the floor at the landing spot that grows and pulses in the
    /// DANGER colour while the boss is in the air, flashes on impact and fades. One sprite created once per boss, not
    /// parented under the boss so its sorting group can't bury it.
    /// </summary>
    public sealed class SmashTelegraph
    {
        private const float FlashSeconds = 0.3f;

        private readonly SpriteRenderer sprite;
        private readonly Vector2 native;
        private readonly float flatness;
        private float radius;
        private Color color;
        private bool showing;
        private float flashLeft;

        public SmashTelegraph(Transform parent, Sprite ringSprite, float floorFlatness)
        {
            var go = new GameObject("SmashTelegraph");
            go.transform.SetParent(parent, false);
            sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = ringSprite;
            sprite.sortingLayerID = SortingLayers.Id(SortingLayers.Ground);
            sprite.sortingOrder = 30;
            native = ringSprite != null ? (Vector2)ringSprite.bounds.size : Vector2.one;
            flatness = floorFlatness;
            go.SetActive(false);
        }

        public void Show(Vector2 center, float groundRadius, Color telegraphColor)
        {
            radius = groundRadius;
            color = telegraphColor;
            showing = true;
            flashLeft = 0f;
            sprite.transform.position = center;
            sprite.gameObject.SetActive(true);
            SetProgress(0f);
        }

        /// <summary>0 at takeoff, 1 at landing: the ring grows to its full size and brightens.</summary>
        public void SetProgress(float progress01)
        {
            if (!showing)
                return;
            Scale(radius * 2f * Mathf.Lerp(0.35f, 1f, progress01));
            Color c = color;
            c.a = Mathf.Lerp(0.25f, 0.8f, progress01) * (0.8f + 0.2f * Mathf.Sin(Time.time * 14f));
            sprite.color = c;
        }

        /// <summary>The impact: full ring at full opacity, fading out over a moment.</summary>
        public void Flash()
        {
            showing = false;
            flashLeft = FlashSeconds;
            Scale(radius * 2f);
            Color c = color;
            c.a = 1f;
            sprite.color = c;
        }

        public void Tick(float dt)
        {
            if (flashLeft <= 0f)
                return;
            flashLeft -= dt;
            Color c = color;
            c.a = Mathf.Clamp01(flashLeft / FlashSeconds);
            sprite.color = c;
            if (flashLeft <= 0f)
                sprite.gameObject.SetActive(false);
        }

        public void Hide()
        {
            showing = false;
            flashLeft = 0f;
            if (sprite != null)
                sprite.gameObject.SetActive(false);
        }

        public void Destroy()
        {
            if (sprite != null && Application.isPlaying)
                Object.Destroy(sprite.gameObject);
        }

        // Floor ellipse: full width, squashed by the floor ratio.
        private void Scale(float diameter) =>
            sprite.transform.localScale = new Vector3(diameter / native.x, diameter * flatness / native.y, 1f);
    }
}
