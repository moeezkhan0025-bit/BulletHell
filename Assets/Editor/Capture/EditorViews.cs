#if UNITY_EDITOR
#pragma warning disable CS0618
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// R1 portfolio capture kit: sets up Editor windows (Project, Hierarchy, Build Profiles, Inspector + Art Slot Board, art pipeline map)
    /// and tries to save a PNG of each from the screen. Falls back to a "snip now" hint when the pixels are blank.
    /// Creates only floating windows of its own and closes them again; nothing is saved, scenes are never modified.
    /// </summary>
    public static class EditorViews
    {
        const string Menu = "BulletHell/Capture/Editor Views/";
        const string SceneGame = "Assets/Scenes/Game.unity";

        public class Result
        {
            public string view, file, path, note;
            public bool ok;
            public long bytes;
        }

        public class ViewDef
        {
            public string id, title, setup;
            public Func<Result, IEnumerator> run;
        }

        static readonly List<EditorWindow> created = new List<EditorWindow>();
        static readonly List<Result> results = new List<Result>();
        static bool busy;
        public static bool Busy => busy;
        static string outDir => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Captures", "Editor");

        public static readonly ViewDef[] Views =
        {
            new ViewDef { id = "project_assets", title = "1 Project window, Assets expanded", run = V1,
                setup = "Open a Project window (two-column), select Assets, expand Art, Data, Scripts, Scenes, Prefabs in the folder tree." },
            new ViewDef { id = "project_scripts", title = "2 Project window, Assets/Scripts", run = V2,
                setup = "Project window (two-column), select Assets/Scripts with its subfolders expanded in the tree." },
            new ViewDef { id = "hierarchy_game", title = "3 Hierarchy, Game scene", run = V3,
                setup = "Open Assets/Scenes/Game.unity (do not enter Play), open a Hierarchy window and expand Player, managers, spawners, pools, UI." },
            new ViewDef { id = "build_profiles", title = "4 Build Profiles", run = V4,
                setup = "File > Build Profiles. Select the Windows / WebGL entries." },
            new ViewDef { id = "inspector_artslot", title = "5 Inspector + Art Slot Board", run = V5,
                setup = "Lock an Inspector on Enemy_Grunt, another on Arm_Blue, open BulletHell > Capture > Art Slot Board, place side by side." },
            new ViewDef { id = "art_pipeline", title = "6 ArtSource to Assets/Art map", run = V6,
                setup = "Open the Art Drop Map window (BulletHell > Capture > Editor Views > Open Art Drop Map window) plus a Project window on Assets/Art." },
        };

        // ---------- menu ----------
        [MenuItem(Menu + "1 Project window - Assets expanded")] static void M1() => RunView(0, true, null);
        [MenuItem(Menu + "2 Project window - Assets/Scripts")] static void M2() => RunView(1, true, null);
        [MenuItem(Menu + "3 Hierarchy - Game scene")] static void M3() => RunView(2, true, null);
        [MenuItem(Menu + "4 Build Profiles window")] static void M4() => RunView(3, true, null);
        [MenuItem(Menu + "5 Inspector + Art Slot Board")] static void M5() => RunView(4, true, null);
        [MenuItem(Menu + "6 ArtSource to Assets/Art map")] static void M6() => RunView(5, true, null);
        [MenuItem(Menu + "All views (one at a time)")] static void MAll() => EditorViewsStepWindow.Open();
        [MenuItem(Menu + "Close view windows")]
        public static void CloseCreated()
        {
            foreach (var w in created) if (w) w.Close();
            created.Clear();
        }

        /// <summary>Runs one view; closes its windows on success (if asked), leaves them open on failure for manual snipping.</summary>
        public static void RunView(int index, bool closeOnSuccess, Action<Result> done)
        {
            if (busy) { Debug.LogWarning("[EditorViews] already running a view."); return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[EditorViews] Leave Play mode first."); return; }
            busy = true;
            var def = Views[index];
            var r = new Result { view = def.title, file = def.id };
            CloseCreated();
            Drive(Wrap(def, r), () =>
            {
                busy = false;
                results.RemoveAll(x => x.file == r.file);
                results.Add(r);
                if (r.ok)
                {
                    Debug.Log($"[EditorViews] SAVED {def.title}: {r.path} ({r.bytes / 1024} KB). {r.note}");
                    if (closeOnSuccess) CloseCreated();
                }
                else
                {
                    var hint = $"Snip '{def.title}' now (Win+Shift+S) and save as {Path.Combine(outDir, def.id + "_" + Stamp() + ".png")}";
                    Debug.LogWarning($"[EditorViews] NOT SAVED {def.title}: {r.note}\n{hint}\nSetup: {def.setup}");
                    var w = created.FirstOrDefault(x => x);
                    if (w) w.ShowNotification(new GUIContent("Snip " + def.title + " now (Win+Shift+S)"), 8);
                }
                done?.Invoke(r);
            });
        }

        public static string Summary()
        {
            var sb = new System.Text.StringBuilder("Editor Views summary:\n");
            foreach (var r in results)
                sb.AppendLine((r.ok ? "  SAVED   " : "  SNIP    ") + r.view + (r.ok ? "  " + r.path + " (" + r.bytes / 1024 + " KB)" : "  - " + r.note));
            return sb.ToString();
        }

        public static void LogSummary() => Debug.Log("[EditorViews] " + Summary());

        static string Stamp() => DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // ---------- coroutine driver ----------
        static IEnumerator Wrap(ViewDef def, Result r)
        {
            IEnumerator inner = null;
            try { inner = def.run(r); } catch (Exception e) { r.note = "setup error: " + e.Message; Debug.LogException(e); yield break; }
            while (true)
            {
                bool more;
                try { more = inner.MoveNext(); }
                catch (Exception e) { r.ok = false; r.note = "error: " + e.Message; Debug.LogException(e); yield break; }
                if (!more) yield break;
                yield return inner.Current;
            }
        }

        static void Drive(IEnumerator e, Action done)
        {
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                bool more;
                try { more = e.MoveNext(); } catch (Exception ex) { Debug.LogException(ex); more = false; }
                if (!more) { EditorApplication.update -= tick; done?.Invoke(); }
            };
            EditorApplication.update += tick;
        }

        /// <summary>Wait at least n editor ticks and s seconds.</summary>
        static IEnumerable Wait(int ticks, double seconds)
        {
            double end = EditorApplication.timeSinceStartup + seconds;
            int n = 0;
            while (n < ticks || EditorApplication.timeSinceStartup < end) { n++; yield return null; }
        }

        // ---------- reflection helpers ----------
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        static Type EdType(string n) => typeof(Editor).Assembly.GetType(n);

        static object Get(object o, string name)
        {
            if (o == null) return null;
            for (var t = o.GetType(); t != null; t = t.BaseType)
            {
                var f = t.GetField(name, All | BindingFlags.DeclaredOnly); if (f != null) return f.GetValue(o);
                var p = t.GetProperty(name, All | BindingFlags.DeclaredOnly); if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(o);
            }
            return null;
        }

        static bool Set(object o, string name, object v)
        {
            for (var t = o.GetType(); t != null; t = t.BaseType)
            {
                var f = t.GetField(name, All | BindingFlags.DeclaredOnly); if (f != null) { f.SetValue(o, v); return true; }
                var p = t.GetProperty(name, All | BindingFlags.DeclaredOnly); if (p != null && p.CanWrite) { p.SetValue(o, v); return true; }
            }
            return false;
        }

        static bool Call(object o, string name, params object[] args)
        {
            if (o == null) return false;
            for (var t = o.GetType(); t != null; t = t.BaseType)
                foreach (var m in t.GetMethods(All | BindingFlags.DeclaredOnly).Where(x => x.Name == name && x.GetParameters().Length == args.Length))
                {
                    try { m.Invoke(o, ConvertArgs(m, args)); return true; }
                    catch (ArgumentException) { }
                    catch (TargetInvocationException e) { Debug.LogWarning("[EditorViews] " + name + ": " + e.InnerException?.Message); return false; }
                }
            return false;
        }

        // Unity 6.3 uses EntityId where older versions used int instance ids; convert through its implicit operator.
        static object[] ConvertArgs(MethodInfo m, object[] args)
        {
            var ps = m.GetParameters();
            var res = new object[args.Length];
            for (int i = 0; i < args.Length; i++) res[i] = ConvertOne(args[i], ps[i].ParameterType);
            return res;
        }

        static object ConvertOne(object a, Type target)
        {
            if (a == null || target.IsInstanceOfType(a)) return a;
            if (a is int ia)
            {
                var op = FindImplicit(target, typeof(int));
                if (op != null) return op.Invoke(null, new object[] { ia });
            }
            if (a is int[] arr && target.IsArray)
            {
                var et = target.GetElementType();
                var outArr = Array.CreateInstance(et, arr.Length);
                for (int i = 0; i < arr.Length; i++) outArr.SetValue(ConvertOne(arr[i], et), i);
                return outArr;
            }
            return a;
        }

        static MethodInfo FindImplicit(Type target, Type from)
        {
            return target.GetMethods(BindingFlags.Static | BindingFlags.Public)
                .FirstOrDefault(x => x.Name == "op_Implicit" && x.ReturnType == target && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == from);
        }

        // ---------- window helpers ----------
        static Vector2 Origin()
        {
            var m = EditorGUIUtility.GetMainWindowPosition();
            return new Vector2(m.x + 60, m.y + 60);
        }

        static EditorWindow Make(Type t, Rect r)
        {
            var w = (EditorWindow)ScriptableObject.CreateInstance(t);
            w.ShowUtility();
            w.position = r;
            created.Add(w);
            return w;
        }

        static Rect ClampToScreen(Rect r)
        {
            var main = EditorGUIUtility.GetMainWindowPosition();
            float maxR = main.xMax - 10, maxB = main.yMax - 10;
            if (r.xMax > maxR) r.width = Mathf.Max(480, maxR - r.x);
            if (r.xMax > maxR) r.x = Mathf.Max(main.x, maxR - r.width);
            if (r.yMax > maxB) r.height = Mathf.Max(300, maxB - r.y);
            return r;
        }

        static EditorWindow MakeProject(Rect r)
        {
            var w = Make(EdType("UnityEditor.ProjectBrowser"), r);
            w.titleContent = new GUIContent("Project");
            return w;
        }

        static int FolderId(string path) => AssetDatabase.LoadMainAssetAtPath(path).GetInstanceID();

        static void ConfigureProject(EditorWindow pb, string selectPath, IEnumerable<string> expand)
        {
            if (!Call(pb, "SetTwoColumns")) Call(pb, "SetViewMode", Enum.ToObject(EdType("UnityEditor.ProjectBrowser+ViewMode"), 1));
            var tree = Get(pb, "m_FolderTree");
            var data = Get(tree, "data");
            if (data != null)
                foreach (var p in expand) Call(data, "SetExpanded", FolderId(p), true);
            Call(pb, "SetFolderSelection", new[] { FolderId(selectPath) }, true);
            pb.Repaint();
        }

        static IEnumerable<string> SubFolders(string root) => AssetDatabase.GetSubFolders(root);

        // ---------- views ----------
        static IEnumerator V1(Result r)
        {
            var o = Origin();
            var pb = MakeProject(ClampToScreen(new Rect(o.x, o.y, 980, 760)));
            foreach (var x in Wait(4, 0.6)) yield return x;
            var exp = new List<string> { "Assets" };
            exp.AddRange(SubFolders("Assets"));
            ConfigureProject(pb, "Assets", exp);
            foreach (var x in Wait(4, 0.4)) yield return x;
            ConfigureProject(pb, "Assets", exp);
            foreach (var x in Capture(r, "project_assets", pb)) yield return x;
        }

        static IEnumerator V2(Result r)
        {
            var o = Origin();
            var pb = MakeProject(ClampToScreen(new Rect(o.x, o.y, 980, 760)));
            foreach (var x in Wait(4, 0.6)) yield return x;
            var exp = new List<string> { "Assets", "Assets/Scripts" };
            exp.AddRange(SubFolders("Assets/Scripts"));
            ConfigureProject(pb, "Assets/Scripts", exp);
            foreach (var x in Wait(4, 0.4)) yield return x;
            ConfigureProject(pb, "Assets/Scripts", exp);
            foreach (var x in Capture(r, "project_scripts", pb)) yield return x;
        }

        static IEnumerator V3(Result r)
        {
            // never discard unsaved changes, never save
            var prev = new List<string>();
            bool gameOpen = false;
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (s.isDirty || string.IsNullOrEmpty(s.path))
                {
                    r.ok = false;
                    r.note = "scene '" + (string.IsNullOrEmpty(s.name) ? "Untitled" : s.name) + "' has unsaved changes; save or discard it yourself, then rerun";
                    Debug.LogWarning("[EditorViews] " + r.note);
                    yield break;
                }
                prev.Add(s.path);
                if (s.path == SceneGame) gameOpen = true;
            }
            if (!gameOpen) EditorSceneManager.OpenScene(SceneGame, OpenSceneMode.Single);
            var o = Origin();
            var hw = Make(EdType("UnityEditor.SceneHierarchyWindow"), ClampToScreen(new Rect(o.x, o.y, 520, 1000)));
            foreach (var x in Wait(5, 0.6)) yield return x;
            var sh = Get(hw, "sceneHierarchy");
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(SceneGame);
            Call(sh, "SetScenesExpanded", new List<string> { scene.name });
            hw.Repaint();
            foreach (var x in Wait(4, 0.5)) yield return x;
            int count = 0;
            foreach (var go in scene.GetRootGameObjects()) ExpandRec(sh, go.transform, 0, ref count);
            hw.Repaint();
            foreach (var x in Wait(4, 0.5)) yield return x;
            r.note = "expanded " + count + " nodes.";
            foreach (var x in Capture(r, "hierarchy_game", hw)) yield return x;
            if (!gameOpen && prev.Count > 0)
            {
                EditorSceneManager.OpenScene(prev[0], OpenSceneMode.Single);
                for (int i = 1; i < prev.Count; i++) EditorSceneManager.OpenScene(prev[i], OpenSceneMode.Additive);
            }
        }

        static void ExpandRec(object sh, Transform t, int depth, ref int count)
        {
            if (t.childCount == 0 || depth > 2) return;
            // UI canvases (FlowUI, DebugOverlay): open the canvas and its SafeArea only, so the UI states are listed but do not push the managers, spawners and pools off the window.
            if (depth > 1 && t.GetComponentInParent<Canvas>() != null) return;
            Call(sh, "ExpandTreeViewItem", t.gameObject.GetInstanceID(), true);
            count++;
            foreach (Transform c in t) ExpandRec(sh, c, depth + 1, ref count);
        }

        static IEnumerator V4(Result r)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.Build.Profile.BuildProfileWindow", false)).FirstOrDefault(t => t != null);
            var before = type == null ? new UnityEngine.Object[0] : Resources.FindObjectsOfTypeAll(type);
            var profiles = AssetDatabase.FindAssets("t:BuildProfile").Select(AssetDatabase.GUIDToAssetPath).ToList();
            var note = profiles.Count == 0
                ? "No Build Profile assets exist in the project (only the built-in platform list is shown)."
                : "Profiles: " + string.Join(", ", profiles);
            bool executed = EditorApplication.ExecuteMenuItem("File/Build Profiles");
            foreach (var x in Wait(6, 1.0)) yield return x;
            EditorWindow w = type == null ? null : Resources.FindObjectsOfTypeAll(type).Cast<EditorWindow>().FirstOrDefault();
            if (w == null) { r.ok = false; r.note = "Build Profiles window did not open (menu executed: " + executed + "). " + note; yield break; }
            if (!before.Contains(w)) created.Add(w);
            var o = Origin();
            if (!w.docked) w.position = ClampToScreen(new Rect(o.x, o.y, 1100, 700));
            foreach (var x in Wait(4, 0.5)) yield return x;
            foreach (var x in Capture(r, "build_profiles", w)) yield return x;
            r.note = (r.note ?? "") + " " + note;
        }

        static IEnumerator V5(Result r)
        {
            var o = Origin();
            var prevSel = Selection.objects;
            var enemy = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Data/Enemies/Enemy_Grunt.asset");
            var arm = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Data/Arms/Arm_Blue.asset");
            var insType = EdType("UnityEditor.InspectorWindow");
            var wins = new List<EditorWindow>();
            float x0 = o.x;
            foreach (var a in new[] { enemy, arm })
            {
                if (a == null) continue;
                Selection.activeObject = a;
                var w = Make(insType, ClampToScreen(new Rect(x0, o.y, 400, 720)));
                foreach (var x in Wait(3, 0.4)) yield return x;
                Set(w, "isLocked", true);
                wins.Add(w);
                x0 += 402;
            }
            Selection.objects = prevSel;
            var before = new HashSet<EditorWindow>(Resources.FindObjectsOfTypeAll<EditorWindow>());
            bool menu = EditorApplication.ExecuteMenuItem("BulletHell/Capture/Art Slot Board");
            foreach (var x in Wait(5, 0.8)) yield return x;
            var added = Resources.FindObjectsOfTypeAll<EditorWindow>().Where(w => !before.Contains(w)).ToList();
            string extra;
            if (!menu || added.Count == 0)
                extra = "Art Slot Board menu not available or opened no window; captured the inspectors only (open BulletHell > Capture > Art Slot Board and snip both).";
            else
            {
                var b = added[0];
                created.Add(b);
                if (!b.docked) b.position = ClampToScreen(new Rect(x0, o.y, 760, 720));
                wins.Add(b);
                extra = "Art Slot Board included.";
            }
            foreach (var x in Wait(4, 0.5)) yield return x;
            foreach (var x in Capture(r, "inspector_artslot", wins.ToArray())) yield return x;
            r.note = (r.note ?? "") + " " + extra;
        }

        static IEnumerator V6(Result r)
        {
            var o = Origin();
            var map = (EditorWindow)ScriptableObject.CreateInstance<ArtDropMapWindow>();
            map.ShowUtility();
            map.position = ClampToScreen(new Rect(o.x, o.y, 600, 640));
            created.Add(map);
            var pb = MakeProject(ClampToScreen(new Rect(o.x + 602, o.y, 760, 640)));
            foreach (var x in Wait(4, 0.6)) yield return x;
            var exp = new List<string> { "Assets", "Assets/Art" };
            exp.AddRange(SubFolders("Assets/Art"));
            ConfigureProject(pb, "Assets/Art", exp);
            foreach (var x in Wait(4, 0.4)) yield return x;
            ConfigureProject(pb, "Assets/Art", exp);
            foreach (var x in Capture(r, "art_pipeline", map, pb)) yield return x;
        }

        // ---------- capture ----------
        static IEnumerable Capture(Result r, string file, params EditorWindow[] ws)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                foreach (var w in ws) { if (!w) continue; w.Focus(); w.Repaint(); }
                InternalEditorUtility.RepaintAllViews();
                foreach (var x in Wait(3, 0.35)) yield return x;
            }
            Grab(r, file, ws);
        }

        static void Grab(Result r, string file, EditorWindow[] ws)
        {
            try
            {
                var live = ws.Where(w => w).ToArray();
                if (live.Length == 0) { r.ok = false; r.note = "window gone"; return; }
                Rect u = live[0].position;
                foreach (var w in live)
                {
                    var p = w.position;
                    u = Rect.MinMaxRect(Mathf.Min(u.xMin, p.xMin), Mathf.Min(u.yMin, p.yMin), Mathf.Max(u.xMax, p.xMax), Mathf.Max(u.yMax, p.yMax));
                }
                float ppp = 1f; // ReadScreenPixel and window.position share the same (physical) unit here
                int pw = Mathf.RoundToInt(u.width * ppp), ph = Mathf.RoundToInt(u.height * ppp);
                var m = typeof(InternalEditorUtility).GetMethods(All).FirstOrDefault(x => (x.Name == "ReadScreenPixel" || x.Name == "ReadScreenPixels") && x.GetParameters().Length == 3);
                if (m == null) { r.ok = false; r.note = "ReadScreenPixel API not found"; return; }
                var colors = (Color[])m.Invoke(null, new object[] { new Vector2(Mathf.Round(u.x), Mathf.Round(u.y)), pw, ph });
                string why;
                if (!LooksReal(colors, out why)) { r.ok = false; r.note = "screen grab looked blank (" + why + "); window probably not visible/focused"; return; }
                var tex = new Texture2D(pw, ph, TextureFormat.RGBA32, false);
                var px = new Color32[colors.Length];
                for (int i = 0; i < px.Length; i++) { var c = colors[i]; px[i] = new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), 255); }
                tex.SetPixels32(px);
                var png = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);
                Directory.CreateDirectory(outDir);
                r.path = Path.Combine(outDir, file + "_" + Stamp() + ".png");
                File.WriteAllBytes(r.path, png);
                r.bytes = png.Length;
                r.ok = true;
            }
            catch (Exception e) { r.ok = false; r.note = "capture failed: " + e.Message; }
        }

        static bool LooksReal(Color[] c, out string why)
        {
            why = "";
            if (c == null || c.Length < 100) { why = "no pixels"; return false; }
            var buckets = new HashSet<int>();
            double sum = 0, sum2 = 0; int n = 0;
            int step = Mathf.Max(1, c.Length / 20000);
            for (int i = 0; i < c.Length; i += step)
            {
                var p = c[i];
                float l = p.r * 0.3f + p.g * 0.59f + p.b * 0.11f;
                sum += l; sum2 += l * l; n++;
                buckets.Add(((int)(p.r * 15) << 8) | ((int)(p.g * 15) << 4) | (int)(p.b * 15));
            }
            double mean = sum / n, sd = Math.Sqrt(Math.Max(0, sum2 / n - mean * mean));
            why = "mean " + mean.ToString("0.00") + ", sd " + sd.ToString("0.000") + ", colors " + buckets.Count;
            return mean > 0.02 && sd > 0.015 && buckets.Count > 6;
        }
    }

    /// <summary>Small helper window for "All views (one at a time)".</summary>
    public class EditorViewsStepWindow : EditorWindow
    {
        int next;
        string last = "";
        bool running;

        public static void Open()
        {
            var w = GetWindow<EditorViewsStepWindow>(true, "Editor Views", true);
            w.minSize = new Vector2(420, 190);
            w.maxSize = new Vector2(420, 260);
            var m = EditorGUIUtility.GetMainWindowPosition();
            w.position = new Rect(m.xMax - 440, m.y + 20, 420, 200);
            w.next = 0; w.last = "";
            w.Show();
        }

        void OnGUI()
        {
            var v = EditorViews.Views;
            GUILayout.Space(6);
            if (next < v.Length)
            {
                EditorGUILayout.LabelField("Next: " + v[next].title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Sets up the view, saves a PNG, closes the window. If the grab is blank you get a 'Snip now' hint and the view stays open.", EditorStyles.wordWrappedMiniLabel);
            }
            else EditorGUILayout.LabelField("All views done.", EditorStyles.boldLabel);
            GUILayout.Space(4);
            if (!string.IsNullOrEmpty(last)) EditorGUILayout.HelpBox(last, MessageType.None);
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = !running && next < v.Length;
                if (GUILayout.Button("Next view", GUILayout.Height(28))) RunNext();
                GUI.enabled = !running;
                if (GUILayout.Button("Skip", GUILayout.Height(28), GUILayout.Width(60))) { next++; Repaint(); }
                GUI.enabled = true;
                if (GUILayout.Button("Close views", GUILayout.Height(28), GUILayout.Width(90))) EditorViews.CloseCreated();
            }
        }

        void RunNext()
        {
            running = true;
            var idx = next;
            Close(); // the step window must not sit on top of what we capture
            EditorViews.RunView(idx, false, r =>
            {
                var w = CreateInstance<EditorViewsStepWindow>();
                w.next = idx + 1;
                w.last = r.ok ? "Saved: " + r.path : "Snip now (Win+Shift+S): " + r.view + ". " + r.note;
                var m = EditorGUIUtility.GetMainWindowPosition();
                w.ShowUtility();
                w.position = new Rect(m.xMax - 440, m.y + 20, 420, 200);
                if (w.next >= EditorViews.Views.Length) EditorViews.LogSummary();
            });
        }
    }
}
#endif
