using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// D6: one menu for Windows and WebGL builds. Every run bumps the build number (version = major.minor.build, kept in
    /// <c>BuildVersion.json</c> at the project root and written to Player Settings, which the Main Menu shows), builds into
    /// <c>Builds/&lt;Platform&gt;/&lt;version&gt;</c>, zips the WebGL build for itch.io, and appends a line to <c>Builds/build_log.csv</c>
    /// (version, platform, result, seconds, size). A failed build does not use up the number. The Editor's active build target is put back afterwards.
    /// </summary>
    public static class BuildMenu
    {
        private const string VersionFile = "BuildVersion.json";
        private const string BuildsRoot = "Builds";
        private const string ExeName = "VoxVegetallis";

        [Serializable]
        private sealed class VersionData
        {
            public int major = 1;
            public int minor = 0;
            public int build;
        }

        public enum Target { Windows, WebGL, WebGLDevelopment }

        // ---------------------------------------------------------------- menu

        [MenuItem("BulletHell/Build/Windows", false, 100)]
        public static void MenuWindows() => Run(Target.Windows);

        [MenuItem("BulletHell/Build/WebGL (release, for itch.io)", false, 101)]
        public static void MenuWebGL() => Run(Target.WebGL);

        [MenuItem("BulletHell/Build/WebGL (development, for stress tests)", false, 102)]
        public static void MenuWebGLDevelopment() => Run(Target.WebGLDevelopment);

        [MenuItem("BulletHell/Build/Windows + WebGL", false, 120)]
        public static void MenuBoth() => Run(Target.Windows, Target.WebGL);

        [MenuItem("BulletHell/Build/Show Next Version", false, 140)]
        public static void ShowNextVersion() => Debug.Log("Next build: " + Format(Next(Load())) + " (current " + Format(Load()) + ")");

        [MenuItem("BulletHell/Build/Open Builds Folder", false, 141)]
        public static void OpenBuildsFolder()
        {
            Directory.CreateDirectory(BuildsRoot);
            EditorUtility.RevealInFinder(Path.GetFullPath(BuildsRoot));
        }

        // ---------------------------------------------------------------- versions

        private static string VersionPath => Path.Combine(ProjectRoot, VersionFile);

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static VersionData Load()
        {
            try
            {
                if (File.Exists(VersionPath))
                    return JsonUtility.FromJson<VersionData>(File.ReadAllText(VersionPath)) ?? new VersionData();
            }
            catch (Exception e)
            {
                Debug.LogWarning("BuildVersion.json could not be read: " + e.Message);
            }
            return new VersionData();
        }

        private static VersionData Next(VersionData current) => new VersionData { major = current.major, minor = current.minor, build = current.build + 1 };

        public static string Format(int major, int minor, int build) => BulletHell.Core.BuildInfo.FormatVersion(major, minor, build);

        private static string Format(VersionData v) => Format(v.major, v.minor, v.build);

        // ---------------------------------------------------------------- building

        /// <summary>Builds the given targets with one new version number. Returns true when every build succeeded.</summary>
        public static bool Run(params Target[] targets)
        {
            VersionData current = Load();
            VersionData next = Next(current);
            string version = Format(next);
            string previousBundle = PlayerSettings.bundleVersion;
            BuildTarget previousTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup previousGroup = BuildPipeline.GetBuildTargetGroup(previousTarget);

            PlayerSettings.bundleVersion = version;
            bool allOk = true;
            try
            {
                foreach (Target target in targets)
                    allOk &= BuildOne(target, version);
            }
            catch (Exception e)
            {
                Debug.LogError("Build failed: " + e);
                allOk = false;
            }
            finally
            {
                if (allOk)
                {
                    File.WriteAllText(VersionPath, JsonUtility.ToJson(next, true));
                    Debug.Log("BUILD OK: version " + version);
                }
                else
                {
                    PlayerSettings.bundleVersion = previousBundle;   // the number is not used up
                    Debug.LogError("BUILD FAILED: version " + version + " was not recorded.");
                }
                if (EditorUserBuildSettings.activeBuildTarget != previousTarget && previousGroup != BuildTargetGroup.Unknown)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(previousGroup, previousTarget);
                AssetDatabase.SaveAssets();
            }
            return allOk;
        }

        private static string[] Scenes()
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                if (scene.enabled)
                    list.Add(scene.path);
            return list.ToArray();
        }

        private static bool BuildOne(Target target, string version)
        {
            bool web = target != Target.Windows;
            string platformName = target == Target.Windows ? "Windows" : target == Target.WebGL ? "WebGL" : "WebGL-dev";
            string folder = Path.Combine(BuildsRoot, web ? "WebGL" : "Windows", version + (target == Target.WebGLDevelopment ? "-dev" : ""));
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);

            var options = new BuildPlayerOptions
            {
                scenes = Scenes(),
                target = web ? BuildTarget.WebGL : BuildTarget.StandaloneWindows64,
                locationPathName = web ? folder : Path.Combine(folder, ExeName + ".exe"),
                options = target == Target.WebGLDevelopment ? BuildOptions.Development : BuildOptions.None,
            };
            if (web)
                ConfigureWebGL(target == Target.WebGLDevelopment);

            var watch = Stopwatch.StartNew();
            BuildReport report = BuildPipeline.BuildPlayer(options);
            watch.Stop();
            BuildSummary summary = report.summary;
            bool ok = summary.result == BuildResult.Succeeded;
            // A WebGL build whose post-build step did not run (module added while the Editor was open) reports Success with an empty folder.
            if (ok && web && !File.Exists(Path.Combine(folder, "index.html")))
            {
                Debug.LogError("WebGL build reported success but wrote no index.html: restart the Unity Editor after installing the WebGL module.");
                ok = false;
            }

            string zip = "";
            if (ok && web)
                zip = Zip(folder, Path.Combine(BuildsRoot, ExeName + "-" + platformName + "-" + version + ".zip"));
            long size = ok ? DirectorySize(folder) : 0;
            Log(version, platformName, ok ? "ok" : summary.result.ToString(), watch.Elapsed.TotalSeconds, size, summary.totalErrors, summary.totalWarnings, folder, zip);
            Debug.Log("BUILD " + platformName + " " + version + ": " + summary.result + " in " + watch.Elapsed.TotalSeconds.ToString("0") + " s, " + (size / 1048576f).ToString("0.0") + " MB -> " + folder);
            return ok;
        }

        // Settings that matter for a browser build: a small download that works on any host, enough memory, no editor-only features.
        private static void ConfigureWebGL(bool development)
        {
            PlayerSettings.WebGL.template = "PROJECT:VoxVegetallis";   // Assets/WebGLTemplates/VoxVegetallis: responsive canvas, persistent saves, loading screen
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;          // works on hosts that do not send Content-Encoding (itch.io does, a plain file server does not)
            PlayerSettings.WebGL.exceptionSupport = development ? WebGLExceptionSupport.FullWithStacktrace : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.initialMemorySize = 256;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.maximumMemorySize = 1024;
            PlayerSettings.runInBackground = true;                       // a browser tab keeps running when it loses focus
            PlayerSettings.defaultWebScreenWidth = 1920;
            PlayerSettings.defaultWebScreenHeight = 1080;
        }

        // ---------------------------------------------------------------- helpers

        private static long DirectorySize(string folder)
        {
            long total = 0;
            foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                total += new FileInfo(file).Length;
            return total;
        }

        private static string Zip(string folder, string zipPath)
        {
            try
            {
                if (File.Exists(zipPath))
                    File.Delete(zipPath);
                System.IO.Compression.ZipFile.CreateFromDirectory(folder, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);
                return zipPath;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Zip failed: " + e.Message);
                return "";
            }
        }

        private static void Log(string version, string platform, string result, double seconds, long size, int errors, int warnings, string folder, string zip)
        {
            Directory.CreateDirectory(BuildsRoot);
            string path = Path.Combine(BuildsRoot, "build_log.csv");
            if (!File.Exists(path))
                File.WriteAllText(path, "time,version,platform,result,seconds,bytes,errors,warnings,folder,zip\n");
            File.AppendAllText(path, string.Join(",", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), version, platform, result,
                seconds.ToString("0"), size, errors, warnings, folder, zip) + "\n");
        }
    }
}
