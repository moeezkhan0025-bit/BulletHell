using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Feedback
{
    /// <summary>Something that wants to know which way a hit was travelling (set just before the damage is applied).</summary>
    public interface IHitReceiver
    {
        void OnHitFrom(Vector2 travelDirection);
    }

    /// <summary>
    /// Reaction to taking damage, for the player and every enemy: white flash, and for direct bullet hits also a scale
    /// punch, a visual-only knockback nudge, sparks, a short hitstop and a camera shake (values in HitFeedbackTuning).
    /// Damage over time, beams and traps only flash, at a limited rate, so a beam doesn't strobe or hitstop-lock the game.
    /// Sits next to the Health it watches.
    /// </summary>
    public sealed class HitFeedback : MonoBehaviour, IHitReceiver
    {
        [SerializeField] private Health health;
        [SerializeField] private ProceduralMotion motion;
        [SerializeField] private SpriteFx fx;
        [Tooltip("Where sparks appear (the body centre). Empty = this transform.")]
        [SerializeField] private Transform sparkAnchor;
        [Tooltip("Player hits also shake the camera; enemy hits shake only if the tuning says so and this is on.")]
        [SerializeField] private bool shakesCamera;

        private HitFeedbackTuning tuning;
        private float flashSeconds;
        private float flashLeft;
        private float lastFlashStart = -10f;
        private Vector2 direction;
        private bool direct;
        private FeedbackTuning global;

        /// <summary>Sets the tuning. `flashSecondsOverride` > 0 replaces the tuning's flash time (EnemyData.HitFlashDuration).</summary>
        public void Configure(HitFeedbackTuning hitTuning, float flashSecondsOverride = 0f)
        {
            tuning = hitTuning;
            flashSeconds = flashSecondsOverride > 0f ? flashSecondsOverride : (tuning != null ? tuning.FlashSeconds : 0f);
            Clear();
        }

        public void Clear()
        {
            flashLeft = 0f;
            lastFlashStart = -10f;
            direct = false;
            if (fx != null)
                fx.SetFlash(0f);
            enabled = false;
        }

        public void OnHitFrom(Vector2 travelDirection)
        {
            direction = travelDirection;
            direct = true;
        }

        private void Awake()
        {
            health.Damaged += OnDamaged;
            enabled = false;
        }

        private void OnDestroy()
        {
            if (health != null)
                health.Damaged -= OnDamaged;
        }

        private void OnDisable()
        {
            flashLeft = 0f;
            if (fx != null)
                fx.SetFlash(0f);
        }

        private void OnDamaged(float applied)
        {
            bool wasDirect = direct;
            direct = false;
            if (tuning == null)
                return;

            float now = Time.time;
            if (flashSeconds > 0f && now - lastFlashStart >= flashSeconds * 1.5f)
            {
                lastFlashStart = now;
                flashLeft = flashSeconds;
                fx.SetFlash(1f);
                enabled = true;
            }

            if (!wasDirect)
                return;

            if (global == null)
                global = GameServices.Ensure().Config.Feedback;
            motion.Punch(tuning.ScalePunch);
            motion.Knock(direction, tuning.KnockbackDistance);
            if (tuning.Sparks > 0)
                FeedbackHub.Play(VfxKind.Spark, (sparkAnchor != null ? sparkAnchor : transform).position, tuning.Sparks);
            GameClock.Hitstop(tuning.HitstopSeconds, global.HitstopScale);
            if (shakesCamera)
                CameraShake.Add(tuning.Shake);
        }

        private void Update()
        {
            flashLeft -= Time.deltaTime;
            if (flashLeft <= 0f)
            {
                fx.SetFlash(0f);
                enabled = false;
                return;
            }
            fx.SetFlash(Mathf.Clamp01(flashLeft / flashSeconds));
        }
    }
}
