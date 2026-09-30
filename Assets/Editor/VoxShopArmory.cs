using System.Linq;
using BulletHell.Armory;
using BulletHell.Core;
using BulletHell.Input;
using BulletHell.Player;
using BulletHell.Shop;
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
    /// VOX VEGETALLIS Shop ("The Mercator's Stall": merchant panel, wood stall with the cards, detail panel) and Armory
    /// (gladiator ring with the hovering bubbles, tabbed card panel, stat strip, Fight!) in the Game scene. Positions are in 1080p
    /// canvas units taken from the mockups. The screens keep their components and wiring (routers, GameFlowUI); the objects
    /// inside are rebuilt. Safe to run again.
    /// </summary>
    public static class VoxShopArmory
    {
        private const string GameScene = "Assets/Scenes/Game.unity";
        private const string CardPrefabPath = "Assets/Prefabs/UI/ShopCard.prefab";
        private const string BubblePrefabPath = "Assets/Prefabs/UI/ArmoryBubble.prefab";
        private const string GlyphsPath = "Assets/Data/UI/ButtonGlyphs.asset";
        private const string CirclePath = "Assets/Art/Placeholder/Circle.png";
        private const string DashedRingPath = "Assets/Art/UI/Backdrop/ring_dashed.png";

        [MenuItem("BulletHell/Vox/9 Build Shop And Armory")]
        public static void Build()
        {
            VoxMenuScreens.BuildRowPrefabs();
            GameObject card = BuildCardPrefab();
            GameObject bubble = BuildBubblePrefab();
            BuildDashedRingTexture();

            Scene scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            BuildShop(card);
            BuildArmory(bubble);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Vox: Shop and Armory built.");
        }

        // ---------------------------------------------------------------- card prefab

        private static GameObject BuildCardPrefab()
        {
            UITheme theme = UITheme.Current;
            var root = new GameObject("ShopCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CancelRelay), typeof(ShopCard));
            var rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = new Vector2(190f, 270f);
            var hit = root.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            var button = root.GetComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;

            RectTransform body = VoxUi.R("Body", rootRect);
            VoxUi.Stretch(body);

            Image frame = VoxUi.Img("Frame", body, theme.GetCardFrame(0), Image.Type.Simple);
            VoxUi.Stretch(frame.rectTransform);

            Image trim = VoxUi.Trim("CrateTrim", body, TrimColor.Leaf);
            trim.rectTransform.anchorMin = new Vector2(0f, 1f);
            trim.rectTransform.anchorMax = new Vector2(1f, 1f);
            trim.rectTransform.pivot = new Vector2(0.5f, 1f);
            trim.rectTransform.offsetMin = new Vector2(14f, -35f);
            trim.rectTransform.offsetMax = new Vector2(-14f, -14f);
            trim.gameObject.SetActive(false);

            Image ring = VoxUi.Img("FocusRing", body, theme.CardFocusRing, Image.Type.Simple);
            VoxUi.Stretch(ring.rectTransform, -9f, -5f, -9f, -5f);

            Image icon = VoxUi.Img("Icon", body, null, Image.Type.Simple);
            icon.preserveAspect = true;
            Frac(icon.rectTransform, 0.17f, 0.52f, 0.83f, 0.87f);

            TMP_Text letter = VoxUi.Txt("IconLetter", body, "?", 64, TextFont.Button, TextTone.OnPanel);
            Frac(letter.rectTransform, 0.17f, 0.52f, 0.83f, 0.87f);

            TMP_Text nameLabel = VoxUi.Txt("Name", body, "Name", 27, TextFont.Button, TextTone.OnPanel, TextAlignmentOptions.Left);
            Frac(nameLabel.rectTransform, 0.085f, 0.385f, 0.95f, 0.49f);
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMin = 16;
            nameLabel.fontSizeMax = 28;
            nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            nameLabel.overflowMode = TextOverflowModes.Ellipsis;
            var nameThemed = nameLabel.GetComponent<ThemedText>();
            nameThemed.Tone = TextTone.Custom;

            TMP_Text rarityLabel = VoxUi.Txt("Rarity", body, "COMMON", 14, TextFont.BodyBold, TextTone.Custom, TextAlignmentOptions.TopLeft);
            Frac(rarityLabel.rectTransform, 0.085f, 0.185f, 0.95f, 0.385f);
            rarityLabel.characterSpacing = 4f;
            rarityLabel.textWrappingMode = TextWrappingModes.Normal;
            rarityLabel.lineSpacing = -10f;

            // Price: coin + number, bottom-left
            Image tag = VoxUi.Img("PriceTag", body, theme.Coin, Image.Type.Simple);
            tag.preserveAspect = true;
            tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = new Vector2(0f, 0f);
            tag.rectTransform.pivot = new Vector2(0f, 0f);
            tag.rectTransform.anchoredPosition = new Vector2(14f, 12f);
            tag.rectTransform.sizeDelta = new Vector2(36f, 36f);
            TMP_Text price = VoxUi.Txt("PriceLabel", tag.transform, "100", 32, TextFont.Button, TextTone.Custom, TextAlignmentOptions.Left);
            price.rectTransform.anchorMin = price.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            price.rectTransform.pivot = new Vector2(0f, 0.5f);
            price.rectTransform.anchoredPosition = new Vector2(8f, 0f);
            price.rectTransform.sizeDelta = new Vector2(130f, 40f);

            // SOLD: dimmed card with the stamp
            RectTransform sold = VoxUi.R("Sold", body);
            VoxUi.Stretch(sold, 4f, 4f, 4f, 4f);
            var soldDim = sold.gameObject.AddComponent<Image>();
            soldDim.color = new Color(0.36f, 0.3f, 0.26f, 0.5f);
            soldDim.raycastTarget = false;
            var soldGroup = sold.gameObject.AddComponent<CanvasGroup>();
            soldGroup.blocksRaycasts = false;
            Image stamp = VoxUi.Img("Stamp", sold, theme.StampSold, Image.Type.Simple);
            stamp.preserveAspect = true;
            Frac(stamp.rectTransform, -0.05f, 0.3f, 1.05f, 0.72f);
            stamp.rectTransform.localRotation = Quaternion.identity;
            sold.gameObject.SetActive(false);

            var card = root.GetComponent<ShopCard>();
            var so = new SerializedObject(card);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("frame").objectReferenceValue = frame;
            so.FindProperty("focusRing").objectReferenceValue = ring;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("iconLetter").objectReferenceValue = letter;
            so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("rarityLabel").objectReferenceValue = rarityLabel;
            so.FindProperty("priceTag").objectReferenceValue = tag;
            so.FindProperty("priceLabel").objectReferenceValue = price;
            so.FindProperty("soldStamp").objectReferenceValue = sold.gameObject;
            so.FindProperty("soldGroup").objectReferenceValue = soldGroup;
            so.FindProperty("crateTrim").objectReferenceValue = trim.gameObject;
            so.FindProperty("liftHeight").floatValue = theme.FocusLiftPx;
            so.FindProperty("liftSeconds").floatValue = theme.FocusSeconds;
            so.FindProperty("flySeconds").floatValue = theme.CardBuySeconds;
            so.FindProperty("liftScale").floatValue = 1.04f;
            so.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Anchors a rect to fractions of its parent (left, bottom, right, top), zero offsets.</summary>
        private static void Frac(RectTransform r, float l, float b, float rt, float t)
        {
            r.anchorMin = new Vector2(l, b);
            r.anchorMax = new Vector2(rt, t);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        // ---------------------------------------------------------------- bubble prefab

        private static GameObject BuildBubblePrefab()
        {
            UITheme theme = UITheme.Current;
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
            var root = new GameObject("ArmoryBubble", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(CancelRelay), typeof(ArmoryBubble));
            var rootRect = (RectTransform)root.transform;
            VoxUi.Center(rootRect, 0f, 0f, 104f, 113f);
            var hit = root.GetComponent<Image>();
            hit.sprite = circle;
            hit.color = new Color(1f, 1f, 1f, 0f);
            var button = root.GetComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;

            RectTransform body = VoxUi.R("Body", rootRect);
            VoxUi.Stretch(body);

            // Focus halo: the big gold bubble behind the normal one.
            Image outline = VoxUi.Img("Outline", body, theme.BubbleFocused, Image.Type.Simple);
            outline.preserveAspect = true;
            VoxUi.Stretch(outline.rectTransform, -16f, -17f, -16f, -17f);
            Image rim = VoxUi.Img("Rim", body, theme.BubbleFilledRim, Image.Type.Simple);
            rim.preserveAspect = true;
            VoxUi.Stretch(rim.rectTransform, -4f, -4f, -4f, -4f);
            Image inner = VoxUi.Img("Inner", body, null, Image.Type.Simple);
            inner.enabled = false;
            inner.rectTransform.sizeDelta = Vector2.zero;

            Image icon = VoxUi.Img("Icon", body, null, Image.Type.Simple);
            icon.preserveAspect = true;
            VoxUi.Center(icon.rectTransform, 0f, 2f, 62f, 62f);
            TMP_Text letter = VoxUi.Txt("IconLetter", body, "?", 40, TextFont.Button, TextTone.OnPanel);
            VoxUi.Stretch(letter.rectTransform);
            TMP_Text plus = VoxUi.Txt("Plus", body, "+", 72, TextFont.Button, TextTone.OnPanel);
            VoxUi.Stretch(plus.rectTransform);

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
            so.FindProperty("focusScale").floatValue = 1.15f;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BubblePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ---------------------------------------------------------------- dashed ring texture

        private static void BuildDashedRingTexture()
        {
            const int w = 1024, h = 614; // ellipse 1000 x 590 incl. line width
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var clear = new Color(1f, 1f, 1f, 0f);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;
            float rx = w * 0.5f - 12f, ry = h * 0.5f - 12f;
            float cx = w * 0.5f, cy = h * 0.5f;
            const float dash = 0.055f, gap = 0.04f; // fractions of a full turn
            for (int s = 0; s < 6000; s++)
            {
                float t = s / 6000f;
                if (Mathf.Repeat(t, dash + gap) > dash)
                    continue;
                float a = t * Mathf.PI * 2f;
                int px = Mathf.RoundToInt(cx + Mathf.Cos(a) * rx), py = Mathf.RoundToInt(cy + Mathf.Sin(a) * ry);
                for (int dy = -4; dy <= 4; dy++)
                    for (int dx = -4; dx <= 4; dx++)
                        if (dx * dx + dy * dy <= 16)
                        {
                            int x = px + dx, y = py + dy;
                            if (x >= 0 && x < w && y >= 0 && y < h)
                                pixels[y * w + x] = Color.white;
                        }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(DashedRingPath));
            System.IO.File.WriteAllBytes(DashedRingPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(DashedRingPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(DashedRingPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        // ---------------------------------------------------------------- shared

        private static Image Backdrop(Transform parent, float darkness)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(VoxMenuScreens.BackdropPath);
            Image back = VoxUi.Img("Backdrop", parent, sprite, Image.Type.Simple, Color.white, true);
            VoxUi.Stretch(back.rectTransform);
            var fitter = back.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            Image dark = VoxUi.Img("Dark", back.transform, null, Image.Type.Simple, new Color(0f, 0f, 0f, darkness));
            VoxUi.Stretch(dark.rectTransform);
            return back;
        }

        private static void RewireStartRoute(GameObject scope, GameObject oldScope, Button button)
        {
            foreach (var router in Object.FindObjectsByType<MenuPrimaryRouter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(router);
                SerializedProperty routes = so.FindProperty("routes");
                bool found = false;
                for (int i = 0; i < routes.arraySize; i++)
                {
                    SerializedProperty s = routes.GetArrayElementAtIndex(i).FindPropertyRelative("Scope");
                    if (s.objectReferenceValue == null || s.objectReferenceValue == oldScope || s.objectReferenceValue == scope)
                    {
                        s.objectReferenceValue = scope;
                        routes.GetArrayElementAtIndex(i).FindPropertyRelative("Button").objectReferenceValue = button;
                        found = true;
                    }
                }
                if (!found)
                {
                    routes.arraySize++;
                    SerializedProperty added = routes.GetArrayElementAtIndex(routes.arraySize - 1);
                    added.FindPropertyRelative("Scope").objectReferenceValue = scope;
                    added.FindPropertyRelative("Button").objectReferenceValue = button;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void Card(GameObject prefab, Transform parent, string name, float x, float y, float w, float h, out ShopCard card)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            VoxUi.TopLeft((RectTransform)go.transform, x, y, w, h);
            card = go.GetComponent<ShopCard>();
        }

        // ---------------------------------------------------------------- Shop

        private static void BuildShop(GameObject cardPrefab)
        {
            UITheme theme = UITheme.Current;
            var old = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.gameObject).FirstOrDefault(g => g.name == "ShopScreen");
            var screen = old.GetComponent<ShopScreen>();
            var tooltipComponent = old.GetComponent<ShopTooltip>();
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
            Transform root = old.transform;
            VoxUi.ClearChildren(root);

            Backdrop(root, 0.5f);

            TMP_Text title = VoxUi.Txt("Title", root, "THE MERCATOR'S STALL", 72, TextFont.Title, TextTone.Gold, TextAlignmentOptions.Left, outline: true);
            VoxUi.TopLeft(title.rectTransform, 46f, 30f, 1100f, 92f);

            // Currency pill
            Image coinPill = VoxUi.Themed("Currency", root, ThemeRole.PillMarble);
            VoxUi.TopRight(coinPill.rectTransform, 41f, 34f, 190f, 70f);
            Image coin = VoxUi.Img("Coin", coinPill.transform, theme.Coin, Image.Type.Simple);
            coin.preserveAspect = true;
            coin.rectTransform.anchorMin = coin.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            coin.rectTransform.pivot = new Vector2(0f, 0.5f);
            coin.rectTransform.anchoredPosition = new Vector2(22f, 7f);
            coin.rectTransform.sizeDelta = new Vector2(44f, 44f);
            TMP_Text currencyValue = VoxUi.Txt("Value", coinPill.transform, "0", 42, TextFont.Button, TextTone.OnPanel, TextAlignmentOptions.Right);
            VoxUi.Stretch(currencyValue.rectTransform, 70f, 14f, 24f, 4f);

            // Left: merchant panel
            Image merchantPanel = VoxUi.Themed("MerchantPanel", root, ThemeRole.Panel);
            VoxUi.TopLeft(merchantPanel.rectTransform, 50f, 146f, 382f, 766f);
            Image merchantTrim = VoxUi.Trim("Trim", merchantPanel.transform, TrimColor.Leaf);
            VoxUi.TopLeft(merchantTrim.rectTransform, 12f, 10f, 358f, 42f);
            BuildMerchant(merchantPanel.transform, circle);
            Image speech = VoxUi.Themed("Speech", merchantPanel.transform, ThemeRole.Panel);
            speech.rectTransform.anchorMin = speech.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            speech.rectTransform.pivot = new Vector2(0.5f, 0f);
            speech.rectTransform.anchoredPosition = new Vector2(0f, 36f);
            speech.rectTransform.sizeDelta = new Vector2(340f, 140f);
            TMP_Text speechText = VoxUi.Txt("Text", speech.transform, "\"Fresh from the Emperor's own garden. No refunds, gladiator.\"", 26, TextFont.Body, TextTone.OnPanel, TextAlignmentOptions.Left);
            speechText.textWrappingMode = TextWrappingModes.Normal;
            VoxUi.Stretch(speechText.rectTransform, 28f, 30f, 24f, 22f);

            // Center: the stall
            Image stall = VoxUi.Themed("Stall", root, ThemeRole.PanelWood);
            VoxUi.TopLeft(stall.rectTransform, 461f, 146f, 996f, 766f);
            Image stallTrim = VoxUi.Trim("Trim", stall.transform, TrimColor.Leaf);
            VoxUi.TopLeft(stallTrim.rectTransform, 12f, 10f, 972f, 42f);
            TMP_Text armsLabel = VoxUi.Txt("ArmsLabel", stall.transform, "ARMS", 26, TextFont.BodyBold, TextTone.Gold, TextAlignmentOptions.Left, caps: true);
            VoxUi.TopLeft(armsLabel.rectTransform, 40f, 62f, 400f, 32f);
            TMP_Text armamentsLabel = VoxUi.Txt("ArmamentsLabel", stall.transform, "ARMAMENTS", 26, TextFont.BodyBold, TextTone.Gold, TextAlignmentOptions.Left, caps: true);
            VoxUi.TopLeft(armamentsLabel.rectTransform, 40f, 410f, 400f, 32f);

            var armCards = new ShopCard[2];
            var armamentCards = new ShopCard[3];
            for (int i = 0; i < armCards.Length; i++)
                Card(cardPrefab, stall.transform, "ArmCard" + (i + 1), 40f + i * 224f, 118f, 190f, 270f, out armCards[i]);
            for (int i = 0; i < armamentCards.Length; i++)
                Card(cardPrefab, stall.transform, "ArmamentCard" + (i + 1), 40f + i * 224f, 450f, 190f, 270f, out armamentCards[i]);
            Card(cardPrefab, stall.transform, "CrateCard", 40f + 3 * 224f, 450f, 190f, 270f, out ShopCard crate);

            // Message line, hint, buttons, bag
            TMP_Text message = VoxUi.Txt("Message", root, "", 30, TextFont.BodyBold, TextTone.Gold, TextAlignmentOptions.Center, outline: true);
            VoxUi.TopLeft(message.rectTransform, 461f, 926f, 996f, 44f);
            TMP_Text hint = VoxUi.HintPill("Hint", root, 36f, 28f, 640f, 62f);

            Button reroll = VoxUi.Btn("Reroll", root, "Reroll", 250f, 70f, 34f);
            VoxUi.BottomRight((RectTransform)reroll.transform, 300f, 28f, 250f, 70f);
            Button leave = VoxUi.Btn("Leave", root, "Leave", 240f, 70f, 36f, ButtonKind.Primary);
            VoxUi.BottomRight((RectTransform)leave.transform, 41f, 28f, 240f, 70f);
            Button menu = VoxUi.Btn("MainMenu", root, "Main Menu", 200f, 58f, 28f);
            VoxUi.TopRight((RectTransform)menu.transform, 252f, 40f, 200f, 58f);
            TMP_Text rerollLabel = reroll.GetComponentInChildren<TMP_Text>();

            Image bag = VoxUi.Themed("Inventory", root, ThemeRole.PanelShade);
            VoxUi.BottomRight(bag.rectTransform, 580f, 28f, 70f, 70f);
            Image bagIcon = VoxUi.Img("Icon", bag.transform, theme.HarvestCrateIcon, Image.Type.Simple);
            bagIcon.preserveAspect = true;
            VoxUi.Center(bagIcon.rectTransform, 0f, 4f, 44f, 44f);
            TMP_Text inventoryLabel = VoxUi.Txt("InventoryLabel", root, "Arms 0   Armaments 0", 24, TextFont.BodyBold, TextTone.OnDark, TextAlignmentOptions.Right);
            VoxUi.BottomRight(inventoryLabel.rectTransform, 666f, 48f, 330f, 34f);

            // Crate overlay
            RectTransform overlay = VoxUi.R("CrateOverlay", root);
            VoxUi.Stretch(overlay);
            var overlayDim = overlay.gameObject.AddComponent<Image>();
            overlayDim.color = new Color(0f, 0f, 0f, 0.72f);
            TMP_Text overlayTitle = VoxUi.Txt("Title", overlay, "The crate holds 3 armaments. Pick 1.", 56, TextFont.Button, TextTone.Gold, TextAlignmentOptions.Center, outline: true);
            VoxUi.Center(overlayTitle.rectTransform, -200f, 290f, 1100f, 80f);
            var choices = new ShopCard[3];
            for (int i = 0; i < choices.Length; i++)
            {
                Card(cardPrefab, overlay, "Choice" + (i + 1), 0f, 0f, 250f, 355f, out choices[i]);
                RectTransform r = (RectTransform)choices[i].transform;
                VoxUi.Center(r, -470f + i * 290f, 10f, 250f, 355f);
            }
            overlay.gameObject.SetActive(false);

            // Right: detail panel (persistent tooltip), drawn last so it stays readable over the crate overlay
            Image detail = VoxUi.Themed("Detail", root, ThemeRole.Panel);
            VoxUi.TopRight(detail.rectTransform, 41f, 146f, 389f, 766f);
            Image bar = VoxUi.Img("Bar", detail.transform, null, Image.Type.Simple, theme.Leaf);
            VoxUi.TopLeft(bar.rectTransform, 16f, 14f, 357f, 28f);
            TMP_Text detailTitle = VoxUi.Txt("Title", detail.transform, "", 48, TextFont.Button, TextTone.OnPanel, TextAlignmentOptions.TopLeft);
            detailTitle.textWrappingMode = TextWrappingModes.Normal;
            detailTitle.enableAutoSizing = true;
            detailTitle.fontSizeMin = 28;
            detailTitle.fontSizeMax = 48;
            VoxUi.TopLeft(detailTitle.rectTransform, 30f, 60f, 330f, 118f);
            TMP_Text detailBody = VoxUi.Txt("Body", detail.transform, "", 27, TextFont.Body, TextTone.OnPanel, TextAlignmentOptions.TopLeft);
            detailBody.textWrappingMode = TextWrappingModes.Normal;
            detailBody.richText = true;
            VoxUi.TopLeft(detailBody.rectTransform, 30f, 196f, 330f, 540f);

            // Wiring
            var tip = tooltipComponent;
            VoxUi.SetRef(tip, "panel", detail.rectTransform);
            VoxUi.SetRef(tip, "body", detailBody);
            VoxUi.SetRef(tip, "title", detailTitle);
            VoxUi.SetBool(tip, "persistent", true);

            VoxUi.SetArray(screen, "armCards", armCards);
            VoxUi.SetArray(screen, "armamentCards", armamentCards);
            VoxUi.SetRef(screen, "crateCard", crate);
            VoxUi.SetRef(screen, "placeholderIcon", circle);
            VoxUi.SetRef(screen, "titleLabel", title);
            VoxUi.SetRef(screen, "currencyLabel", currencyValue);
            VoxUi.SetRef(screen, "messageLabel", message);
            VoxUi.SetRef(screen, "hintLabel", hint);
            VoxUi.SetRef(screen, "inventoryLabel", inventoryLabel);
            VoxUi.SetRef(screen, "rerollButton", reroll);
            VoxUi.SetRef(screen, "rerollLabel", rerollLabel);
            VoxUi.SetRef(screen, "leaveButton", leave);
            VoxUi.SetRef(screen, "menuButton", menu);
            VoxUi.SetRef(screen, "inventoryTarget", bag.rectTransform);
            VoxUi.SetRef(screen, "tooltip", tip);
            VoxUi.SetRef(screen, "crateOverlay", overlay.gameObject);
            VoxUi.SetArray(screen, "crateChoiceCards", choices);
            RewireStartRoute(old, old, leave);
            old.SetActive(false);
            EditorUtility.SetDirty(screen);
        }

        private static void BuildMerchant(Transform panel, Sprite circle)
        {
            // Placeholder mercator (an olive): kept from M9b, scaled into the panel. Final art replaces the whole group.
            RectTransform merchant = VoxUi.R("Merchant", panel);
            VoxUi.TopCenter(merchant, 0f, 40f, 420f, 560f);
            merchant.localScale = Vector3.one * 0.6f;

            void Blob(string name, Vector2 pos, Vector2 size, Color color)
            {
                Image image = VoxUi.Img(name, merchant, circle, Image.Type.Simple, color);
                VoxUi.Center(image.rectTransform, pos.x, pos.y, size.x, size.y);
            }

            Blob("Body", new Vector2(0f, -110f), new Vector2(330f, 380f), new Color(0.42f, 0.5f, 0.2f));
            Blob("Belly", new Vector2(0f, -150f), new Vector2(210f, 230f), new Color(0.6f, 0.68f, 0.32f));
            Blob("Head", new Vector2(0f, 130f), new Vector2(230f, 250f), new Color(0.5f, 0.6f, 0.25f));
            Blob("Pimento", new Vector2(0f, 170f), new Vector2(60f, 60f), new Color(0.85f, 0.2f, 0.15f));
            Blob("EyeL", new Vector2(-45f, 80f), new Vector2(30f, 38f), new Color(0.1f, 0.08f, 0.06f));
            Blob("EyeR", new Vector2(45f, 80f), new Vector2(30f, 38f), new Color(0.1f, 0.08f, 0.06f));
            Blob("Laurel", new Vector2(0f, 235f), new Vector2(210f, 60f), new Color(0.3f, 0.65f, 0.25f));
            TMP_Text name = VoxUi.Txt("Nameplate", merchant, "Mercator Oliva", 38, TextFont.Button, TextTone.OnPanel);
            VoxUi.Center(name.rectTransform, 0f, -352f, 440f, 56f);
        }

        // ---------------------------------------------------------------- Armory

        private static void BuildArmory(GameObject bubblePrefab)
        {
            UITheme theme = UITheme.Current;
            var old = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.gameObject).FirstOrDefault(g => g.name == "ArmoryScreen");
            var screen = old.GetComponent<ArmoryScreen>();
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath).GetComponent<ShopCard>();
            Transform root = old.transform;

            // Keep the ring's existing structure (Feet, doll layers, slots, anchors, bubbles, tethers): restyle in place.
            Transform feet = root.Find("Feet");
            var ring = feet.GetComponent<ArmoryRing>();

            // Everything else is rebuilt. Detach the ring first so ClearChildren spares it.
            feet.SetParent(null, false);
            VoxUi.ClearChildren(root);

            Backdrop(root, 0.5f);

            TMP_Text title = VoxUi.Txt("Title", root, "ARMORY", 72, TextFont.Title, TextTone.Gold, TextAlignmentOptions.Left, outline: true);
            VoxUi.TopLeft(title.rectTransform, 46f, 30f, 900f, 92f);
            TMP_Text message = VoxUi.Txt("Message", root, "", 34, TextFont.Button, TextTone.OnDark, TextAlignmentOptions.Center, outline: true);
            VoxUi.TopLeft(message.rectTransform, 140f, 318f, 800f, 52f);

            // Stage: spotlight glow, dashed ring, pedestal (under the feet), then the ring object itself
            feet.SetParent(root, false);
            var feetRect = (RectTransform)feet;
            VoxUi.Center(feetRect, -416f, -238f, 10f, 10f);
            RebuildStage(feetRect, ring, theme);

            // Stat strip (info panel)
            Image info = VoxUi.Themed("InfoPanel", root, ThemeRole.Panel);
            VoxUi.BottomLeft(info.rectTransform, 50f, 26f, 960f, 150f);
            Image infoTrim = VoxUi.Trim("Trim", info.transform, TrimColor.Leaf);
            VoxUi.TopLeft(infoTrim.rectTransform, 12f, 10f, 936f, 21f);
            TMP_Text infoText = VoxUi.Txt("Info", info.transform, "", 26, TextFont.Body, TextTone.OnPanel, TextAlignmentOptions.TopLeft);
            infoText.richText = true;
            infoText.textWrappingMode = TextWrappingModes.Normal;
            infoText.overflowMode = TextOverflowModes.Truncate;
            VoxUi.Stretch(infoText.rectTransform, 32f, 18f, 28f, 40f);

            // Right: tabs + card panel
            Image panel = VoxUi.Themed("Inventory", root, ThemeRole.Panel, true);
            VoxUi.TopRight(panel.rectTransform, 50f, 136f, 794f, 726f);

            RectTransform tabsRoot = VoxUi.R("Tabs", root);
            VoxUi.TopRight(tabsRoot, 50f, 136f, 794f, 70f);
            var tabs = tabsRoot.gameObject.AddComponent<ArmoryTabs>();
            var tabsLayout = tabsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.padding = new RectOffset(8, 8, 0, 0);
            tabsLayout.childAlignment = TextAnchor.MiddleLeft;
            tabsLayout.childControlWidth = tabsLayout.childControlHeight = true;
            tabsLayout.childForceExpandWidth = tabsLayout.childForceExpandHeight = false;
            Button armsTab = Tab(tabsRoot, "ArmsTab", "Arms", 250f, out Image armsImage, out TMP_Text armsLabel);
            Button armamentsTab = Tab(tabsRoot, "ArmamentsTab", "Armaments", 370f, out Image armamentsImage, out TMP_Text armamentsLabel);
            Image tabStrip = VoxUi.Trim("Strip", tabsRoot, TrimColor.Leaf);
            var tabStripLe = tabStrip.gameObject.AddComponent<LayoutElement>();
            tabStripLe.flexibleWidth = 1f;
            tabStripLe.minHeight = 66f;
            TabBadge(armsImage.transform, "L1", false);
            TabBadge(armamentsImage.transform, "R1", true);
            VoxUi.SetRef(tabs, "armsButton", armsTab);
            VoxUi.SetRef(tabs, "armamentsButton", armamentsTab);
            VoxUi.SetRef(tabs, "armsImage", armsImage);
            VoxUi.SetRef(tabs, "armamentsImage", armamentsImage);
            VoxUi.SetRef(tabs, "armsLabel", armsLabel);
            VoxUi.SetRef(tabs, "armamentsLabel", armamentsLabel);

            RectTransform viewport = VoxUi.R("Viewport", panel.transform);
            VoxUi.Stretch(viewport, 18f, 18f, 18f, 92f);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = VoxUi.R("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(168f, 238f);
            grid.spacing = new Vector2(20f, 26f);
            grid.padding = new RectOffset(22, 22, 22, 22);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
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
            VoxUi.SetRef(follow, "scroll", scroll);
            TMP_Text empty = VoxUi.Txt("Empty", panel.transform, "", 32, TextFont.Button, TextTone.Muted);
            VoxUi.Stretch(empty.rectTransform, 30f, 30f, 30f, 120f);

            // Buttons and hint (the hint tucks under Fight!, like the mockup)
            TMP_Text hint = VoxUi.HintPill("Hint", root, 50f, 152f, 450f, 60f, fromRight: true);
            Button fight = VoxUi.Btn("Fight", root, "Fight!", 420f, 104f, 62f, ButtonKind.Primary);
            VoxUi.BottomRight((RectTransform)fight.transform, 50f, 40f, 420f, 104f);
            Button remove = VoxUi.Btn("Remove", root, "Remove", 210f, 60f, 32f);
            VoxUi.BottomRight((RectTransform)remove.transform, 490f, 60f, 210f, 60f);
            Button menu = VoxUi.Btn("MainMenu", root, "Main Menu", 210f, 58f, 28f);
            VoxUi.TopRight((RectTransform)menu.transform, 50f, 40f, 210f, 58f);

            // Wire the screen
            VoxUi.SetRef(screen, "ring", ring);
            VoxUi.SetRef(screen, "bubbleRoot", feet.Find("Bubbles"));
            VoxUi.SetRef(screen, "infoLabel", infoText);
            VoxUi.SetRef(screen, "tabs", tabs);
            VoxUi.SetRef(screen, "cardPrefab", cardPrefab);
            VoxUi.SetRef(screen, "gridContent", content);
            VoxUi.SetRef(screen, "gridScroll", scroll);
            VoxUi.SetRef(screen, "emptyLabel", empty);
            VoxUi.SetRef(screen, "placeholderIcon", circle);
            VoxUi.SetRef(screen, "titleLabel", title);
            VoxUi.SetRef(screen, "messageLabel", message);
            VoxUi.SetRef(screen, "hintLabel", hint);
            VoxUi.SetRef(screen, "removeButton", remove);
            VoxUi.SetRef(screen, "fightButton", fight);
            VoxUi.SetRef(screen, "menuButton", menu);
            RewireStartRoute(old, old, fight);
            // the Feet ring sits under the info panel and tabs in sibling order: the stage draws first
            feet.SetSiblingIndex(1);
            old.SetActive(false);
            EditorUtility.SetDirty(screen);
            _ = bubblePrefab;
        }

        private static void TabBadge(Transform tab, string text, bool right)
        {
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
            Image disc = VoxUi.Img("Badge" + text, tab, circle, Image.Type.Simple, UITheme.Current.InkSoil);
            RectTransform r = disc.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(right ? 1f : 0f, 0.5f);
            r.pivot = new Vector2(right ? 1f : 0f, 0.5f);
            r.anchoredPosition = new Vector2(right ? -14f : 14f, 2f);
            r.sizeDelta = new Vector2(34f, 34f);
            TMP_Text label = VoxUi.Txt("Label", r, text, 17, TextFont.Button, TextTone.OnDark);
            VoxUi.Stretch(label.rectTransform);
        }

        private static Button Tab(Transform parent, string name, string label, float width, out Image image, out TMP_Text text)
        {
            image = VoxUi.Themed(name, parent, ThemeRole.Tab, true);
            var le = image.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minHeight = 66f;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            image.gameObject.AddComponent<CancelRelay>();
            text = VoxUi.Txt("Label", image.transform, label, 36, TextFont.Button, TextTone.Custom);
            VoxUi.Stretch(text.rectTransform, 46f, 6f, 46f, 0f);
            return button;
        }

        // Restyle the ring's stage objects in place: pedestal, slot medallions, tethers, bubbles.
        private static void RebuildStage(RectTransform feet, ArmoryRing ring, UITheme theme)
        {
            // Pedestal: replace the three placeholder ellipses with the kit pedestal; add the dashed ring and spotlight.
            Transform pedestal = feet.Find("Pedestal");
            VoxUi.ClearChildren(pedestal);
            Image pedestalImage = VoxUi.Img("PedestalBase", pedestal, theme.Pedestal, Image.Type.Simple);
            pedestalImage.preserveAspect = true;
            VoxUi.Center(pedestalImage.rectTransform, 0f, 0f, 570f, 153f);

            Transform oldRing = feet.Find("DashedRing");
            if (oldRing != null)
                Object.DestroyImmediate(oldRing.gameObject);
            var dashed = AssetDatabase.LoadAssetAtPath<Sprite>(DashedRingPath);
            Image dashedImage = VoxUi.Img("DashedRing", feet, dashed, Image.Type.Simple, theme.Sage);
            dashedImage.preserveAspect = false;
            var so = new SerializedObject(ring);
            float ppu = so.FindProperty("pixelsPerUnit").floatValue;
            var tuning = so.FindProperty("ring").objectReferenceValue as ArmRingTuning;
            float rx = (tuning != null ? tuning.RadiusX : 0.85f) * ppu;
            float ry = (tuning != null ? tuning.RadiusY : 0.51f) * ppu;
            float voff = (tuning != null ? tuning.VerticalOffset : 0.1f) * ppu;
            VoxUi.Center(dashedImage.rectTransform, 0f, voff, rx * 2f + 24f, ry * 2f + 24f);
            dashedImage.transform.SetSiblingIndex(0);
            pedestal.SetSiblingIndex(1);

            // Slot medallions
            for (int i = 0; i < ArmLoadout.SlotCount; i++)
            {
                Transform slot = feet.Find("Slot" + i);
                if (slot == null)
                    continue;
                VoxUi.ClearChildren(slot);
                var rect = (RectTransform)slot;
                rect.sizeDelta = new Vector2(92f, 100f);
                var slotButton = slot.GetComponent<ArmorySlotButton>();
                RectTransform body = VoxUi.R("Body", slot);
                VoxUi.Stretch(body);
                Image focus = VoxUi.Img("FocusRing", body, theme.ArmSlotSelected, Image.Type.Simple);
                focus.preserveAspect = true;
                VoxUi.Stretch(focus.rectTransform, -16f, -12f, -16f, -12f);
                Image frame = VoxUi.Img("Frame", body, theme.ArmSlotFilled, Image.Type.Simple);
                frame.preserveAspect = true;
                VoxUi.Stretch(frame.rectTransform);
                TMP_Text label = VoxUi.Txt("Label", body, "+", 44, TextFont.Button, TextTone.Faint);
                VoxUi.Stretch(label.rectTransform);
                VoxUi.SetRef(slotButton, "body", body);
                VoxUi.SetRef(slotButton, "frame", frame);
                VoxUi.SetRef(slotButton, "focusRing", focus);
                VoxUi.SetRef(slotButton, "label", label);
                VoxUi.SetFloat(slotButton, "filledAlpha", 0.3f);
                VoxUi.SetFloat(slotButton, "emptyAlpha", 0.85f);
            }

            // Tethers: one tiled vine strand each (no glow)
            foreach (var tether in feet.GetComponentsInChildren<ArmoryTether>(true))
            {
                VoxUi.SetFloat(tether, "strandWidth", 12f);
                VoxUi.SetFloat(tether, "glowAlpha", 0f);
                VoxUi.SetFloat(tether, "strandAlpha", 1f);
            }
        }
    }
}
