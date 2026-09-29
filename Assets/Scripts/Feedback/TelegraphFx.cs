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
        }

        public void ClearWindup()
        {
            if (!winding)
                return;
            winding = false;
            motion.SetWindup(0f);
            fx.SetDanger(0f);
        }

        /// <summary>A steady colour wash (e.g. overheated). Amount 0 clears it.</summary>
        public void SetStatusTint(Color color, float amount) => fx.SetTint(color, amount);
    }
}
