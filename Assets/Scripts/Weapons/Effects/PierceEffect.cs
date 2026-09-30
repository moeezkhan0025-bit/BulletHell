using System.Collections.Generic;
using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>Projectiles pass through extra enemies before stopping. Each stack adds the same amount.</summary>
    [CreateAssetMenu(fileName = "Effect_Pierce", menuName = "BulletHell/Effects/Pierce")]
    public sealed class PierceEffect : ArmEffect
    {
        [Tooltip("Extra enemies a projectile passes through, per stack.")]
        [SerializeField, Min(1)] private int pierceCount = 1;

        public int PierceCount => pierceCount;

        public override void ModifyShot(ref ShotProperties shot, int stacks) => shot.Pierce += pierceCount * stacks;

        public override void Describe(int stacks, List<string> lines)
        {
            int total = pierceCount * stacks;
            lines.Add("Bullets pass through " + total + (total == 1 ? " extra enemy" : " extra enemies"));
        }

#if UNITY_EDITOR
        public void Set(int count)
        {
            pierceCount = count;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
