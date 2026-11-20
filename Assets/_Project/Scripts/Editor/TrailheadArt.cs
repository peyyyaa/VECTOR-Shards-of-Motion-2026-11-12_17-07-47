using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Saves the trailhead art into Assets/_Project/Art/Village/Trailhead as sprites (32 PPU, Point, bottom-centre pivot):
/// everything TrailheadPainter draws, plus the pack's red banner recoloured to Region 1 green.
/// Used by Build Village (the village trailhead) and Room 1 > Add Trail Home (the trail home in Region 1).
/// </summary>
public static class TrailheadArt
{
    public const string Folder = "Assets/_Project/Art/Village/Trailhead";
    private const string VillageTexture = "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Village Props.png";
    private const float PPU = 32f;

    /// <summary>Draws and imports every piece; returns them by name (e.g. "Post", "Stone_Lit", "Banner_Green").</summary>
    public static Dictionary<string, Sprite> Build()
    {
        Directory.CreateDirectory(Folder);
        List<string> paths = new List<string>();
        foreach (TrailheadPainter.Image im in TrailheadPainter.DrawAll()) paths.Add(WritePng(im.name, im.px, im.w, im.h));
        string banner = GreenBanner();
        if (banner != null) paths.Add(banner);

        Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        foreach (string path in paths)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PPU;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            TextureImporterSettings s = new TextureImporterSettings();
            importer.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            s.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(s);
            importer.SaveAndReimport();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) sprites[Path.GetFileNameWithoutExtension(path)] = sprite;
        }
        return sprites;
    }

    // The pack's red banner, recoloured to Region 1 green (read from the pack PNG on disk, so the texture
    // doesn't need Read/Write enabled).
    private static string GreenBanner()
    {
        Sprite red = AssetDatabase.LoadAllAssetsAtPath(VillageTexture).OfType<Sprite>().FirstOrDefault(s => s.name == "TX Village Props - Banner");
        if (red == null || !File.Exists(VillageTexture)) return null;
        Texture2D sheet = new Texture2D(2, 2);
        sheet.LoadImage(File.ReadAllBytes(VillageTexture));
        Rect r = red.rect;
        int w = (int)r.width, h = (int)r.height;
        Color32[] all = sheet.GetPixels32();
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 c = all[((int)r.y + y) * sheet.width + (int)r.x + x];
                px[y * w + x] = c.a == 0 ? c : new Color32((byte)(c.g * 0.55f), (byte)Mathf.Min(255, c.r * 0.72f), (byte)Mathf.Min(255, c.b * 0.6f + 10), c.a);
            }
        Object.DestroyImmediate(sheet);
        return WritePng("Banner_Green", px, w, h);
    }

    private static string WritePng(string name, Color32[] pixels, int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        string path = $"{Folder}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        return path;
    }
}
