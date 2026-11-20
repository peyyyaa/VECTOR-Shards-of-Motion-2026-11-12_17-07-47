using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws the small pieces of the village trailhead (the way into the regions, east end of Forge &amp; East Road), in the
/// Village Props pack's wood colours, at 32 px per metre:
///   gateway posts, a signpost and the crossbeam, the hanging "SHATTERED GROUNDS" sign, the two closed-road planks, the rune stones
///   (dark / lit), and the little map pinned on the trail-map board.
/// Lettering uses a 3x5 pixel font carved into the wood. Plain drawing only; VillageBuilder saves and places them.
/// </summary>
public static class TrailheadPainter
{
    public struct Image { public string name; public int w, h; public Color32[] px; }

    private static readonly Color32 Edge = new Color32(74, 47, 28, 255), Wood = new Color32(150, 108, 66, 255);
    private static readonly Color32 WoodLight = new Color32(176, 132, 84, 255), WoodDark = new Color32(122, 86, 52, 255);
    private static readonly Color32 Old = new Color32(112, 86, 62, 255), OldDark = new Color32(92, 70, 50, 255);
    private static readonly Color32 Carve = new Color32(255, 240, 205, 255), CarveOld = new Color32(198, 178, 148, 255);
    private static readonly Color32 Rope = new Color32(196, 168, 112, 255), Closed = new Color32(178, 58, 44, 255), ClosedText = new Color32(255, 232, 214, 255);
    private static readonly Color32 Stone = new Color32(126, 126, 122, 255), StoneDark = new Color32(98, 96, 92, 255), StoneEdge = new Color32(62, 58, 54, 255);
    private static readonly Color32 Rune = new Color32(88, 104, 108, 255), RuneLit = new Color32(130, 240, 250, 255), RuneCore = new Color32(220, 255, 255, 255);
    private static readonly Color32 Paper = new Color32(236, 222, 184, 255), Ink = new Color32(74, 47, 28, 255), Pin = new Color32(190, 50, 40, 255);

    private static Color32[] px;
    private static int W, H;

    public static List<Image> DrawAll()
    {
        return new List<Image>
        {
            Post("Post", 86), Post("SignPost", 100), Beam(),
            HangingSign("Sign_Region1", new[] { "SHATTERED", "GROUNDS" }),
            ClosedPlank("Plank_Region2", new[] { "GRAVITY", "DIVIDE" }),
            ClosedPlank("Plank_Region3", new[] { "MECH.", "DEPTHS" }),
            RuneStone("Stone_Dark", false), RuneStone("Stone_Lit", true),
            MapNotice(),
        };
    }

    // ------------------------------------------------------------------ pieces

