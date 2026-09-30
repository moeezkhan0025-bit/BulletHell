using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// Renders the open scene's main camera plus every overlay canvas into a PNG at a chosen resolution, without needing the
    /// Game window to be focused (overlay canvases are temporarily switched to the capture camera and restored).
    /// Used to check the VOX VEGETALLIS screens against the mockups. Output goes where the caller says (outside Assets).
    /// </summary>
    public static class VoxShots
    {
        /// <summary>Captures to <temp>/shots/<name>.png (for scripted checks).</summary>
        public static string CaptureNamed(string name, int width = 1920, int height = 1080) =>
            Capture(Path.Combine(Path.GetTempPath(), "shots", name + ".png"), width, height);

        public static string Capture(string path, int width = 1920, int height = 1080)
        {
            Camera main = Camera.main;
            if (main == null)
                return "no main camera";

            var camGo = Object.Instantiate(main.gameObject);
            camGo.name = "VoxShotCamera";
            camGo.hideFlags = HideFlags.HideAndDontSave;
            foreach (var listener in camGo.GetComponentsInChildren<AudioListener>())
                Object.DestroyImmediate(listener);
            Camera cam = camGo.GetComponent<Camera>();
            cam.enabled = false;

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            cam.targetTexture = rt;
            cam.aspect = width / (float)height;

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var restore = new System.Collections.Generic.List<(Canvas canvas, RenderMode mode, Camera cam, float plane)>();
            foreach (Canvas canvas in canvases)
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    continue;
                restore.Add((canvas, canvas.renderMode, canvas.worldCamera, canvas.planeDistance));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = Mathf.Max(1f, cam.nearClipPlane + 1f);
            }

            try
            {
                Canvas.ForceUpdateCanvases();
                cam.Render();
                Canvas.ForceUpdateCanvases();
                cam.Render();

                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                RenderTexture.active = previous;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }
            finally
            {
                foreach (var entry in restore)
                {
                    entry.canvas.renderMode = entry.mode;
                    entry.canvas.worldCamera = entry.cam;
                    entry.canvas.planeDistance = entry.plane;
                }
                cam.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(camGo);
            }
            return "ok " + path;
        }
    }
}
