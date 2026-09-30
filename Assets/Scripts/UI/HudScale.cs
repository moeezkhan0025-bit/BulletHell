using BulletHell.Core;
using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>
    /// Scales a piece of the combat HUD by Settings > Gameplay > HUD Scale. Put on a corner- or top-anchored HUD element (its pivot is at
    /// the anchor, so it grows away from the screen edge and stays inside the safe area). Follows the setting instantly.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class HudScale : MonoBehaviour
    {
        private void OnEnable()
        {
            GameServices.Ensure().Settings.Changed += Apply;
            Apply();
        }

        private void OnDisable()
        {
            if (GameServices.HasInstance)
                GameServices.Ensure().Settings.Changed -= Apply;
        }

        private void Apply() => transform.localScale = Vector3.one * GameServices.Ensure().Settings.Current.hudScale;
    }
}
