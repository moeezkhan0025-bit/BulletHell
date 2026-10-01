using System;
using System.Collections.Generic;
using System.IO;
using BulletHell.Capture;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace BulletHell.EditorTools.Capture
{
    /// <summary>
    /// Editor side of the art provenance: gathers the facts about an asset from the AssetDatabase and the docs, then lets the data-driven
    /// <see cref="ArtProvenanceEngine"/> decide FINAL or PLACEHOLDER. Rules live in Assets/Data/Capture/ArtProvenanceRules.asset.
    /// Facts used: asset path, an ArtSource master PNG with the same relative path (Tools/export_art.ps1 mapping), Docs/ART_CHECKLIST.md
    /// check marks, Docs/CREDITS.md audio entries.
    /// </summary>
    public static class ArtProvenance
    {
        private static ArtProvenanceRules rules;
        private static ChecklistIndex checklist;
        private static string creditsText;
        private static readonly Dictionary<string, ProvenanceResult> pathCache = new Dictionary<string, ProvenanceResult>();
        private static readonly Dictionary<int, ProvenanceResult> objectCache = new Dictionary<int, ProvenanceResult>();

        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        public static ArtProvenanceRules Rules
        {
            get
            {
                if (rules == null)
                    rules = LoadOrCreateRules();
                return rules;
            }
        }

        [MenuItem("BulletHell/Capture/Create Art Provenance Rules")]
        public static void CreateRulesMenu()
        {
            ArtProvenanceRules r = LoadOrCreateRules();
            Selection.activeObject = r;
            EditorGUIUtility.PingObject(r);
        }

        public static ArtProvenanceRules LoadOrCreateRules()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ArtProvenanceRules>(ArtProvenanceRules.AssetPath);
            if (existing != null)
                return existing;
            EnsureFolder("Assets/Data/Capture");
            var created = ArtProvenanceRules.CreateDefault();
            AssetDatabase.CreateAsset(created, ArtProvenanceRules.AssetPath);
            AssetDatabase.SaveAssets();
            return created;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>Forget cached verdicts and re-read the docs (the Art Slot Board calls this on Refresh).</summary>
        public static void Invalidate()
        {
            rules = null;
            checklist = null;
            creditsText = null;
            pathCache.Clear();
            objectCache.Clear();
        }

        private static ChecklistIndex Checklist
        {
            get
            {
                if (checklist == null)
                    checklist = ChecklistIndex.Parse(ReadDoc(Rules.checklistPath));
                return checklist;
            }
        }

        private static string Credits
        {
            get
            {
                if (creditsText == null)
                    creditsText = ReadDoc(Rules.creditsPath) ?? "";
                return creditsText;
            }
        }

        private static string ReadDoc(string relativePath)
        {
            try
            {
                string full = Path.Combine(ProjectRoot, relativePath);
                return File.Exists(full) ? File.ReadAllText(full) : "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        // ---- classification --------------------------------------------------------------------------------------------

        public static ArtAssetKind KindOf(UnityEngine.Object obj)
        {
            if (obj is AudioClip) return ArtAssetKind.Audio;
            if (obj is Sprite || obj is Texture2D || obj is SpriteLibraryAsset) return ArtAssetKind.Sprite;
            return ArtAssetKind.Other;
        }

        /// <summary>Path of the source file of an object (a sprite's texture, a material's main texture). Empty for runtime or built-in objects.</summary>
        public static string PathOf(UnityEngine.Object obj)
        {
            if (obj == null) return "";
            string p = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(p) && obj is Sprite s && s.texture != null)
                p = AssetDatabase.GetAssetPath(s.texture);
            if (string.IsNullOrEmpty(p) && obj is Material m && m.mainTexture != null)
                p = AssetDatabase.GetAssetPath(m.mainTexture);
            if (!string.IsNullOrEmpty(p) && !p.StartsWith("Assets/", StringComparison.Ordinal))
                return "";   // Library/unity default resources, packages
            return p ?? "";
        }

        public static ProvenanceResult ClassifyPath(string assetPath, ArtAssetKind kind)
        {
            string key = kind + "|" + assetPath;
            if (pathCache.TryGetValue(key, out ProvenanceResult cached))
                return cached;
            ProvenanceResult result = ArtProvenanceEngine.Classify(Rules, BuildFacts(assetPath, kind), Checklist);
            pathCache[key] = result;
            return result;
        }

        public static ProvenanceFacts BuildFacts(string assetPath, ArtAssetKind kind)
        {
            var f = new ProvenanceFacts { assetPath = assetPath ?? "", kind = kind, isBuiltIn = string.IsNullOrEmpty(assetPath) };
            if (f.isBuiltIn)
                return f;

            ArtProvenanceRules r = Rules;
            string artPrefix = r.artFolder.TrimEnd('/') + "/";
            if (assetPath.StartsWith(artPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string rel = assetPath.Substring(artPrefix.Length);
                f.hasArtSourceMaster = File.Exists(Path.Combine(ProjectRoot, r.artSourceFolder, rel));
            }
            if (kind == ArtAssetKind.Audio)
                f.mentionedInCredits = CreditsMentions(assetPath);
            return f;
        }

        private static bool CreditsMentions(string audioPath)
        {
            string stem = Path.GetFileNameWithoutExtension(audioPath);
            int us = stem.LastIndexOf('_');
            if (us > 0 && int.TryParse(stem.Substring(us + 1), out _))
                stem = stem.Substring(0, us);
            return stem.Length > 2 && Credits.IndexOf(stem, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Verdict for a sprite, texture, audio clip, sprite library, material or prefab. Containers are final only when everything they show is.</summary>
        public static ProvenanceResult Classify(UnityEngine.Object obj)
        {
            if (obj == null)
                return new ProvenanceResult(ArtStatus.Placeholder, "empty", "Empty slot");
            int id = obj.GetInstanceID();
            if (objectCache.TryGetValue(id, out ProvenanceResult cached))
                return cached;

            ProvenanceResult result;
            if (obj is SpriteLibraryAsset || obj is GameObject)
                result = ClassifyContainer(obj);
            else
                result = ClassifyPath(PathOf(obj), obj is Material ? ArtAssetKind.Sprite : KindOf(obj));
            objectCache[id] = result;
            return result;
        }

        private static ProvenanceResult ClassifyContainer(UnityEngine.Object container)
        {
            var sprites = new List<Sprite>();
            if (container is GameObject go)
            {
                foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                    if (sr.sprite != null)
                        sprites.Add(sr.sprite);
            }
            else
            {
                var so = new SerializedObject(container);
                SerializedProperty p = so.GetIterator();
                bool enter = true;
                while (p.Next(enter))
                {
                    enter = p.propertyType == SerializedPropertyType.Generic;
                    if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue is Sprite s)
                        sprites.Add(s);
                }
            }
            if (sprites.Count == 0)
                return ClassifyPath(AssetDatabase.GetAssetPath(container), ArtAssetKind.Other);

            int placeholders = 0;
            foreach (Sprite s in sprites)
                if (!Classify(s).IsFinal)
                    placeholders++;
            if (placeholders == 0)
                return new ProvenanceResult(ArtStatus.Final, "container", "All " + sprites.Count + " sprites inside are final");
            return new ProvenanceResult(ArtStatus.Placeholder, "container", placeholders + " of " + sprites.Count + " sprites inside are placeholders");
        }

        /// <summary>The audio line of the annotation legend, derived from the verdict of every clip under Assets.</summary>
        public static string AudioLegendText()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });
            int total = 0, placeholders = 0;
            foreach (string g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                total++;
                if (!ClassifyPath(path, ArtAssetKind.Audio).IsFinal)
                    placeholders++;
            }
            if (total == 0)
                return "AUDIO: no clips in the project";
            if (placeholders == total)
                return "AUDIO: all " + total + " clips are CC0 placeholders";
            return "AUDIO: " + placeholders + " of " + total + " clips are CC0 placeholders";
        }

        // ---- import-settings text --------------------------------------------------------------------------------------

        public static string PixelSize(UnityEngine.Object obj)
        {
            switch (obj)
            {
                case Sprite s: return Mathf.RoundToInt(s.rect.width) + "x" + Mathf.RoundToInt(s.rect.height);
                case Texture t: return t.width + "x" + t.height;
                case AudioClip a: return a.length.ToString("0.00") + " s";
                default: return "";
            }
        }

        public static string ImportSummary(UnityEngine.Object obj)
        {
            if (obj == null) return "";
            string path = PathOf(obj);
            if (string.IsNullOrEmpty(path)) return "built-in";
            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (importer is TextureImporter ti)
            {
                string pivot = "";
                if (obj is Sprite s && s.rect.width > 0)
                    pivot = ", pivot (" + (s.pivot.x / s.rect.width).ToString("0.00") + "," + (s.pivot.y / s.rect.height).ToString("0.00") + ")";
                return PixelSize(obj) + " px, PPU " + ti.spritePixelsPerUnit.ToString("0.#") + ", " + ti.textureCompression + ", " + ti.spriteImportMode + pivot;
            }
            if (importer is AudioImporter ai)
            {
                AudioImporterSampleSettings d = ai.defaultSampleSettings;
                return (ai.forceToMono ? "mono" : "stereo") + " " + d.loadType + ", " + d.compressionFormat + " q" + d.quality.ToString("0.0") + ", " + PixelSize(obj);
            }
            return "";
        }
    }
}
