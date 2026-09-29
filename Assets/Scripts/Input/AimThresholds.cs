using UnityEngine;

namespace BulletHell.Input
{
    /// <summary>
    /// The InputTuning thresholds scaled by the player's aim sensitivity setting, within safe limits. The asset is never
    /// changed; at sensitivity 1 the tuning values are returned exactly. Higher sensitivity = less stick travel needed.
    /// </summary>
    public static class AimThresholds
    {
        private const float MinSelect = 0.5f, MaxSelect = 0.95f;
        private const float MinDeselect = 0.3f, MaxDeselect = 0.9f;
        private const float MinDeadzone = 0.15f, MaxDeadzone = 0.8f;

        public static float Select(InputTuning tuning, float sensitivity) =>
            IsNeutral(sensitivity) ? tuning.SelectThreshold : Mathf.Clamp(tuning.SelectThreshold / sensitivity, MinSelect, MaxSelect);

        /// <summary>Never above the scaled select threshold, so the hysteresis band can't invert.</summary>
        public static float Deselect(InputTuning tuning, float sensitivity)
        {
            if (IsNeutral(sensitivity))
                return tuning.DeselectThreshold;
            float scaled = Mathf.Clamp(tuning.DeselectThreshold / sensitivity, MinDeselect, MaxDeselect);
            return Mathf.Min(scaled, Select(tuning, sensitivity));
        }

        public static float LockedDeadzone(InputTuning tuning, float sensitivity) =>
            IsNeutral(sensitivity) ? tuning.LockedAimDeadzone : Mathf.Clamp(tuning.LockedAimDeadzone / sensitivity, MinDeadzone, MaxDeadzone);

        private static bool IsNeutral(float sensitivity) => sensitivity <= 0f || Mathf.Approximately(sensitivity, 1f);
    }
}
