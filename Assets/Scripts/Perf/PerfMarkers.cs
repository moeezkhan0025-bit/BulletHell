using Unity.Profiling;

namespace BulletHell.Perf
{
    /// <summary>
    /// Profiler markers around the main gameplay systems. They show up in the Profiler and, in the PerfLogger's CSV, as one
    /// column each (the total milliseconds spent in that marker during the frame). In release builds the markers compile away.
    /// Use: <c>using var _ = PerfMarkers.NavUpdate.Auto();</c> at the top of a method.
    /// </summary>
    public static class PerfMarkers
    {
        public static readonly ProfilerMarker NavUpdate = new ProfilerMarker(ProfilerCategory.Scripts, "BH.Nav.Update");
        public static readonly ProfilerMarker NavSeparation = new ProfilerMarker(ProfilerCategory.Scripts, "BH.Nav.Separation");
        public static readonly ProfilerMarker FlowBuild = new ProfilerMarker(ProfilerCategory.Scripts, "BH.Flow.Build");
        public static readonly ProfilerMarker EnemyBrain = new ProfilerMarker(ProfilerCategory.Scripts, "BH.Enemy.Brain");
        public static readonly ProfilerMarker EnemyAttacker = new ProfilerMarker(ProfilerCategory.Scripts, "BH.Enemy.Attacker");
        public static readonly ProfilerMarker EnemyMotion = new ProfilerMarker(ProfilerCategory.Scripts, "BH.Enemy.Motion");
        public static readonly ProfilerMarker SpriteFx = new ProfilerMarker(ProfilerCategory.Scripts, "BH.Enemy.SpriteFx");
        public static readonly ProfilerMarker BulletUpdate = new ProfilerMarker(ProfilerCategory.Scripts, "BH.Bullet.Update");
        public static readonly ProfilerMarker ArmsFire = new ProfilerMarker(ProfilerCategory.Scripts, "BH.Arms.Fire");
        public static readonly ProfilerMarker FxHub = new ProfilerMarker(ProfilerCategory.Scripts, "BH.Fx.Hub");
        public static readonly ProfilerMarker ArenaFader = new ProfilerMarker(ProfilerCategory.Scripts, "BH.Arena.Fader");
        public static readonly ProfilerMarker DebugOverlay = new ProfilerMarker(ProfilerCategory.Scripts, "BH.UI.DebugOverlay");
        public static readonly ProfilerMarker PerfTools = new ProfilerMarker(ProfilerCategory.Scripts, "BH.Perf.Tools");

        /// <summary>Marker names in CSV column order (see PerfLogger).</summary>
        public static readonly string[] Names =
        {
            "BH.Nav.Update", "BH.Nav.Separation", "BH.Flow.Build", "BH.Enemy.Brain", "BH.Enemy.Attacker", "BH.Enemy.Motion",
            "BH.Enemy.SpriteFx", "BH.Bullet.Update", "BH.Arms.Fire", "BH.Fx.Hub", "BH.Arena.Fader", "BH.UI.DebugOverlay", "BH.Perf.Tools",
        };
    }
}
