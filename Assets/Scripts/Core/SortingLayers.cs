using UnityEngine;

namespace BulletHell.Core
{
    /// <summary>
    /// The sorting layers of the 3/4 view, back to front. Characters and obstacles share Default and are ordered by
    /// their feet position (Transparency Sort Mode = Custom Axis (0,1,0)), so lower on screen draws in front.
    /// The layers are created by BulletHell/M7.5/Setup Everything.
    /// </summary>
    public static class SortingLayers
    {
        public const string Background = "Background";
        public const string Ground = "Ground";
        public const string Characters = "Default";
        public const string Bullets = "Bullets";
        public const string Foreground = "Foreground";

        /// <summary>Layer id for a name (an unknown name falls back to Default).</summary>
        public static int Id(string layerName) => SortingLayer.NameToID(layerName);
    }
}
