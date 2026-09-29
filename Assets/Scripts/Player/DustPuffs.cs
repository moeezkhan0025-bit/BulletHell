using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>
    /// A fixed set of dust puff sprites, created once and reused (no allocations while playing). Each puff drifts out
    /// from the landing spot, grows and fades. Lives outside the player so puffs stay where the player landed.
    /// </summary>
    public sealed class DustPuffs
    {
        private const int Capacity = 24;

        private readonly Transform root;
        private readonly SpriteRenderer[] renderers = new SpriteRenderer[Capacity];
        private readonly Vector2[] origin = new Vector2[Capacity];
        private readonly Vector2[] drift = new Vector2[Capacity];
        private readonly float[] age = new float[Capacity];
        private readonly bool[] live = new bool[Capacity];
        private readonly JumpTuning tuning;
        private int next;

        public DustPuffs(JumpTuning jumpTuning)
        {
            tuning = jumpTuning;
            root = new GameObject("DustPuffs").transform;
            for (int i = 0; i < Capacity; i++)
            {
                var go = new GameObject("Puff");
                go.transform.SetParent(root, false);
                renderers[i] = go.AddComponent<SpriteRenderer>();
                renderers[i].sortingLayerID = SortingLayers.Id(SortingLayers.Ground);
                renderers[i].sortingOrder = 20;
                go.SetActive(false);
            }
        }

        public void Destroy()
        {
            if (root != null)
                Object.Destroy(root.gameObject);
        }

        /// <summary>Puffs out from a spot on the ground.</summary>
        public void Spawn(Vector2 position)
        {
            int count = Mathf.Min(tuning.DustCount, Capacity);
            for (int i = 0; i < count; i++)
            {
                int slot = next;
                next = (next + 1) % Capacity;

                float angle = (i + Random.value * 0.6f) * (2f * Mathf.PI / Mathf.Max(1, count));
                origin[slot] = position;
                // The floor is seen at an angle, so the puffs travel more sideways than up the screen.
                drift[slot] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.5f) * tuning.DustSpread;
                age[slot] = 0f;
                live[slot] = true;
                renderers[slot].sprite = tuning.DustSprite;
                renderers[slot].gameObject.SetActive(true);
                Apply(slot);
            }
        }

        public void Update(float deltaTime)
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (!live[i])
                    continue;
                age[i] += deltaTime;
                if (age[i] >= tuning.DustSeconds)
                {
                    live[i] = false;
                    renderers[i].gameObject.SetActive(false);
                    continue;
                }
                Apply(i);
            }
        }

        private void Apply(int i)
        {
            float t = Mathf.Clamp01(age[i] / tuning.DustSeconds);
            float eased = 1f - (1f - t) * (1f - t);
            Transform tf = renderers[i].transform;
            tf.position = origin[i] + drift[i] * eased + new Vector2(0f, 0.08f * eased);
            float size = Mathf.Lerp(tuning.DustStartSize, tuning.DustEndSize, eased);
            Vector2 native = renderers[i].sprite != null ? (Vector2)renderers[i].sprite.bounds.size : Vector2.one;
            tf.localScale = new Vector3(size / native.x, size / native.y, 1f);
            Color color = tuning.DustColor;
            color.a *= 1f - t;
            renderers[i].color = color;
        }
    }
}
