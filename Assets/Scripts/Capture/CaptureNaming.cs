using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace BulletHell.Capture
{
    /// <summary>
    /// R1 portfolio capture kit - file names and folders for gameplay captures.
    ///
    /// Videos and screenshots are named <c>&lt;scenario&gt;_&lt;date&gt;_&lt;time&gt;.&lt;ext&gt;</c>, for example
    /// <c>hero-loop_2026-10-01_143005.mp4</c>, and go to <c>Captures/Gameplay/</c> in the project folder (git-ignored).
    /// Pure functions, so the tests can check the format without a clock or a disk.
    /// </summary>
    public static class CaptureNaming
    {
        /// <summary>Scenario prefix used when no capture scenario is running (a plain F11 / F12 during normal play).</summary>
        public const string DefaultPrefix = "gameplay";

        /// <summary>Project-relative folder of videos and gameplay screenshots.</summary>
        public const string GameplayFolder = "Captures/Gameplay";

        /// <summary>Project-relative folder of placeholder-annotated screenshots.</summary>
        public const string StateFolder = "Captures/State";

        public const string DateTimeFormat = "yyyy-MM-dd_HHmmss";

        /// <summary>"hero-loop_2026-10-01_143005.mp4". Prefix is lower-cased and made file-safe; extension has no dot.</summary>
        public static string FileName(string prefix, DateTime when, string extension)
        {
            string ext = (extension ?? "").TrimStart('.');
            string stamp = when.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
            return Sanitize(prefix) + "_" + stamp + (ext.Length > 0 ? "." + ext : "");
        }

        /// <summary>The name without extension (the Recorder adds the extension itself).</summary>
        public static string BaseName(string prefix, DateTime when) => FileName(prefix, when, "");

        /// <summary>Lower-case letters, digits and dashes; everything else becomes a dash. Empty becomes the default prefix.</summary>
        public static string Sanitize(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return DefaultPrefix;
            var sb = new StringBuilder(prefix.Length);
            bool lastDash = false;
            foreach (char c in prefix.Trim().ToLowerInvariant())
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
                if (ok)
                {
                    sb.Append(c);
                    lastDash = false;
                }
                else if (!lastDash && sb.Length > 0)
                {
                    sb.Append('-');
                    lastDash = true;
                }
            }
            while (sb.Length > 0 && sb[sb.Length - 1] == '-')
                sb.Length--;
            return sb.Length == 0 ? DefaultPrefix : sb.ToString();
        }

        /// <summary>Absolute folder for gameplay captures: next to Assets in the Editor, under the persistent data path in player builds.</summary>
        public static string OutputFolder(string relativeFolder = GameplayFolder)
        {
#if UNITY_EDITOR
            string root = Directory.GetParent(Application.dataPath).FullName;
#else
            string root = Application.persistentDataPath;
#endif
            return Path.Combine(root, relativeFolder).Replace('\\', '/');
        }

        /// <summary>Full path of a new capture file with a fresh time stamp (the folder is created).</summary>
        public static string NewPath(string prefix, string extension, string relativeFolder = GameplayFolder)
        {
            string folder = OutputFolder(relativeFolder);
            Directory.CreateDirectory(folder);
            return folder + "/" + FileName(prefix, DateTime.Now, extension);
        }
    }
}
