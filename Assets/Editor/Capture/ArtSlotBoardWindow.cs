using System;
using System.Collections.Generic;
using BulletHell.Capture;
using UnityEditor;
using UnityEngine;

namespace BulletHell.EditorTools.Capture
{
    /// <summary>
    /// Every place the game references a sprite, sprite library or audio clip, with FINAL / PLACEHOLDER status and the spec a final asset must meet.
    /// Menu: BulletHell/Capture/Art Slot Board. Export: Captures/Editor/art_slot_board.md.
    /// </summary>
    public sealed class ArtSlotBoardWindow : EditorWindow
    {
        private const float RowHeight = 42f;
        private const float WSection = 82f, WSlot = 280f, WAsset = 220f, WStatus = 112f, WSize = 78f;

        // VoxKit palette tokens (Assets/Data/UI/VoxVegetallis.asset)
        private static readonly Color Ink = new Color32(0x2E, 0x1F, 0x14, 255);
        private static readonly Color Marble = new Color32(0xF4, 0xEE, 0xDC, 255);
        private static readonly Color MarbleShade = new Color32(0xE2, 0xD6, 0xBC, 255);
        private static readonly Color Leaf = new Color32(0x5E, 0x9F, 0x3E, 255);
        private static readonly Color Carrot = new Color32(0xE5, 0x7A, 0x24, 255);
        private static readonly Color Gold = new Color32(0xE9, 0xB6, 0x3A, 255);

        private List<ArtSlot> slots = new List<ArtSlot>();
        private readonly List<ArtSlot> view = new List<ArtSlot>();
        private Vector2 scroll;
        private string search = "";
        private int statusFilter;          // 0 all, 1 final, 2 placeholder
        private int sectionFilter = -1;    // -1 all
        private bool hideEmpty;

        private GUIStyle chipStyle, cellStyle, specStyle, linkStyle, headerStyle, bigStyle, sectionChipStyle;
        private Texture2D white;

        [MenuItem("BulletHell/Capture/Art Slot Board")]
        public static void Open()
        {
            var w = GetWindow<ArtSlotBoardWindow>("Art Slot Board");
            w.minSize = new Vector2(1000, 560);
            w.Refresh();
            w.Show();
        }

        private void OnEnable()
        {
            if (slots.Count == 0)
                Refresh();
        }

        private void Refresh()
        {
            ArtProvenance.Invalidate();
            ArtProvenance.LoadOrCreateRules();
            slots = ArtSlotScanner.Scan();
            ApplyFilter();
            Repaint();
        }

