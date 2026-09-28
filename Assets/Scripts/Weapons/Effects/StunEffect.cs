using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>A hit has a chance to stun the target (it stops moving) for a short time. Ignores beams.</summary>
    [CreateAssetMenu(fileName = "Effect_Stun", menuName = "BulletHell/Effects/Stun")]
    public sealed class StunEffect : ArmEffect
    {
        [SerializeField, Range(0f, 1f)] private float chance = 0.3f;
        [SerializeField, Min(0.05f)] private float duration = 0.8f;

        public float Chance => chance;
        public float Duration => duration;

        public override void OnHit(Collider2D target, float damage)
        {
            if (Random.value <= chance && target.TryGetComponent(out StatusEffects status))
                status.ApplyStun(duration);
        }

#if UNITY_EDITOR
        public void Set(float stunChance, float stunDuration)
        {
            chance = stunChance;
            duration = stunDuration;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
