using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Small pieces of finale art drawn in code (32 PPU, point filtered) into Art/Finale, made on demand by the finale
/// room builders: the Heart Cart (iron-banded ore cart body, wheel, shard-ore chunk), the Momentum Column's glass
/// (with notches at the engine's stages), and 1-pixel sprites for the column's liquid, its surface line and light beams.
/// Big art (backgrounds, the bound/healed Great Shard, pedestals) comes from the team; these are placeholders until then.
/// </summary>
public static class FinaleArt
{
    public const string Folder = "Assets/_Project/Art/Finale";
    public const int ColumnInnerPx = 192, ColumnCapPx = 8, ColumnWidthPx = 20; // 6 m inside, 0.625 m wide
    public const float ColumnInnerHeight = ColumnInnerPx / 32f, ColumnInnerWidth = 12f / 32f;

    public static Sprite Get(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png");

    /// <summary>Draws every piece that doesn't exist yet (notches at the given fractions of the column).</summary>
    public static void Ensure(float[] notches)
    {
        if (Get("Cart_Body") == null) Save("Cart_Body", CartBody(out int w, out int h), w, h, new Vector2(0.5f, 0.5f));
        if (Get("Cart_Wheel") == null) Save("Cart_Wheel", Wheel(out int w, out int h), w, h, new Vector2(0.5f, 0.5f));
        if (Get("Shard_Ore") == null) Save("Shard_Ore", Ore(out int w, out int h), w, h, new Vector2(0.5f, 0.5f));
        if (Get("Hammer_Head") == null) Save("Hammer_Head", HammerHead(out int hw, out int hh), hw, hh, new Vector2(0.5f, 0.5f));
        if (Get("Pixel") == null) Save("Pixel", new[] { (Color32)Color.white }, 1, 1, new Vector2(0.5f, 0.5f));
        if (Get("Pixel_Bottom") == null) Save("Pixel_Bottom", new[] { (Color32)Color.white }, 1, 1, new Vector2(0.5f, 0f));
        Save("Column_Glass", Glass(notches, out int gw, out int gh), gw, gh, new Vector2(0.5f, ColumnCapPx / (float)gh)); // redrawn: notches may change
    }

    // ------------------------------------------------------------------ drawing (x right, y up, origin bottom-left)

    private static readonly Color32 Clear = new Color32(0, 0, 0, 0), Outline = new Color32(40, 28, 22, 255),
        Wood = new Color32(118, 78, 46, 255), WoodLight = new Color32(150, 104, 62, 255), WoodDark = new Color32(86, 56, 34, 255),
        Iron = new Color32(70, 74, 82, 255), IronLight = new Color32(118, 124, 134, 255), Brass = new Color32(176, 130, 58, 255),
        BrassLight = new Color32(222, 178, 96, 255), Cyan = new Color32(110, 230, 245, 255), CyanDark = new Color32(46, 150, 180, 255);

    // 40 x 22: a wooden ore cart, wider at the top, iron bands and rim.
    private static Color32[] CartBody(out int w, out int h)
    {
        w = 40; h = 22;
        Color32[] p = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            int inset = (h - 1 - y) / 4; // narrower toward the bottom
            for (int x = inset; x < w - inset; x++)
            {
                bool edge = x == inset || x == w - 1 - inset || y == 0 || y == h - 1;
                Color32 c = edge ? Outline : y >= h - 3 ? (y == h - 2 ? IronLight : Iron) : (x - inset) % 9 == 4 ? Iron
                          : y % 6 == 0 ? WoodDark : (y % 6 == 1 ? WoodLight : Wood);
                p[y * w + x] = c;
            }
        }
        foreach (int x in new[] { 8, 31 }) for (int y = 3; y < h - 3; y += 5) p[y * w + x] = IronLight; // rivets
        return p;
    }

