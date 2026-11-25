using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Shards of Motion > Room 8 > 0. Import Engine Art.
/// Adapts the team's PixelLab Momentum Engine images (Art/Engine/Source, 224 x 224 each, same canvas, true pixel art):
///   Engine_Empty.png (frame, empty socket, empty hook), Engine_Running.png (same, assembled and glowing),
///   Engine_Parts.png (flywheel, bearing, counterweight side by side)
/// into sprites in Art/Engine (32 PPU, Point):
///   Engine_Base     - the empty engine, cropped to the machine (190 x 151, bottom-centre pivot): the body in Room 8.
///   Engine_Base_Loaded - the same with the counterweight hanging from the hook (from the running image): after install.
///   Engine_Wheel    - the flywheel cut as a circle out of the running image (92 x 92, centre pivot, cut on the rim): it spins.
///   Engine_Rune_00..11 - the running image's glowing runes, split into 12 segments along the frame (left post bottom->top,
///                     top beam left->right, right post top->bottom), same size and pivot as the base: a momentum meter.
///   Part_Flywheel, Part_Bearing, Part_Counterweight - the parts sheet cut apart (centre pivot): the assembly screen.
///   Striker         - a bronze striker plate (drawn here, 10 x 38): the paddle the drive crate hits.
///   Cursor_Ring     - a white ring cursor (drawn here, 16 x 16) for the assembly screen.
/// Positions in the source (pixels from the top-left of the 224 canvas): machine x 23..212, y 39..189; wheel centre (111.5, 111.5)
/// radius 45.5 (wheel centre (112, 109.5), fitted to the rim); rune columns left x < 62, right x > 155, top beam y < 56 (the cyan arc beside the wheel, x 62..70, is skipped).
/// </summary>
public static class EngineArtTool
{
    public const string Folder = "Assets/_Project/Art/Engine";
    private const string Source = Folder + "/Source";
    public const int RuneSegments = 12;

    // Source geometry (top-left pixel coordinates in the 224 x 224 canvas).
    private const int CropX = 23, CropY = 39, CropW = 190, CropH = 151;
    public const float WheelCX = 112f, WheelCY = 109.5f, WheelR = 45.5f; // fitted to the rim (continuous pixel coordinates)
    /// <summary>The counterweight's centre where it hangs from the hook in the running image.</summary>
    public const float WeightCX = 200f, WeightCY = 121f;
    private const int WeightX0 = 186, WeightX1 = 214, WeightY0 = 106, WeightY1 = 140;

    /// <summary>A source pixel position relative to Engine_Base's pivot (bottom centre), in source pixels, y up.</summary>
    public static Vector2 FromBase(float x, float y) => new Vector2(x - (CropX + CropW * 0.5f), CropY + CropH - y);

    /// <summary>The wheel centre relative to Engine_Base's pivot (bottom centre), in metres.</summary>
    public static Vector2 WheelOffset => FromBase(WheelCX, WheelCY) / 32f;

