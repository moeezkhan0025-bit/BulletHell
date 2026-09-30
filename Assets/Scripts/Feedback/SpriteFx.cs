using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Feedback
{
    /// <summary>
    /// The one channel for shader feedback on a character's sprites: white hit flash, DANGER wind-up pulse, a tint
    /// overlay (e.g. overheating) and dissolve. Written through a MaterialPropertyBlock, so no material instances and no
    /// allocations, and it never touches SpriteRenderer.color (which stays the character's own colour and blink alpha).
    /// The renderers need a material from the Sprite_Character shader graph.
    /// </summary>
    public sealed class SpriteFx : MonoBehaviour
    {
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        private static readonly int DangerAmountId = Shader.PropertyToID("_DangerAmount");
        private static readonly int DangerColorId = Shader.PropertyToID("_DangerColor");
        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
        private static readonly int TintAmountId = Shader.PropertyToID("_TintAmount");
        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int GlowAmountId = Shader.PropertyToID("_GlowAmount");

        [SerializeField] private SpriteRenderer[] renderers = new SpriteRenderer[0];

        private MaterialPropertyBlock block;
        private FeedbackTuning tuning;
        private float flash;
        private float danger;
        private float dissolve;
        private Color tint = Color.white;
        private float tintAmount;
        private Color glow = Color.black;
        private float glowAmount;
        private bool dirty = true;

        public SpriteRenderer[] Renderers => renderers;

        public void SetRenderers(SpriteRenderer[] targets)
        {
            renderers = targets;
            dirty = true;
            Apply();
        }

        /// <summary>White hit flash, 0..1.</summary>
        public void SetFlash(float amount) => Set(ref flash, amount);
        /// <summary>DANGER-colour pulse, 0..1 (the caller animates it).</summary>
        public void SetDanger(float amount) => Set(ref danger, amount);
        public void SetDissolve(float amount) => Set(ref dissolve, amount);

        /// <summary>Tints the sprite towards a colour by an amount 0..1 (0 = none).</summary>
        public void SetTint(Color color, float amount)
        {
            if (tint == color && Mathf.Approximately(tintAmount, amount))
                return;
            tint = color;
            tintAmount = amount;
            dirty = true;
        }

        /// <summary>Additive glow (a boss phase, heat) by an amount 0..1 (0 = none). HDR colours glow brighter.</summary>
        public void SetGlow(Color color, float amount)
        {
            amount = Mathf.Clamp01(amount);
            if (glow == color && Mathf.Approximately(glowAmount, amount))
                return;
            glow = color;
            glowAmount = amount;
            dirty = true;
        }

        public void ClearAll()
        {
            flash = danger = dissolve = tintAmount = glowAmount = 0f;
            dirty = true;
            Apply();
        }

        private void Set(ref float field, float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(field, value))
                return;
            field = value;
            dirty = true;
        }

        private void Awake() => block = new MaterialPropertyBlock();

        private void LateUpdate()
        {
            using var _ = BulletHell.Perf.PerfMarkers.SpriteFx.Auto();
            Apply();
        }

        /// <summary>Pushes changed values to the renderers.</summary>
        public void Apply()
        {
            if (!dirty || renderers == null)
                return;
            dirty = false;
            if (block == null)
                block = new MaterialPropertyBlock();
            if (tuning == null)
                tuning = GameServices.Ensure().Config.Feedback;

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer r = renderers[i];
                if (r == null)
                    continue;
                r.GetPropertyBlock(block);
                block.SetFloat(FlashAmountId, flash);
                block.SetColor(FlashColorId, tuning.FlashColor);
                block.SetFloat(DangerAmountId, danger);
                block.SetColor(DangerColorId, tuning.DangerColor);
                block.SetColor(TintColorId, tint);
                block.SetFloat(TintAmountId, tintAmount);
                block.SetFloat(DissolveAmountId, dissolve);
                block.SetColor(GlowColorId, glow);
                block.SetFloat(GlowAmountId, glowAmount);
                r.SetPropertyBlock(block);
            }
        }
    }
}
