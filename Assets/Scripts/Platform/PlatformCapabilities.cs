using UnityEngine;

namespace BulletHell.Platform
{
    /// <summary>The only place that asks "which platform is this". Everything else asks a capability question.</summary>
    public static class PlatformCapabilities
    {
        /// <summary>False where a Quit button is against platform rules (iOS, consoles).</summary>
        public static bool CanQuitApplication
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.Switch:
                    case RuntimePlatform.GameCoreXboxOne:
                    case RuntimePlatform.GameCoreXboxSeries:
                    case RuntimePlatform.PS4:
                    case RuntimePlatform.PS5:
                        return false;
                    default:
                        return true;
                }
            }
        }
    }
}
