using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>Flashes a sprite white for a moment whenever the Health takes damage.</summary>
    public sealed class HitFlash : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private SpriteRenderer target;

        private Color baseColor = Color.white;
        private float duration;
        private float timeLeft;

        public void Configure(Color color, float flashDuration)
        {
            baseColor = color;
            duration = flashDuration;
            Clear();
        }

        /// <summary>Stops any flash in progress and restores the base colour.</summary>
        public void Clear()
        {
            timeLeft = 0f;
            target.color = baseColor;
            enabled = false;
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

        private void OnDamaged(float _)
        {
            if (duration <= 0f)
                return;
            timeLeft = duration;
            target.color = Color.white;
            enabled = true;
        }

        private void Update()
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f)
                Clear();
        }
    }
}
