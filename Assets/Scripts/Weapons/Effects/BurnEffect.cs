using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>Sets targets on fire: damage over time. Hitting a burning target refreshes the duration (no stacking).</summary>
    [CreateAssetMenu(fileName = "Effect_Burn", menuName = "BulletHell/Effects/Burn")]
    public sealed class BurnEffect : ArmEffect
    {
        [SerializeField, Min(0.01f)] private float damagePerSecond = 2f;
        [SerializeField, Min(0.1f)] private float duration = 3f;
        [Tooltip("Seconds between burn damage ticks.")]
        [SerializeField, Min(0.05f)] private float tickInterval = 0.5f;

        public float DamagePerSecond => damagePerSecond;
        public float Duration => duration;
        public float TickInterval => tickInterval;

        public override void OnHit(Collider2D target, float damage) => Apply(target);

        public override void OnBeamHit(Collider2D target, float dt) => Apply(target);

        private void Apply(Collider2D target)
        {
            if (target.TryGetComponent(out StatusEffects status))
                status.ApplyBurn(damagePerSecond, duration, tickInterval);
        }

#if UNITY_EDITOR
        public void Set(float dps, float burnDuration, float tick)
        {
            damagePerSecond = dps;
            duration = burnDuration;
            tickInterval = tick;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
