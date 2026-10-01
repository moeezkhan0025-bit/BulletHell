using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BulletHell.EditorTools.GameplayCapture
{
    /// <summary>
    /// R1 portfolio capture kit - puts the Game view at a fixed size (1920x1080) for a capture and restores it afterwards.
    /// Done by reflection on UnityEditor.GameViewSizes / UnityEditor.GameView (no public API): reuses an existing fixed-resolution
    /// entry of that size or adds a custom one, selects it, and on <see cref="Restore"/> selects the previous entry again and removes
    /// the entry it added. Every step is guarded: if Unity changes these internals the capture still works (the screenshot is then
    /// resampled, the Recorder forces its own size) and a warning is logged once.
    /// </summary>
    public static class RecordingGameView
    {
        private const string CustomName = "VV Capture";
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static int previousIndex = -1;
        private static int addedIndex = -1;
        private static bool applied;
        private static bool warned;

        public static bool IsApplied => applied;

        /// <summary>Selects a width x height Game view size. False when it could not (reflection failed or no Game view is open).</summary>
        public static bool Apply(int width, int height)
        {
            if (applied)
                return true;
            try
            {
                EditorWindow view = FindGameView();
                if (view == null)
                    return false;

                object group = CurrentGroup();
                Type groupType = group.GetType();
                MethodInfo getTotal = groupType.GetMethod("GetTotalCount", Any);
                MethodInfo getSize = groupType.GetMethod("GetGameViewSize", Any);
                int total = (int)getTotal.Invoke(group, null);

                previousIndex = GetSelectedIndex(view);
                int found = -1;
                for (int i = 0; i < total && found < 0; i++)
                {
                    object size = getSize.Invoke(group, new object[] { i });
                    if (SizeIs(size, width, height))
                        found = i;
                }

                addedIndex = -1;
                if (found < 0)
                {
                    Type sizeType = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
                    Type kind = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");
                    object fixedKind = Enum.Parse(kind, "FixedResolution");
                    object entry = Activator.CreateInstance(sizeType, fixedKind, width, height, CustomName);
                    groupType.GetMethod("AddCustomSize", Any).Invoke(group, new[] { entry });
                    found = (int)getTotal.Invoke(group, null) - 1;
                    addedIndex = found;
                }

                SetSelectedIndex(view, found);
                view.Repaint();
                applied = true;
                return true;
            }
            catch (Exception exception)
            {
                Warn(exception);
                return false;
            }
        }

        /// <summary>Puts the Game view back as it was before <see cref="Apply"/>.</summary>
        public static void Restore()
        {
            if (!applied)
                return;
            applied = false;
            try
            {
                EditorWindow view = FindGameView();
                if (view != null && previousIndex >= 0)
                    SetSelectedIndex(view, previousIndex);
                if (addedIndex >= 0)
                {
                    object group = CurrentGroup();
                    Type groupType = group.GetType();
                    int total = (int)groupType.GetMethod("GetTotalCount", Any).Invoke(group, null);
                    if (addedIndex < total)
                        groupType.GetMethod("RemoveCustomSize", Any).Invoke(group, new object[] { addedIndex });
                }
                view?.Repaint();
            }
            catch (Exception exception)
            {
                Warn(exception);
            }
            finally
            {
                previousIndex = -1;
                addedIndex = -1;
            }
        }

        private static EditorWindow FindGameView()
        {
            Type gameView = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameView == null)
                return null;
            UnityEngine.Object[] views = Resources.FindObjectsOfTypeAll(gameView);
            return views.Length > 0 ? views[0] as EditorWindow : null;
        }

        private static object CurrentGroup()
        {
            Type sizes = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
            Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizes);
            object instance = singleton.GetProperty("instance", Any).GetValue(null);
            return sizes.GetProperty("currentGroup", Any).GetValue(instance);
        }

        private static bool SizeIs(object size, int width, int height)
        {
            Type type = size.GetType();
            object kind = type.GetProperty("sizeType", Any)?.GetValue(size);
            bool fixedResolution = kind == null || kind.ToString() == "FixedResolution";
            int w = (int)type.GetProperty("width", Any).GetValue(size);
            int h = (int)type.GetProperty("height", Any).GetValue(size);
            return fixedResolution && w == width && h == height;
        }

        private static int GetSelectedIndex(EditorWindow view) =>
            (int)view.GetType().GetProperty("selectedSizeIndex", Any).GetValue(view);

        private static void SetSelectedIndex(EditorWindow view, int index) =>
            view.GetType().GetProperty("selectedSizeIndex", Any).SetValue(view, index);

        private static void Warn(Exception exception)
        {
            if (warned)
                return;
            warned = true;
            Debug.LogWarning("Capture: could not set the Game view size by reflection (" + exception.GetType().Name + ": " + exception.Message +
                             "). Set the Game view to 1920x1080 by hand; videos are forced to 1920x1080 by the Recorder anyway.");
        }
    }
}
