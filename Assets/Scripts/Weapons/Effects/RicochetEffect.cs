using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>
    /// After a hit the projectile redirects to the nearest other target in range; with none in range it reflects off
    /// the surface it hit. Bounce counts stack additively, the longest range wins.
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_Ricochet", menuName = "BulletHell/Effects/Ricochet")]
    public sealed class RicochetEffect : ArmEffect
    {
        [SerializeField, Min(1)] private int bounces = 2;
        [Tooltip("How far a ricochet looks for the next target (world units).")]
        [SerializeField, Min(0.5f)] private float range = 6f;

        public int Bounces => bounces;
        public float Range => range;

        public override void ModifyShot(ref ShotProperties shot)
        {
            shot.Bounces += bounces;
            shot.BounceRange = Mathf.Max(shot.BounceRange, range);
        }

#if UNITY_EDITOR
        public void Set(int bounceCount, float bounceRange)
        {
            bounces = bounceCount;
            range = bounceRange;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
