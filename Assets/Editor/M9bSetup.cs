using System.Linq;
using BulletHell.Core;
using BulletHell.Input;
using BulletHell.Save;
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
    /// One-shot M9b setup: RarityTable, ShopTuning and the reworked ShopPool assets, arm rarities and price tiers, the
    /// ShopCard prefab, and the new card-based Shop screen in the Game scene (replacing the old list screen). Safe to run
    /// again: assets are updated, the prefab and the screen are rebuilt.
    /// </summary>
    public static class M9bSetup
    {
        private const string ShopDir = "Assets/Data/Shop";
        private const string ArmDir = "Assets/Data/Arms";
        private const string ConfigPath = "Assets/Resources/GameConfig.asset";
        private const string CardPrefabPath = "Assets/Prefabs/UI/ShopCard.prefab";
        private const string GameScene = "Assets/Scenes/Game.unity";
        private const string CirclePath = "Assets/Art/Placeholder/Circle.png";
        private const string GlyphsPath = "Assets/Data/UI/ButtonGlyphs.asset";

        private static readonly Vector2 CardSize = new Vector2(210f, 290f);

        [MenuItem("BulletHell/M9b/Setup Everything")]
        public static void Run()
        {
            CreateDataAssets();
            GameObject card = BuildCardPrefab();
            BuildShopScreen(card);
            AssetDatabase.SaveAssets();
            Debug.Log("M9b setup complete.");
        }

        // ---------------------------------------------------------------- data

        private static void CreateDataAssets()
        {
            var table = LoadOrCreate<RarityTable>(ShopDir + "/RarityTable.asset");
            var tuning = LoadOrCreate<ShopTuning>(ShopDir + "/ShopTuning.asset");
            var pool = AssetDatabase.LoadAssetAtPath<ShopPool>(ShopDir + "/ShopPool.asset");
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<ShopPool>();
                AssetDatabase.CreateAsset(pool, ShopDir + "/ShopPool.asset");
            }

            // Arm rarities and price tiers (rarer arms cost more and get more armament slots).
            SetArm("Arm_Red", ArmamentRarity.Common, 2);
            SetArm("Arm_Blue", ArmamentRarity.Common, 2);
            SetArm("Arm_Green", ArmamentRarity.Rare, 3);
            SetArm("Arm_Ember", ArmamentRarity.Rare, 3);
            SetArm("Arm_Purple", ArmamentRarity.Epic, 4);
            SetArm("Arm_Piercer", ArmamentRarity.Epic, 4);

            // The pool draws from every registered arm and armament.
            var registry = AssetDatabase.LoadAssetAtPath<AssetRegistry>("Assets/Data/AssetRegistry.asset");
            WeaponArmData[] arms = AssetDatabase.FindAssets("t:WeaponArmData")
                .Select(g => AssetDatabase.LoadAssetAtPath<WeaponArmData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null).OrderBy(a => a.name).ToArray();
            ArmamentData[] armaments = registry.Armaments.Where(a => a != null).ToArray();
            pool.Set(arms, armaments);

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var so = new SerializedObject(config);
            so.FindProperty("shopPool").objectReferenceValue = pool;
            so.FindProperty("rarityTable").objectReferenceValue = table;
            so.FindProperty("shopTuning").objectReferenceValue = tuning;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static void SetArm(string name, ArmamentRarity rarity, int tier)
        {
            var arm = AssetDatabase.LoadAssetAtPath<WeaponArmData>($"{ArmDir}/{name}.asset");
            if (arm != null)
                arm.SetShopMeta(rarity, tier);
        }

        // ---------------------------------------------------------------- card prefab

        private static GameObject BuildCardPrefab()
        {
            var root = new GameObject("ShopCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CancelRelay), typeof(ShopCard));
            var rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = CardSize;
            var hit = root.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f); // invisible, only there so the whole card is clickable
            var button = root.GetComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;

            RectTransform body = Rect("Body", rootRect);
            UiBuilder.Stretch(body);

            RectTransform frameRect = Rect("Frame", body);
            UiBuilder.Stretch(frameRect);
            var frame = frameRect.gameObject.AddComponent<Image>();
            frame.raycastTarget = false;
            var themed = frameRect.gameObject.AddComponent<ThemedImage>();
            themed.KeepColor = true;
            themed.Role = ThemeRole.Card;

            RectTransform ringRect = Rect("FocusRing", body);
            ringRect.anchorMin = Vector2.zero;
            ringRect.anchorMax = Vector2.one;
            ringRect.offsetMin = new Vector2(-14f, -14f);
            ringRect.offsetMax = new Vector2(14f, 14f);
            var ring = ringRect.gameObject.AddComponent<Image>();
            ring.raycastTarget = false;

            RectTransform iconRect = Rect("Icon", body);
            iconRect.anchoredPosition = new Vector2(0f, 20f);
            iconRect.sizeDelta = new Vector2(112f, 112f);
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Text letter = UiBuilder.CreateText("IconLetter", body, "?", 64, TextAnchor.MiddleCenter);
            Place((RectTransform)letter.transform, new Vector2(0f, 20f), new Vector2(112f, 112f));
            letter.color = new Color(0.15f, 0.1f, 0.1f);
            letter.fontStyle = FontStyle.Bold;

            Text nameLabel = UiBuilder.CreateText("Name", body, "Name", 26, TextAnchor.MiddleCenter);
            var nameRect = (RectTransform)nameLabel.transform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(0f, -8f);
            nameRect.sizeDelta = new Vector2(-24f, 56f);
            nameLabel.color = new Color(0.25f, 0.13f, 0.08f);
            nameLabel.fontStyle = FontStyle.Bold;
            nameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameLabel.resizeTextForBestFit = true;
            nameLabel.resizeTextMinSize = 16;
            nameLabel.resizeTextMaxSize = 26;

            Text rarityLabel = UiBuilder.CreateText("Rarity", body, "COMMON", 22, TextAnchor.MiddleCenter);
            Place((RectTransform)rarityLabel.transform, new Vector2(0f, -50f), new Vector2(180f, 30f));
            rarityLabel.fontStyle = FontStyle.Bold;

            RectTransform tagRect = Rect("PriceTag", body);
            tagRect.anchorMin = tagRect.anchorMax = new Vector2(0.5f, 0f);
            tagRect.pivot = new Vector2(0.5f, 0f);
            tagRect.anchoredPosition = new Vector2(0f, 14f);
            tagRect.sizeDelta = new Vector2(150f, 52f);
            var tag = tagRect.gameObject.AddComponent<Image>();
            tag.raycastTarget = false;
            var tagThemed = tagRect.gameObject.AddComponent<ThemedImage>();
            tagThemed.Role = ThemeRole.Tab;
            Text price = UiBuilder.CreateText("PriceLabel", tagRect, "100", 32, TextAnchor.MiddleCenter);
            UiBuilder.Stretch((RectTransform)price.transform);
            price.fontStyle = FontStyle.Bold;

            RectTransform soldRect = Rect("Sold", body);
            UiBuilder.Stretch(soldRect);
            var soldImage = soldRect.gameObject.AddComponent<Image>();
            soldImage.color = new Color(0f, 0f, 0f, 0.6f);
            soldImage.raycastTarget = false;
            var soldGroup = soldRect.gameObject.AddComponent<CanvasGroup>();
            soldGroup.blocksRaycasts = false;
            Text soldText = UiBuilder.CreateText("SoldLabel", soldRect, "SOLD", 64, TextAnchor.MiddleCenter);
            UiBuilder.Stretch((RectTransform)soldText.transform);
            soldText.color = new Color(1f, 0.25f, 0.2f);
            soldText.fontStyle = FontStyle.Bold;
            soldText.horizontalOverflow = HorizontalWrapMode.Overflow;
            soldText.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
            soldRect.gameObject.SetActive(false);

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
            so.FindProperty("soldStamp").objectReferenceValue = soldRect.gameObject;
            so.FindProperty("soldGroup").objectReferenceValue = soldGroup;
            so.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ---------------------------------------------------------------- the screen

        private static void BuildShopScreen(GameObject cardPrefab)
        {
            EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            Transform safe = GameObject.Find("FlowUI/SafeArea").transform;
            GameObject oldRoot = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.gameObject).FirstOrDefault(g => g.name == "ShopScreen");
            if (oldRoot != null)
                Object.DestroyImmediate(oldRoot);

            RectTransform root = Rect("ShopScreen", safe);
            UiBuilder.Stretch(root);
            var screen = root.gameObject.AddComponent<ShopScreen>();

            // Backdrop
            RectTransform backdrop = Rect("Backdrop", root);
            UiBuilder.Stretch(backdrop);
            var back = backdrop.gameObject.AddComponent<Image>();
            back.color = new Color(0.1f, 0.06f, 0.05f, 0.94f);

            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
            BuildMerchant(root, circle);

            // Stall: the panel behind the cards
            RectTransform stall = Rect("Stall", root);
            Place(stall, new Vector2(230f, 25f), new Vector2(1240f, 770f));
            stall.gameObject.AddComponent<Image>();
            stall.gameObject.AddComponent<ThemedImage>().Role = ThemeRole.Panel;

            RectTransform header = Rect("Header", stall);
            header.anchorMin = header.anchorMax = new Vector2(0.5f, 1f);
            header.pivot = new Vector2(0.5f, 0.5f);
            header.anchoredPosition = new Vector2(0f, 8f);
            header.sizeDelta = new Vector2(560f, 110f);
            header.gameObject.AddComponent<Image>();
            header.gameObject.AddComponent<ThemedImage>().Role = ThemeRole.Header;
            Text title = UiBuilder.CreateText("Title", header, "Shop - round 1", 40, TextAnchor.MiddleCenter);
            UiBuilder.Stretch((RectTransform)title.transform);
            title.color = new Color(0.16f, 0.32f, 0.06f);
            title.fontStyle = FontStyle.Bold;

            // Cards: 2 arms on top, 3 armaments below, the crate to the right of them.
            var armCards = new ShopCard[2];
            var armamentCards = new ShopCard[3];
            float[] topX = { -230f, 30f };
            float[] bottomX = { -360f, -100f, 160f };
            for (int i = 0; i < armCards.Length; i++)
                armCards[i] = Card(cardPrefab, stall, "ArmCard" + (i + 1), new Vector2(topX[i], 150f), CardSize);
            for (int i = 0; i < armamentCards.Length; i++)
                armamentCards[i] = Card(cardPrefab, stall, "ArmamentCard" + (i + 1), new Vector2(bottomX[i], -170f), CardSize);
            ShopCard crate = Card(cardPrefab, stall, "CrateCard", new Vector2(440f, -20f), new Vector2(240f, 330f));

            Text message = UiBuilder.CreateText("Message", root, "", 30, TextAnchor.MiddleCenter);
            Place((RectTransform)message.transform, new Vector2(230f, -388f), new Vector2(1200f, 44f));

            // Currency (top-right)
            RectTransform currency = Rect("Currency", root);
            currency.anchorMin = currency.anchorMax = new Vector2(1f, 1f);
            currency.pivot = new Vector2(1f, 1f);
            currency.anchoredPosition = new Vector2(-40f, -32f);
            currency.sizeDelta = new Vector2(340f, 96f);
            currency.gameObject.AddComponent<Image>();
            var currencyThemed = currency.gameObject.AddComponent<ThemedImage>();
            currencyThemed.PixelScaleOverride = 4f;
            currencyThemed.Role = ThemeRole.Slot;
            Text coinsLabel = UiBuilder.CreateText("Coins", currency, "Coins", 26, TextAnchor.MiddleLeft);
            Place((RectTransform)coinsLabel.transform, new Vector2(-70f, 0f), new Vector2(150f, 60f));
            coinsLabel.color = new Color(0.33f, 0.16f, 0.1f);
            Text currencyValue = UiBuilder.CreateText("Value", currency, "0", 52, TextAnchor.MiddleRight);
            Place((RectTransform)currencyValue.transform, new Vector2(70f, 0f), new Vector2(160f, 70f));
            currencyValue.color = new Color(0.33f, 0.16f, 0.1f);
            currencyValue.fontStyle = FontStyle.Bold;

            // Buttons
            Button reroll = UiBuilder.CreateButton("Reroll", root, "Reroll", 90f);
            Place((RectTransform)reroll.transform, new Vector2(70f, -448f), new Vector2(340f, 84f));
            Button leave = UiBuilder.CreateButton("Leave", root, "Leave", 90f);
            Place((RectTransform)leave.transform, new Vector2(420f, -448f), new Vector2(300f, 84f));
            Button menu = UiBuilder.CreateButton("MainMenu", root, "Main Menu", 60f);
            Place((RectTransform)menu.transform, new Vector2(-830f, 405f), new Vector2(240f, 64f));
            foreach (Button b in new[] { reroll, leave, menu })
                b.gameObject.AddComponent<CancelRelay>();
            Text rerollLabel = reroll.GetComponentInChildren<Text>();
            foreach (Text t in new[] { rerollLabel, leave.GetComponentInChildren<Text>() })
                t.fontSize = 40;

            // Inventory: where bought cards fly to
            RectTransform inventory = Rect("Inventory", root);
            Place(inventory, new Vector2(-760f, -400f), new Vector2(150f, 150f));
            inventory.gameObject.AddComponent<Image>();
            var inventoryThemed = inventory.gameObject.AddComponent<ThemedImage>();
            inventoryThemed.PixelScaleOverride = 5f;
            inventoryThemed.Role = ThemeRole.Slot;
            Text bag = UiBuilder.CreateText("Bag", inventory, "BAG", 40, TextAnchor.MiddleCenter);
            UiBuilder.Stretch((RectTransform)bag.transform);
            bag.color = new Color(0.33f, 0.16f, 0.1f);
            bag.fontStyle = FontStyle.Bold;
            Text inventoryLabel = UiBuilder.CreateText("InventoryLabel", root, "Arms 0   Armaments 0", 26, TextAnchor.MiddleCenter);
            Place((RectTransform)inventoryLabel.transform, new Vector2(-760f, -500f), new Vector2(330f, 40f));

            Text hint = UiBuilder.CreateText("Hint", root, "", 26, TextAnchor.MiddleCenter);
            Place((RectTransform)hint.transform, new Vector2(230f, -507f), new Vector2(1200f, 36f));
            hint.color = new Color(0.8f, 0.72f, 0.6f);

            // Crate choice overlay (3 big cards)
            RectTransform overlay = Rect("CrateOverlay", root);
            UiBuilder.Stretch(overlay);
            var overlayDim = overlay.gameObject.AddComponent<Image>();
            overlayDim.color = new Color(0f, 0f, 0f, 0.82f);
            Text overlayTitle = UiBuilder.CreateText("Title", overlay, "The crate holds 3 armaments. Pick 1.", 48, TextAnchor.MiddleCenter);
            Place((RectTransform)overlayTitle.transform, new Vector2(0f, 262f), new Vector2(1400f, 70f));
            var choices = new ShopCard[3];
            float[] choiceX = { -330f, 0f, 330f };
            for (int i = 0; i < choices.Length; i++)
                choices[i] = Card(cardPrefab, overlay, "Choice" + (i + 1), new Vector2(choiceX[i], 20f), new Vector2(270f, 372f));
            overlay.gameObject.SetActive(false);

            // Tooltip (last: it draws over everything)
            RectTransform tip = Rect("Tooltip", root);
            tip.sizeDelta = new Vector2(620f, 100f);
            tip.gameObject.AddComponent<Image>();
            tip.gameObject.AddComponent<ThemedImage>().Role = ThemeRole.Tooltip;
            var tipLayout = tip.gameObject.AddComponent<VerticalLayoutGroup>();
            tipLayout.padding = new RectOffset(40, 40, 36, 36);
            tipLayout.childControlWidth = tipLayout.childControlHeight = true;
            tipLayout.childForceExpandWidth = true;
            tipLayout.childForceExpandHeight = false;
            tip.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Text tipText = UiBuilder.CreateText("Body", tip, "", 26, TextAnchor.UpperLeft);
            tipText.supportRichText = true;
            tipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            var tooltip = root.gameObject.AddComponent<ShopTooltip>();
            var tipSo = new SerializedObject(tooltip);
            tipSo.FindProperty("panel").objectReferenceValue = tip;
            tipSo.FindProperty("body").objectReferenceValue = tipText;
            tipSo.ApplyModifiedPropertiesWithoutUndo();
            tip.gameObject.SetActive(false);

            // Wire the screen
            var so = new SerializedObject(screen);
            SetArray(so.FindProperty("armCards"), armCards);
            SetArray(so.FindProperty("armamentCards"), armamentCards);
            so.FindProperty("crateCard").objectReferenceValue = crate;
            so.FindProperty("placeholderIcon").objectReferenceValue = circle;
            so.FindProperty("titleLabel").objectReferenceValue = title;
            so.FindProperty("currencyLabel").objectReferenceValue = currencyValue;
            so.FindProperty("messageLabel").objectReferenceValue = message;
            so.FindProperty("hintLabel").objectReferenceValue = hint;
            so.FindProperty("inventoryLabel").objectReferenceValue = inventoryLabel;
            so.FindProperty("rerollButton").objectReferenceValue = reroll;
            so.FindProperty("rerollLabel").objectReferenceValue = rerollLabel;
            so.FindProperty("leaveButton").objectReferenceValue = leave;
            so.FindProperty("menuButton").objectReferenceValue = menu;
            so.FindProperty("inventoryTarget").objectReferenceValue = inventory;
            so.FindProperty("tooltip").objectReferenceValue = tooltip;
            so.FindProperty("crateOverlay").objectReferenceValue = overlay.gameObject;
            SetArray(so.FindProperty("crateChoiceCards"), choices);
            so.FindProperty("input").objectReferenceValue = Object.FindFirstObjectByType<MenuInputReader>(FindObjectsInactive.Include);
            so.FindProperty("glyphs").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ButtonGlyphLibrary>(GlyphsPath);
            so.ApplyModifiedPropertiesWithoutUndo();

            // GameFlowUI shows this screen; the Start button leaves the Shop.
            var flow = Object.FindFirstObjectByType<GameFlowUI>(FindObjectsInactive.Include);
            var flowSo = new SerializedObject(flow);
            flowSo.FindProperty("shop").objectReferenceValue = screen;
            flowSo.ApplyModifiedPropertiesWithoutUndo();
            RewireStartRoutes(oldRoot, root.gameObject, leave);

            root.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void BuildMerchant(RectTransform root, Sprite circle)
        {
            RectTransform merchant = Rect("Merchant", root);
            Place(merchant, new Vector2(-720f, 20f), new Vector2(420f, 640f));

            Image Blob(string name, Vector2 pos, Vector2 size, Color color)
            {
                RectTransform r = Rect(name, merchant);
                Place(r, pos, size);
                var image = r.gameObject.AddComponent<Image>();
                image.sprite = circle;
                image.color = color;
                image.raycastTarget = false;
                return image;
            }

            // A placeholder mercator (an olive): body, head, eyes, laurel. Final art replaces this whole group.
            Blob("Body", new Vector2(0f, -110f), new Vector2(330f, 380f), new Color(0.42f, 0.5f, 0.2f));
            Blob("Belly", new Vector2(0f, -150f), new Vector2(210f, 230f), new Color(0.6f, 0.68f, 0.32f));
            Blob("Head", new Vector2(0f, 150f), new Vector2(230f, 250f), new Color(0.5f, 0.6f, 0.25f));
            Blob("Pimento", new Vector2(0f, 190f), new Vector2(60f, 60f), new Color(0.85f, 0.2f, 0.15f));
            Blob("EyeL", new Vector2(-45f, 100f), new Vector2(30f, 38f), new Color(0.1f, 0.08f, 0.06f));
            Blob("EyeR", new Vector2(45f, 100f), new Vector2(30f, 38f), new Color(0.1f, 0.08f, 0.06f));
            Blob("Laurel", new Vector2(0f, 250f), new Vector2(210f, 60f), new Color(0.3f, 0.65f, 0.25f));

            Text name = UiBuilder.CreateText("Nameplate", merchant, "Mercator Oliva", 34, TextAnchor.MiddleCenter);
            Place((RectTransform)name.transform, new Vector2(0f, -300f), new Vector2(420f, 50f));
            name.fontStyle = FontStyle.Bold;
        }

        private static void RewireStartRoutes(GameObject oldRoot, GameObject newRoot, Button leave)
        {
            foreach (var router in Object.FindObjectsByType<MenuPrimaryRouter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(router);
                SerializedProperty routes = so.FindProperty("routes");
                bool found = false;
                for (int i = 0; i < routes.arraySize; i++)
                {
                    SerializedProperty scope = routes.GetArrayElementAtIndex(i).FindPropertyRelative("Scope");
                    // A route whose scope was the old Shop root (now destroyed) or is empty is the Shop's route.
                    if (scope.objectReferenceValue == null || scope.objectReferenceValue == oldRoot)
                    {
                        scope.objectReferenceValue = newRoot;
                        routes.GetArrayElementAtIndex(i).FindPropertyRelative("Button").objectReferenceValue = leave;
                        found = true;
                    }
                }
                if (!found)
                {
                    routes.arraySize++;
                    SerializedProperty added = routes.GetArrayElementAtIndex(routes.arraySize - 1);
                    added.FindPropertyRelative("Scope").objectReferenceValue = newRoot;
                    added.FindPropertyRelative("Button").objectReferenceValue = leave;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ---------------------------------------------------------------- helpers

        private static ShopCard Card(GameObject prefab, Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return go.GetComponent<ShopCard>();
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
