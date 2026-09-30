using System;
using System.Collections.Generic;
using BulletHell.Core;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace BulletHell.Cosmetics
{
    /// <summary>
    /// The gladiator paper doll: one SpriteRenderer + SpriteResolver per slot (Body, Armor, Head, Accessory 1, Accessory 2),
    /// all drawn on the same character template so they line up with no per-part offsets. A SpriteLibrary above them holds
    /// the art; Apply picks each slot's chosen variant from the profile. Accessory 1 hangs on the head anchor and
    /// Accessory 2 on the back anchor; the anchors are children of the Motion transform, so the procedural animation
    /// (breathing, hop, tilt, squash) moves the whole doll and its accessories together. The same component is on the
    /// player (applies the profile when it wakes) and on the customization preview (the screen calls Apply).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class GladiatorCosmetics : MonoBehaviour
    {
        [Serializable]
        public struct Layer
        {
            public CosmeticSlot slot;
            public SpriteRenderer renderer;
            public SpriteResolver resolver;
        }

        [SerializeField] private Layer[] layers = new Layer[0];
        [Tooltip("Head-area anchor: Accessory 1 (hats, horns, bows, crests) hangs here.")]
        [SerializeField] private Transform headAnchor;
        [Tooltip("Back/torso anchor: Accessory 2 (capes, banners, backpacks) hangs here.")]
        [SerializeField] private Transform backAnchor;
        [SerializeField] private bool applyProfileOnAwake = true;

        private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();

        public Transform HeadAnchor => headAnchor;
        public Transform BackAnchor => backAnchor;

        /// <summary>Every layer's renderer, back to front. Feedback (flash, dissolve) and the blink go through all of them.</summary>
        public IReadOnlyList<SpriteRenderer> Renderers
        {
            get
            {
                if (renderers.Count != layers.Length)
                {
                    renderers.Clear();
                    foreach (Layer layer in layers)
                        if (layer.renderer != null)
                            renderers.Add(layer.renderer);
                }
                return renderers;
            }
        }

        private void Awake()
        {
            if (applyProfileOnAwake)
            {
                GameServices services = GameServices.Ensure();
                if (services.Config.ShowCustomizationInGame)
                    Apply(services.Profile);
                else
                    ApplyOriginalOnly(services.Profile);
            }
        }

        /// <summary>
        /// Gameplay look while the customizer parts are off (GameConfig.ShowCustomizationInGame): only the default Body (the
        /// original player sprite) is drawn; every other layer is hidden. The profile still holds the chosen look.
        /// </summary>
        public void ApplyOriginalOnly(ProfileService profile)
        {
            foreach (Layer layer in layers)
            {
                if (layer.renderer == null || layer.resolver == null)
                    continue;
                IReadOnlyList<CosmeticPartData> options = profile.Options(layer.slot);
                bool shown = layer.slot == CosmeticSlot.Body && options.Count > 0
                    && layer.resolver.SetCategoryAndLabel(CosmeticSlots.Category(layer.slot), options[0].Label);
                layer.renderer.enabled = shown && layer.renderer.sprite != null;
            }
        }

        public void Apply(ProfileService profile)
        {
            foreach (Layer layer in layers)
            {
                if (layer.renderer == null || layer.resolver == null)
                    continue;
                CosmeticPartData part = profile.Get(layer.slot);
                bool shown = part != null && layer.resolver.SetCategoryAndLabel(CosmeticSlots.Category(layer.slot), part.Label);
                layer.renderer.enabled = shown && layer.renderer.sprite != null;
            }
        }

        /// <summary>Sets the alpha of every layer (the invulnerability blink). Colors are otherwise left alone.</summary>
        public void SetAlpha(float alpha)
        {
            foreach (SpriteRenderer r in Renderers)
            {
                Color color = r.color;
                color.a = alpha;
                r.color = color;
            }
        }
    }
}
