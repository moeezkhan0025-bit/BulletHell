using BulletHell.Arena;
using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>
    /// The player's layered structure. The root sits on the ground at the FEET and is what moves, collides (footprint),
    /// sorts and stands on traps. Everything you see hangs under Visuals (body, headgear, cape, the arms ring and the
    /// small damage Core), placed at body-centre height above the feet; the ground Shadow stays on the root. The jump
    /// (M7.6) will lift Visuals only, leaving the root, its footprint and the shadow on the ground.
    /// </summary>
    public sealed class PlayerVisualRig : MonoBehaviour
    {
        [SerializeField] private PlayerData data;
        [Tooltip("Parent of everything that is drawn above the ground: body, cosmetics, arms, core.")]
        [SerializeField] private Transform visuals;
        [Tooltip("The small damage hitbox's centre: a child of the root, low near the feet on the ground plane.")]
        [SerializeField] private Transform core;
        [Tooltip("Visible marker of the damage core, sized to the hitbox. Optional.")]
        [SerializeField] private SpriteRenderer coreMarker;
        [SerializeField] private SpriteRenderer shadow;

        /// <summary>Everything drawn above the ground.</summary>
        public Transform Visuals => visuals;
        /// <summary>Centre of the damage hitbox: enemy bullets aim at and hit this.</summary>
        public Transform Core => core;

        private void Awake()
        {
            visuals.localPosition = new Vector3(0f, data.BodyCenterHeight, 0f);
            core.localPosition = new Vector3(0f, data.CoreFootOffset, 0f);
            if (coreMarker != null)
            {
                Vector2 native = coreMarker.sprite != null ? (Vector2)coreMarker.sprite.bounds.size : Vector2.one;
                float diameter = data.HitboxRadius * 2f;
                coreMarker.transform.localScale = new Vector3(diameter / native.x, diameter / native.y, 1f);
            }

            if (shadow != null)
            {
                PerspectiveTuning tuning = GameServices.Ensure().Config.Perspective;
                float width = data.BodyRadius * 2f * tuning.ShadowWidth;
                Vector2 native = shadow.sprite != null ? (Vector2)shadow.sprite.bounds.size : Vector2.one;
                shadow.transform.localScale = new Vector3(width / native.x, width * tuning.ShadowFlatness / native.y, 1f);
                shadow.color = tuning.ShadowColor;
            }
        }
    }
}