    [MenuItem("Tools/Shards of Motion/Room 8/0. Import Engine Art")]
    public static void Import()
    {
        Texture2D empty = Read(Source + "/Engine_Empty.png"), running = Read(Source + "/Engine_Running.png"), parts = Read(Source + "/Engine_Parts.png");
        if (empty == null || running == null || parts == null)
        {
            Debug.LogError("Engine art: put Engine_Empty.png, Engine_Running.png and Engine_Parts.png in " + Source + ".");
            return;
        }

        // Body.
        Save("Engine_Base", Crop(empty, CropX, CropY, CropW, CropH), CropW, CropH, SpriteAlignment.BottomCenter);
        Color32[] loaded = Crop(empty, CropX, CropY, CropW, CropH);
        for (int y = WeightY0; y < WeightY1; y++)
            for (int x = WeightX0; x < WeightX1; x++)
            {
                Color32 c = Get(running, x, y);
                if (c.a > 0) loaded[(CropY + CropH - 1 - y) * CropW + (x - CropX)] = c;
            }
        Save("Engine_Base_Loaded", loaded, CropW, CropH, SpriteAlignment.BottomCenter);

        // Wheel: exactly the rim's circle, centred on the wheel's true middle (so it spins without wobbling). Inside the rim
        // everything is wheel; in the outer 1.5 px band only the wheel's own pixels are kept (not the frame or the glow
        // around it, which belong to the still engine).
        int d = Mathf.CeilToInt(WheelR) * 2;
        Color32[] wheel = new Color32[d * d];
        for (int y = 0; y < d; y++)
            for (int x = 0; x < d; x++)
            {
                float sx = WheelCX - d * 0.5f + x + 0.5f, sy = WheelCY - d * 0.5f + (d - 1 - y) + 0.5f; // texture rows run bottom-up
                float dist = Mathf.Sqrt((sx - WheelCX) * (sx - WheelCX) + (sy - WheelCY) * (sy - WheelCY));
                int px = Mathf.FloorToInt(sx), py = Mathf.FloorToInt(sy);
                bool keep = dist <= WheelR - 1.5f || (dist <= WheelR && IsWheelPixel(running, empty, px, py));
                wheel[y * d + x] = keep ? Get(running, px, py) : new Color32(0, 0, 0, 0);
            }
        Save("Engine_Wheel", wheel, d, d, SpriteAlignment.Center);

        // Rune segments: glowing pixels that only the running image has, on the posts and the top beam, in path order.
        List<(Vector2Int p, float t)> runes = new List<(Vector2Int, float)>();
        int leftTop = 224, leftBottom = 0, rightTop = 224, rightBottom = 0, beamLeft = 224, beamRight = 0;
        for (int y = CropY; y < CropY + CropH; y++)
            for (int x = CropX; x < CropX + CropW; x++)
            {
                if (!IsGlow(running, empty, x, y)) continue;
                if (y < 56) { beamLeft = Mathf.Min(beamLeft, x); beamRight = Mathf.Max(beamRight, x); }
                else if (x < 62) { leftTop = Mathf.Min(leftTop, y); leftBottom = Mathf.Max(leftBottom, y); }
                else if (x > 155) { rightTop = Mathf.Min(rightTop, y); rightBottom = Mathf.Max(rightBottom, y); }
            }
        float leftLen = Mathf.Max(1, leftBottom - leftTop), beamLen = Mathf.Max(1, beamRight - beamLeft), rightLen = Mathf.Max(1, rightBottom - rightTop);
        for (int y = CropY; y < CropY + CropH; y++)
            for (int x = CropX; x < CropX + CropW; x++)
            {
                if (!IsGlow(running, empty, x, y)) continue;
                float t;
                if (y < 56) t = leftLen + (x - beamLeft);
                else if (x < 62) t = leftBottom - y;
                else if (x > 155) t = leftLen + beamLen + (y - rightTop);
                else continue; // glow around the wheel itself (x 62..155): not part of the meter
                runes.Add((new Vector2Int(x, y), t / (leftLen + beamLen + rightLen)));
            }
        for (int s = 0; s < RuneSegments; s++)
        {
            Color32[] px = new Color32[CropW * CropH];
            foreach ((Vector2Int p, float t) in runes)
                if (Mathf.Min(RuneSegments - 1, Mathf.FloorToInt(t * RuneSegments)) == s)
                    px[(CropY + CropH - 1 - p.y) * CropW + (p.x - CropX)] = Get(running, p.x, p.y);
            Save($"Engine_Rune_{s:00}", px, CropW, CropH, SpriteAlignment.BottomCenter);
        }

        // Parts: the three objects of the parts sheet, cut at their empty columns.
        string[] names = { "Part_Flywheel", "Part_Bearing", "Part_Counterweight" };
        int part = 0, start = -1;
        for (int x = 0; x <= parts.width && part < names.Length; x++)
        {
            bool any = x < parts.width && ColumnHasPixels(parts, x);
            if (any && start < 0) start = x;
            if (!any && start >= 0) { SavePart(parts, names[part++], start, x); start = -1; }
        }

        Save("Striker", Striker(out int sw, out int sh), sw, sh, SpriteAlignment.BottomCenter);
        Save("Cursor_Ring", Ring(out int cw, out int ch), cw, ch, SpriteAlignment.Center);
        AssetDatabase.SaveAssets();
        Debug.Log($"Engine art imported into {Folder} ({runes.Count} rune pixels in {RuneSegments} segments). Next: Room 8 > 1. Build Graybox, then Create UI Prefabs.");
    }

