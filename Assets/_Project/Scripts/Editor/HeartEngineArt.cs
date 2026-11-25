using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Art for the Heart Engine hall (finale, part 2), made on demand by Room9Builder and Room9Dressing into
/// Art/Finale/HeartEngine (32 PPU, point filtered, FullRect so tiled sprites work):
///   Drawn in code (small, geometric pieces in the finale's wood / iron / brass / cyan palette):
///     Hammer_Head_Large (26x28) - the winch hammer: iron block, brass bands, a cyan rune, an eye for the chain.
///     Ring_Handle (18x20)       - the winch cord's brass pull ring on a slider plate (P2 tethers it).
///     Ratchet_Wheel (26x26), Ratchet_Pawl (14x6) - the hammer drum's ratchet (WinchRatchet).
///     Sprocket (32x32)          - the chain lift's top sprockets (ChainGears).
///     Lift_Deck (52x10)         - a chain-lift platform: oak planks with iron ends.
///     Beam (8x16)               - a wooden beam tile (tiled vertically: shaft posts, balcony post and struts).
///     Pipe (16x12)              - a plank ore chute tile (tiled along its length).
///     Hopper (32x28)            - the ore hopper on P1's balcony, heaped with shard ore.
///     Chain_Tile (32x6), Rope_Tile (32x4) + materials Line_Chain / Line_Rope - repeating textures for LineRenderers
///                                 (texture mode Tile: one 32 px repeat per metre, the game's pixel size).
///   Cut from the Legacy Fantasy sheets in Assets/ThirdParty/LegacyFantasy (pixel rectangles, top-left origin), copied
///   so the third-party files keep their own import settings: mossy rocks, standing stones, blue runes, plants, a bush.
/// Re-run to redraw (Room 8 Part 2 > 0. Build Heart Engine Art).
/// </summary>
public static class HeartEngineArt
{
    public const string Folder = "Assets/_Project/Art/Finale/HeartEngine";
    private const string Legacy = "Assets/ThirdParty/LegacyFantasy";

    public static Sprite Get(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png");
    public static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/{name}.mat");

    // Declared before LegacyCuts: static fields initialise in order.
    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f), Bottom = new Vector2(0.5f, 0f);

    // (sheet, sprite name, x0, y0, x1, y1 inclusive, top-left origin, pivot)
    private static readonly (string sheet, string name, int x0, int y0, int x1, int y1, Vector2 pivot)[] LegacyCuts =
    {
        ("Props-Rocks.png", "LF_Rock_Moss_A", 224, 85, 271, 127, Bottom),
        ("Props-Rocks.png", "LF_Rock_Moss_B", 193, 97, 222, 127, Bottom),
        ("Props-Rocks.png", "LF_Rock_Moss_Long", 212, 132, 270, 159, Bottom),
        ("Props-Rocks.png", "LF_Rock_Moss_Small", 176, 105, 191, 127, Bottom),
        ("Props-Rocks.png", "LF_Rock_Moss_Tall", 6, 82, 60, 159, Bottom),
        ("Props-Rocks.png", "LF_Stone_Tall", 71, 177, 107, 255, Bottom),
        ("Props-Rocks.png", "LF_Stone_Wide", 117, 193, 152, 255, Bottom),
        ("Props-Rocks.png", "LF_Rune_A", 145, 272, 158, 286, Center),
        ("Props-Rocks.png", "LF_Rune_B", 147, 288, 156, 302, Center),
        ("Props-Rocks.png", "LF_Rune_C", 178, 288, 189, 302, Center),
        ("Props-Rocks.png", "LF_Rune_D", 240, 288, 254, 302, Center),
        ("Tiles.png", "LF_Crystal_Flower", 306, 310, 333, 351, Bottom),
        ("Tiles.png", "LF_Blue_Plant", 274, 315, 301, 351, Bottom),
        ("Tiles.png", "LF_Fern", 320, 359, 351, 383, Bottom),
        ("Tiles.png", "LF_Mushroom", 321, 251, 334, 271, Bottom),
        ("Tiles.png", "LF_Cattail", 257, 326, 271, 351, Bottom),
        ("Tree-Assets.png", "LF_Bush_Dark", 210, 197, 333, 282, Bottom),
    };

    [MenuItem("Tools/Shards of Motion/Room 8 Part 2/0. Build Heart Engine Art")]
    public static void BuildAll() { Ensure(true); Debug.Log("Heart Engine art drawn into " + Folder + "."); }

