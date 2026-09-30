using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// VOX VEGETALLIS UI kit import. Reads Assets/Art/UI/VoxKit/vox_ui_kit_manifest.json and applies the 2x UI import
    /// settings (PPU 200 = half size on the 1080p canvas), 9-slice borders, and Repeat wrap for the checker trims.
    /// Also imports the TMP essentials and builds the TextMeshPro font assets. Safe to run again.
    /// </summary>
    public static class VoxKitSetup
    {
        public const string KitRoot = "Assets/Art/UI/VoxKit/";
        public const float UiPpu = 200f;
        private const string FontRoot = "Assets/Fonts/";

        [MenuItem("BulletHell/Vox/1 Import Kit Sprites")]
        public static void ImportKit()
        {
            string json = File.ReadAllText(KitRoot + "vox_ui_kit_manifest.json");
            int spritesStart = json.IndexOf("\"sprites\"", System.StringComparison.Ordinal);
            int tokensStart = json.IndexOf("\"tokens\"", System.StringComparison.Ordinal);
            string spritesJson = json.Substring(spritesStart, tokensStart - spritesStart);

            // Each sprite is "name": { "size": [...], "nine_slice_border_LTRB": [...]?, "note": ... }
            var entry = new Regex("\"(?<n>[a-z0-9_]+)\"\\s*:\\s*\\{(?<b>[^{}]*)\\}", RegexOptions.Singleline);
            var border = new Regex("nine_slice_border_LTRB\"\\s*:\\s*\\[(?<v>[^\\]]*)\\]", RegexOptions.Singleline);
            int count = 0, sliced = 0;
            foreach (Match m in entry.Matches(spritesJson))
            {
                string name = m.Groups["n"].Value;
                string path = KitRoot + name + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning("Vox kit: no texture for manifest entry " + name);
                    continue;
                }

                Vector4 b = Vector4.zero;
                Match bm = border.Match(m.Groups["b"].Value);
                if (bm.Success)
                {
                    string[] p = Regex.Replace(bm.Groups["v"].Value, "\\s", "").Split(',');
                    float l = float.Parse(p[0], CultureInfo.InvariantCulture);
                    float t = float.Parse(p[1], CultureInfo.InvariantCulture);
                    float r = float.Parse(p[2], CultureInfo.InvariantCulture);
                    float bo = float.Parse(p[3], CultureInfo.InvariantCulture);
                    b = new Vector4(l, bo, r, t); // Unity: left, bottom, right, top
                    sliced++;
                }

                bool tiling = name.StartsWith("trim_checker_") || name == "tether_vine_segment";
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = UiPpu;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = tiling ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.spriteBorder = b;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = tiling || b != Vector4.zero ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
                settings.spriteExtrude = 0;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                count++;
            }
            Debug.Log("Vox kit: imported " + count + " sprites (" + sliced + " sliced).");
        }

        [MenuItem("BulletHell/Vox/1b Assign Kit Icons")]
        public static void AssignIcons()
        {
            var map = new (string asset, string icon)[]
            {
                ("Assets/Data/Ammo/Ammo_Basic.asset", "icon_ammo_basic"),
                ("Assets/Data/Ammo/Ammo_Shotgun.asset", "icon_ammo_shotgun"),
                ("Assets/Data/Ammo/Ammo_Laser.asset", "icon_ammo_laser"),
                ("Assets/Data/Ammo/Ammo_Gatling.asset", "icon_ammo_gatling"),
                ("Assets/Data/Armaments/Armament_Homing.asset", "icon_armament_homing"),
                ("Assets/Data/Armaments/Armament_Pierce.asset", "icon_armament_pierce"),
                ("Assets/Data/Armaments/Armament_Ricochet.asset", "icon_armament_ricochet"),
                ("Assets/Data/Armaments/Armament_BulletSpeed.asset", "icon_armament_velocity"),
                ("Assets/Data/Armaments/Armament_AutoFire.asset", "icon_armament_autofire"),
            };
            foreach (var (asset, icon) in map)
            {
                var target = AssetDatabase.LoadAssetAtPath<Object>(asset);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(KitRoot + icon + ".png");
                if (target == null || sprite == null)
                {
                    Debug.LogWarning("Vox kit: cannot assign " + icon + " to " + asset);
                    continue;
                }
                var so = new SerializedObject(target);
                so.FindProperty("icon").objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Vox kit: ammo and armament icons assigned.");
        }

        [MenuItem("BulletHell/Vox/2 Import TMP Essentials")]
        public static void ImportTmpEssentials()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
            {
                Debug.Log("Vox kit: TMP essentials already imported.");
                return;
            }
            const string pkg = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
            AssetDatabase.ImportPackage(pkg, false);
            Debug.Log("Vox kit: importing TMP essentials.");
        }

        private static readonly (string ttf, string asset, int sampling, int padding, int atlas)[] Fonts =
        {
            ("CinzelDecorative-Bold.ttf", "CinzelDecorative-Bold SDF.asset", 96, 10, 2048),
            ("CinzelDecorative-Black.ttf", "CinzelDecorative-Black SDF.asset", 96, 10, 2048),
            ("LilitaOne-Regular.ttf", "LilitaOne SDF.asset", 80, 8, 1024),
            ("Nunito_600SemiBold.ttf", "Nunito-SemiBold SDF.asset", 72, 8, 1024),
            ("Nunito_800ExtraBold.ttf", "Nunito-ExtraBold SDF.asset", 72, 8, 1024),
        };

        [MenuItem("BulletHell/Vox/3 Create TMP Font Assets")]
        public static void CreateFontAssets()
        {
            var chars = new System.Text.StringBuilder();
            for (int c = 32; c < 127; c++) chars.Append((char)c);
            for (int c = 160; c < 256; c++) chars.Append((char)c);
            chars.Append("→←↑↓·°–—‘’“”…×•✕");

            foreach (var f in Fonts)
            {
                string assetPath = FontRoot + f.asset;
                if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null)
                    AssetDatabase.DeleteAsset(assetPath);
                var font = AssetDatabase.LoadAssetAtPath<Font>(FontRoot + f.ttf);
                if (font == null)
                {
                    Debug.LogError("Vox kit: font missing " + f.ttf);
                    continue;
                }
                TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(font, f.sampling, f.padding, GlyphRenderMode.SDFAA,
                    f.atlas, f.atlas, AtlasPopulationMode.Dynamic, true);
                fa.name = Path.GetFileNameWithoutExtension(f.asset);
                AssetDatabase.CreateAsset(fa, assetPath);
                fa.atlasTexture.name = fa.name + " Atlas";
                fa.material.name = fa.name + " Material";
                AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
                AssetDatabase.AddObjectToAsset(fa.material, fa);
                fa.TryAddCharacters(chars.ToString(), out string missing);
                if (!string.IsNullOrEmpty(missing))
                    Debug.LogWarning("Vox kit: " + fa.name + " is missing glyphs: " + missing);
                EditorUtility.SetDirty(fa);
                Debug.Log("Vox kit: font asset " + assetPath);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
