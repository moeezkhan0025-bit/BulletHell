using System;
using UnityEngine;

namespace BulletHell.Core
{
    /// <summary>Hit points for any damageable thing. Its owner sets the maximum from data via Initialize.</summary>
    public sealed class Health : MonoBehaviour, IDamageable
    {
        public float Max { get; private set; } = 1f;
        public float Current { get; private set; } = 1f;
        public bool IsAlive => Current > 0f;
        public float Fraction => Max > 0f ? Current / Max : 0f;

        /// <summary>Raised with the damage actually applied.</summary>
        public event Action<float> Damaged;
        public event Action Died;
        /// <summary>Raised whenever Current or Max changes (damage, initialize, revive).</summary>
        public event Action Changed;

        public void Initialize(float max)
        {
            Max = Mathf.Max(0.01f, max);
            Current = Max;
            Changed?.Invoke();
        }

        public void Revive()
        {
            Current = Max;
            Changed?.Invoke();
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            float applied = Mathf.Min(amount, Current);
            Current -= applied;
            Damaged?.Invoke(applied);
            Changed?.Invoke();
            if (Current <= 0f)
            {
                Current = 0f;
                Died?.Invoke();
            }
        }
    }
}
