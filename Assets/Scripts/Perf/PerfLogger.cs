using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;

namespace BulletHell.Perf
{
    /// <summary>
    /// Records one row per frame while running: real frame time, main and render thread CPU time, garbage allocated in the
    /// frame, batches / set-pass / draw calls, the milliseconds spent in each BulletHell profiler marker, and the object
    /// counts. Everything is kept in one preallocated float array and only written to a CSV when it stops, so logging
    /// allocates nothing and does no file I/O while measuring. A row holds the frame that just finished (profiler recorders
    /// report the last completed frame). Development builds and the editor only.
    /// </summary>
    public sealed class PerfLogger : IDisposable
    {
        private const int MaxFrames = 60000;
        private static readonly string[] FixedColumns =
        {
            "frame", "t_s", "dt_ms", "main_ms", "render_ms", "gpu_ms", "wait_present_ms", "gc_bytes", "gc_allocs", "gc_collect_ms",
            "instantiate_ms", "physics2d_ms", "physics_sync_ms", "camera_render_ms", "canvas_batch_ms", "canvas_overlay_ms",
            "wait_gfx_ms", "wait_vsync_ms", "batches", "setpass", "draws",
        };
        private static readonly string[] TailColumns = { "bullets", "enemies", "particles", "timescale", "state" };

        private readonly ProfilerRecorder main;
        private readonly ProfilerRecorder render;
        private readonly ProfilerRecorder gpu;
        private readonly ProfilerRecorder waitPresent;
        private readonly ProfilerRecorder gcAllocs;
        private readonly ProfilerRecorder gcCollect;
        private readonly ProfilerRecorder instantiate;
        private readonly ProfilerRecorder physics2D;
        private readonly ProfilerRecorder physicsSync;
        private readonly ProfilerRecorder cameraRender;
        private readonly ProfilerRecorder canvasBatch;
        private readonly ProfilerRecorder canvasOverlay;
        private readonly ProfilerRecorder waitGfx;
        private readonly ProfilerRecorder waitVSync;
        private readonly ProfilerRecorder gc;
        private readonly ProfilerRecorder batches;
        private readonly ProfilerRecorder setPass;
        private readonly ProfilerRecorder draws;
        private readonly ProfilerRecorder[] markers = new ProfilerRecorder[PerfMarkers.Names.Length];
        private readonly int columns;
        private float[] rows;
        private int frames;
        private float startTime;
        private string label = "run";
        private string notes = "";

        public bool Running { get; private set; }
        public int Frames => frames;
        /// <summary>Path of the last CSV written ("" before the first).</summary>
        public string LastPath { get; private set; } = "";

        public PerfLogger()
        {
            main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            // Stat names checked against this player's own list (PerfLogs/stats_available.txt, -perfdiscover).
            render = ProfilerRecorder.StartNew(ProfilerCategory.Render, "CPU Render Thread Frame Time");
            gpu = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time");
            waitPresent = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Gfx.WaitForPresentOnGfxThread");
            gcAllocs = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocation In Frame Count");
            gcCollect = ProfilerRecorder.StartNew(new ProfilerCategory("GC"), "GC.Collect");
            instantiate = ProfilerRecorder.StartNew(ProfilerCategory.Loading, "Instantiate");
            physics2D = ProfilerRecorder.StartNew(new ProfilerCategory("Physics2D"), "Physics2D.Simulate");
            physicsSync = ProfilerRecorder.StartNew(new ProfilerCategory("Physics2D"), "Physics2D.SyncTransformChanges");
            cameraRender = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Camera.Render");
            canvasBatch = ProfilerRecorder.StartNew(new ProfilerCategory("UI Render"), "Canvas.BuildBatch");
            canvasOverlay = ProfilerRecorder.StartNew(new ProfilerCategory("UI Render"), "Canvas.RenderOverlays");
            waitGfx = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Gfx.WaitForGfxCommandsFromMainThread");
            waitVSync = ProfilerRecorder.StartNew(new ProfilerCategory("VSync"), "WaitForTargetFPS");
            gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            for (int i = 0; i < markers.Length; i++)
                markers[i] = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, PerfMarkers.Names[i]);
            columns = FixedColumns.Length + markers.Length + TailColumns.Length;
        }

        public void Dispose()
        {
            main.Dispose();
            render.Dispose();
            gpu.Dispose();
            waitPresent.Dispose();
            gcAllocs.Dispose();
            gcCollect.Dispose();
            instantiate.Dispose();
            physics2D.Dispose();
            physicsSync.Dispose();
            cameraRender.Dispose();
            canvasBatch.Dispose();
            canvasOverlay.Dispose();
            waitGfx.Dispose();
            waitVSync.Dispose();
            gc.Dispose();
            batches.Dispose();
            setPass.Dispose();
            draws.Dispose();
            foreach (ProfilerRecorder marker in markers)
                marker.Dispose();
        }

        /// <summary>Starts a run. `extraNotes` (stress settings, pacing) go into the meta file.</summary>
        public void Start(string runLabel, string extraNotes)
        {
            label = string.IsNullOrEmpty(runLabel) ? "run" : runLabel;
            notes = extraNotes ?? "";
            if (rows == null)
                rows = new float[MaxFrames * columns];
            frames = 0;
            startTime = Time.unscaledTime;
            Running = true;
        }

