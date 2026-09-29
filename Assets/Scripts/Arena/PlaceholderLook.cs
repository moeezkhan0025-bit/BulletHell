using UnityEngine;

namespace BulletHell.Arena
{
    /// <summary>
    /// Stand-in styling for placeholder sprites (flat circles and squares) so they sit in the painted backdrop:
    /// a soft contact shadow on the floor and a dark outline behind the sprite. Both are child renderers, created once
    /// and reused, so final art replaces them by removing the call. Numbers come from PerspectiveTuning.
    /// </summary>
    public static class PlaceholderLook
    {
        private const string ShadowName = "ContactShadow";
        private const string OutlineName = "Outline";

        /// <summary>Adds (or updates) a flat ellipse shadow child under a renderer. Sizes are in the parent's local units (the owner's transform unless a parent is given).</summary>
        public static SpriteRenderer ContactShadow(SpriteRenderer owner, Sprite ellipse, Vector2 localCenter, Vector2 localSize, PerspectiveTuning tuning, int order = -2, Transform parent = null)
        {
            if (ellipse == null)
                return null;
            SpriteRenderer shadow = Child(parent != null ? parent : owner.transform, ShadowName);
            shadow.sprite = ellipse;
            shadow.sortingLayerID = owner.sortingLayerID;
            shadow.sortingOrder = owner.sortingOrder + order;
            Vector2 native = ellipse.bounds.size;
            shadow.transform.localPosition = localCenter;
            shadow.transform.localScale = new Vector3(localSize.x / native.x, localSize.y / native.y, 1f);
            shadow.color = tuning.ShadowColor;
            return shadow;
        }

        /// <summary>Adds (or updates) a dark, slightly larger copy of the sprite behind it: the outline.</summary>
        public static void Outline(SpriteRenderer owner, PerspectiveTuning tuning)
        {
            SpriteRenderer outline = Child(owner.transform, OutlineName);
            outline.sprite = owner.sprite;
            outline.sortingLayerID = owner.sortingLayerID;
            outline.sortingOrder = owner.sortingOrder - 1;
            outline.transform.localPosition = Vector3.zero;
            outline.transform.localScale = Vector3.one * tuning.PlaceholderOutlineScale;
            Color c = tuning.PlaceholderOutline;
            c.a *= owner.color.a;
            outline.color = c;
        }

        private static SpriteRenderer Child(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t == null)
            {
                var go = new GameObject(name);
                t = go.transform;
                t.SetParent(parent, false);
                go.AddComponent<SpriteRenderer>();
            }
            return t.GetComponent<SpriteRenderer>();
        }
    }
}