    /// <summary>Draws / cuts every piece (all of them when 'redraw', else only missing ones). Returns false if a source sheet is missing.</summary>
    public static bool Ensure(bool redraw = false)
    {
        Directory.CreateDirectory(Folder);
        void Make(string name, System.Func<(Color32[] px, int w, int h)> draw, Vector2 pivot, bool repeat = false)
        {
            if (!redraw && Get(name) != null) return;
            var (px, w, h) = draw();
            Save(name, px, w, h, pivot, repeat);
        }
        Make("Hammer_Head_Large", HammerHead, Center);
        Make("Ring_Handle", RingHandle, Bottom);
        Make("Ratchet_Wheel", RatchetWheel, Center);
        Make("Ratchet_Pawl", Pawl, new Vector2(2.5f / 14f, 0.5f));
        Make("Sprocket", Sprocket, Center);
        Make("Lift_Deck", Deck, Center);
        Make("Beam", Beam, Center, true);
        Make("Pipe", Pipe, Center, true);
        Make("Hopper", Hopper, Bottom);
        Make("Chain_Tile", ChainTile, Center, true);
        Make("Rope_Tile", RopeTile, Center, true);
        Make("Grate", Grate, Center, true);
        Make("Bramble", Bramble, new Vector2(0.5f, 0f), true);
        Make("Dots_Tile", DotsTile, Center, true);
        Make("Chevron", Chevron, Center);
        Make("Ruler", Ruler, Center, true);
        Make("Ruler_Tick", RulerTick, new Vector2(0f, 0.5f));
        Make("Ruler_Marker", RulerMarker, new Vector2(0f, 0.5f));
        LineMaterial("Line_Chain", "Chain_Tile", redraw);
        LineMaterial("Line_Rope", "Rope_Tile", redraw);
        LineMaterial("Line_Dots", "Dots_Tile", redraw);

        bool ok = true;
        foreach (var cut in LegacyCuts)
        {
            if (!redraw && Get(cut.name) != null) continue;
            string path = $"{Legacy}/{cut.sheet}";
            if (!File.Exists(path)) { Debug.LogError("Heart Engine art: missing " + path); ok = false; continue; }
            Texture2D sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            sheet.LoadImage(File.ReadAllBytes(path)); // the file's own pixels (no need to make the imported texture readable)
            int w = cut.x1 - cut.x0 + 1, h = cut.y1 - cut.y0 + 1;
            Color32[] all = sheet.GetPixels32(), px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = all[(sheet.height - 1 - (cut.y1 - y)) * sheet.width + cut.x0 + x];
            Object.DestroyImmediate(sheet);
            Save(cut.name, px, w, h, cut.pivot, false);
        }
        return ok;
    }

    // ------------------------------------------------------------------ palette (as FinaleArt)

    private static readonly Color32 Clear = new Color32(0, 0, 0, 0), Outline = new Color32(40, 28, 22, 255),
        Wood = new Color32(118, 78, 46, 255), WoodLight = new Color32(150, 104, 62, 255), WoodDark = new Color32(86, 56, 34, 255),
        Iron = new Color32(70, 74, 82, 255), IronLight = new Color32(118, 124, 134, 255), IronDark = new Color32(48, 50, 58, 255),
        Brass = new Color32(176, 130, 58, 255), BrassLight = new Color32(222, 178, 96, 255), BrassDark = new Color32(128, 90, 40, 255),
        Cyan = new Color32(110, 230, 245, 255), CyanDark = new Color32(46, 150, 180, 255), CyanPale = new Color32(200, 250, 255, 255);

