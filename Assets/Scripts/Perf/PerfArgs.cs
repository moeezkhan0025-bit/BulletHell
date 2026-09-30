using System;
using UnityEngine;

namespace BulletHell.Perf
{
    /// <summary>
    /// Command-line switches of the performance tools (development builds and the editor only; release builds see none).
    /// -perfstress  start the stress test on its own (Boot goes straight to a new run at round 3)
    /// -perflabel X  name of the run in the CSV file names (default "run")
    /// -perfseconds N  length of the stress test in seconds (default 90)
    /// -perfenemies N  swarm size (default 80)
    /// -perfvsync 0|1|default  VSync during the test (default 0 = uncapped, so the real CPU/GPU cost shows)
    /// -perfoverlay  show the performance overlay
    /// -perfdiscover  write the list of available profiler stats next to the CSV
    /// -perfnoquit  do not quit the application when the stress test ends
    /// </summary>
    public static class PerfArgs
    {
        private static string[] args;

        private static string[] Args => args ?? (args = Environment.GetCommandLineArgs());

        public static bool Has(string name)
        {
            if (!Debug.isDebugBuild)
                return false;
            foreach (string a in Args)
                if (string.Equals(a, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        public static string Value(string name, string fallback)
        {
            if (!Debug.isDebugBuild)
                return fallback;
            for (int i = 0; i < Args.Length - 1; i++)
                if (string.Equals(Args[i], name, StringComparison.OrdinalIgnoreCase))
                    return Args[i + 1];
            return fallback;
        }

        public static int Int(string name, int fallback) =>
            int.TryParse(Value(name, null), out int value) ? value : fallback;
    }
}