        /// <summary>Adds the row for the frame that just finished. Call once per frame, early in Update.</summary>
        public void Sample(float unscaledDelta, PerfStats stats, int timeScaleFlag, int stateIndex)
        {
            if (!Running)
                return;
            if (frames >= MaxFrames)
            {
                Stop();
                return;
            }

            int o = frames * columns;
            rows[o++] = Time.frameCount;
            rows[o++] = Time.unscaledTime - startTime;
            rows[o++] = unscaledDelta * 1000f;
            rows[o++] = NanosToMs(main);
            rows[o++] = NanosToMs(render);
            rows[o++] = NanosToMs(gpu);
            rows[o++] = NanosToMs(waitPresent);
            rows[o++] = gc.Valid ? gc.LastValue : 0f;
            rows[o++] = gcAllocs.Valid ? gcAllocs.LastValue : 0f;
            rows[o++] = NanosToMs(gcCollect);
            rows[o++] = NanosToMs(instantiate);
            rows[o++] = NanosToMs(physics2D);
            rows[o++] = NanosToMs(physicsSync);
            rows[o++] = NanosToMs(cameraRender);
            rows[o++] = NanosToMs(canvasBatch);
            rows[o++] = NanosToMs(canvasOverlay);
            rows[o++] = NanosToMs(waitGfx);
            rows[o++] = NanosToMs(waitVSync);
            rows[o++] = batches.Valid ? batches.LastValue : 0f;
            rows[o++] = setPass.Valid ? setPass.LastValue : 0f;
            rows[o++] = draws.Valid ? draws.LastValue : 0f;
            for (int i = 0; i < markers.Length; i++)
                rows[o++] = NanosToMs(markers[i]);
            rows[o++] = stats.Bullets;
            rows[o++] = stats.Enemies;
            rows[o++] = stats.Particles;
            rows[o++] = timeScaleFlag;
            rows[o++] = stateIndex;
            frames++;
        }

        private static float NanosToMs(ProfilerRecorder recorder) => recorder.Valid ? recorder.LastValue * 1e-6f : 0f;

        /// <summary>Stops and writes `PerfLogs/&lt;label&gt;_&lt;time&gt;.csv` and `..._meta.txt` into the persistent data folder. Returns the CSV path.</summary>
        public string Stop()
        {
            if (!Running)
                return LastPath;
            Running = false;

            string folder = Path.Combine(Application.persistentDataPath, "PerfLogs");
            Directory.CreateDirectory(folder);
            string stem = $"{label}_{DateTime.Now:yyyyMMdd_HHmmss}";
            string csv = Path.Combine(folder, stem + ".csv");

            var sb = new StringBuilder(frames * columns * 6 + 512);
            sb.Append(string.Join(",", FixedColumns)).Append(',').Append(string.Join(",", PerfMarkers.Names)).Append(',')
              .Append(string.Join(",", TailColumns)).Append('\n');
            for (int f = 0; f < frames; f++)
            {
                int o = f * columns;
                for (int c = 0; c < columns; c++)
                {
                    if (c > 0)
                        sb.Append(',');
                    sb.Append(rows[o + c].ToString("0.###", CultureInfo.InvariantCulture));
                }
                sb.Append('\n');
            }
            File.WriteAllText(csv, sb.ToString());
            File.WriteAllText(Path.Combine(folder, stem + "_meta.txt"), BuildMeta());
            LastPath = csv;
            Debug.Log($"PerfLogger: wrote {frames} frames to {csv}");
            return csv;
        }

        private string BuildMeta()
        {
            var sb = new StringBuilder(1024);
            sb.Append("label: ").Append(label).Append('\n');
            sb.Append("frames: ").Append(frames).Append('\n');
            sb.Append("date: ").Append(DateTime.Now.ToString("s")).Append('\n');
            sb.Append("unity: ").Append(Application.unityVersion).Append('\n');
            sb.Append("development build: ").Append(Debug.isDebugBuild).Append("  editor: ").Append(Application.isEditor).Append('\n');
            sb.Append("scripting backend: ").Append(
#if ENABLE_IL2CPP
                "IL2CPP"
#else
                "Mono"
#endif
            ).Append('\n');
            sb.Append("cpu: ").Append(SystemInfo.processorType).Append(" x").Append(SystemInfo.processorCount).Append('\n');
            sb.Append("gpu: ").Append(SystemInfo.graphicsDeviceName).Append(" (").Append(SystemInfo.graphicsDeviceType).Append(")\n");
            sb.Append("resolution: ").Append(Screen.width).Append('x').Append(Screen.height).Append("  fullscreen: ").Append(Screen.fullScreenMode)
              .Append("  refresh: ").Append(Screen.currentResolution.refreshRateRatio.value.ToString("0.##", CultureInfo.InvariantCulture)).Append(" Hz\n");
            sb.Append("vSyncCount: ").Append(QualitySettings.vSyncCount).Append("  targetFrameRate: ").Append(Application.targetFrameRate).Append('\n');
            sb.Append("quality: ").Append(QualitySettings.names[QualitySettings.GetQualityLevel()]).Append('\n');
            sb.Append("args: ").Append(string.Join(" ", Environment.GetCommandLineArgs())).Append('\n');
            sb.Append("notes: ").Append(notes).Append('\n');
            return sb.ToString();
        }

        /// <summary>Writes every profiler stat this player can record (category and name) so column names can be checked.</summary>
        public static void DumpAvailableStats()
        {
            var handles = new List<ProfilerRecorderHandle>(4096);
            ProfilerRecorderHandle.GetAvailable(handles);
            var sb = new StringBuilder(handles.Count * 40);
            foreach (ProfilerRecorderHandle handle in handles)
            {
                ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                sb.Append(description.Category.Name).Append('\t').Append(description.Name).Append('\n');
            }
            string folder = Path.Combine(Application.persistentDataPath, "PerfLogs");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "stats_available.txt"), sb.ToString());
        }
    }
}