    // ------------------------------------------------------------------ helpers

    // Part of the running wheel: differs from the empty engine and isn't cyan glow.
    private static bool IsWheelPixel(Texture2D running, Texture2D empty, int x, int y)
    {
        Color32 a = Get(running, x, y), b = Get(empty, x, y);
        if (a.a == 0 || (a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a)) return false;
        return !(a.b > 150 && a.g > 150 && a.r < 140);
    }

    private static bool IsGlow(Texture2D running, Texture2D empty, int x, int y)
    {
        Color32 a = Get(running, x, y), b = Get(empty, x, y);
        if (a.a == 0 || (a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a)) return false;
        return a.b > 150 && a.g > 150 && a.r < 140;
    }

    // Pixel at top-left coordinates (x right, y down).
    private static Color32 Get(Texture2D t, int x, int y)
    {
        if (x < 0 || y < 0 || x >= t.width || y >= t.height) return new Color32(0, 0, 0, 0);
        return t.GetPixel(x, t.height - 1 - y);
    }

    private static Color32[] Crop(Texture2D t, int x0, int y0, int w, int h)
    {
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[(h - 1 - y) * w + x] = Get(t, x0 + x, y0 + y);
        return px;
    }

    private static bool ColumnHasPixels(Texture2D t, int x)
    {
        for (int y = 0; y < t.height; y++) if (t.GetPixel(x, y).a > 0f) return true;
        return false;
    }

    private static void SavePart(Texture2D sheet, string name, int x0, int x1)
    {
        int top = sheet.height, bottom = -1;
        for (int y = 0; y < sheet.height; y++)
            for (int x = x0; x < x1; x++)
                if (Get(sheet, x, y).a > 0) { top = Mathf.Min(top, y); bottom = Mathf.Max(bottom, y); }
        int w = x1 - x0, h = bottom - top + 1;
        Save(name, Crop(sheet, x0, top, w, h), w, h, SpriteAlignment.Center);
    }

    // A bronze striker plate with rivets, 10 x 38.
    private static Color32[] Striker(out int w, out int h)
    {
        w = 10; h = 38;
        Color32 outline = new Color32(58, 36, 20, 255), bronze = new Color32(170, 118, 52, 255), light = new Color32(214, 164, 86, 255),
                dark = new Color32(122, 80, 36, 255), rivet = new Color32(96, 100, 108, 255);
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = x == 0 || y == 0 || x == w - 1 || y == h - 1 ? outline : x == 1 ? light : x == w - 2 ? dark : bronze;
        foreach (int y in new[] { 5, 18, 31 }) { px[y * w + 4] = rivet; px[y * w + 5] = rivet; }
        return px;
    }

    private static Color32[] Ring(out int w, out int h)
    {
        w = h = 16;
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(8f, 8f));
                if ((d <= 7.5f && d >= 5.5f) || d <= 1.6f) px[y * w + x] = new Color32(255, 255, 255, 255);
            }
        return px;
    }

    private static Texture2D Read(string path)
    {
        if (!File.Exists(path)) return null;
        Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        t.LoadImage(File.ReadAllBytes(path));
        return t;
    }

    private static void Save(string name, Color32[] pixels, int w, int h, SpriteAlignment pivot)
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
        s.spriteAlignment = (int)pivot;
        s.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(s);
        importer.SaveAndReimport();
    }

    public static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png");
}
