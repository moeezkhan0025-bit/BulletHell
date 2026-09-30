using System.Collections.Generic;
using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>
    /// Bullets steer towards the nearest enemy inside a forward cone. Stacks raise the turn rate and widen the cone. A bullet
    /// picks a new target after every bounce and every pierce.
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_Homing", menuName = "BulletHell/Effects/Homing")]
    public sealed class HomingEffect : ArmEffect
    {
        [Tooltip("Degrees per second a bullet can turn, with a single stack.")]
        [SerializeField, Min(1f)] private float turnRate = 120f;
        [Tooltip("Extra turn rate for every stack after the first.")]
        [SerializeField, Min(0f)] private float turnRatePerStack = 60f;
        [Tooltip("Total width of the forward cone (degrees) with a single stack.")]
        [SerializeField, Range(5f, 360f)] private float cone = 60f;
        [Tooltip("Extra cone width for every stack after the first.")]
        [SerializeField, Min(0f)] private float conePerStack = 30f;
        [Tooltip("How far ahead a target can be (world units).")]
        [SerializeField, Min(0.5f)] private float range = 9f;

        public float TurnRateFor(int stacks) => turnRate + Mathf.Max(0, stacks - 1) * turnRatePerStack;
        public float ConeFor(int stacks) => Mathf.Min(360f, cone + Mathf.Max(0, stacks - 1) * conePerStack);

        public override void ModifyShot(ref ShotProperties shot, int stacks)
        {
            shot.HomingTurnRate = Mathf.Max(shot.HomingTurnRate, TurnRateFor(stacks));
            shot.HomingCone = Mathf.Max(shot.HomingCone, ConeFor(stacks));
            shot.HomingRange = Mathf.Max(shot.HomingRange, range);
        }

        public override void Describe(int stacks, List<string> lines) =>
            lines.Add("Bullets home in on enemies in a " + ConeFor(stacks).ToString("0") + " degree cone (turn rate " +
                      TurnRateFor(stacks).ToString("0") + " deg/s), re-targeting after each bounce or pierce");

#if UNITY_EDITOR
        public void Set(float baseTurnRate, float turnPerStack, float baseCone, float conePerExtraStack, float targetRange)
        {
            turnRate = baseTurnRate;
            turnRatePerStack = turnPerStack;
            cone = baseCone;
            conePerStack = conePerExtraStack;
            range = targetRange;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