    // 10 x 10: an iron wheel with four spokes (it turns, so the spokes show).
    private static Color32[] Wheel(out int w, out int h)
    {
        w = h = 10;
        Color32[] p = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = x + 0.5f - 5f, dy = y + 0.5f - 5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 5f) continue;
                p[y * w + x] = d > 4f ? Outline : d > 3f ? Iron : (Mathf.Abs(dx) < 0.6f || Mathf.Abs(dy) < 0.6f || d < 1.2f) ? IronLight : Clear;
            }
        return p;
    }

    // 22 x 18: an iron hammer head with brass bands and a ring on top for its chain.
    private static Color32[] HammerHead(out int w, out int h)
    {
        w = 22; h = 18;
        Color32[] p = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool ring = y >= 14 && x >= 8 && x <= 13 && !(y >= 15 && y <= 16 && x >= 10 && x <= 11);
                bool block = y < 14;
                if (!ring && !block) continue;
                bool edge = ring ? (y == 17 || x == 8 || x == 13 || y == 14) : (x == 0 || x == w - 1 || y == 0 || y == 13);
                p[y * w + x] = edge ? Outline : ring ? Iron : (x == 4 || x == 17) ? Brass : (y >= 11 ? IronLight : Iron);
            }
        return p;
    }

    // 12 x 10: a glowing shard-ore chunk.
    private static Color32[] Ore(out int w, out int h)
    {
        w = 12; h = 10;
        string[] rows =
        {
            "....##......",
            "...#cc#.##..",
            "..#cCCc#cc#.",
            ".#cCCCCcCc#.",
            ".#cCCCCCCc#.",
            "#cCCCCCCCcc#",
            "#ccCCCCCCcc#",
            ".#ccccCCcc#.",
            "..##cccc##..",
            "....####....",
        };
        Color32[] p = new Color32[w * h];
        for (int r = 0; r < h; r++)
            for (int x = 0; x < w; x++)
            {
                char ch = rows[h - 1 - r][x];
                p[r * w + x] = ch == '#' ? Outline : ch == 'C' ? Cyan : ch == 'c' ? CyanDark : Clear;
            }
        return p;
    }

    // The column's glass: brass caps top and bottom, glass walls, faint glass inside, brass notches at the stages.
    private static Color32[] Glass(float[] notches, out int w, out int h)
    {
        w = ColumnWidthPx; h = ColumnInnerPx + ColumnCapPx * 2;
        Color32[] p = new Color32[w * h];
        Color32 glassEdge = new Color32(190, 230, 240, 200), glassFill = new Color32(160, 210, 225, 40);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool cap = y < ColumnCapPx || y >= h - ColumnCapPx;
                if (cap)
                {
                    bool edge = x == 0 || x == w - 1 || y == 0 || y == h - 1 || y == ColumnCapPx - 1 || y == h - ColumnCapPx;
                    p[y * w + x] = edge ? Outline : (y % 3 == 0 ? BrassLight : Brass);
                }
                else if (x <= 1 || x >= w - 2) p[y * w + x] = x == 0 || x == w - 1 ? Outline : glassEdge;
                else if (x >= 4 && x < w - 4) p[y * w + x] = glassFill;
                else p[y * w + x] = Clear;
            }
        foreach (float f in notches)
        {
            int y = ColumnCapPx + Mathf.Clamp(Mathf.RoundToInt(f * ColumnInnerPx), 1, ColumnInnerPx - 1);
            for (int x = 0; x < w; x++) if (x < 4 || x >= w - 4) p[y * w + x] = BrassLight;
            p[y * w + 2] = p[y * w + w - 3] = Brass;
        }
        return p;
    }

    private static void Save(string name, Color32[] pixels, int w, int h, Vector2 pivot)
    {
        Directory.CreateDirectory(Folder);
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        string path = $"{Folder}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        TextureImporterSettings s = new TextureImporterSettings();
        importer.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = pivot;
        s.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(s);
        importer.SaveAndReimport();
    }
}
