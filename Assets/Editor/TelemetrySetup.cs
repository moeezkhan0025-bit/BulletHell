using BulletHell.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// D3 setup: adds a "Stats" button to the Pause screen's debug row (next to Boss / HP- / HP+) that opens the playtest summary.
    /// Safe to run again.
    /// </summary>
    public static class TelemetrySetup
    {
        private const string GameScene = "Assets/Scenes/Game.unity";

        [MenuItem("BulletHell/D3/Add Stats Button To Pause")]
        public static void AddStatsButton()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            var picker = Object.FindFirstObjectByType<DebugRoundPicker>(FindObjectsInactive.Include);
            var so = new SerializedObject(picker);
            var boss = so.FindProperty("bossButton").objectReferenceValue as Button;
            var healthUp = so.FindProperty("healthUpButton").objectReferenceValue as Button;
            Transform row = boss.transform.parent;

            Transform old = row.Find("StatsButton");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            GameObject clone = Object.Instantiate(boss.gameObject, row);
            clone.name = "StatsButton";
            clone.transform.SetSiblingIndex(healthUp.transform.GetSiblingIndex() + 1);
            foreach (var text in clone.GetComponentsInChildren<TMP_Text>(true))
                text.text = "Stats";
            var button = clone.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();   // drop the copied Boss listeners (persistent ones)

            // Nine children in one row: tighten it so no label wraps (widths in px at 1080p; the row is about 800 wide).
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            var widths = new System.Collections.Generic.Dictionary<string, float>
            {
                { "Label", 126f }, { "Lower", 56f }, { "Raise", 56f }, { "Go", 88f }, { "Preview", 104f }, { "Boss", 96f },
                { "HpDown", 76f }, { "HpUp", 76f }, { "StatsButton", 96f },
            };
            foreach (Transform child in row)
                if (widths.TryGetValue(child.name, out float width) && child.TryGetComponent(out LayoutElement element))
                {
                    element.preferredWidth = width;
                    if (child.name == "Label")
                    {
                        element.flexibleWidth = 0f;
                        var caption = child.GetComponent<TMP_Text>();
                        caption.enableAutoSizing = true;   // "DEBUG round 12" stays on its two lines
                        caption.fontSizeMax = caption.fontSize;
                        caption.fontSizeMin = 16f;
                        caption.textWrappingMode = TextWrappingModes.Normal;
                    }
                }

            so.FindProperty("statsButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("D3: Stats button added to the Pause debug row.");
        }
    }
}
