using UnityEditor;
using UnityEngine;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// Import settings for painted art (Docs/ART_SPEC.md, painted HD style). The 2x exports of the ORIGINAL scale are
    /// 220 px per P, so the arena (backdrop) imports at 220 pixels per unit. Characters (files named enemy_*) are
    /// baked at the locked 1.5x character scale: 220 / 1.5 pixels per unit. Applied when a texture under
    /// Assets/Art/ScaleTest is first imported (or its .meta is missing); change them afterwards in the Inspector and they stay.
    /// </summary>
    public sealed class ScaleTestImport : AssetPostprocessor
    {
        private const string Folder = "Assets/Art/ScaleTest/";
        private const float ArenaPixelsPerUnit = 220f;
        private const float CharacterScale = 1.5f;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder) || !assetImporter.importSettingsMissing)
                return;

            bool isCharacter = assetPath.Contains("enemy_");
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = isCharacter ? ArenaPixelsPerUnit / CharacterScale : ArenaPixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 4096;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            // Characters: pivot at the feet (tpl_character_1024: feet at x = 512, y = 880 from the top of 1024).
            settings.spritePivot = isCharacter ? new Vector2(0.5f, 1f - 880f / 1024f) : new Vector2(0.5f, 0.5f);
            importer.SetTextureSettings(settings);
        }
    }
}
