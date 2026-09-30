using BulletHell.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// Builder helpers for the VOX VEGETALLIS screens (editor only). All positions are in 1080p canvas units, taken from the
    /// mockups (display px x 0.96). Everything created here is themed through UITheme components (ThemedImage, ThemedTrim,
    /// ThemedText, ThemedButton), so restyling later means editing the theme asset, not the screens.
    /// </summary>
    public static class VoxUi
    {
        public static UITheme Theme => UITheme.Current;

        public static RectTransform R(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform r, float l = 0f, float b = 0f, float rt = 0f, float t = 0f)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(l, b);
            r.offsetMax = new Vector2(-rt, -t);
        }

        /// <summary>Anchored to the top-left of the parent: x to the right, y DOWN from the top edge.</summary>
        public static RectTransform TopLeft(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
            return r;
        }

        /// <summary>Anchored to the top-right: x is the distance from the right edge, y from the top.</summary>
        public static RectTransform TopRight(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 1f);
            r.anchoredPosition = new Vector2(-x, -y);
            r.sizeDelta = new Vector2(w, h);
            return r;
        }

        public static RectTransform BottomLeft(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
            return r;
        }

        public static RectTransform BottomRight(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 0f);
            r.anchoredPosition = new Vector2(-x, y);
            r.sizeDelta = new Vector2(w, h);
            return r;
        }

        public static RectTransform TopCenter(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
            return r;
        }

        public static RectTransform Center(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
            return r;
        }

        /// <summary>Removes every child of a transform (immediately; used when a screen is rebuilt).</summary>
        public static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        public static Image Img(string name, Transform parent, Sprite sprite, Image.Type type = Image.Type.Simple, Color? color = null, bool raycast = false)
        {
            RectTransform r = R(name, parent);
            var image = r.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.color = color ?? Color.white;
            image.raycastTarget = raycast;
            if (type == Image.Type.Sliced && Theme != null)
                image.pixelsPerUnitMultiplier = Theme.BorderMultiplier;
            return image;
        }

        /// <summary>An Image whose sprite comes from a theme role (ThemedImage keeps it in sync).</summary>
        public static Image Themed(string name, Transform parent, ThemeRole role, bool raycast = false)
        {
            RectTransform r = R(name, parent);
            var image = r.gameObject.AddComponent<Image>();
            image.raycastTarget = raycast;
            r.gameObject.AddComponent<ThemedImage>().Role = role;
            return image;
        }

        /// <summary>A checker trim strip (tiled), sized by the caller.</summary>
        public static Image Trim(string name, Transform parent, TrimColor color)
        {
            RectTransform r = R(name, parent);
            var image = r.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            r.gameObject.AddComponent<ThemedTrim>().Color = color;
            return image;
        }

        public static TMP_Text Txt(string name, Transform parent, string text, float size, TextFont font, TextTone tone,
                                   TextAlignmentOptions align = TextAlignmentOptions.Center, bool caps = false, bool outline = false)
        {
            RectTransform r = R(name, parent);
            var label = r.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = align;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            var themed = r.gameObject.AddComponent<ThemedText>();
            themed.Font = font;
            themed.Tone = tone;
            themed.Caps = caps;
            themed.Outline = outline;
            return label;
        }

        /// <summary>A themed button: Image + Button + ThemedButton + CancelRelay, with a centered Lilita One label.</summary>
        public static Button Btn(string name, Transform parent, string label, float w, float h, float fontSize = 36f, ButtonKind kind = ButtonKind.Normal)
        {
            RectTransform r = R(name, parent);
            r.sizeDelta = new Vector2(w, h);
            var image = r.gameObject.AddComponent<Image>();
            var button = r.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            r.gameObject.AddComponent<CancelRelay>();
            TMP_Text text = Txt("Label", r, label, fontSize, TextFont.Button, TextTone.OnPanel);
            Stretch((RectTransform)text.transform, 0f, 10f, 0f, 0f);
            // Label color comes from ThemedButton (marble buttons: soil, primary: marble).
            Object.DestroyImmediate(text.GetComponent<ThemedText>());
            var themed = r.gameObject.AddComponent<ThemedButton>();
            themed.Kind = kind;
            return button;
        }

        /// <summary>The dark pill under the screens that holds the button prompts ("[Cross] Select  [Circle] Back").</summary>
        public static TMP_Text HintPill(string name, Transform parent, float x, float y, float w, float h, bool fromRight = false, bool fromTop = false)
        {
            RectTransform pill = R(name + "Pill", parent);
            if (fromTop)
                (fromRight ? (System.Func<RectTransform, float, float, float, float, RectTransform>)TopRight : TopLeft)(pill, x, y, w, h);
            else
                (fromRight ? (System.Func<RectTransform, float, float, float, float, RectTransform>)BottomRight : BottomLeft)(pill, x, y, w, h);
            var image = pill.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            var themed = pill.gameObject.AddComponent<ThemedImage>();
            themed.KeepColor = true;
            themed.Role = ThemeRole.HintBar;
            image.color = new Color(1f, 1f, 1f, 0.86f);
            var layout = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 4, 8);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fitter = pill.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            TMP_Text hint = Txt(name, pill, "", 26, TextFont.BodyBold, TextTone.OnDark, TextAlignmentOptions.Center);
            return hint;
        }

        public static void SetRef(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(property);
            if (p == null)
            {
                Debug.LogWarning("VoxUi: " + target.GetType().Name + " has no property " + property);
                return;
            }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray(Object target, string property, Object[] items)
        {
            var so = new SerializedObject(target);
            SerializedProperty array = so.FindProperty(property);
            if (array == null)
            {
                Debug.LogWarning("VoxUi: " + target.GetType().Name + " has no property " + property);
                return;
            }
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFloat(Object target, string property, float value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(property);
            if (p != null)
            {
                p.floatValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static void SetBool(Object target, string property, bool value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(property);
            if (p != null)
            {
                p.boolValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
