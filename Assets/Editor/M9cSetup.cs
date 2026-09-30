using System.Linq;
using TMPro;
using BulletHell.Armory;
using BulletHell.Core;
using BulletHell.Input;
using BulletHell.Player;
using BulletHell.Shop;
using BulletHell.UI;
using BulletHell.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// One-shot M9c setup: builds the new Armory screen (gladiator ring, hovering bubbles, tabbed inventory) in the Game
    /// scene, replacing the old list screen. Safe to run again: the screen and the bubble prefab are rebuilt.
    /// </summary>
    public static class M9cSetup
    {
        private const string GameScene = "Assets/Scenes/Game.unity";
        private const string CirclePath = "Assets/Art/Placeholder/Circle.png";
        private const string GlyphsPath = "Assets/Data/UI/ButtonGlyphs.asset";
        private const string CardPrefabPath = "Assets/Prefabs/UI/ShopCard.prefab";
        private const string BubblePrefabPath = "Assets/Prefabs/UI/ArmoryBubble.prefab";
        private const float PixelsPerUnit = 285f;
        private static readonly Vector2 FeetPosition = new Vector2(-455f, -45f);

        [MenuItem("BulletHell/M9c/Setup Armory")]
        public static void Run()
        {
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
            GameObject bubblePrefab = BuildBubblePrefab(circle);
            BuildArmoryScreen(bubblePrefab, circle);
            AssetDatabase.SaveAssets();
            Debug.Log("M9c setup complete.");
        }

        // ---------------------------------------------------------------- bubble prefab

        private static GameObject BuildBubblePrefab(Sprite circle)
        {
            var root = new GameObject("ArmoryBubble", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(CancelRelay), typeof(ArmoryBubble));
            var rootRect = (RectTransform)root.transform;
            Place(rootRect, Vector2.zero, new Vector2(112f, 112f));
            var hit = root.GetComponent<Image>();
            hit.sprite = circle;
            hit.color = new Color(1f, 1f, 1f, 0f);
            var button = root.GetComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;

            RectTransform body = Rect("Body", rootRect);
            UiBuilder.Stretch(body);

            Image outline = Circle("Outline", body, circle, new Color(1f, 0.92f, 0.4f, 1f));
            var outlineRect = outline.rectTransform;
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = new Vector2(-10f, -10f);
            outlineRect.offsetMax = new Vector2(10f, 10f);

            Image rim = Circle("Rim", body, circle, Color.white);
            UiBuilder.Stretch(rim.rectTransform);
            Image inner = Circle("Inner", body, circle, new Color(0.16f, 0.1f, 0.12f, 0.95f));
            inner.rectTransform.anchorMin = Vector2.zero;
            inner.rectTransform.anchorMax = Vector2.one;
            inner.rectTransform.offsetMin = new Vector2(9f, 9f);
            inner.rectTransform.offsetMax = new Vector2(-9f, -9f);

            Image icon = Circle("Icon", body, circle, Color.white);
            Place(icon.rectTransform, Vector2.zero, new Vector2(64f, 64f));
            icon.preserveAspect = true;

            TMP_Text letter = UiBuilder.CreateText("IconLetter", body, "?", 40, TextAnchor.MiddleCenter);
            UiBuilder.Stretch((RectTransform)letter.transform);
            letter.fontStyle = FontStyles.Bold;
            letter.color = new Color(0.1f, 0.08f, 0.08f);
            letter.raycastTarget = false;

            TMP_Text plus = UiBuilder.CreateText("Plus", body, "+", 64, TextAnchor.MiddleCenter);
            UiBuilder.Stretch((RectTransform)plus.transform);
            plus.fontStyle = FontStyles.Bold;
            plus.color = new Color(0.9f, 0.86f, 0.8f);
            plus.raycastTarget = false;

            var bubble = root.GetComponent<ArmoryBubble>();
            var so = new SerializedObject(bubble);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("group").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("outline").objectReferenceValue = outline;
            so.FindProperty("rim").objectReferenceValue = rim;
            so.FindProperty("inner").objectReferenceValue = inner;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("iconLetter").objectReferenceValue = letter;
            so.FindProperty("plus").objectReferenceValue = plus;
            so.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BubblePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ---------------------------------------------------------------- the screen

        private static void BuildArmoryScreen(GameObject bubblePrefab, Sprite circle)
        {
            EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            Transform safe = GameObject.Find("FlowUI/SafeArea").transform;
            GameObject oldRoot = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.gameObject).FirstOrDefault(g => g.name == "ArmoryScreen");
            if (oldRoot != null)
                Object.DestroyImmediate(oldRoot);

            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath).GetComponent<ShopCard>();

            RectTransform root = Rect("ArmoryScreen", safe);
            UiBuilder.Stretch(root);
            var screen = root.gameObject.AddComponent<ArmoryScreen>();

            RectTransform backdrop = Rect("Backdrop", root);
            UiBuilder.Stretch(backdrop);
            backdrop.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.06f, 0.05f, 0.94f);

            TMP_Text title = UiBuilder.CreateText("Title", root, "Armory", 48, TextAnchor.MiddleCenter);
            Place((RectTransform)title.transform, new Vector2(-330f, 480f), new Vector2(900f, 70f));
            title.fontStyle = FontStyles.Bold;
            TMP_Text message = UiBuilder.CreateText("Message", root, "", 28, TextAnchor.MiddleCenter);
            Place((RectTransform)message.transform, new Vector2(500f, 487f), new Vector2(900f, 44f));
            message.color = new Color(1f, 0.92f, 0.6f);

            // ---- left: the gladiator, its ring, the bubbles
            RectTransform feet = Rect("Feet", root);
            Place(feet, FeetPosition, new Vector2(10f, 10f));
            var ring = feet.gameObject.AddComponent<ArmoryRing>();

            RectTransform pedestal = Rect("Pedestal", feet);
            Place(pedestal, Vector2.zero, new Vector2(10f, 10f));
            Image shadow = Circle("PedestalShadow", pedestal, circle, new Color(0f, 0f, 0f, 0.45f));
            Place(shadow.rectTransform, Vector2.zero, new Vector2(470f, 170f));
            Image baseImage = Circle("PedestalBase", pedestal, circle, new Color(0.5f, 0.32f, 0.2f));
            Place(baseImage.rectTransform, new Vector2(0f, -8f), new Vector2(400f, 140f));
            Image top = Circle("PedestalTop", pedestal, circle, new Color(0.78f, 0.6f, 0.38f));
            Place(top.rectTransform, Vector2.zero, new Vector2(360f, 118f));

            RectTransform backArms = Rect("BackArms", feet);
            RectTransform doll = Rect("Doll", feet);
            Place(doll, Vector2.zero, new Vector2(10f, 10f));
            var dollLayers = new Image[5];
            string[] layerNames = { "Accessory2", "Body", "Armor", "Head", "Accessory1" };
            for (int i = 0; i < dollLayers.Length; i++)
            {
                RectTransform layer = Rect(layerNames[i], doll);
                Place(layer, Vector2.zero, new Vector2(10f, 10f));
                dollLayers[i] = layer.gameObject.AddComponent<Image>();
                dollLayers[i].raycastTarget = false;
                dollLayers[i].enabled = false;
            }
            RectTransform frontArms = Rect("FrontArms", feet);

            // Slot hit areas and tether anchors
            var slotButtons = new ArmorySlotButton[ArmLoadout.SlotCount];
            var anchors = new RectTransform[ArmLoadout.SlotCount];
            for (int i = 0; i < ArmLoadout.SlotCount; i++)
                slotButtons[i] = BuildSlotButton(feet, circle, i);
            for (int i = 0; i < ArmLoadout.SlotCount; i++)
            {
                anchors[i] = Rect("Anchor" + i, feet);
                Place(anchors[i], Vector2.zero, new Vector2(4f, 4f));
            }

            // Tethers (under the bubbles) and the bubbles themselves
            RectTransform bubbleRoot = Rect("Bubbles", feet);
            Place(bubbleRoot, Vector2.zero, new Vector2(10f, 10f));
            var tethers = new ArmoryTether[ArmInstance.MaxArmamentSlots];
            var bubbles = new ArmoryBubble[ArmInstance.MaxArmamentSlots];
            for (int i = 0; i < tethers.Length; i++)
                tethers[i] = BuildTether(bubbleRoot, i);
            for (int i = 0; i < bubbles.Length; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(bubblePrefab, bubbleRoot);
                go.name = "Bubble" + (i + 1);
                bubbles[i] = go.GetComponent<ArmoryBubble>();
            }

            var ringSo = new SerializedObject(ring);
            ringSo.FindProperty("feet").objectReferenceValue = feet;
            ringSo.FindProperty("pixelsPerUnit").floatValue = PixelsPerUnit;
            ringSo.FindProperty("ring").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ArmRingTuning>("Assets/Data/Player/ArmRingTuning.asset");
            ringSo.FindProperty("playerData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlayerData>("Assets/Data/Player/PlayerData.asset");
            ringSo.FindProperty("backArms").objectReferenceValue = backArms;
            ringSo.FindProperty("doll").objectReferenceValue = doll;
            ringSo.FindProperty("frontArms").objectReferenceValue = frontArms;
            SetArray(ringSo.FindProperty("dollLayers"), dollLayers);
            SetArray(ringSo.FindProperty("slotButtons"), slotButtons);
            SetArray(ringSo.FindProperty("anchors"), anchors);
            ringSo.ApplyModifiedPropertiesWithoutUndo();

            // Info panel (arm stats, item text, before -> after)
            RectTransform infoPanel = Rect("InfoPanel", root);
            Place(infoPanel, new Vector2(-500f, -425f), new Vector2(840f, 160f));
            infoPanel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            TMP_Text info = UiBuilder.CreateText("Info", infoPanel, "", 24, TextAnchor.UpperLeft);
            UiBuilder.Stretch((RectTransform)info.transform);
            ((RectTransform)info.transform).offsetMin = new Vector2(24f, 12f);
            ((RectTransform)info.transform).offsetMax = new Vector2(-24f, -12f);
            info.richText = true;
            info.textWrappingMode = TextWrappingModes.Normal;
            info.overflowMode = TextOverflowModes.Truncate;

            // ---- right: tabs and the card grid
            RectTransform panel = Rect("Inventory", root);
            Place(panel, new Vector2(500f, -35f), new Vector2(900f, 740f));
            panel.gameObject.AddComponent<Image>();
            panel.gameObject.AddComponent<ThemedImage>().Role = ThemeRole.Panel;

            RectTransform tabsRoot = Rect("Tabs", root);
            Place(tabsRoot, new Vector2(500f, 385f), new Vector2(900f, 80f));
            var tabs = tabsRoot.gameObject.AddComponent<ArmoryTabs>();
            Button armsTab = BuildTab(tabsRoot, "ArmsTab", new Vector2(-220f, 0f), out Image armsImage, out TMP_Text armsLabel);
            Button armamentsTab = BuildTab(tabsRoot, "ArmamentsTab", new Vector2(220f, 0f), out Image armamentsImage, out TMP_Text armamentsLabel);
            var tabsSo = new SerializedObject(tabs);
            tabsSo.FindProperty("armsButton").objectReferenceValue = armsTab;
            tabsSo.FindProperty("armamentsButton").objectReferenceValue = armamentsTab;
            tabsSo.FindProperty("armsImage").objectReferenceValue = armsImage;
            tabsSo.FindProperty("armamentsImage").objectReferenceValue = armamentsImage;
            tabsSo.FindProperty("armsLabel").objectReferenceValue = armsLabel;
            tabsSo.FindProperty("armamentsLabel").objectReferenceValue = armamentsLabel;
            tabsSo.ApplyModifiedPropertiesWithoutUndo();

            RectTransform viewport = Rect("Viewport", panel);
            UiBuilder.Stretch(viewport);
            viewport.offsetMin = new Vector2(20f, 20f);
            viewport.offsetMax = new Vector2(-20f, -20f);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(220f, 300f);
            grid.spacing = new Vector2(24f, 24f);
            grid.padding = new RectOffset(34, 34, 24, 24);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            var follow = panel.gameObject.AddComponent<ScrollFollowSelection>();
            var followSo = new SerializedObject(follow);
            followSo.FindProperty("scroll").objectReferenceValue = scroll;
            followSo.ApplyModifiedPropertiesWithoutUndo();

            TMP_Text empty = UiBuilder.CreateText("Empty", panel, "", 30, TextAnchor.MiddleCenter);
            UiBuilder.Stretch((RectTransform)empty.transform);
            empty.color = new Color(0.8f, 0.72f, 0.6f);

            // ---- buttons and hint
            Button fight = UiBuilder.CreateButton("Fight", root, "Fight!", 90f);
            Place((RectTransform)fight.transform, new Vector2(650f, -450f), new Vector2(420f, 84f));
            Button remove = UiBuilder.CreateButton("Remove", root, "Remove", 70f);
            Place((RectTransform)remove.transform, new Vector2(300f, -450f), new Vector2(260f, 84f));
            Button menu = UiBuilder.CreateButton("MainMenu", root, "Main Menu", 60f);
            Place((RectTransform)menu.transform, new Vector2(-830f, 480f), new Vector2(240f, 64f));
            foreach (Button b in new[] { fight, remove, menu })
                b.gameObject.AddComponent<CancelRelay>();
            foreach (TMP_Text t in new[] { fight.GetComponentInChildren<TMP_Text>(), remove.GetComponentInChildren<TMP_Text>() })
                t.fontSize = 40;

            TMP_Text hint = UiBuilder.CreateText("Hint", root, "", 24, TextAnchor.MiddleCenter);
            Place((RectTransform)hint.transform, new Vector2(0f, -522f), new Vector2(1800f, 36f));
            hint.color = new Color(0.8f, 0.72f, 0.6f);

            // ---- wire the screen
            var so = new SerializedObject(screen);
            so.FindProperty("ring").objectReferenceValue = ring;
            so.FindProperty("bubbleRoot").objectReferenceValue = bubbleRoot;
            so.FindProperty("bubbleRadius").floatValue = 190f;
            SetArray(so.FindProperty("bubbles"), bubbles);
            SetArray(so.FindProperty("tethers"), tethers);
            so.FindProperty("infoLabel").objectReferenceValue = info;
            so.FindProperty("tabs").objectReferenceValue = tabs;
            so.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
            so.FindProperty("gridContent").objectReferenceValue = content;
            so.FindProperty("gridScroll").objectReferenceValue = scroll;
            so.FindProperty("emptyLabel").objectReferenceValue = empty;
            so.FindProperty("placeholderIcon").objectReferenceValue = circle;
            so.FindProperty("titleLabel").objectReferenceValue = title;
            so.FindProperty("messageLabel").objectReferenceValue = message;
            so.FindProperty("hintLabel").objectReferenceValue = hint;
            so.FindProperty("removeButton").objectReferenceValue = remove;
            so.FindProperty("fightButton").objectReferenceValue = fight;
            so.FindProperty("menuButton").objectReferenceValue = menu;
            so.FindProperty("input").objectReferenceValue = Object.FindFirstObjectByType<MenuInputReader>(FindObjectsInactive.Include);
            so.FindProperty("glyphs").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ButtonGlyphLibrary>(GlyphsPath);
            so.ApplyModifiedPropertiesWithoutUndo();

            var flow = Object.FindFirstObjectByType<GameFlowUI>(FindObjectsInactive.Include);
            var flowSo = new SerializedObject(flow);
            flowSo.FindProperty("armory").objectReferenceValue = screen;
            flowSo.ApplyModifiedPropertiesWithoutUndo();
            RewireStartRoutes(oldRoot, root.gameObject, fight);

            root.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static ArmorySlotButton BuildSlotButton(Transform parent, Sprite circle, int slot)
        {
            var root = new GameObject("Slot" + slot, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CancelRelay), typeof(ArmorySlotButton));
            root.transform.SetParent(parent, false);
            var rect = (RectTransform)root.transform;
            Place(rect, Vector2.zero, new Vector2(104f, 104f));
            var hit = root.GetComponent<Image>();
            hit.sprite = circle;
            hit.color = new Color(1f, 1f, 1f, 0f);
            var button = root.GetComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;

            RectTransform body = Rect("Body", rect);
            UiBuilder.Stretch(body);
            Image ring = Circle("FocusRing", body, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/M75/Glyph_PS_Circle.png") ?? circle, new Color(1f, 0.92f, 0.4f, 1f));
            ring.rectTransform.anchorMin = Vector2.zero;
            ring.rectTransform.anchorMax = Vector2.one;
            ring.rectTransform.offsetMin = new Vector2(-8f, -8f);
            ring.rectTransform.offsetMax = new Vector2(8f, 8f);
            Image frame = Circle("Frame", body, circle, new Color(0.2f, 0.14f, 0.12f, 0.55f));
            UiBuilder.Stretch(frame.rectTransform);
            TMP_Text label = UiBuilder.CreateText("Label", body, "", 30, TextAnchor.MiddleCenter);
            UiBuilder.Stretch((RectTransform)label.transform);
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;

            var component = root.GetComponent<ArmorySlotButton>();
            var so = new SerializedObject(component);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("frame").objectReferenceValue = frame;
            so.FindProperty("focusRing").objectReferenceValue = ring;
            so.FindProperty("label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
            return component;
        }

        private static ArmoryTether BuildTether(Transform parent, int index)
        {
            var go = new GameObject("Tether" + (index + 1), typeof(RectTransform), typeof(ArmoryTether));
            go.transform.SetParent(parent, false);
            RectTransform glow = Rect("Glow", go.transform);
            var glowImage = glow.gameObject.AddComponent<Image>();
            glowImage.raycastTarget = false;
            RectTransform strand = Rect("Strand", go.transform);
            var strandImage = strand.gameObject.AddComponent<Image>();
            strandImage.raycastTarget = false;
            var tether = go.GetComponent<ArmoryTether>();
            var so = new SerializedObject(tether);
            so.FindProperty("strand").objectReferenceValue = strandImage;
            so.FindProperty("glow").objectReferenceValue = glowImage;
            so.ApplyModifiedPropertiesWithoutUndo();
            go.SetActive(false);
            return tether;
        }

        private static Button BuildTab(Transform parent, string name, Vector2 position, out Image image, out TMP_Text label)
        {
            RectTransform rect = Rect(name, parent);
            Place(rect, position, new Vector2(400f, 76f));
            image = rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.96f, 0.8f);
            colors.selectedColor = new Color(1f, 0.96f, 0.8f);
            button.colors = colors;
            rect.gameObject.AddComponent<CancelRelay>();
            label = UiBuilder.CreateText("Label", rect, name, 34, TextAnchor.MiddleCenter);
            UiBuilder.Stretch((RectTransform)label.transform);
            label.color = new Color(0.25f, 0.13f, 0.08f);
            label.raycastTarget = false;
            return button;
        }

        private static void RewireStartRoutes(GameObject oldRoot, GameObject newRoot, Button fight)
        {
            foreach (var router in Object.FindObjectsByType<MenuPrimaryRouter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(router);
                SerializedProperty routes = so.FindProperty("routes");
                bool found = false;
                for (int i = 0; i < routes.arraySize; i++)
                {
                    SerializedProperty scope = routes.GetArrayElementAtIndex(i).FindPropertyRelative("Scope");
                    if (scope.objectReferenceValue == oldRoot)
                    {
                        scope.objectReferenceValue = newRoot;
                        routes.GetArrayElementAtIndex(i).FindPropertyRelative("Button").objectReferenceValue = fight;
                        found = true;
                    }
                }
                if (!found)
                {
                    routes.arraySize++;
                    SerializedProperty added = routes.GetArrayElementAtIndex(routes.arraySize - 1);
                    added.FindPropertyRelative("Scope").objectReferenceValue = newRoot;
                    added.FindPropertyRelative("Button").objectReferenceValue = fight;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ---------------------------------------------------------------- helpers

        private static Image Circle(string name, Transform parent, Sprite circle, Color color)
        {
            RectTransform rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = circle;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform Rect(string name, Transform parent) => UiBuilder.CreateRect(name, parent);

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetArray(SerializedProperty array, Object[] items)
        {
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
