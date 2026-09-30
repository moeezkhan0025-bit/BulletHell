using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using BulletHell.Core;
using BulletHell.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// Builds the single UI theme (Assets/Data/UI/VoxVegetallis.asset) from the kit manifest tokens and sprites, assigns it
    /// to GameConfig, sets the TMP defaults and fallbacks, and removes the retired Mega Cozy theme asset. Safe to run again.
    /// </summary>
    public static class VoxThemeSetup
    {
        private const string ThemePath = "Assets/Data/UI/VoxVegetallis.asset";
        private const string OldThemePath = "Assets/Data/UI/UITheme.asset";
        private const string ConfigPath = "Assets/Resources/GameConfig.asset";
        private const string Kit = VoxKitSetup.KitRoot;

        [MenuItem("BulletHell/Vox/4 Build Theme")]
        public static void Build()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ThemePath));
                theme = ScriptableObject.CreateInstance<UITheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            string manifest = File.ReadAllText(Kit + "vox_ui_kit_manifest.json");
            var so = new SerializedObject(theme);

            // Palette tokens
            foreach (string key in new[] { "inkSoil", "marble", "marbleShade", "gold", "goldDark", "tomato", "leaf", "carrot", "corn", "sage", "faintText", "mutedText" })
            {
                string manifestKey = Regex.Replace(key, "([A-Z])", "_$1").ToLowerInvariant();
                SetColor(so, "palette." + key, Token(manifest, manifestKey));
            }
            SetColor(so, "rarityCommon", Token(manifest, "common"));
            SetColor(so, "rarityRare", Token(manifest, "rare"));
            SetColor(so, "rarityEpic", Token(manifest, "epic"));
            SetColor(so, "rarityLegendary", Token(manifest, "legendary"));
            SetColor(so, "heatCool", Token(manifest, "leaf"));
            SetColor(so, "heatWarm", Token(manifest, "carrot"));
            SetColor(so, "heatHot", Token(manifest, "tomato"));
            Color ink = Token(manifest, "ink_soil");
            SetColor(so, "textOnPanel", ink);
            SetColor(so, "textOnButton", ink);
            SetColor(so, "textOnLight", ink);
            SetColor(so, "textOnButtonDim", Token(manifest, "faint_text"));
            SetColor(so, "textOnDark", Token(manifest, "marble"));

            // Metrics and motion (manifest: outline 6, shadow 9, focus lift 12 px / 0.12 s, buy 0.35 s, bubble 0.25 s / 0.05 s, transition 0.2 s)
            so.FindProperty("outlinePx").floatValue = 6f;
            so.FindProperty("dropShadowPx").floatValue = 9f;
            so.FindProperty("focusLiftPx").floatValue = 12f;
            so.FindProperty("focusSeconds").floatValue = 0.12f;
            so.FindProperty("cardBuySeconds").floatValue = 0.35f;
            so.FindProperty("bubbleInSeconds").floatValue = 0.25f;
            so.FindProperty("bubbleStaggerSeconds").floatValue = 0.05f;
            so.FindProperty("screenTransitionSeconds").floatValue = 0.2f;
            so.FindProperty("pixelScale").floatValue = 1f;

            // Fonts
            SetObj(so, "titleFont", Font("CinzelDecorative-Bold SDF"));
            SetObj(so, "logoFont", Font("CinzelDecorative-Black SDF"));
            SetObj(so, "buttonFont", Font("LilitaOne SDF"));
            SetObj(so, "bodyFont", Font("Nunito-SemiBold SDF"));
            SetObj(so, "bodyBoldFont", Font("Nunito-ExtraBold SDF"));

            // Sprites: property -> kit file
            var map = new Dictionary<string, string>
            {
                ["panelMarble"] = "panel_marble", ["panelShade"] = "panel_shade", ["panelWood"] = "panel_wood", ["panelCorn"] = "panel_corn",
                ["pillMarble"] = "pill_marble", ["pillHintbar"] = "pill_hintbar", ["rowFocusRing"] = "row_focus_ring",
                ["button.normal"] = "button_normal", ["button.highlighted"] = "button_focused", ["button.selected"] = "button_focused",
                ["button.pressed"] = "button_pressed", ["button.disabled"] = "button_disabled",
                ["buttonPrimary"] = "button_primary", ["buttonArrowLeft"] = "button_arrow_left", ["buttonArrowRight"] = "button_arrow_right",
                ["laurel"] = "laurel_right", ["tab"] = "tab_normal", ["tabSelected"] = "tab_selected",
                ["trimLeaf"] = "trim_checker_leaf", ["trimCarrot"] = "trim_checker_carrot", ["trimTomato"] = "trim_checker_tomato", ["trimCorn"] = "trim_checker_corn",
                ["cardCommon"] = "card_common", ["cardRare"] = "card_rare", ["cardEpic"] = "card_epic", ["cardLegendary"] = "card_legendary",
                ["cardFocusRing"] = "card_focus_ring", ["stampSold"] = "stamp_sold",
                ["slotAmmo"] = "slot_ammo", ["slotAmmoEmpty"] = "slot_ammo_empty", ["slotActiveRing"] = "slot_active_ring",
                ["bubbleEmpty"] = "bubble_empty", ["bubbleFilledRim"] = "bubble_filled_rim_white", ["bubbleFocused"] = "bubble_focused",
                ["armSlotFilled"] = "armslot_filled", ["armSlotSelected"] = "armslot_selected", ["tetherVine"] = "tether_vine_segment",
                ["heartFull"] = "heart_tomato_full", ["heartEmpty"] = "heart_tomato_empty", ["heatTrack"] = "heatbar_track", ["heatFill"] = "heatbar_fill_white",
                ["portraitRing"] = "portrait_ring_leaf", ["coin"] = "icon_coin_seed",
                ["sliderTrack"] = "slider_track", ["sliderFill"] = "slider_fill_white", ["sliderHandle"] = "slider_handle", ["sliderHandleFocused"] = "slider_handle_focused",
                ["toggleOff"] = "toggle_track_off", ["toggleOn"] = "toggle_track_on", ["toggleKnob"] = "toggle_knob",
                ["pedestal"] = "pedestal_marble", ["spotlight"] = "spotlight_arch", ["harvestCrateIcon"] = "icon_harvest_crate",
            };
            foreach (var pair in map)
                SetObj(so, pair.Key, AssetDatabase.LoadAssetAtPath<Sprite>(Kit + pair.Value + ".png"));

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);

            // GameConfig -> the new theme
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var cso = new SerializedObject(config);
            cso.FindProperty("uiTheme").objectReferenceValue = theme;
            cso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);

            ConfigureTmp();
            RecolorRarityTable(theme);

            if (AssetDatabase.LoadAssetAtPath<Object>(OldThemePath) != null)
                AssetDatabase.DeleteAsset(OldThemePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Vox theme: built " + ThemePath + ", assigned to GameConfig, old theme removed.");
        }

        /// <summary>Re-applies the theme to every prefab that has themed components, so no prefab keeps a sprite from a retired theme.</summary>
        [MenuItem("BulletHell/Vox/4b Refresh Themed Prefabs")]
        public static void RefreshThemedPrefabs()
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    bool any = false;
                    foreach (var button in root.GetComponentsInChildren<ThemedButton>(true)) { button.Apply(); any = true; }
                    foreach (var image in root.GetComponentsInChildren<ThemedImage>(true)) { image.Apply(); any = true; }
                    foreach (var text in root.GetComponentsInChildren<ThemedText>(true)) { text.Apply(); any = true; }
                    if (any)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        count++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            Debug.Log("Vox theme: refreshed " + count + " prefabs.");
        }

        private static Color Token(string manifest, string key)
        {
            Match m = Regex.Match(manifest, "\"" + key + "\"\\s*:\\s*\"(#[0-9A-Fa-f]{6})\"");
            if (!m.Success)
            {
                Debug.LogWarning("Vox theme: token missing " + key);
                return Color.magenta;
            }
            ColorUtility.TryParseHtmlString(m.Groups[1].Value, out Color c);
            return c;
        }

        private static TMP_FontAsset Font(string name) => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/" + name + ".asset");

        private static void SetColor(SerializedObject so, string path, Color c)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p == null) { Debug.LogWarning("Vox theme: no property " + path); return; }
            p.colorValue = c;
        }

        private static void SetObj(SerializedObject so, string path, Object value)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p == null || value == null) { Debug.LogWarning("Vox theme: cannot set " + path); return; }
            p.objectReferenceValue = value;
        }

        private static void ConfigureTmp()
        {
            var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            foreach (string n in new[] { "CinzelDecorative-Bold SDF", "CinzelDecorative-Black SDF", "LilitaOne SDF", "Nunito-SemiBold SDF", "Nunito-ExtraBold SDF" })
            {
                TMP_FontAsset fa = Font(n);
                if (fa == null || liberation == null) continue;
                if (fa.fallbackFontAssetTable == null) fa.fallbackFontAssetTable = new List<TMP_FontAsset>();
                if (!fa.fallbackFontAssetTable.Contains(liberation))
                    fa.fallbackFontAssetTable.Add(liberation);
                EditorUtility.SetDirty(fa);
            }
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset");
            if (settings != null)
            {
                var so = new SerializedObject(settings);
                so.FindProperty("m_defaultFontAsset").objectReferenceValue = Font("Nunito-SemiBold SDF");
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
            }
        }

        private static void RecolorRarityTable(UITheme theme)
        {
            var table = AssetDatabase.LoadAssetAtPath<BulletHell.Shop.RarityTable>("Assets/Data/Shop/RarityTable.asset");
            if (table == null) return;
            var so = new SerializedObject(table);
            SerializedProperty entries = so.FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty e = entries.GetArrayElementAtIndex(i);
                int rarity = e.FindPropertyRelative("Rarity").enumValueIndex;
                e.FindPropertyRelative("Color").colorValue = theme.GetRarityColor(rarity);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
        }
    }
}
