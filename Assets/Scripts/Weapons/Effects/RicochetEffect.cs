using System.Collections.Generic;
using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>
    /// Bullets bounce off walls and obstacles instead of stopping. Only wall/obstacle bounces count: hitting an enemy
    /// never spends a bounce (it ends the bullet unless Pierce is left). The first stack gives the base count, every
    /// further stack adds a fixed extra.
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_Ricochet", menuName = "BulletHell/Effects/Ricochet")]
    public sealed class RicochetEffect : ArmEffect
    {
        [Tooltip("Wall/obstacle bounces with a single stack.")]
        [SerializeField, Min(1)] private int bounces = 3;
        [Tooltip("Extra bounces for every stack after the first.")]
        [SerializeField, Min(0)] private int extraPerStack = 1;

        public int Bounces => bounces;
        public int ExtraPerStack => extraPerStack;

        public int BouncesFor(int stacks) => bounces + Mathf.Max(0, stacks - 1) * extraPerStack;

        public override void ModifyShot(ref ShotProperties shot, int stacks) => shot.Bounces += BouncesFor(stacks);

        public override void Describe(int stacks, List<string> lines) =>
            lines.Add("Bullets bounce off walls and obstacles up to " + BouncesFor(stacks) + " times");

#if UNITY_EDITOR
        public void Set(int bounceCount, int extra)
        {
            bounces = bounceCount;
            extraPerStack = extra;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
