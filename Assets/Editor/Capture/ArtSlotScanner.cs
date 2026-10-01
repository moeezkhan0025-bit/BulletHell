using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BulletHell.Capture;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace BulletHell.EditorTools.Capture
{
    /// <summary>One place where the game references a sprite, sprite library, texture or audio clip.</summary>
    public sealed class ArtSlot
    {
        public ArtSection section;
        public ArtAssetKind kind;
        public string ownerPath;
        public string ownerName;
        public string ownerType;
        public string propertyPath;
        public UnityEngine.Object owner;
        public UnityEngine.Object current;
        public bool empty;
        public ArtStatus status;
        public string reason;
        public string size;
        public string spec;
        public string now;

        public string SlotLabel => ownerName + "  >  " + propertyPath;
        public bool IsFinal => status == ArtStatus.Final;
    }

    /// <summary>
    /// Finds every object reference to a Sprite / Texture2D / SpriteLibraryAsset / AudioClip in the data assets (Assets/Data, Assets/Resources) and
    /// prefabs (Assets/Prefabs) by SerializedProperty iteration; no field names are hard-coded.
    /// </summary>
    public static class ArtSlotScanner
    {
        private static readonly string[] DataFolders = { "Assets/Data", "Assets/Resources" };
        private static readonly string[] PrefabFolders = { "Assets/Prefabs" };

        public static List<ArtSlot> Scan()
        {
            ArtProvenanceRules rules = ArtProvenance.Rules;
            var slots = new List<ArtSlot>(512);

            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", DataFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                    continue;
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null)
                    continue;
                ScanObject(asset, path, asset.name, slots, rules, true);
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", PrefabFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;
                foreach (Component c in prefab.GetComponentsInChildren<Component>(true))
                {
                    if (c == null || c is Transform)
                        continue;
                    string owner = Path.GetFileName(path) + " : " + RelativePath(c.transform, prefab.transform) + " (" + c.GetType().Name + ")";
                    ScanObject(c, path, owner, slots, rules, false);
                }
            }

            slots.Sort((a, b) =>
            {
                int s = a.section.CompareTo(b.section);
                if (s != 0) return s;
                s = string.Compare(a.ownerPath, b.ownerPath, StringComparison.OrdinalIgnoreCase);
                return s != 0 ? s : string.Compare(a.propertyPath, b.propertyPath, StringComparison.Ordinal);
            });
            return slots;
        }

        private static string RelativePath(Transform t, Transform root)
        {
            if (t == root) return t.name;
            var sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null && p != root; p = p.parent)
                sb.Insert(0, p.name + "/");
            return sb.ToString();
        }

        private static void ScanObject(UnityEngine.Object target, string ownerPath, string ownerName, List<ArtSlot> slots, ArtProvenanceRules rules, bool includeEmpty)
        {
            SerializedObject so;
            try { so = new SerializedObject(target); }
            catch (Exception) { return; }

            SerializedProperty p = so.GetIterator();
            bool enter = true;
            while (p.Next(enter))
            {
                enter = p.propertyType == SerializedPropertyType.Generic;
                if (p.propertyType != SerializedPropertyType.ObjectReference)
                    continue;
                if (p.name == "m_Script")
                    continue;

                UnityEngine.Object value = p.objectReferenceValue;
                bool isArt;
                ArtAssetKind kind;
                if (value != null)
                {
                    kind = ArtProvenance.KindOf(value);
                    isArt = kind != ArtAssetKind.Other;
                }
                else
                {
                    string declared = DeclaredType(p.type);
                    kind = declared == "AudioClip" ? ArtAssetKind.Audio : ArtAssetKind.Sprite;
                    isArt = declared == "Sprite" || declared == "Texture2D" || declared == "SpriteLibraryAsset" || declared == "AudioClip";
                }
                if (!isArt)
                    continue;
                if (value == null && !includeEmpty)
                    continue;

                slots.Add(MakeSlot(target, ownerPath, ownerName, p.propertyPath, value, kind, rules));
            }
        }

        private static string DeclaredType(string propertyType)
        {
            // "PPtr<Sprite>" or "PPtr<$MyType>"
            int a = propertyType.IndexOf('<');
            int b = propertyType.LastIndexOf('>');
            if (a < 0 || b <= a) return propertyType;
            return propertyType.Substring(a + 1, b - a - 1).TrimStart('$');
        }

        private static ArtSlot MakeSlot(UnityEngine.Object owner, string ownerPath, string ownerName, string propertyPath,
                                        UnityEngine.Object value, ArtAssetKind kind, ArtProvenanceRules rules)
        {
            var slot = new ArtSlot
            {
                owner = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ownerPath),
                ownerPath = ownerPath,
                ownerName = ownerName,
                ownerType = owner.GetType().Name,
                propertyPath = propertyPath,
                current = value,
                empty = value == null,
                kind = kind,
            };
            string assetPath = value != null ? ArtProvenance.PathOf(value) : "";
            slot.section = ArtProvenanceEngine.SectionOf(rules, kind, ownerPath, slot.ownerType, propertyPath);
            if (value == null)
            {
                slot.status = ArtStatus.Placeholder;
                slot.reason = "Empty slot: the game draws a stand-in shape or nothing";
                slot.size = "";
                slot.now = "";
            }
            else
            {
                ProvenanceResult r = ArtProvenance.Classify(value);
                slot.status = r.status;
                slot.reason = r.reason;
                slot.size = ArtProvenance.PixelSize(value);
                slot.now = ArtProvenance.ImportSummary(value);
            }
            string needs = ArtProvenanceEngine.SpecFor(rules, slot.section, ownerPath, assetPath, propertyPath);
            slot.spec = string.IsNullOrEmpty(slot.now) ? needs : needs + "  [now: " + slot.now + "]";
            return slot;
        }

        // ---- export ----------------------------------------------------------------------------------------------------

        public static string ExportMarkdown(List<ArtSlot> slots)
        {
            string dir = Path.Combine(ArtProvenance.ProjectRoot, "Captures", "Editor");
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, "art_slot_board.md");

            var sb = new StringBuilder();
            int fin = 0, ph = 0;
            foreach (ArtSlot s in slots)
                if (s.IsFinal) fin++; else ph++;
            sb.AppendLine("# Art slot board");
            sb.AppendLine();
            sb.AppendLine("Generated " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " by BulletHell/Capture/Art Slot Board. Verdicts come from `ArtProvenance` (rules: `Assets/Data/Capture/ArtProvenanceRules.asset`).");
            sb.AppendLine();
            sb.AppendLine("**" + slots.Count + " slots: " + fin + " FINAL, " + ph + " PLACEHOLDER** (empty slots count as placeholder).");
            sb.AppendLine();
            sb.AppendLine("| Section | Final | Placeholder |");
            sb.AppendLine("|---|---|---|");
            foreach (ArtSection sec in Enum.GetValues(typeof(ArtSection)))
            {
                int f = 0, p = 0;
                foreach (ArtSlot s in slots)
                    if (s.section == sec) { if (s.IsFinal) f++; else p++; }
                if (f + p > 0)
                    sb.AppendLine("| " + sec + " | " + f + " | " + p + " |");
            }
            foreach (ArtSection sec in Enum.GetValues(typeof(ArtSection)))
            {
                var rows = slots.FindAll(s => s.section == sec);
                if (rows.Count == 0) continue;
                sb.AppendLine();
                sb.AppendLine("## " + sec);
                sb.AppendLine();
                sb.AppendLine("| Slot | Current asset | Status | Size | Why | Spec a final asset must meet |");
                sb.AppendLine("|---|---|---|---|---|---|");
                foreach (ArtSlot s in rows)
                {
                    string cur = s.current != null ? "`" + AssetDatabase.GetAssetPath(s.current) + (s.current is Sprite ? " (" + s.current.name + ")" : "") + "`" : "(empty)";
                    sb.AppendLine("| " + Esc(s.SlotLabel) + " | " + Esc(cur) + " | " + (s.IsFinal ? "FINAL" : "PLACEHOLDER") + " | " + s.size + " | " + Esc(s.reason) + " | " + Esc(s.spec) + " |");
                }
            }
            File.WriteAllText(file, sb.ToString(), new UTF8Encoding(false));
            return file;
        }

        private static string Esc(string s) => (s ?? "").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }
}
