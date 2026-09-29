using UnityEngine;

namespace BulletHell.Arena
{
    /// <summary>Which damage stage a breakable obstacle shows for its remaining health. Plain math.</summary>
    public static class ObstacleHealthStages
    {
        /// <summary>
        /// 0 = undamaged ... stageCount-1 = nearly broken. With 3 stages: full health is 0, below 2/3 is 1, below 1/3 is 2.
        /// A broken obstacle (no health) is not a stage; the owner handles that.
        /// </summary>
        public static int Stage(float healthFraction, int stageCount)
        {
            if (stageCount <= 1)
                return 0;
            float lost = 1f - Mathf.Clamp01(healthFraction);
            return Mathf.Clamp(Mathf.FloorToInt(lost * stageCount + 0.0001f), 0, stageCount - 1);
        }
    }
}
