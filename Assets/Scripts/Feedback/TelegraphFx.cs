using UnityEngine;

namespace BulletHell.Feedback
{
    /// <summary>
    /// Makes enemy attacks readable: while an attack winds up the enemy inflates, trembles and pulses the reserved DANGER
    /// colour (shader), growing stronger as the attack gets closer. The AI behaviours only report progress 0..1.
    /// Also holds the flat tint used for states like overheating.
    /// </summary>
    public sealed class TelegraphFx : MonoBehaviour
    {
        [SerializeField] private ProceduralMotion motion;
        [SerializeField] private SpriteFx fx;

        private MotionTuning tuning;
        private bool winding;
        private SpriteRenderer dangerGlow;
        private bool glowBuilt;

        public void Configure(MotionTuning motionTuning) => tuning = motionTuning;

        /// <summary>Wind-up progress: 0 = just started, 1 = about to strike.</summary>
        public void SetWindup(float progress01)
        {
            progress01 = Mathf.Clamp01(progress01);
            winding = true;
            float speed = tuning != null ? tuning.PulseSpeed : 24f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * speed);
            motion.SetWindup(progress01);
            fx.SetDanger(Mathf.Lerp(0.35f, 1f, pulse) * Mathf.Lerp(0.5f, 1f, progress01));
            ShowGlow(progress01, pulse);
        }

        public void ClearWindup()
        {
            if (!winding)
                return;
            winding = false;
            motion.SetWindup(0f);
            fx.SetDanger(0f);
            if (dangerGlow != null)
                dangerGlow.gameObject.SetActive(false);
        }

        // A DANGER-coloured glow on the floor under the enemy while it winds up (a procedural placeholder; grows with the wind-up).
        private void ShowGlow(float progress01, float pulse)
        {
            if (!glowBuilt)
            {
                glowBuilt = true;
                FeedbackTuning feedback = BulletHell.Core.GameServices.Ensure().Config.Feedback;
                if (feedback != null && feedback.GlowSprite != null)
                {
                    var go = new GameObject("DangerGlow");
                    go.transform.SetParent(transform, false);
                    dangerGlow = go.AddComponent<SpriteRenderer>();
                    dangerGlow.sprite = feedback.GlowSprite;
                    dangerGlow.sortingLayerID = BulletHell.Core.SortingLayers.Id(BulletHell.Core.SortingLayers.Ground);
                    dangerGlow.sortingOrder = 28;
                }
            }
            if (dangerGlow == null)
                return;
            FeedbackTuning tuningAsset = BulletHell.Core.GameServices.Ensure().Config.Feedback;
            float radius = TryGetComponent(out BulletHell.Enemies.Enemy enemy) ? enemy.FootprintRadius : 0.4f;
            float diameter = Mathf.Max(0.8f, radius * 2f) * Mathf.Lerp(1.6f, 2.6f, progress01);
            float flat = BulletHell.Core.GameServices.Ensure().Config.Perspective.ShadowFlatness;
            Vector2 native = dangerGlow.sprite.bounds.size;
            dangerGlow.transform.localScale = new Vector3(diameter / native.x, diameter * flat / native.y, 1f);
            Color c = tuningAsset.DangerColor;
            c.a = Mathf.Lerp(0.25f, 0.75f, progress01) * Mathf.Lerp(0.7f, 1f, pulse);
            dangerGlow.color = c;
            dangerGlow.gameObject.SetActive(true);
        }

        /// <summary>A steady colour wash (e.g. overheated). Amount 0 clears it.</summary>
        public void SetStatusTint(Color color, float amount) => fx.SetTint(color, amount);
    }
}
