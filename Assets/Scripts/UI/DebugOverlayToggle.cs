using BulletHell.Core;
using BulletHell.Settings;
using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>
    /// Shows or hides the debug objects (the overlay) from the "Show debug overlay" setting. Release builds never show
    /// them, whatever the setting says. Put it on an object that stays active.
    /// </summary>
    public sealed class DebugOverlayToggle : MonoBehaviour
    {
        [SerializeField] private GameObject[] targets = new GameObject[0];

        private SettingsService settings;

        private void Awake() => settings = GameServices.Ensure().Settings;

        private void OnEnable()
        {
            settings.Changed += Apply;
            Apply();
        }

        private void OnDisable() => settings.Changed -= Apply;

        private void Apply()
        {
            bool visible = Debug.isDebugBuild && settings.Current.showDebugOverlay;
            foreach (GameObject target in targets)
                if (target != null)
                    target.SetActive(visible);
        }
    }
}
