using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>Projectiles pass through extra enemies before stopping. Stacks additively.</summary>
    [CreateAssetMenu(fileName = "Effect_Pierce", menuName = "BulletHell/Effects/Pierce")]
    public sealed class PierceEffect : ArmEffect
    {
        [Tooltip("Extra enemies a projectile passes through.")]
        [SerializeField, Min(1)] private int pierceCount = 2;

        public int PierceCount => pierceCount;

        public override void ModifyShot(ref ShotProperties shot) => shot.Pierce += pierceCount;

#if UNITY_EDITOR
        public void Set(int count)
        {
            pierceCount = count;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
