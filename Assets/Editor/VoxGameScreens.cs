using BulletHell.Pickups;
using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Input;
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
    /// VOX VEGETALLIS look of the Game scene UI: the combat HUD (tomato hearts, heat bar, portrait ring, ammo slots, round
    /// and currency pills), banners, the flow panels, Settings (from Pause) and the confirm dialog. Shop and Armory are built by
    /// <see cref="VoxShopArmory"/>. Safe to run again.
    /// </summary>
    public static class VoxGameScreens
    {
        private const string GameScene = "Assets/Scenes/Game.unity";
        private const string GlyphsPath = "Assets/Data/UI/ButtonGlyphs.asset";

        [MenuItem("BulletHell/Vox/8 Build Game HUD And Panels")]
        public static void BuildGameHudAndPanels()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            VoxMenuScreens.BuildRowPrefabs();
            Transform safe = GameObject.Find("FlowUI/SafeArea").transform;

            BuildHud(safe);
            RestyleBanners(safe);

            foreach (var panel in Object.FindObjectsByType<FlowPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                VoxMenuScreens.RestyleFlowPanel(panel);
            var settings = Object.FindFirstObjectByType<SettingsScreen>(FindObjectsInactive.Include);
            if (settings != null)
                VoxMenuScreens.RebuildSettings(settings, 0.8f);   // over the paused arena: dim it enough that the title and rows read
            var confirm = Object.FindFirstObjectByType<ConfirmDialog>(FindObjectsInactive.Include);
            if (confirm != null)
            {
                VoxMenuScreens.RebuildConfirm(confirm);
                confirm.transform.SetAsLastSibling();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Vox: Game HUD and panels built.");
        }

        // ---------------------------------------------------------------- HUD

        private static void BuildHud(Transform safe)
        {
            UITheme theme = UITheme.Current;
            Transform existing = safe.Find("CombatHud");
            GameObject oldHud = existing != null ? existing.gameObject : null;
            CombatHud oldCombat = oldHud != null ? oldHud.GetComponent<CombatHud>() : null;

            // Keep the CombatHud root (the flow UI and tests find it by type); rebuild what is inside it.
            RectTransform hud;
            CombatHud combat;
            if (oldCombat != null)
            {
                hud = (RectTransform)oldHud.transform;
                VoxUi.ClearChildren(hud);
                combat = oldCombat;
            }
            else
            {
                hud = VoxUi.R("CombatHud", safe);
                VoxUi.Stretch(hud);
                hud.SetSiblingIndex(0);
                combat = hud.gameObject.AddComponent<CombatHud>();
            }
            RectTransform content = VoxUi.R("Content", hud);
            VoxUi.Stretch(content);

            // ---- bottom left: marble panel with hearts + heat, portrait ring on top
            RectTransform left = VoxUi.R("BottomLeft", content);
            VoxUi.BottomLeft(left, 20f, 20f, 580f, 190f);
            left.gameObject.AddComponent<HudScale>();   // Settings > Gameplay > HUD Scale

            Image panel = VoxUi.Themed("Panel", left, ThemeRole.Panel);
            VoxUi.BottomLeft(panel.rectTransform, 96f, 0f, 480f, 152f);
            Image trim = VoxUi.Trim("Trim", panel.transform, TrimColor.Leaf);
            trim.rectTransform.anchorMin = new Vector2(0f, 1f);
            trim.rectTransform.anchorMax = new Vector2(1f, 1f);
            trim.rectTransform.pivot = new Vector2(0.5f, 1f);
            trim.rectTransform.offsetMin = new Vector2(14f, -34f);
            trim.rectTransform.offsetMax = new Vector2(-14f, -13f);

            HudPortrait portrait = VoxMenuScreens.BuildPortrait(left, 1f);
            VoxUi.BottomLeft((RectTransform)portrait.transform, 0f, 0f, 174f, 183f);

            RectTransform heartsRect = VoxUi.R("Hearts", left);
            VoxUi.BottomLeft(heartsRect, 196f, 76f, 380f, 50f);
            var heartImages = new Image[8];
            for (int i = 0; i < heartImages.Length; i++)
            {
                Image heart = VoxUi.Img("Heart" + (i + 1), heartsRect, theme.HeartFull, Image.Type.Simple);
                heart.preserveAspect = true;
                VoxUi.TopLeft(heart.rectTransform, i * 48f, 0f, 46f, 46f);
                heart.gameObject.SetActive(i < 5);
                heartImages[i] = heart;
            }
            var hearts = heartsRect.gameObject.AddComponent<HudHearts>();
            VoxUi.SetArray(hearts, "hearts", heartImages);
            VoxUi.SetRef(hearts, "fullHeart", theme.HeartFull);
            VoxUi.SetRef(hearts, "emptyHeart", theme.HeartEmpty);

            TMP_Text heatWord = VoxUi.Txt("HeatLabel", left, "HEAT", 22, TextFont.BodyBold, TextTone.Muted, TextAlignmentOptions.Left, caps: true);
            VoxUi.BottomLeft(heatWord.rectTransform, 198f, 30f, 80f, 30f);

            RectTransform heatRect = VoxUi.R("HeatBar", left);
            VoxUi.BottomLeft(heatRect, 280f, 28f, 272f, 30f);
            Image track = VoxUi.Img("Track", heatRect, theme.HeatTrack, Image.Type.Sliced);
            VoxUi.Stretch(track.rectTransform);
            Image fill = VoxUi.Img("Fill", heatRect, theme.HeatFill, Image.Type.Sliced, theme.Leaf);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = new Vector2(7f, 7f);
            fill.rectTransform.offsetMax = new Vector2(-7f, -7f);
            var group = heatRect.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var heat = heatRect.gameObject.AddComponent<HudHeatBar>();
            VoxUi.SetRef(heat, "fill", fill);
            VoxUi.SetRef(heat, "group", group);

            // ---- bottom right: dark tray with four ammo slots
            RectTransform right = VoxUi.R("BottomRight", content);
            VoxUi.BottomRight(right, 20f, 20f, 580f, 172f);
            right.gameObject.AddComponent<HudScale>();
            Image tray = VoxUi.Themed("Tray", right, ThemeRole.HintBar);
            tray.GetComponent<ThemedImage>().KeepColor = true;
            tray.color = new Color(1f, 1f, 1f, 0.88f);
            VoxUi.Stretch(tray.rectTransform);
            Sprite ringSprite = M75Art.Load("Ring");
            var slots = new HudAmmoSlot[AmmoSlotSet.Count];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = BuildSlot(right, i, ringSprite, theme);

            // ---- top: round / wave pill and currency pill (RunHud)
            var runHud = Object.FindFirstObjectByType<RunHud>(FindObjectsInactive.Include);
            BuildRunHud(runHud, safe, theme);

            // ---- controller
            var player = Object.FindFirstObjectByType<PlayerHealth>();
            var so = new SerializedObject(combat);
            so.FindProperty("health").objectReferenceValue = player;
            so.FindProperty("arms").objectReferenceValue = player.GetComponent<ArmSelectionController>();
            so.FindProperty("fire").objectReferenceValue = player.GetComponent<ArmFireController>();
            so.FindProperty("ammo").objectReferenceValue = player.GetComponent<AmmoSlots>();
            so.FindProperty("pickups").objectReferenceValue = player.GetComponent<AmmoPickupCollector>();
            so.FindProperty("content").objectReferenceValue = content.gameObject;
            so.FindProperty("portrait").objectReferenceValue = portrait;
            so.FindProperty("hearts").objectReferenceValue = hearts;
            so.FindProperty("heatBar").objectReferenceValue = heat;
            so.FindProperty("glyphs").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ButtonGlyphLibrary>(GlyphsPath);
            SerializedProperty slotList = so.FindProperty("slots");
            slotList.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
                slotList.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(combat);
        }

        private static HudAmmoSlot BuildSlot(RectTransform parent, int index, Sprite ring, UITheme theme)
        {
            RectTransform slot = VoxUi.R("Slot" + (index + 1), parent);
            VoxUi.BottomLeft(slot, 26f + index * 133f, 24f, 113f, 123f);
            Image highlight = VoxUi.Img("Highlight", slot, theme.SlotActiveRing, Image.Type.Simple);
            highlight.preserveAspect = true;
            VoxUi.Stretch(highlight.rectTransform, -11f, -9f, -11f, -9f);
            Image frame = VoxUi.Img("Frame", slot, theme.SlotAmmo, Image.Type.Simple);
            frame.preserveAspect = true;
            VoxUi.Stretch(frame.rectTransform);
            Image icon = VoxUi.Img("Icon", slot, null, Image.Type.Simple);
            icon.preserveAspect = true;
            VoxUi.Center(icon.rectTransform, 0f, 6f, 78f, 78f);
            Image hold = VoxUi.Img("HoldRing", slot, ring, Image.Type.Filled, new Color(0.914f, 0.714f, 0.227f));
            hold.fillMethod = Image.FillMethod.Radial360;
            hold.fillOrigin = 2;
            hold.fillClockwise = true;
            hold.fillAmount = 0f;
            VoxUi.Stretch(hold.rectTransform, -8f, -8f, -8f, -8f);

            RectTransform glyphRect = VoxUi.R("Glyph", slot);
            VoxUi.BottomRight(glyphRect, -8f, -8f, 38f, 38f);
            Image disc = VoxUi.Img("Disc", glyphRect, null, Image.Type.Simple);
            VoxUi.Stretch(disc.rectTransform);
            TMP_Text label = VoxUi.Txt("Label", glyphRect, "", 22, TextFont.Button, TextTone.Custom);
            VoxUi.Stretch(label.rectTransform);

            var component = slot.gameObject.AddComponent<HudAmmoSlot>();
            VoxUi.SetRef(component, "frame", frame);
            VoxUi.SetRef(component, "icon", icon);
            VoxUi.SetRef(component, "highlight", highlight);
            VoxUi.SetRef(component, "holdRing", hold);
            VoxUi.SetRef(component, "glyph", disc);
            VoxUi.SetRef(component, "glyphLabel", label);
            return component;
        }

        private static void BuildRunHud(RunHud runHud, Transform safe, UITheme theme)
        {
            if (runHud == null)
                return;
            GameObject go = runHud.gameObject;
            foreach (var old in go.GetComponents<TMP_Text>())
                Object.DestroyImmediate(old);
            foreach (var graphic in go.GetComponents<CanvasRenderer>())
                Object.DestroyImmediate(graphic);
            VoxUi.ClearChildren(go.transform);
            var rect = (RectTransform)go.transform;
            VoxUi.Stretch(rect);

            // Round / wave pill (top center): ROUND n | Wave x / y, with checker ends
            Image pill = VoxUi.Themed("RoundPill", rect, ThemeRole.PillMarble);
            VoxUi.TopCenter(pill.rectTransform, 0f, 24f, 580f, 78f);
            pill.gameObject.AddComponent<HudScale>();
            Image trimL = VoxUi.Trim("TrimL", pill.transform, TrimColor.Leaf);
            trimL.rectTransform.anchorMin = new Vector2(0f, 0f);
            trimL.rectTransform.anchorMax = new Vector2(0f, 1f);
            trimL.rectTransform.offsetMin = new Vector2(14f, 22f);
            trimL.rectTransform.offsetMax = new Vector2(34f, -10f);
            Image trimR = VoxUi.Trim("TrimR", pill.transform, TrimColor.Leaf);
            trimR.rectTransform.anchorMin = new Vector2(1f, 0f);
            trimR.rectTransform.anchorMax = new Vector2(1f, 1f);
            trimR.rectTransform.offsetMin = new Vector2(-34f, 22f);
            trimR.rectTransform.offsetMax = new Vector2(-14f, -10f);
            TMP_Text round = VoxUi.Txt("Round", pill.transform, "ROUND 1", 38, TextFont.Title, TextTone.OnPanel, TextAlignmentOptions.Center);
            round.rectTransform.anchorMin = new Vector2(0f, 0f);
            round.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            round.rectTransform.offsetMin = new Vector2(30f, 18f);
            round.rectTransform.offsetMax = new Vector2(0f, -8f);
            VoxUi.Fit(round, 24f, 38f);
            Image divider = VoxUi.Img("Divider", pill.transform, null, Image.Type.Simple, theme.InkSoil);
            divider.rectTransform.anchorMin = divider.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            divider.rectTransform.sizeDelta = new Vector2(4f, 46f);
            divider.rectTransform.anchoredPosition = new Vector2(0f, 6f);
            TMP_Text wave = VoxUi.Txt("Wave", pill.transform, "Wave 1 / 3", 34, TextFont.Button, TextTone.OnPanel, TextAlignmentOptions.Center);
            wave.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            wave.rectTransform.anchorMax = new Vector2(1f, 1f);
            wave.rectTransform.offsetMin = new Vector2(0f, 18f);
            wave.rectTransform.offsetMax = new Vector2(-30f, -8f);
            VoxUi.Fit(wave, 22f, 34f);

            // Currency pill (top right)
            Image coinPill = VoxUi.Themed("CoinPill", rect, ThemeRole.PillMarble);
            VoxUi.TopRight(coinPill.rectTransform, 28f, 26f, 340f, 76f);
            coinPill.gameObject.AddComponent<HudScale>();
            Image coin = VoxUi.Img("Coin", coinPill.transform, theme.Coin, Image.Type.Simple);
            coin.preserveAspect = true;
            coin.rectTransform.anchorMin = coin.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            coin.rectTransform.pivot = new Vector2(0f, 0.5f);
            coin.rectTransform.anchoredPosition = new Vector2(24f, 8f);
            coin.rectTransform.sizeDelta = new Vector2(44f, 44f);
            TMP_Text coins = VoxUi.Txt("Coins", coinPill.transform, "0", 40, TextFont.Button, TextTone.OnPanel, TextAlignmentOptions.Left);
            coins.rectTransform.anchorMin = new Vector2(0f, 0f);
            coins.rectTransform.anchorMax = new Vector2(1f, 1f);
            coins.rectTransform.offsetMin = new Vector2(78f, 16f);
            coins.rectTransform.offsetMax = new Vector2(-18f, -6f);
            VoxUi.Fit(coins, 24f, 40f);

            VoxUi.SetRef(runHud, "roundLabel", round);
            VoxUi.SetRef(runHud, "waveLabel", wave);
            VoxUi.SetRef(runHud, "currencyLabel", coins);
            VoxUi.SetRef(runHud, "spawner", Object.FindFirstObjectByType<WaveSpawner>());
            EditorUtility.SetDirty(runHud);
        }

        // ---------------------------------------------------------------- banners

        private static void RestyleBanners(Transform safe)
        {
            var round = Object.FindFirstObjectByType<RoundIntroBanner>(FindObjectsInactive.Include);
            if (round != null)
            {
                var label = new SerializedObject(round).FindProperty("label").objectReferenceValue as TMP_Text;
                if (label != null)
                {
                    VoxMenuScreens.Style(label, TextFont.Logo, TextTone.Gold, 130, outline: true);
                    label.fontStyle = FontStyles.Normal;
                }
            }
            var wave = Object.FindFirstObjectByType<WaveBanner>(FindObjectsInactive.Include);
            if (wave != null)
            {
                var label = new SerializedObject(wave).FindProperty("label").objectReferenceValue as TMP_Text;
                if (label != null)
                {
                    VoxMenuScreens.Style(label, TextFont.Button, TextTone.Gold, 96, outline: true);
                    label.fontStyle = FontStyles.Normal;
                }
            }
            var boss = Object.FindFirstObjectByType<BossHealthBar>(FindObjectsInactive.Include);
            if (boss != null)
            {
                var label = new SerializedObject(boss).FindProperty("namePlate").objectReferenceValue as TMP_Text;
                if (label != null)
                {
                    VoxMenuScreens.Style(label, TextFont.Title, TextTone.OnDark, 34, outline: true);
                    label.fontStyle = FontStyles.Normal;
                    VoxUi.Fit(label, 20f, 34f);
                }
                ((RectTransform)boss.transform).anchoredPosition = new Vector2(0f, -112f);   // below the round / wave pill
                if (!boss.TryGetComponent<HudScale>(out _))
                    boss.gameObject.AddComponent<HudScale>();
                Transform bar = boss.transform.Find("Bar");
                if (bar != null)
                {
                    UITheme theme = UITheme.Current;
                    Image back = bar.Find("Back")?.GetComponent<Image>();
                    if (back != null)
                    {
                        back.sprite = theme.HeatTrack;
                        back.type = Image.Type.Sliced;
                        back.pixelsPerUnitMultiplier = theme.BorderMultiplier;
                        back.color = Color.white;
                    }
                    Transform frame = bar.Find("Frame");
                    if (frame != null)
                        frame.gameObject.SetActive(false);
                    foreach (string n in new[] { "Trail", "Fill" })
                    {
                        var r = bar.Find(n)?.GetComponent<RectTransform>();
                        if (r != null)
                        {
                            r.offsetMin = new Vector2(8f, 8f);
                            r.offsetMax = new Vector2(-8f, -8f);
                        }
                    }
                    VoxUi.SetFloat(boss, "trailSpeed", 0.8f);
                    var so = new SerializedObject(boss);
                    so.FindProperty("phase1Color").colorValue = theme.Carrot;
                    so.FindProperty("phase2Color").colorValue = theme.Tomato;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
    }
}
