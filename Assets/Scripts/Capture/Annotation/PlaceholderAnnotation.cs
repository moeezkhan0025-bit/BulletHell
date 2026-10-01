#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Reflection;
using BulletHell.Arena;
using BulletHell.Bosses;
using BulletHell.Enemies;
using BulletHell.Pickups;
using BulletHell.Player;
using BulletHell.Projectiles;
using UnityEngine;

namespace BulletHell.Capture
{
    /// <summary>
    /// Capture tool: labels on-screen elements in the Game view as FINAL ART or PLACEHOLDER (IMGUI, so a normal screenshot includes it).
    /// Editor and development builds only. The final/placeholder verdict comes from <see cref="IsFinal"/>, a delegate the editor assigns
    /// (runtime code cannot reference editor code). Menu: BulletHell/Capture/Placeholder Annotation.
    /// </summary>
    public static class PlaceholderAnnotation
    {
        private static bool enabled;

        /// <summary>Draw the labels and the legend. Off by default; the editor menu sets it.</summary>
        public static bool Enabled
        {
            get => enabled;
            set
            {
                enabled = value;
                if (value)
                    PlaceholderAnnotationHost.Ensure();
            }
        }

        /// <summary>True when the object (a Sprite or AudioClip) is final art. Assigned by the editor bridge; null in a dev build.</summary>
        public static Func<UnityEngine.Object, bool> IsFinal;

        /// <summary>Legend text for the audio line, e.g. "AUDIO: all 41 clips are CC0 placeholders". Assigned by the editor bridge.</summary>
        public static Func<string> AudioLegend;

        // VoxKit palette tokens (the editor bridge overwrites them from Assets/Data/UI/VoxVegetallis.asset).
        public static Color InkSoil = new Color32(0x2E, 0x1F, 0x14, 255);
        public static Color Marble = new Color32(0xF4, 0xEE, 0xDC, 255);
        public static Color Leaf = new Color32(0x5E, 0x9F, 0x3E, 255);
        public static Color Carrot = new Color32(0xE5, 0x7A, 0x24, 255);
        public static Color Gold = new Color32(0xE9, 0xB6, 0x3A, 255);

        /// <summary>Status chip font (Lilita One) and name font (Nunito ExtraBold); optional, set by the editor bridge.</summary>
        public static Font ChipFont;
        public static Font NameFont;
    }

    /// <summary>Hidden host that owns OnGUI. Created on demand when the annotation is switched on.</summary>
    public sealed class PlaceholderAnnotationHost : MonoBehaviour
    {
        private enum Cat { Player, Arm, Enemy, Boss, PlayerBullet, EnemyBullet, Obstacle, Trap, Coin, AmmoPickup, Backdrop }

        private struct Item
        {
            public Renderer renderer;
            public string name;
            public bool isFinal;
            public bool backdrop;
        }

        private const float RefreshSeconds = 0.25f;
        private const int MaxItems = 64;

        private static PlaceholderAnnotationHost instance;
        private static FieldInfo hostileField;

        private readonly List<Item> items = new List<Item>(MaxItems);
        private readonly Dictionary<long, int> keyToItem = new Dictionary<long, int>(MaxItems);
        private readonly Dictionary<long, float> bestArea = new Dictionary<long, float>(MaxItems);
        private float lastRefresh = -10f;
        private int finalCount;
        private int placeholderCount;
        private string audioLine = "";

        private GUIStyle statusStyle;
        private GUIStyle nameStyle;
        private GUIStyle legendTitle;
        private GUIStyle legendText;
        private int styleHeight = -1;

