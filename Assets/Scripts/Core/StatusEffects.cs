using UnityEngine;

namespace BulletHell.Core
{
    /// <summary>
    /// Timed status effects on anything with Health (burn = damage over time, stun = can't move). Applying an effect
    /// that is already active refreshes its duration and keeps the stronger value, so effects never stack up unbounded.
    /// Idle when nothing is active (the component disables itself).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class StatusEffects : MonoBehaviour
    {
        private Health health;
        private float burnDamagePerSecond;
        private float burnInterval = 0.5f;
        private float burnTimeLeft;
        private float burnTimer;
        private float stunTimeLeft;

        public bool IsBurning => burnTimeLeft > 0f;
        public bool IsStunned => stunTimeLeft > 0f;

        private void Awake()
        {
            health = GetComponent<Health>();
            enabled = false;
        }

        public void ApplyBurn(float damagePerSecond, float duration, float tickInterval)
        {
            if (!health.IsAlive || damagePerSecond <= 0f || duration <= 0f)
                return;
            if (!IsBurning)
                burnTimer = 0f;
            burnDamagePerSecond = Mathf.Max(IsBurning ? burnDamagePerSecond : 0f, damagePerSecond);
            burnInterval = Mathf.Max(0.05f, tickInterval);
            burnTimeLeft = Mathf.Max(burnTimeLeft, duration);
            enabled = true;
        }

        public void ApplyStun(float duration)
        {
            if (!health.IsAlive || duration <= 0f)
                return;
            stunTimeLeft = Mathf.Max(stunTimeLeft, duration);
            enabled = true;
        }

        /// <summary>Removes every active effect (on death and respawn).</summary>
        public void Clear()
        {
            burnTimeLeft = 0f;
            burnTimer = 0f;
            burnDamagePerSecond = 0f;
            stunTimeLeft = 0f;
            enabled = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (burnTimeLeft > 0f)
            {
                burnTimeLeft -= dt;
                burnTimer += dt;
                while (burnTimer >= burnInterval)
                {
                    burnTimer -= burnInterval;
                    health.TakeDamage(burnDamagePerSecond * burnInterval);
                }
                if (burnTimeLeft <= 0f)
                    burnDamagePerSecond = 0f;
            }

            if (stunTimeLeft > 0f)
                stunTimeLeft -= dt;

            if (!health.IsAlive || (burnTimeLeft <= 0f && stunTimeLeft <= 0f))
                Clear();
        }
    }
}