    // A wooden post, 6 px wide (gateway: 86 px = 2.7 m; signpost: 100 px), rounded cap, a little grain.
    private static Image Post(string name, int height)
    {
        Begin(6, height);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                Color32 c = x == 0 || x == W - 1 ? Edge : x == 1 ? WoodLight : x == W - 2 ? WoodDark : Wood;
                if (y > 0 && y < H - 2 && x >= 2 && x <= 3 && (y * 7 + x * 3) % 11 == 0) c = WoodDark;
                Set(x, y, c);
            }
        for (int x = 0; x < W; x++) { Set(x, H - 1, Edge); Set(x, H - 2, x == 0 || x == W - 1 ? Edge : WoodLight); }
        Set(0, H - 1, default); Set(W - 1, H - 1, default);
        return Done(name);
    }

    // Crossbeam: 96 x 6, end caps.
    private static Image Beam()
    {
        Begin(96, 6);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                Set(x, y, y == 0 || y == H - 1 || x == 0 || x == W - 1 ? Edge : y == H - 2 ? WoodLight : y == 1 ? WoodDark : Wood);
        foreach (int x in new[] { 3, W - 4 }) { Set(x, 2, Edge); Set(x, 3, Edge); } // pegs
        return Done("Beam");
    }

    // A plank on two ropes; the top 5 px are rope (hung from the beam).
    private static Image HangingSign(string name, string[] lines)
    {
        int textW = Max(lines) * 4 - 1;
        int pw = textW + 10, ph = lines.Length * 7 + 4, rope = 5;
        Begin(pw, ph + rope);
        Board(0, 0, pw, ph, Wood, WoodDark, WoodLight);
        for (int i = 0; i < lines.Length; i++) Letters(lines[i], (pw - (lines[i].Length * 4 - 1)) / 2, ph - 4 - 5 - i * 7, Carve);
        foreach (int x in new[] { 3, pw - 4 })
        {
            for (int y = ph; y < ph + rope; y++) Set(x, y, Rope);
            Set(x, ph - 2, Edge); // nail
        }
        return Done(name);
    }

    // An old dark plank pointing east, two lines, with a red CLOSED strip nailed across its lower edge.
    private static Image ClosedPlank(string name, string[] lines)
    {
        int textW = Max(lines) * 4 - 1;
        int bw = Mathf.Max(textW, 23) + 8, ph = lines.Length * 7 + 9;
        Begin(bw + 5, ph);
        Board(0, 0, bw, ph, Old, OldDark, Old);
        for (int y = 1; y < ph - 1; y++) // arrow tip
        {
            int d = Mathf.Abs(y - ph / 2);
            int len = 5 - d * 5 / (ph / 2 + 1);
            for (int x = 0; x < len; x++) Set(bw + x, y, x == len - 1 ? Edge : Old);
        }
        for (int i = 0; i < lines.Length; i++) Letters(lines[i], 4, ph - 3 - 5 - i * 7, CarveOld);
        // CLOSED strip: 25 x 7, nailed across the lower edge
        int sx = (bw - 25) / 2, sy = 1;
        for (int x = 0; x < 25; x++)
            for (int y = 0; y < 7; y++)
                Set(sx + x, sy + y, y == 0 || y == 6 || x == 0 || x == 24 ? new Color32(110, 30, 24, 255) : Closed);
        Letters("CLOSED", sx + 1, sy + 1, ClosedText);
        return Done(name);
    }

    // Flat stone, 22 x 7, with a spiral rune (dark, or lit cyan).
    private static Image RuneStone(string name, bool lit)
    {
        Begin(22, 7);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                bool corner = (x == 0 || x == W - 1) && (y == 0 || y == H - 1);
                if (corner) continue;
                bool edge = x == 0 || x == W - 1 || y == 0 || y == H - 1;
                Set(x, y, edge ? StoneEdge : y == H - 2 ? Stone : y < 3 ? StoneDark : Stone);
            }
        Color32 r = lit ? RuneLit : Rune;
        int[,] spiral = { { 9, 3 }, { 10, 3 }, { 11, 3 }, { 12, 4 }, { 12, 5 }, { 11, 5 }, { 10, 5 }, { 9, 5 }, { 8, 4 }, { 8, 3 }, { 8, 2 }, { 9, 1 }, { 10, 1 }, { 11, 1 }, { 12, 1 }, { 13, 2 } };
        for (int i = 0; i < spiral.GetLength(0); i++) Set(spiral[i, 0], spiral[i, 1], r);
        if (lit) { Set(10, 3, RuneCore); Set(11, 3, RuneCore); Set(5, 4, RuneLit); Set(16, 4, RuneLit); }
        return Done(name);
    }

    // The little map pinned on the trail-map board: 36 x 24 paper, dashed path with room dots, a red pin.
    private static Image MapNotice()
    {
        Begin(36, 24);
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) Set(x, y, (x + y * 3) % 17 == 0 ? new Color32(222, 204, 160, 255) : Paper);
        for (int x = 0; x < W; x++) { Set(x, 0, new Color32(190, 168, 120, 255)); Set(x, H - 1, new Color32(214, 196, 150, 255)); }
        Vector2Int[] pts = { new Vector2Int(4, 6), new Vector2Int(10, 5), new Vector2Int(15, 8), new Vector2Int(21, 6), new Vector2Int(26, 10),
                             new Vector2Int(22, 14), new Vector2Int(15, 15), new Vector2Int(9, 17), new Vector2Int(17, 20) };
        for (int i = 0; i < pts.Length - 1; i++) Dashed(pts[i], pts[i + 1]);
        for (int i = 1; i < pts.Length; i++) { Set(pts[i].x, pts[i].y, Ink); }
        Set(3, 6, Ink); Set(4, 7, Ink); Set(5, 6, Ink); // the village
        // fog on the right
        for (int y = 3; y < H - 3; y++) for (int x = 29; x < W - 2; x++) if ((x * 3 + y * 5) % 4 == 0) Set(x, y, new Color32(150, 140, 120, 255));
        Set(21, 7, Pin); Set(22, 7, Pin); Set(21, 8, Pin); Set(22, 8, Pin); Set(21, 6, Edge); // pin on the furthest room
        Set(17, 1, Edge); Set(18, 1, Edge); // tack at the bottom
        Set(17, H - 2, Edge); Set(18, H - 2, Edge);
        return Done("Map_Notice");
    }

    // ------------------------------------------------------------------ helpers

    private static void Board(int x0, int y0, int w, int h, Color32 body, Color32 dark, Color32 light)
    {
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool edge = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                Color32 c = edge ? Edge : y == h - 2 ? light : y == 1 ? dark : body;
                if (!edge && (x * 5 + y * 11) % 23 == 0) c = dark; // grain
                Set(x0 + x, y0 + y, c);
            }
    }

    // 3x5 glyphs, rows top to bottom.
    private static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
    {
        { 'A', new[] { ".#.", "#.#", "###", "#.#", "#.#" } }, { 'B', new[] { "##.", "#.#", "##.", "#.#", "##." } },
        { 'C', new[] { ".##", "#..", "#..", "#..", ".##" } }, { 'D', new[] { "##.", "#.#", "#.#", "#.#", "##." } },
        { 'E', new[] { "###", "#..", "##.", "#..", "###" } }, { 'F', new[] { "###", "#..", "##.", "#..", "#.." } },
        { 'G', new[] { ".##", "#..", "#.#", "#.#", ".##" } }, { 'H', new[] { "#.#", "#.#", "###", "#.#", "#.#" } },
        { 'I', new[] { "###", ".#.", ".#.", ".#.", "###" } }, { 'J', new[] { "..#", "..#", "..#", "#.#", ".#." } },
        { 'K', new[] { "#.#", "#.#", "##.", "#.#", "#.#" } }, { 'L', new[] { "#..", "#..", "#..", "#..", "###" } },
        { 'M', new[] { "#.#", "###", "###", "#.#", "#.#" } }, { 'N', new[] { "##.", "#.#", "#.#", "#.#", "#.#" } },
        { 'O', new[] { ".#.", "#.#", "#.#", "#.#", ".#." } }, { 'P', new[] { "##.", "#.#", "##.", "#..", "#.." } },
        { 'Q', new[] { ".#.", "#.#", "#.#", "##.", ".##" } }, { 'R', new[] { "##.", "#.#", "##.", "#.#", "#.#" } },
        { 'S', new[] { ".##", "#..", ".#.", "..#", "##." } }, { 'T', new[] { "###", ".#.", ".#.", ".#.", ".#." } },
        { 'U', new[] { "#.#", "#.#", "#.#", "#.#", "###" } }, { 'V', new[] { "#.#", "#.#", "#.#", "#.#", ".#." } },
        { 'W', new[] { "#.#", "#.#", "###", "###", "#.#" } }, { 'X', new[] { "#.#", "#.#", ".#.", "#.#", "#.#" } },
        { 'Y', new[] { "#.#", "#.#", ".#.", ".#.", ".#." } }, { 'Z', new[] { "###", "..#", ".#.", "#..", "###" } },
        { '.', new[] { "...", "...", "...", "...", ".#." } }, { ' ', new[] { "...", "...", "...", "...", "..." } },
    };

    // Draws text with its bottom-left at (x, y); 4 px per letter.
    private static void Letters(string text, int x, int y, Color32 c)
    {
        foreach (char ch in text)
        {
            if (Font.TryGetValue(char.ToUpperInvariant(ch), out string[] g))
                for (int row = 0; row < 5; row++)
                    for (int col = 0; col < 3; col++)
                        if (g[row][col] == '#')
                            Set(x + col, y + (4 - row), c);
            x += 4;
        }
    }

    private static int Max(string[] lines) { int m = 0; foreach (string l in lines) m = Mathf.Max(m, l.Length); return m; }

    private static void Dashed(Vector2Int a, Vector2Int b)
    {
        int steps = Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
        for (int i = 0; i <= steps; i++)
        {
            if (i % 3 == 2) continue;
            float t = steps == 0 ? 0f : i / (float)steps;
            Set(Mathf.RoundToInt(Mathf.Lerp(a.x, b.x, t)), Mathf.RoundToInt(Mathf.Lerp(a.y, b.y, t)), Ink);
        }
    }

    private static void Begin(int w, int h) { W = w; H = h; px = new Color32[w * h]; }

    private static Image Done(string name) => new Image { name = name, w = W, h = H, px = px };

    private static void Set(int x, int y, Color32 c)
    {
        if (x < 0 || y < 0 || x >= W || y >= H) return;
        px[y * W + x] = c;
    }
}
