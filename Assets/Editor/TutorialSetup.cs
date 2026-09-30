using BulletHell.Enemies;
using BulletHell.Input;
using BulletHell.Pickups;
using BulletHell.Platform;
using BulletHell.Player;
using BulletHell.UI;
using BulletHell.Weapons;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// D2 setup: adds the gameplay control names to the button glyph asset, creates the Tutorial data asset, and builds the
    /// tutorial card plus its controller in the Game scene (VoxVegetallis look). Safe to run again: the card is rebuilt, the
    /// data asset and glyph labels are only filled where missing.
    /// </summary>
    public static class TutorialSetup
    {
        private const string GameScene = "Assets/Scenes/Game.unity";
        private const string GlyphsPath = "Assets/Data/UI/ButtonGlyphs.asset";
        private const string TutorialPath = "Assets/Data/UI/Tutorial.asset";

        [MenuItem("BulletHell/D2/Build Tutorial")]
        public static void Build()
        {
            AddGlyphLabels();
            TutorialData data = EnsureData();
            BuildPanel(data);
            Debug.Log("D2: tutorial glyph labels, data and panel built.");
        }

        // ---------------------------------------------------------------- glyph labels

        // Labels of the actions after Start, per family, in UiAction order: Move, Aim, Fire, Lock, Jump, Ammo1-4, SkipTutorial.
        private static string[] Labels(GlyphFamily family)
        {
            switch (family)
            {
                case GlyphFamily.PlayStation: return new[] { "Right Stick", "Left Stick", "R1", "L3", "R2", "Cross", "Circle", "Square", "Triangle", "Create" };
                case GlyphFamily.Xbox: return new[] { "Right Stick", "Left Stick", "RB", "LS Click", "RT", "A", "B", "X", "Y", "View" };
                case GlyphFamily.Nintendo: return new[] { "Right Stick", "Left Stick", "R", "L Stick Click", "ZR", "B", "A", "Y", "X", "-" };
                case GlyphFamily.Keyboard: return new[] { "WASD", "Mouse", "Click", "L", "Space", "1", "2", "3", "4", "Tab" };
                default: return new string[10];   // Touch: no onboarding until its controls exist (M12)
            }
        }

        public static void AddGlyphLabels()
        {
            var library = AssetDatabase.LoadAssetAtPath<ButtonGlyphLibrary>(GlyphsPath);
            var sets = library.Sets;
            int first = (int)UiAction.Move;
            foreach (var set in sets)
            {
                string[] existing = set.ActionLabels ?? new string[0];
                var labels = new string[(int)UiAction.SkipTutorial + 1];
                for (int i = 0; i < labels.Length; i++)
                    labels[i] = i < existing.Length ? existing[i] : "";
                string[] add = Labels(set.Family);
                for (int i = 0; i < add.Length; i++)
                    if (string.IsNullOrEmpty(labels[first + i]))
                        labels[first + i] = add[i];
                set.ActionLabels = labels;
            }
            library.SetSets(sets);
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- data asset

        private static TutorialData.Step S(TutorialStepKind kind, string title, string text, UiAction[] keys, string waiting = "") =>
            new TutorialData.Step { Kind = kind, Title = title, Text = text, Keys = keys, WaitingText = waiting };

        public static TutorialData EnsureData()
        {
            var data = AssetDatabase.LoadAssetAtPath<TutorialData>(TutorialPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<TutorialData>();
                AssetDatabase.CreateAsset(data, TutorialPath);
            }
            if (data.Steps.Length == 0)
            {
                data.SetSteps(new[]
                {
                    S(TutorialStepKind.Move, "MOVE", "Push {0} to move around the arena.", new[] { UiAction.Move }),
                    S(TutorialStepKind.SelectArm, "SELECT AN ARM", "Push {0} toward an arm to select it.", new[] { UiAction.Aim }),
                    S(TutorialStepKind.Fire, "FIRE", "Hold {0} to fire the selected arm.", new[] { UiAction.Fire }),
                    S(TutorialStepKind.Lock, "LOCK", "Press {0} to lock your arm, then aim it freely with {1}.", new[] { UiAction.Lock, UiAction.Aim }),
                    S(TutorialStepKind.Jump, "JUMP", "Press {0} to jump over enemies and low walls.", new[] { UiAction.Jump }),
                    S(TutorialStepKind.SwapAmmo, "SWAP AMMO", "Press {0} or {1} to swap ammo.", new[] { UiAction.Ammo1, UiAction.Ammo2 },
                      "Walk over an ammo pickup to collect a second ammo type."),
                });
            }
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            return data;
        }

        // ---------------------------------------------------------------- scene

        private static void BuildPanel(TutorialData data)
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            Transform safe = GameObject.Find("FlowUI/SafeArea").transform;
            data = AssetDatabase.LoadAssetAtPath<TutorialData>(TutorialPath);   // reload: references loaded before the scene opened can come back null
            UITheme theme = UITheme.Current;

            Transform old = safe.Find("TutorialPanel");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            // A root with a CanvasGroup (what the controller fades), below the round pill like the boss bar.
            RectTransform root = VoxUi.R("TutorialPanel", safe);
            VoxUi.TopCenter(root, 0f, 112f, 900f, 168f);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            root.SetSiblingIndex(safe.Find("BossHud") != null ? safe.Find("BossHud").GetSiblingIndex() + 1 : 1);

            RectTransform card = VoxUi.R("Card", root);
            VoxUi.Stretch(card);
            Image panel = VoxUi.Themed("Panel", card, ThemeRole.Panel);
            VoxUi.Stretch(panel.rectTransform);
            Image trim = VoxUi.Trim("Trim", card, TrimColor.Leaf);
            trim.rectTransform.anchorMin = new Vector2(0f, 1f);
            trim.rectTransform.anchorMax = new Vector2(1f, 1f);
            trim.rectTransform.pivot = new Vector2(0.5f, 1f);
            trim.rectTransform.offsetMin = new Vector2(16f, -34f);
            trim.rectTransform.offsetMax = new Vector2(-16f, -14f);

            TMP_Text counter = VoxUi.Txt("Counter", card, "LESSON 1 OF 6", 22, TextFont.BodyBold, TextTone.Muted, TextAlignmentOptions.Center, caps: true);
            counter.rectTransform.anchorMin = new Vector2(0f, 1f);
            counter.rectTransform.anchorMax = new Vector2(1f, 1f);
            counter.rectTransform.pivot = new Vector2(0.5f, 1f);
            counter.rectTransform.offsetMin = new Vector2(24f, -66f);
            counter.rectTransform.offsetMax = new Vector2(-24f, -38f);
            VoxUi.Fit(counter, 16f, 22f);

            TMP_Text instruction = VoxUi.Txt("Instruction", card, "Push [Right Stick] to move around the arena.", 40, TextFont.Button, TextTone.OnPanel, TextAlignmentOptions.Center);
            instruction.rectTransform.anchorMin = new Vector2(0f, 0f);
            instruction.rectTransform.anchorMax = new Vector2(1f, 1f);
            instruction.rectTransform.offsetMin = new Vector2(36f, 40f);
            instruction.rectTransform.offsetMax = new Vector2(-36f, -68f);
            instruction.richText = true;
            instruction.textWrappingMode = TextWrappingModes.Normal;   // two lines are fine; autosize keeps it inside the card
            instruction.enableAutoSizing = true;
            instruction.fontSizeMin = 26f;
            instruction.fontSizeMax = 40f;
            instruction.overflowMode = TextOverflowModes.Ellipsis;

            TMP_Text skip = VoxUi.Txt("Skip", card, "[Create] Skip tutorial", 22, TextFont.BodyBold, TextTone.Muted, TextAlignmentOptions.Left);
            skip.richText = true;
            skip.rectTransform.anchorMin = new Vector2(0f, 0f);
            skip.rectTransform.anchorMax = new Vector2(0.6f, 0f);
            skip.rectTransform.pivot = new Vector2(0f, 0f);
            skip.rectTransform.offsetMin = new Vector2(36f, 12f);
            skip.rectTransform.offsetMax = new Vector2(0f, 40f);
            VoxUi.Fit(skip, 16f, 22f);

            // Progress pips, bottom right
            RectTransform pipRow = VoxUi.R("Pips", card);
            pipRow.anchorMin = pipRow.anchorMax = new Vector2(1f, 0f);
            pipRow.pivot = new Vector2(1f, 0f);
            pipRow.anchoredPosition = new Vector2(-36f, 16f);
            pipRow.sizeDelta = new Vector2(180f, 22f);
            var layout = pipRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var pips = new Image[data.Steps.Length];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = VoxUi.Img("Pip" + (i + 1), pipRow, M75Art.Load("Circle"), Image.Type.Simple, theme.MarbleShade);
                pips[i].rectTransform.sizeDelta = new Vector2(20f, 20f);
            }

            var view = root.gameObject.AddComponent<TutorialPanel>();
            VoxUi.SetRef(view, "group", group);
            VoxUi.SetRef(view, "card", card);
            VoxUi.SetRef(view, "counterLabel", counter);
            VoxUi.SetRef(view, "instruction", instruction);
            VoxUi.SetRef(view, "skipLabel", skip);
            VoxUi.SetArray(view, "pips", pips);

            // Controller: its own object next to the panel, wired to the player, spawner and input.
            Transform oldController = safe.Find("TutorialController");
            if (oldController != null)
                Object.DestroyImmediate(oldController.gameObject);
            var controllerGo = new GameObject("TutorialController");
            controllerGo.transform.SetParent(safe, false);
            var controller = controllerGo.AddComponent<TutorialController>();
            var player = Object.FindFirstObjectByType<PlayerHealth>();
            VoxUi.SetRef(controller, "data", data);
            VoxUi.SetRef(controller, "panel", view);
            VoxUi.SetRef(controller, "input", Object.FindFirstObjectByType<GameplayInputReader>());
            VoxUi.SetRef(controller, "arms", player.GetComponent<ArmSelectionController>());
            VoxUi.SetRef(controller, "jump", player.GetComponent<JumpController>());
            VoxUi.SetRef(controller, "ammo", player.GetComponent<AmmoSlots>());
            VoxUi.SetRef(controller, "spawner", Object.FindFirstObjectByType<WaveSpawner>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