        public static void Ensure()
        {
            if (instance != null || !Application.isPlaying)
                return;
            var go = new GameObject("PlaceholderAnnotation") { hideFlags = HideFlags.HideAndDontSave };
            instance = go.AddComponent<PlaceholderAnnotationHost>();
            if (Application.isPlaying)
                DontDestroyOnLoad(go);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnLoad()
        {
            if (PlaceholderAnnotation.Enabled)
                Ensure();
        }

        private void OnGUI()
        {
            if (!PlaceholderAnnotation.Enabled)
                return;
            if (Event.current.type != EventType.Repaint)
                return;
            Camera cam = Camera.main;
            if (cam == null)
                return;

            if (Time.realtimeSinceStartup - lastRefresh > RefreshSeconds)
            {
                lastRefresh = Time.realtimeSinceStartup;
                Refresh(cam);
            }

            BuildStyles();
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.6f, 2.5f);
            for (int i = 0; i < items.Count; i++)
            {
                Item it = items[i];
                if (it.renderer == null || !it.renderer.enabled)
                    continue;
                DrawLabel(cam, it, scale);
            }
            DrawLegend(scale);
        }

        // ---- gathering -------------------------------------------------------------------------------------------------

        private void Refresh(Camera cam)
        {
            items.Clear();
            keyToItem.Clear();
            bestArea.Clear();
            finalCount = 0;
            placeholderCount = 0;
            Func<UnityEngine.Object, bool> isFinal = PlaceholderAnnotation.IsFinal;
            audioLine = PlaceholderAnnotation.AudioLegend != null ? PlaceholderAnnotation.AudioLegend() : "";

            SpriteRenderer[] all = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                SpriteRenderer r = all[i];
                if (r == null || !r.enabled || r.sprite == null || !r.gameObject.activeInHierarchy)
                    continue;
                if (r.color.a < 0.05f)
                    continue;
                string lower = r.name;
                if (IsAuxiliary(lower))
                    continue;
                Bounds b = r.bounds;
                Vector3 vp = cam.WorldToViewportPoint(b.center);
                if (vp.z < 0f || vp.x < -0.05f || vp.x > 1.05f || vp.y < -0.05f || vp.y > 1.05f)
                    continue;

                if (!Classify(r, out Cat cat, out UnityEngine.Object keyObj, out string label))
                    continue;

                long key = ((long)cat << 40) ^ (uint)(keyObj != null ? keyObj.GetInstanceID() : 0);
                float area = b.size.x * b.size.y;
                if (keyToItem.TryGetValue(key, out int idx))
                {
                    if (area <= bestArea[key])
                        continue;
                    bestArea[key] = area;
                    items[idx] = MakeItem(r, label, isFinal, cat);
                    continue;
                }
                if (items.Count >= MaxItems)
                    continue;
                keyToItem[key] = items.Count;
                bestArea[key] = area;
                items.Add(MakeItem(r, label, isFinal, cat));
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].isFinal) finalCount++;
                else placeholderCount++;
            }
        }

        private static bool IsAuxiliary(string rendererName)
        {
            return rendererName.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) >= 0
                || rendererName.IndexOf("glow", StringComparison.OrdinalIgnoreCase) >= 0
                || rendererName.IndexOf("telegraph", StringComparison.OrdinalIgnoreCase) >= 0
                || rendererName.IndexOf("silhouette", StringComparison.OrdinalIgnoreCase) >= 0
                || rendererName.IndexOf("halo", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Item MakeItem(SpriteRenderer r, string label, Func<UnityEngine.Object, bool> isFinal, Cat cat)
        {
            bool fin = false;
            if (isFinal != null)
            {
                Sprite s = r.sprite;
                fin = isFinal(s);
            }
            return new Item { renderer = r, name = label, isFinal = fin, backdrop = cat == Cat.Backdrop };
        }

        private static bool Classify(SpriteRenderer r, out Cat cat, out UnityEngine.Object key, out string label)
        {
            key = null;
            label = "";
            cat = Cat.Player;

            ArmVisual arm = r.GetComponentInParent<ArmVisual>();
            if (arm != null)
            {
                cat = Cat.Arm;
                key = arm;
                label = arm.Data != null ? Nice(arm.Data.name) + " arm" : "Arm";
                return true;
            }
            Projectile p = r.GetComponentInParent<Projectile>();
            if (p != null)
            {
                bool hostile = IsHostile(p);
                cat = hostile ? Cat.EnemyBullet : Cat.PlayerBullet;
                key = null;
                label = hostile ? "Enemy bullet" : "Player bullet";
                return true;
            }
            Enemy e = r.GetComponentInParent<Enemy>();
            if (e != null)
            {
                bool boss = e.GetComponent<BossController>() != null;
                cat = boss ? Cat.Boss : Cat.Enemy;
                key = e.Data;
                label = (boss ? "Boss " : "") + (e.Data != null ? Nice(e.Data.name) : "Enemy");
                return true;
            }
            Obstacle o = r.GetComponentInParent<Obstacle>();
            if (o != null)
            {
                cat = Cat.Obstacle;
                key = o.Data;
                label = o.Data != null ? Nice(o.Data.name) : "Obstacle";
                return true;
            }
            Trap t = r.GetComponentInParent<Trap>();
            if (t != null)
            {
                cat = Cat.Trap;
                key = t.Data;
                label = t.Data != null ? Nice(t.Data.name) : "Trap";
                return true;
            }
            if (r.GetComponentInParent<CoinPickup>() != null)
            {
                cat = Cat.Coin;
                label = "Coin";
                return true;
            }
            AmmoPickup ap = r.GetComponentInParent<AmmoPickup>();
            if (ap != null)
            {
                cat = Cat.AmmoPickup;
                key = ap.Ammo;
                label = ap.Ammo != null ? ap.Ammo.DisplayName + " pickup" : "Ammo pickup";
                return true;
            }
            if (r.GetComponentInParent<PlayerHealth>() != null)
            {
                cat = Cat.Player;
                label = "Player";
                return true;
            }
            if (r.GetComponentInParent<ArenaScenery>() != null)
            {
                cat = Cat.Backdrop;
                label = "Arena backdrop";
                return true;
            }
            return false;
        }

        private static bool IsHostile(Projectile p)
        {
            if (hostileField == null)
                hostileField = typeof(Projectile).GetField("hostile", BindingFlags.Instance | BindingFlags.NonPublic);
            return hostileField != null && (bool)hostileField.GetValue(p);
        }

        private static string Nice(string assetName)
        {
            foreach (string prefix in new[] { "Enemy_", "Arm_", "Obstacle_", "Trap_", "Boss_" })
                if (assetName.StartsWith(prefix, StringComparison.Ordinal))
                    return assetName.Substring(prefix.Length);
            return assetName;
        }

        // ---- drawing ---------------------------------------------------------------------------------------------------

        private void BuildStyles()
        {
            if (styleHeight == Screen.height && statusStyle != null)
                return;
            styleHeight = Screen.height;
            float s = Mathf.Clamp(Screen.height / 1080f, 0.6f, 2.5f);
            statusStyle = new GUIStyle(GUI.skin.label)
            {
                font = PlaceholderAnnotation.ChipFont,
                fontSize = Mathf.RoundToInt(15 * s),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                normal = { textColor = PlaceholderAnnotation.Marble },
            };
            nameStyle = new GUIStyle(statusStyle)
            {
                font = PlaceholderAnnotation.NameFont,
                fontSize = Mathf.RoundToInt(14 * s),
                normal = { textColor = PlaceholderAnnotation.InkSoil },
            };
            legendTitle = new GUIStyle(statusStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.RoundToInt(18 * s),
                normal = { textColor = PlaceholderAnnotation.InkSoil },
            };
            legendText = new GUIStyle(nameStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.RoundToInt(15 * s),
            };
        }

        private void DrawLabel(Camera cam, Item it, float scale)
        {
            Bounds b = it.renderer.bounds;
            Vector3 top = it.backdrop ? b.center + new Vector3(0f, b.extents.y * 0.55f, 0f) : new Vector3(b.center.x, b.max.y, b.center.z);
            Vector3 sp = cam.WorldToScreenPoint(top);
            if (sp.z < 0f)
                return;

            string status = it.isFinal ? "FINAL ART" : "PLACEHOLDER";
            float h = 26f * scale;
            float nameW = Mathf.Max(60f * scale, it.name.Length * 8.2f * scale + 16f * scale);
            float statW = (it.isFinal ? 92f : 118f) * scale;
            float w = nameW + statW;

            float x = Mathf.Clamp(sp.x - w * 0.5f, 6f, Screen.width - w - 6f);
            float y = Screen.height - sp.y - h - 6f * scale;
            y = Mathf.Clamp(y, 6f, Screen.height - h - 6f);

            DrawChip(new Rect(x, y, nameW, h), new Rect(x + nameW, y, statW, h), it.name, status, it.isFinal, scale);
        }

        private void DrawChip(Rect left, Rect right, string nameText, string statusText, bool fin, float scale)
        {
            float outline = 2f * scale;
            float shadow = 4f * scale;
            Rect whole = new Rect(left.x, left.y, left.width + right.width, left.height);
            Color ink = PlaceholderAnnotation.InkSoil;

            Fill(new Rect(whole.x + shadow, whole.y + shadow, whole.width, whole.height), new Color(ink.r, ink.g, ink.b, 0.85f));
            Fill(new Rect(whole.x - outline, whole.y - outline, whole.width + outline * 2f, whole.height + outline * 2f), ink);
            Fill(left, PlaceholderAnnotation.Marble);
            Fill(right, fin ? PlaceholderAnnotation.Leaf : PlaceholderAnnotation.Carrot);
            Fill(new Rect(right.x - outline * 0.5f, right.y, outline, right.height), ink);

            GUI.Label(left, nameText, nameStyle);
            GUI.Label(right, statusText, statusStyle);
        }

        private void DrawLegend(float scale)
        {
            string audio = string.IsNullOrEmpty(audioLine) ? "AUDIO: provenance unavailable in this build" : audioLine;
            float pad = 12f * scale;
            float lineH = 28f * scale;
            float w = 400f * scale;
            float h = pad * 2f + lineH * 3.2f;
            Rect box = new Rect(16f * scale, 16f * scale, w, h);
            Color ink = PlaceholderAnnotation.InkSoil;

            Fill(new Rect(box.x + 6f * scale, box.y + 6f * scale, box.width, box.height), new Color(ink.r, ink.g, ink.b, 0.85f));
            Fill(new Rect(box.x - 3f * scale, box.y - 3f * scale, box.width + 6f * scale, box.height + 6f * scale), ink);
            Fill(box, PlaceholderAnnotation.Marble);

            float y = box.y + pad * 0.6f;
            GUI.Label(new Rect(box.x + pad, y, w - pad * 2f, lineH), "ART STATUS ON SCREEN", legendTitle);
            y += lineH;

            float chipW = 118f * scale;
            float chipH = 24f * scale;
            Rect fin = new Rect(box.x + pad, y + 2f * scale, chipW * 0.85f, chipH);
            Fill(new Rect(fin.x - 2f * scale, fin.y - 2f * scale, fin.width + 4f * scale, fin.height + 4f * scale), ink);
            Fill(fin, PlaceholderAnnotation.Leaf);
            GUI.Label(fin, "FINAL ART", statusStyle);
            GUI.Label(new Rect(fin.xMax + 8f * scale, y, 40f * scale, lineH), finalCount.ToString(), legendTitle);

            Rect ph = new Rect(box.x + pad + 170f * scale, y + 2f * scale, chipW, chipH);
            Fill(new Rect(ph.x - 2f * scale, ph.y - 2f * scale, ph.width + 4f * scale, ph.height + 4f * scale), ink);
            Fill(ph, PlaceholderAnnotation.Carrot);
            GUI.Label(ph, "PLACEHOLDER", statusStyle);
            GUI.Label(new Rect(ph.xMax + 8f * scale, y, 40f * scale, lineH), placeholderCount.ToString(), legendTitle);
            y += lineH;

            GUI.Label(new Rect(box.x + pad, y, w - pad * 2f, lineH), audio, legendText);
        }

        private static void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
#endif
