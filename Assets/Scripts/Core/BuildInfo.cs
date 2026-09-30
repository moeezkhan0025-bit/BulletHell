using UnityEngine;

namespace BulletHell.Core
{
    /// <summary>
    /// What the running game knows about its own build. The version comes from Player Settings (the build menu writes
    /// "major.minor.build" there and bumps the build number on every build), so nothing needs editing by hand.
    /// </summary>
    public static class BuildInfo
    {
        /// <summary>"major.minor.build" as the build menu writes it to Player Settings.</summary>
        public static string FormatVersion(int major, int minor, int build) => major + "." + minor + "." + build;

        /// <summary>"1.0.12": the Player Settings version.</summary>
        public static string Version => Application.version;

        /// <summary>The short line the Main Menu shows: "v1.0.12", plus " dev" for development builds and " web" in the browser.</summary>
        public static string Display
        {
            get
            {
                string text = "v" + Version;
                if (Debug.isDebugBuild)
                    text += " dev";
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                    text += " web";
                return text;
            }
        }
    }
}