    // Drawing helpers: x right, y up, origin bottom-left.
    private static Color32[] Canvas(int w, int h) => new Color32[w * h];
    private static void Set(Color32[] p, int w, int x, int y, Color32 c) { if (x >= 0 && x < w && y >= 0 && y < p.Length / w) p[y * w + x] = c; }
    private static void Rect(Color32[] p, int w, int x0, int y0, int x1, int y1, Color32 c) { for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) Set(p, w, x, y, c); }

    // Rows drawn top-down as text: '.' clear, '#' outline, 'c' cyan dark, 'C' cyan, 'W' white-cyan.
    private static void Glyph(Color32[] p, int w, int left, int top, string[] rows)
    {
        for (int r = 0; r < rows.Length; r++)
            for (int i = 0; i < rows[r].Length; i++)
            {
                char ch = rows[r][i];
                Color32 c = ch == '#' ? IronDark : ch == 'c' ? CyanDark : ch == 'C' ? Cyan : ch == 'W' ? CyanPale : Clear;
                if (c.a > 0) Set(p, w, left + i, top - r, c);
            }
    }

    // ------------------------------------------------------------------ pieces

    // 26 x 28: an iron hammer block (rounded corners, polished striking faces), two brass bands, a glowing rune, the chain's eye.
    // 26 px (0.81 m) wide: at rest it just touches a cart standing against the striker.
    private static (Color32[], int, int) HammerHead()
    {
        int w = 26, h = 28;
        Color32[] p = Canvas(w, h);
        const int top = 21;
        for (int y = 0; y <= top; y++)
            for (int x = 0; x < w; x++)
            {
                bool corner = (x == 0 || x == w - 1) && (y == 0 || y == top);
                if (corner) continue;
                bool edge = x == 0 || x == w - 1 || y == 0 || y == top || ((x == 1 || x == w - 2) && (y == 1 || y == top - 1));
                Color32 c = edge ? Outline
                          : (x <= 2 || x >= w - 3) ? IronLight                 // striking faces
                          : (x == 5 || x == 6 || x == w - 7 || x == w - 6) ? (x == 5 || x == w - 7 ? BrassLight : Brass)
                          : y >= top - 3 ? IronLight : y <= 2 ? IronDark : Iron;
                Set(p, w, x, y, c);
            }
        foreach (int x in new[] { 5, 6, w - 7, w - 6 }) { Set(p, w, x, 4, BrassDark); Set(p, w, x, top - 4, BrassDark); } // band rivets
        Glyph(p, w, w / 2 - 4, 16, new[]
        {
            "...cc...",
            "..cCCc..",
            ".cCWWCc.",
            "cCW..WCc",
            "cC....Cc",
            "cCW..WCc",
            ".cCWWCc.",
            "..cCCc..",
            "...cc...",
            "...cc...",
            "..c..c..",
        });
        // The eye: a ring on a short neck for the chain.
        for (int y = top + 1; y < h; y++)
            for (int x = w / 2 - 5; x <= w / 2 + 4; x++)
            {
                float dx = x + 0.5f - w / 2f, dy = y + 0.5f - 24.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (y <= top + 2 && x >= w / 2 - 2 && x <= w / 2 + 1) { Set(p, w, x, y, x == w / 2 - 2 || x == w / 2 + 1 ? Outline : Iron); continue; }
                if (d > 4f || d < 1.6f) continue;
                Set(p, w, x, y, d > 3.2f || d < 2.3f ? Outline : IronLight);
            }
        return (p, w, h);
    }

    // 18 x 20: a brass pull ring standing on an iron slider plate (pivot: bottom centre, on the floor slot).
    private static (Color32[], int, int) RingHandle()
    {
        int w = 18, h = 20;
        Color32[] p = Canvas(w, h);
        Rect(p, w, 1, 0, 16, 3, Outline);
        Rect(p, w, 2, 1, 15, 2, Iron);
        Rect(p, w, 2, 2, 15, 2, IronLight);
        Rect(p, w, 7, 4, 10, 5, Outline);
        Rect(p, w, 8, 4, 9, 5, Brass);
        for (int y = 5; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = x + 0.5f - 9f, dy = y + 0.5f - 12.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 7.4f || d < 3.6f) continue;
                bool edge = d > 6.5f || d < 4.5f;
                Set(p, w, x, y, edge ? Outline : (dx + dy < -1f ? BrassDark : dx + dy > 2f ? BrassLight : Brass));
            }
        return (p, w, h);
    }

    // 26 x 26: a brass ratchet wheel: 12 saw teeth, an iron hub and four spokes.
    private static (Color32[], int, int) RatchetWheel()
    {
        int w = 26, h = 26;
        Color32[] p = Canvas(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = x + 0.5f - 13f, dy = y + 0.5f - 13f, d = Mathf.Sqrt(dx * dx + dy * dy);
                float f = Mathf.Repeat(Mathf.Atan2(dy, dx) / (2f * Mathf.PI) * 12f, 1f);
                float rim = 9.5f + 3.3f * f; // a sawtooth: each tooth ramps out, then drops straight back
                if (d > rim) continue;
                Color32 c;
                if (d > rim - 1f || (d > 8.5f && d < 9.5f)) c = Outline;
                else if (d > 9.5f) c = dx + dy > 0f ? BrassLight : Brass;
                else if (d < 2.2f) c = IronLight;
                else if (d < 3.2f) c = Outline;
                else c = (Mathf.Abs(dx) < 1.1f || Mathf.Abs(dy) < 1.1f) ? Brass : (d > 7.6f ? BrassDark : new Color32(70, 52, 30, 255));
                Set(p, w, x, y, c);
            }
        return (p, w, h);
    }

    // 14 x 6: the pawl: a pivot boss on the left, a hooked iron tooth on the right (pivot at the boss).
    private static (Color32[], int, int) Pawl()
    {
        int w = 14, h = 6;
        Color32[] p = Canvas(w, h);
        string[] rows =
        {
            ".###..........",
            "#BBB##########",
            "#BbB#IIIIIIII#",
            "#BBB#iiiiiii#.",
            ".###.#####ii#.",
            "..........##..",
        };
        for (int r = 0; r < h; r++)
            for (int x = 0; x < w; x++)
            {
                char ch = rows[r][x];
                Color32 c = ch == '#' ? Outline : ch == 'B' ? Brass : ch == 'b' ? BrassLight : ch == 'I' ? IronLight : ch == 'i' ? Iron : Clear;
                Set(p, w, x, h - 1 - r, c);
            }
        return (p, w, h);
    }

    // 32 x 32: an iron chain sprocket: 10 teeth, five round lightening holes, a brass hub.
    private static (Color32[], int, int) Sprocket()
    {
        int w = 32, h = 32;
        Color32[] p = Canvas(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = x + 0.5f - 16f, dy = y + 0.5f - 16f, d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Atan2(dy, dx);
                float f = Mathf.Repeat(a / (2f * Mathf.PI) * 10f, 1f);
                float rim = f > 0.25f && f < 0.75f ? 15.5f : 12.6f;
                if (d > rim) continue;
                // five holes between the spokes
                bool hole = false;
                for (int k = 0; k < 5; k++)
                {
                    float ha = k * 2f * Mathf.PI / 5f + 0.3f;
                    float hx = dx - Mathf.Cos(ha) * 7.5f, hy = dy - Mathf.Sin(ha) * 7.5f;
                    if (hx * hx + hy * hy < 6.2f) hole = true;
                }
                if (hole) continue;
                Color32 c = d > rim - 1f ? Outline : d > 11.6f ? (dx + dy > 0f ? IronLight : Iron)
                          : d > 10.8f ? IronDark : d < 2.4f ? BrassLight : d < 3.6f ? Brass : d < 4.4f ? Outline : Iron;
                Set(p, w, x, y, c);
            }
        return (p, w, h);
    }

    // 52 x 10: a lift platform: oak planks between iron end caps, bolts.
    private static (Color32[], int, int) Deck()
    {
        int w = 52, h = 10;
        Color32[] p = Canvas(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool edge = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                bool cap = x <= 4 || x >= w - 5;
                Color32 c = edge ? Outline : cap ? (y >= h - 3 ? IronLight : Iron)
                          : y == h - 2 ? WoodLight : (x - 5) % 9 == 8 ? WoodDark : y <= 2 ? WoodDark : Wood;
                Set(p, w, x, y, c);
            }
        foreach (int x in new[] { 2, w - 3 }) Set(p, w, x, 5, IronLight);
        return (p, w, h);
    }

    // 8 x 16 (tiles vertically): a wooden beam with grain and one iron bolt.
    private static (Color32[], int, int) Beam()
    {
        int w = 8, h = 16;
        Color32[] p = Canvas(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                Set(p, w, x, y, x == 0 || x == 7 ? Outline : x == 1 ? WoodLight : x == 6 ? WoodDark
                              : (x == 3 && y % 5 != 0) || (x == 5 && y % 7 == 3) ? WoodDark : Wood);
        Rect(p, w, 3, 8, 4, 9, IronLight);
        Set(p, w, 4, 8, Iron);
        return (p, w, h);
    }

    // 16 x 12 (tiles along x): a plank chute: two planks, an iron strap.
    private static (Color32[], int, int) Pipe()
    {
        int w = 16, h = 12;
        Color32[] p = Canvas(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 c = y == 0 || y == h - 1 ? Outline : x <= 1 ? (y == h - 2 ? IronLight : Iron)
                          : y == 6 ? WoodDark : y == h - 2 || y == 5 ? WoodLight : Wood;
                Set(p, w, x, y, c);
            }
        return (p, w, h);
    }

    // 32 x 28: a wooden ore hopper on short legs, iron-banded, heaped with glowing shard ore.
    private static (Color32[], int, int) Hopper()
    {
        int w = 32, h = 28;
        Color32[] p = Canvas(w, h);
        // legs
        Rect(p, w, 6, 0, 8, 5, Outline); Set(p, w, 7, 1, WoodDark); Set(p, w, 7, 2, Wood); Set(p, w, 7, 3, Wood); Set(p, w, 7, 4, Wood);
        Rect(p, w, 23, 0, 25, 5, Outline); Set(p, w, 24, 1, WoodDark); Set(p, w, 24, 2, Wood); Set(p, w, 24, 3, Wood); Set(p, w, 24, 4, Wood);
        // the bin: narrow at the bottom (spout), wide at the top
        for (int y = 5; y <= 20; y++)
        {
            int inset = (20 - y) / 2;
            for (int x = 1 + inset; x <= 30 - inset; x++)
            {
                bool edge = x == 1 + inset || x == 30 - inset || y == 5 || y == 20;
                Color32 c = edge ? Outline : (y == 9 || y == 17) ? Iron : y == 18 ? IronLight
                          : (x % 6 == 0) ? WoodDark : y == 19 ? WoodLight : Wood;
                Set(p, w, x, y, c);
            }
        }
        // ore heaped above the rim
        Glyph(p, w, 5, 27, new[]
        {
            "..........##..........",
            "......##.#WC#..##.....",
            ".....#CC#cCCc##Cc#....",
            "...##cCCCcCWCcCCCc##..",
            "..#cCCcCCCCCCcCCcCCc#.",
            ".#cCCCCcCCCCcCCCCCCCc#",
            "#cccCCccccCCcccCCcccc#",
        });
        return (p, w, h);
    }

    // 32 x 6 (repeats along a line): chain links, alternately face-on (an oval) and edge-on (a bar), 8 px each.
    private static (Color32[], int, int) ChainTile()
    {
        int w = 32, h = 6;
        Color32[] p = Canvas(w, h);
        for (int i = 0; i < 4; i++)
        {
            int x0 = i * 8;
            if (i % 2 == 0)
            {
                Rect(p, w, x0, 0, x0 + 8, 5, Outline);
                Rect(p, w, x0 + 1, 1, x0 + 7, 4, Iron);
                Rect(p, w, x0 + 1, 4, x0 + 7, 4, IronLight);
                Rect(p, w, x0 + 2, 2, x0 + 6, 3, Clear);
            }
            else
            {
                Rect(p, w, x0 - 1, 1, x0 + 9, 4, Outline);
                Rect(p, w, x0, 2, x0 + 8, 3, IronLight);
                Rect(p, w, x0, 2, x0 + 8, 2, Iron);
            }
        }
        return (p, w, h);
    }

    // 32 x 4 (repeats along a line): twisted hemp rope.
    private static (Color32[], int, int) RopeTile()
    {
        int w = 32, h = 4;
        Color32[] p = Canvas(w, h);
        Color32 light = new Color32(196, 160, 104, 255), mid = new Color32(158, 122, 76, 255), dark = new Color32(104, 78, 50, 255);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                Set(p, w, x, y, y == 0 || y == h - 1 ? dark : ((x + y * 2) % 4 < 2 ? light : mid));
        return (p, w, h);
    }

    // 16 x 8 (tiles along x): an iron floor grate: a top rail, bars, dark gaps (the shaft below shows through).
    private static (Color32[], int, int) Grate()
    {
        int w = 16, h = 8;
        Color32[] p = Canvas(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 c = y == h - 1 ? IronLight : y == h - 2 ? Iron : y == 0 ? IronDark
                          : x % 4 == 0 ? Iron : x % 4 == 1 ? IronDark : new Color32(20, 22, 28, 200);
                Set(p, w, x, y, c);
            }
        return (p, w, h);
    }

    // 16 x 14 (tiles along x, bottom pivot): purple thorn brambles: two twisting stems with pale spikes (a hazard reads by colour).
    private static (Color32[], int, int) Bramble()
    {
        int w = 16, h = 14;
        Color32[] p = Canvas(w, h);
        Color32 stem = new Color32(112, 52, 120, 255), stemDark = new Color32(70, 30, 82, 255), stemLight = new Color32(158, 84, 162, 255),
                spike = new Color32(226, 196, 230, 255);
        string[] rows = // top row first; S stem, D dark, L light, * spike
        {
            "....*.......*...",
            "...DL......LD...",
            "..*.SL....LS.*..",
            ".....SD..DS.....",
            "..*DS.S**S.SD*..",
            "...L..SDDS..L...",
            ".*S...LSSL...S*.",
            "..SL.*.SS.*.LS..",
            "*..SD..SS..DS..*",
            ".D..SL.SS.LS..D.",
            "S*...SDSSDS...*S",
            "SL..*.SSSS.*..LS",
            "DSD..DSSSSD..DSD",
            "DDSSSSDDDDSSSSDD",
        };
        for (int r = 0; r < h; r++)
            for (int x = 0; x < w; x++)
            {
                char ch = rows[r][x];
                Color32 c = ch == 'S' ? stem : ch == 'D' ? stemDark : ch == 'L' ? stemLight : ch == '*' ? spike : Clear;
                Set(p, w, x, h - 1 - r, c);
            }
        return (p, w, h);
    }

    // 32 x 4 (repeats along a line): two soft dashes per metre, white (tinted by the line's colour): the hammer's swing path.
    private static (Color32[], int, int) DotsTile()
    {
        int w = 32, h = 4;
        Color32[] p = Canvas(w, h);
        for (int d = 0; d < 2; d++)
            for (int x = d * 16 + 4; x < d * 16 + 12; x++)
                for (int y = 0; y < h; y++)
                {
                    bool end = x == d * 16 + 4 || x == d * 16 + 11, edge = y == 0 || y == h - 1;
                    if (end && edge) continue; // rounded ends
                    Set(p, w, x, y, new Color32(255, 255, 255, (byte)(edge || end ? 150 : 255)));
                }
        return (p, w, h);
    }

    // 12 x 8: a cyan chevron pointing up (rotate it for down): the chain lift's direction marks.
    private static (Color32[], int, int) Chevron()
    {
        int w = 12, h = 8;
        Color32[] p = Canvas(w, h);
        for (int x = 0; x < w; x++)
        {
            int peak = x < 6 ? x : 11 - x;           // 0..5..0: a roof shape
            for (int t = 0; t < 3; t++)
            {
                int y = peak + t;
                if (y >= h) continue;
                Set(p, w, x, y, t == 0 ? CyanDark : t == 1 ? Cyan : CyanPale);
            }
        }
        return (p, w, h);
    }

    // 6 x 8 (tiles vertically): a brass measuring strip with a small mark every 8 px (0.25 m).
    private static (Color32[], int, int) Ruler()
    {
        int w = 6, h = 8;
        Color32[] p = Canvas(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                Set(p, w, x, y, x == 0 || x == w - 1 ? Outline : y == 0 && x >= 3 ? BrassDark : x == 1 ? BrassLight : Brass);
        return (p, w, h);
    }

    // 12 x 3: a major tick (0.5 m marks), pivot at its left end (it sticks out to the right of the ruler).
    private static (Color32[], int, int) RulerTick()
    {
        int w = 12, h = 3;
        Color32[] p = Canvas(w, h);
        Rect(p, w, 0, 0, w - 1, h - 1, Outline);
        Rect(p, w, 0, 1, w - 2, 1, BrassLight);
        return (p, w, h);
    }

    // 10 x 9: the live height marker, a cyan arrow pointing left (at the hammer), pivot at its tip.
    private static (Color32[], int, int) RulerMarker()
    {
        int w = 10, h = 9;
        Color32[] p = Canvas(w, h);
        for (int x = 0; x < w; x++)
        {
            int half = Mathf.Min(4, x / 1);          // widens from the tip
            for (int y = 4 - half; y <= 4 + half; y++)
            {
                bool edge = y == 4 - half || y == 4 + half || x == w - 1 || x == 0;
                Set(p, w, x, y, edge ? CyanDark : (y == 4 ? CyanPale : Cyan));
            }
        }
        return (p, w, h);
    }

    // ------------------------------------------------------------------ saving

    private static void LineMaterial(string name, string texture, bool redraw)
    {
        string path = $"{Folder}/{name}.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null && !redraw) return;
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/{texture}.png");
        if (m == null)
        {
            m = new Material(AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat")); // same shader as the other lines
            AssetDatabase.CreateAsset(m, path);
        }
        m.mainTexture = tex;
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
    }

    private static void Save(string name, Color32[] pixels, int w, int h, Vector2 pivot, bool repeat)
    {
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
        importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        TextureImporterSettings s = new TextureImporterSettings();
        importer.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = pivot;
        s.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(s);
        importer.SaveAndReimport();
    }
}