        private void ApplyFilter()
        {
            view.Clear();
            string q = search.Trim();
            foreach (ArtSlot s in slots)
            {
                if (statusFilter == 1 && !s.IsFinal) continue;
                if (statusFilter == 2 && s.IsFinal) continue;
                if (sectionFilter >= 0 && (int)s.section != sectionFilter) continue;
                if (hideEmpty && s.empty) continue;
                if (q.Length > 0)
                {
                    string hay = s.SlotLabel + " " + (s.current != null ? s.current.name : "") + " " + s.section + " " + s.spec;
                    if (hay.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                }
                view.Add(s);
            }
        }

        private void BuildStyles()
        {
            if (white == null)
            {
                white = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                white.SetPixel(0, 0, Color.white);
                white.Apply();
            }
            if (chipStyle != null) return;
            chipStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 11, normal = { textColor = Marble } };
            sectionChipStyle = new GUIStyle(chipStyle) { fontSize = 11, normal = { textColor = Ink } };
            cellStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip, fontSize = 11, normal = { textColor = Ink } };
            specStyle = new GUIStyle(cellStyle) { wordWrap = true, fontSize = 10, alignment = TextAnchor.UpperLeft };
            linkStyle = new GUIStyle(cellStyle) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.12f, 0.33f, 0.62f) } };
            headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11, alignment = TextAnchor.MiddleLeft, normal = { textColor = Marble } };
            bigStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, alignment = TextAnchor.MiddleLeft, normal = { textColor = Ink } };
        }

        private void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, white);
            GUI.color = old;
        }

        private void Chip(Rect r, string text, Color bg, GUIStyle style)
        {
            Fill(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), Ink);
            Fill(r, bg);
            GUI.Label(r, text, style);
        }

        private void OnGUI()
        {
            BuildStyles();
            Fill(new Rect(0, 0, position.width, position.height), Marble);

            DrawToolbar();
            DrawSummary();
            DrawColumnHeader();
            DrawRows();
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(64))) Refresh();
            if (GUILayout.Button("Export markdown", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                string file = ArtSlotScanner.ExportMarkdown(slots);
                Debug.Log("Art slot board written to " + file);
                EditorUtility.RevealInFinder(file);
            }
            GUILayout.Space(8);
            EditorGUI.BeginChangeCheck();
            statusFilter = EditorGUILayout.Popup(statusFilter, new[] { "All status", "FINAL only", "PLACEHOLDER only" }, EditorStyles.toolbarPopup, GUILayout.Width(120));
            var names = new List<string> { "All sections" };
            foreach (ArtSection s in Enum.GetValues(typeof(ArtSection))) names.Add(s.ToString());
            int idx = EditorGUILayout.Popup(sectionFilter + 1, names.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(110));
            sectionFilter = idx - 1;
            hideEmpty = GUILayout.Toggle(hideEmpty, "Hide empty slots", EditorStyles.toolbarButton, GUILayout.Width(110));
            GUILayout.FlexibleSpace();
            search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.Width(220));
            if (EditorGUI.EndChangeCheck())
                ApplyFilter();
            GUILayout.EndHorizontal();
        }

        private void DrawSummary()
        {
            int fin = 0;
            foreach (ArtSlot s in slots) if (s.IsFinal) fin++;
            int ph = slots.Count - fin;

            Rect band = GUILayoutUtility.GetRect(position.width, 74f);
            Fill(band, MarbleShade);
            Fill(new Rect(band.x, band.yMax - 3, band.width, 3), Ink);

            float x = band.x + 14, y = band.y + 8;
            Chip(new Rect(x, y, 110, 28), "FINAL  " + fin, Leaf, chipStyle);
            x += 122;
            Chip(new Rect(x, y, 150, 28), "PLACEHOLDER  " + ph, Carrot, chipStyle);
            x += 166;
            GUI.Label(new Rect(x, y - 2, 300, 32), slots.Count + " slots  (" + view.Count + " shown)", bigStyle);

            // per section counts, click to filter
            x = band.x + 14;
            y = band.y + 42;
            foreach (ArtSection sec in Enum.GetValues(typeof(ArtSection)))
            {
                int f = 0, p = 0;
                foreach (ArtSlot s in slots) if (s.section == sec) { if (s.IsFinal) f++; else p++; }
                if (f + p == 0) continue;
                float w = 118;
                var r = new Rect(x, y, w, 24);
                Color bg = (int)sec == sectionFilter ? Gold : Marble;
                Fill(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), Ink);
                Fill(r, bg);
                GUI.Label(new Rect(r.x + 4, r.y, 62, r.height), sec.ToString(), sectionChipStyle);
                // tiny F / P counters
                Fill(new Rect(r.x + 66, r.y + 4, 22, 16), Leaf);
                GUI.Label(new Rect(r.x + 66, r.y + 4, 22, 16), f.ToString(), chipStyle);
                Fill(new Rect(r.x + 90, r.y + 4, 24, 16), Carrot);
                GUI.Label(new Rect(r.x + 90, r.y + 4, 24, 16), p.ToString(), chipStyle);
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    sectionFilter = (int)sec == sectionFilter ? -1 : (int)sec;
                    ApplyFilter();
                    Event.current.Use();
                }
                x += w + 6;
            }
        }

        private void DrawColumnHeader()
        {
            Rect h = GUILayoutUtility.GetRect(position.width, 24f);
            Fill(h, Ink);
            float x = h.x + 6;
            GUI.Label(new Rect(x, h.y, WSection, h.height), "SECTION", headerStyle); x += WSection;
            GUI.Label(new Rect(x, h.y, WSlot, h.height), "SLOT (asset > property)", headerStyle); x += WSlot;
            GUI.Label(new Rect(x, h.y, WAsset, h.height), "CURRENT ASSET", headerStyle); x += WAsset;
            GUI.Label(new Rect(x, h.y, WStatus, h.height), "STATUS", headerStyle); x += WStatus;
            GUI.Label(new Rect(x, h.y, WSize, h.height), "SIZE", headerStyle); x += WSize;
            GUI.Label(new Rect(x, h.y, 400, h.height), "SPEC A FINAL ASSET MUST MEET", headerStyle);
        }

        private void DrawRows()
        {
            Rect area = GUILayoutUtility.GetRect(position.width, 100f, GUILayout.ExpandHeight(true));
            float contentH = view.Count * RowHeight;
            var content = new Rect(0, 0, area.width - 16f, contentH);
            scroll = GUI.BeginScrollView(area, scroll, content);

            int first = Mathf.Max(0, (int)(scroll.y / RowHeight));
            int last = Mathf.Min(view.Count, first + (int)(area.height / RowHeight) + 2);
            float specW = Mathf.Max(200f, content.width - (6 + WSection + WSlot + WAsset + WStatus + WSize) - 8);

            for (int i = first; i < last; i++)
            {
                ArtSlot s = view[i];
                var row = new Rect(0, i * RowHeight, content.width, RowHeight);
                Fill(row, i % 2 == 0 ? Marble : new Color(0.93f, 0.89f, 0.78f, 1f));
                Fill(new Rect(row.x, row.yMax - 1, row.width, 1), new Color(Ink.r, Ink.g, Ink.b, 0.25f));

                float x = 6;
                // section tag
                GUI.Label(new Rect(x, row.y, WSection, RowHeight), s.section.ToString(), cellStyle);
                x += WSection;

                // slot (click pings the owner)
                var slotRect = new Rect(x, row.y, WSlot - 6, RowHeight);
                GUI.Label(slotRect, new GUIContent(s.SlotLabel, s.ownerPath + "\n" + s.propertyPath + "\n" + s.reason), cellStyle);
                if (Event.current.type == EventType.MouseDown && slotRect.Contains(Event.current.mousePosition) && s.owner != null)
                {
                    EditorGUIUtility.PingObject(s.owner);
                    Selection.activeObject = s.owner;
                    Event.current.Use();
                }
                x += WSlot;

                // current asset (click pings)
                var assetRect = new Rect(x, row.y, WAsset - 6, RowHeight);
                if (s.current != null)
                {
                    if (GUI.Button(assetRect, new GUIContent(s.current.name, AssetDatabase.GetAssetPath(s.current)), linkStyle))
                    {
                        EditorGUIUtility.PingObject(s.current);
                        Selection.activeObject = s.current;
                    }
                }
                else
                {
                    GUI.Label(assetRect, "(empty)", cellStyle);
                }
                x += WAsset;

                // status chip
                var chip = new Rect(x, row.y + 10, WStatus - 14, 22);
                Chip(chip, s.IsFinal ? "FINAL" : "PLACEHOLDER", s.IsFinal ? Leaf : Carrot, chipStyle);
                x += WStatus;

                GUI.Label(new Rect(x, row.y, WSize, RowHeight), s.empty ? "empty" : s.size, cellStyle);
                x += WSize;

                GUI.Label(new Rect(x, row.y + 2, specW, RowHeight - 3), new GUIContent(s.spec, s.spec), specStyle);
            }
            GUI.EndScrollView();
        }
    }
}
