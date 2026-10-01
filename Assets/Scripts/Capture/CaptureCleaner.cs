using System.Collections.Generic;
using BulletHell.AI;
using BulletHell.Perf;
using BulletHell.Projectiles;
using BulletHell.Telemetry;
using BulletHell.UI;
using UnityEngine;

namespace BulletHell.Capture
{
    /// <summary>
    /// R1 portfolio capture kit - "Clean capture": hides the debug overlays (DebugOverlay canvas, performance overlay F8, telemetry
    /// summary F9, AI flow-field / line-of-sight view, bullet path view F6) and the OS cursor, and keeps the HUD. The settings file is
    /// never touched (the overlays are switched off on the objects, not through the "Show debug overlay" setting), and everything is
    /// put back when the scenario ends. <see cref="Tick"/> runs every frame so an overlay a setting change or a key press turns back
    /// on is hidden again. The build version / "dev" label only exists on the Main Menu, which scenarios skip.
    /// </summary>
    public sealed class CaptureCleaner
    {
        private readonly List<GameObject> hidden = new List<GameObject>();
        private readonly List<bool> wasActive = new List<bool>();
        private bool applied;
        private bool cursorWasVisible;
        private bool pathsWereOn;

        public bool IsApplied => applied;

        /// <summary>Finds the overlay objects of the current scene (once) and hides them.</summary>
        public void Apply()
        {
            Restore();
            foreach (DebugOverlay overlay in Object.FindObjectsByType<DebugOverlay>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Track(overlay.gameObject);
            foreach (AiDebugView view in Object.FindObjectsByType<AiDebugView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Track(view.gameObject);

            cursorWasVisible = Cursor.visible;
            pathsWereOn = BulletPathDebug.Enabled;
            applied = true;
            Tick();
        }

        private void Track(GameObject go)
        {
            hidden.Add(go);
            wasActive.Add(go.activeSelf);
        }

        /// <summary>Keeps everything hidden. Cheap: a few flag reads per frame.</summary>
        public void Tick()
        {
            if (!applied)
                return;
            for (int i = 0; i < hidden.Count; i++)
                if (hidden[i] != null && hidden[i].activeSelf)
                    hidden[i].SetActive(false);

            PerfHost perf = PerfHost.Instance;
            if (perf != null && perf.Overlay != null && perf.Overlay.Visible)
                perf.Overlay.SetVisible(false);
            if (TelemetryOverlay.IsOpen)
                TelemetryOverlay.Close();
            if (BulletPathDebug.Enabled)
                BulletPathDebug.Enabled = false;
            if (Cursor.visible)
                Cursor.visible = false;
        }

        /// <summary>Puts the hidden objects, the cursor and the bullet path view back as they were.</summary>
        public void Restore()
        {
            if (!applied)
                return;
            for (int i = 0; i < hidden.Count; i++)
                if (hidden[i] != null)
                    hidden[i].SetActive(wasActive[i]);
            hidden.Clear();
            wasActive.Clear();
            Cursor.visible = cursorWasVisible;
            BulletPathDebug.Enabled = pathsWereOn;
            applied = false;
        }
    }
}
