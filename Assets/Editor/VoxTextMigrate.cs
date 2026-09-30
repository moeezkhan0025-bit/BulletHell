using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// One-shot Text -> TextMeshPro migration for every scene and prefab.
    /// Phase A (run while scripts still declare Text fields): records every serialized reference to a legacy Text, then swaps
    /// each Text for a TextMeshProUGUI on the same GameObject (text, size, color, alignment, style, wrap, outline copied).
    /// Phase B (run after the scripts declare TMP_Text fields): restores the recorded references onto the new components.
    /// The reference list goes to the scratchpad JSON so it survives the recompile in between.
    /// </summary>
    public static class VoxTextMigrate
    {
        private const string Dump = "C:/Users/moeez/AppData/Local/Temp/claude/C--Dev-BulletHell/7ff0c1b1-2289-4cc6-bf6b-93cb4e9cbc8c/scratchpad/text_migration.json";
        private const string BodyFont = "Assets/Fonts/Nunito-SemiBold SDF.asset";
        private const string ButtonFont = "Assets/Fonts/LilitaOne SDF.asset";

        [Serializable]
        private class Ref
        {
            public string asset;
            public string owner;      // sibling-index path of the owner GameObject
            public string component;  // component type name
            public int componentIndex;
            public string property;   // serialized property path
            public string target;     // sibling-index path of the GameObject that held the Text
        }

        [Serializable]
        private class RefList
        {
            public List<Ref> refs = new List<Ref>();
        }

        private static string PathOf(Transform t)
        {
            var parts = new List<int>();
            for (; t != null; t = t.parent)
                parts.Add(t.GetSiblingIndex());
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static Transform Resolve(IEnumerable<GameObject> roots, string path)
        {
            // Root index is the sibling index among scene roots (or 0 for a prefab root).
            string[] parts = path.Split('/');
            var rootList = new List<GameObject>(roots);
            int rootIndex = int.Parse(parts[0]);
            if (rootIndex < 0 || rootIndex >= rootList.Count)
                return null;
            Transform t = rootList[rootIndex].transform;
            for (int i = 1; i < parts.Length; i++)
            {
                int idx = int.Parse(parts[i]);
                if (idx >= t.childCount)
                    return null;
                t = t.GetChild(idx);
            }
            return t;
        }

        private static List<string> AllAssets(out List<string> scenes, out List<string> prefabs)
        {
            scenes = new List<string>(Directory.GetFiles("Assets/Scenes", "*.unity"));
            prefabs = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
                prefabs.Add(AssetDatabase.GUIDToAssetPath(guid));
            for (int i = 0; i < scenes.Count; i++) scenes[i] = scenes[i].Replace('\\', '/');
            return null;
        }

        // ---------------------------------------------------------------- Phase A

        [MenuItem("BulletHell/Vox/Text Migration/A Capture refs and swap Text for TMP")]
        public static void PhaseA()
        {
            AllAssets(out var scenes, out var prefabs);
            var list = new RefList();
            int swapped = 0;
            int instancesSkipped = 0;

            foreach (string path in prefabs)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (root.GetComponentsInChildren<Text>(true).Length == 0)
                        continue;
                    var roots = new List<GameObject> { root };
                    Capture(path, roots, list);
                    swapped += Swap(roots, ref instancesSkipped);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            string previous = SceneManager.GetActiveScene().path;
            foreach (string path in scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                GameObject[] roots = scene.GetRootGameObjects();
                bool any = false;
                foreach (GameObject r in roots)
                    if (r.GetComponentsInChildren<Text>(true).Length > 0) { any = true; break; }
                if (!any)
                    continue;
                Capture(path, new List<GameObject>(roots), list);
                swapped += Swap(new List<GameObject>(roots), ref instancesSkipped);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            if (!string.IsNullOrEmpty(previous))
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);

            File.WriteAllText(Dump, JsonUtility.ToJson(list, true));
            AssetDatabase.SaveAssets();
            Debug.Log("Vox text migration A: " + swapped + " Text -> TMP, " + list.refs.Count + " references recorded, " +
                      instancesSkipped + " prefab-instance Texts skipped.");
        }

        private static void Capture(string asset, List<GameObject> roots, RefList list)
        {
            foreach (GameObject root in roots)
            {
                foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null || mb is Graphic)
                        continue;
                    var so = new SerializedObject(mb);
                    SerializedProperty it = so.GetIterator();
                    while (it.NextVisible(true))
                    {
                        if (it.propertyType != SerializedPropertyType.ObjectReference)
                            continue;
                        if (!(it.objectReferenceValue is Text text))
                            continue;
                        string type = mb.GetType().Name;
                        int index = 0;
                        foreach (var other in mb.GetComponents<MonoBehaviour>())
                        {
                            if (other == mb) break;
                            if (other != null && other.GetType() == mb.GetType()) index++;
                        }
                        list.refs.Add(new Ref
                        {
                            asset = asset,
                            owner = PathOf(mb.transform),
                            component = type,
                            componentIndex = index,
                            property = it.propertyPath,
                            target = PathOf(text.transform),
                        });
                    }
                }
            }
        }

        private static int Swap(List<GameObject> roots, ref int instancesSkipped)
        {
            var body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFont);
            var button = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ButtonFont);
            int swapped = 0;
            foreach (GameObject root in roots)
            {
                foreach (Text text in root.GetComponentsInChildren<Text>(true))
                {
                    GameObject go = text.gameObject;
                    if (PrefabUtility.IsPartOfPrefabInstance(go) && !PrefabUtility.IsAddedComponentOverride(text))
                    {
                        instancesSkipped++;
                        Debug.LogWarning("Vox text migration: Text on a prefab instance was left alone: " + go.name);
                        continue;
                    }

                    string content = text.text;
                    int size = text.fontSize;
                    Color color = text.color;
                    TextAnchor anchor = text.alignment;
                    FontStyle style = text.fontStyle;
                    bool wrap = text.horizontalOverflow == HorizontalWrapMode.Wrap;
                    bool overflowV = text.verticalOverflow == VerticalWrapMode.Overflow;
                    bool raycast = text.raycastTarget;
                    bool rich = text.supportRichText;
                    float lineSpacing = text.lineSpacing;
                    bool bestFit = text.resizeTextForBestFit;
                    int minSize = text.resizeTextMinSize;
                    int maxSize = text.resizeTextMaxSize;
                    bool enabled = text.enabled;
                    bool inButton = go.GetComponentInParent<Button>() != null;

                    Outline outline = go.GetComponent<Outline>();
                    Shadow shadow = go.GetComponent<Shadow>();
                    Color effectColor = outline != null ? outline.effectColor : shadow != null ? shadow.effectColor : Color.clear;
                    bool hasEffect = outline != null || shadow != null;

                    if (outline != null) UnityEngine.Object.DestroyImmediate(outline);
                    if (shadow != null) UnityEngine.Object.DestroyImmediate(shadow);
                    UnityEngine.Object.DestroyImmediate(text);

                    var tmp = go.AddComponent<TextMeshProUGUI>();
                    tmp.font = inButton ? button : body;
                    tmp.text = content;
                    tmp.fontSize = size;
                    tmp.color = color;
                    tmp.alignment = MapAlignment(anchor);
                    tmp.fontStyle = (style == FontStyle.Bold || style == FontStyle.BoldAndItalic ? FontStyles.Bold : FontStyles.Normal)
                                    | (style == FontStyle.Italic || style == FontStyle.BoldAndItalic ? FontStyles.Italic : FontStyles.Normal);
                    tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                    tmp.overflowMode = overflowV ? TextOverflowModes.Overflow : TextOverflowModes.Truncate;
                    tmp.raycastTarget = raycast;
                    tmp.richText = rich;
                    tmp.lineSpacing = (lineSpacing - 1f) * 100f;
                    if (bestFit)
                    {
                        tmp.enableAutoSizing = true;
                        tmp.fontSizeMin = minSize;
                        tmp.fontSizeMax = maxSize;
                    }
                    if (hasEffect)
                    {
                        tmp.outlineWidth = 0.2f;
                        tmp.outlineColor = effectColor;
                    }
                    tmp.enabled = enabled;
                    EditorUtility.SetDirty(go);
                    swapped++;
                }
            }
            return swapped;
        }

        private static TextAlignmentOptions MapAlignment(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        // ---------------------------------------------------------------- Phase B

        [MenuItem("BulletHell/Vox/Text Migration/B Restore references")]
        public static void PhaseB()
        {
            var list = JsonUtility.FromJson<RefList>(File.ReadAllText(Dump));
            var byAsset = new Dictionary<string, List<Ref>>();
            foreach (Ref r in list.refs)
            {
                if (!byAsset.TryGetValue(r.asset, out var l))
                    byAsset[r.asset] = l = new List<Ref>();
                l.Add(r);
            }

            int restored = 0, failed = 0;
            string previous = SceneManager.GetActiveScene().path;
            foreach (var pair in byAsset)
            {
                string path = pair.Key;
                if (path.EndsWith(".prefab"))
                {
                    GameObject root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        Restore(new List<GameObject> { root }, pair.Value, ref restored, ref failed);
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
                else
                {
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    Restore(new List<GameObject>(scene.GetRootGameObjects()), pair.Value, ref restored, ref failed);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            if (!string.IsNullOrEmpty(previous))
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            Debug.Log("Vox text migration B: " + restored + " references restored, " + failed + " failed.");
        }

        private static void Restore(List<GameObject> roots, List<Ref> refs, ref int restored, ref int failed)
        {
            foreach (Ref r in refs)
            {
                Transform owner = Resolve(roots, r.owner);
                Transform target = Resolve(roots, r.target);
                if (owner == null || target == null)
                {
                    failed++;
                    Debug.LogWarning("Vox text migration: cannot resolve " + r.asset + " " + r.owner + " -> " + r.target);
                    continue;
                }
                var tmp = target.GetComponent<TMP_Text>();
                MonoBehaviour owning = null;
                int seen = 0;
                foreach (var mb in owner.GetComponents<MonoBehaviour>())
                {
                    if (mb != null && mb.GetType().Name == r.component)
                    {
                        if (seen == r.componentIndex) { owning = mb; break; }
                        seen++;
                    }
                }
                if (owning == null || tmp == null)
                {
                    failed++;
                    Debug.LogWarning("Vox text migration: missing owner or TMP for " + r.asset + " " + r.component + "." + r.property);
                    continue;
                }
                var so = new SerializedObject(owning);
                SerializedProperty p = so.FindProperty(r.property);
                if (p == null)
                {
                    failed++;
                    Debug.LogWarning("Vox text migration: property gone " + r.component + "." + r.property);
                    continue;
                }
                p.objectReferenceValue = tmp;
                so.ApplyModifiedPropertiesWithoutUndo();
                if (p.objectReferenceValue == tmp) restored++;
                else
                {
                    failed++;
                    Debug.LogWarning("Vox text migration: type mismatch on " + r.component + "." + r.property);
                }
            }
        }
    }
}
