using UnityEditor;

/// <summary>
/// Import settings for the third-party pixel art in Assets/ThirdParty (see Assets/ThirdParty/CREDITS.md), applied
/// automatically the first time each file is imported: Sprite, 32 pixels per unit (the same pixel size as the rest of
/// the game: Legacy Fantasy's 16 px tiles become 0.5 m), point filtering, no compression, no mipmaps.
/// Multi-sprite sheets are sliced later by the tools that use them. Settings changed by hand afterwards are kept.
/// </summary>
public class ThirdPartyArtImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/ThirdParty/")) return;
        TextureImporter importer = (TextureImporter)assetImporter;
        if (!importer.importSettingsMissing) return; // only on first import
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = UnityEngine.FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
    }
}
