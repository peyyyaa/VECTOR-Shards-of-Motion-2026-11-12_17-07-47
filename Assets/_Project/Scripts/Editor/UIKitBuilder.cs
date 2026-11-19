using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>
/// Editor tool: Tools > Shards of Motion > UI Kit.
///   1. Build Font      - TMP font asset for Jersey 10 (OFL), with LiberationSans as fallback for the
///                        physics symbols Jersey doesn't have (μ, Δ, ², ≈, →).
///   2. Build UI Kit    - draws the original "Field Journal" pixel UI kit (parchment page, note card, tape,
///                        ring binding, title ribbon, bookmark tabs, wax seal, buttons, divider, icons) into
///                        Art/Sprites/UI as 9-slice-ready sprites. Each art pixel shows as 3x3 screen pixels at 1080p.
///   3. Build Preview   - Scenes/Prototype/Art_UIKit_Preview: a mock journal spread assembled from the kit.
/// Everything is drawn in code (no external art); artists can repaint any PNG later without code changes.
/// </summary>
public static class UIKitBuilder
{
    private const string FontTtf = "Assets/_Project/Art/Fonts/Jersey10-Regular.ttf";
    private const string FontAssetPath = "Assets/_Project/Art/Fonts/Jersey10 SDF.asset";
    private const string FallbackFont = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string KitFolder = "Assets/_Project/Art/Sprites/UI";
    private const string PreviewScene = "Assets/_Project/Scenes/Prototype/Art_UIKit_Preview.unity";
    private const float ArtPixelsPerUnit = 100f / 3f; // canvas reference 100 px/unit -> 1 art px = 3 canvas px

    // Palette: parchment, ink, Village-style green and gold, wax red, magic cyan, burst orange.
    private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    private static readonly Color32 Ink = Hex("4a2f1c"), InkSoft = Hex("6b4a2e");
    private static readonly Color32 PaperL = Hex("f4e4be"), PaperM = Hex("ead39f"), PaperD = Hex("d8b67c"), PaperEdge = Hex("b88e5a");
    private static readonly Color32 GreenD = Hex("3e6230"), Green = Hex("5e8a3e"), GreenL = Hex("86b058");
    private static readonly Color32 GoldD = Hex("a57f2c"), Gold = Hex("d4ac48"), GoldL = Hex("f1d684");
    private static readonly Color32 RedD = Hex("7e2a22"), Red = Hex("b2443a"), RedL = Hex("d8695c");
    private static readonly Color32 Cyan = Hex("49c2d0"), CyanL = Hex("bfeef2"), Orange = Hex("e8893a"), OrangeL = Hex("f7c27a");
    private static readonly Color32 Metal = Hex("8c8478"), MetalL = Hex("c4bcae"), WoodD = Hex("5c4128"), Wood = Hex("7b5a3a"), WoodL = Hex("9c7650");
    private static readonly Color32 White = Hex("fffbf0");

    // ------------------------------------------------------------------ 1. font

    [MenuItem("Tools/Shards of Motion/UI Kit/1. Build Font")]
    public static void BuildFont()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontTtf);
        if (font == null) { Debug.LogError("UI Kit: " + FontTtf + " not found."); return; }
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null) { Debug.Log("UI Kit: font asset already exists."); return; }

        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        asset.name = "Jersey10 SDF";
        TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFont);
        if (fallback != null) asset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };

        AssetDatabase.CreateAsset(asset, FontAssetPath);
        asset.atlasTextures[0].name = "Jersey10 SDF Atlas";
        AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
        asset.material.name = "Jersey10 SDF Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        AssetDatabase.SaveAssets();
        Debug.Log("UI Kit: Jersey 10 font asset created (fallback: LiberationSans for μ Δ ² ≈ →).");
    }

    // ------------------------------------------------------------------ 2. kit

    [MenuItem("Tools/Shards of Motion/UI Kit/2. Build UI Kit")]
    public static void BuildKit()
    {
        Directory.CreateDirectory(KitFolder);

        Save("UI_Page", Page(64, 64, 7), new Vector4(16, 16, 16, 16));
        Save("UI_Card", Card(48, 48, 3), new Vector4(12, 12, 12, 12));
        Save("UI_Cover", Cover(48, 48), new Vector4(16, 16, 16, 16));
        Save("UI_Shadow", Shadow(24, 24), new Vector4(10, 10, 10, 10));
        Save("UI_Gutter", Gutter(), Vector4.zero);
        Save("UI_Tape", Tape(), Vector4.zero);
        Save("UI_Rings", Rings(), Vector4.zero);
        Save("UI_Block", Block(), Vector4.zero);
        Save("UI_ArrowShaft", ArrowShaft(), new Vector4(2, 0, 2, 0));
        Save("UI_ArrowHead", ArrowHead(), Vector4.zero);
        Save("UI_Ribbon", Ribbon(64, 16), new Vector4(9, 4, 9, 4));
        Save("UI_Button", Button(48, 16, false), new Vector4(6, 6, 6, 6));
        Save("UI_Button_Pressed", Button(48, 16, true), new Vector4(6, 6, 6, 6));
        Save("UI_Divider", Divider(), new Vector4(4, 0, 4, 0));
        Save("UI_Seal", Seal(), Vector4.zero);
        foreach ((string name, Color32 body, Color32 light) in new[] { ("Green", Green, GreenL), ("Gold", Gold, GoldL), ("Cyan", Cyan, CyanL),
                                                                        ("Orange", Orange, OrangeL), ("Red", Red, RedL), ("Locked", Metal, MetalL) })
            Save("UI_Tab_" + name, Tab(body, light), Vector4.zero);

        foreach (KeyValuePair<string, string[]> icon in Icons)
            Save("UI_Icon_" + icon.Key, Stamp(icon.Value), Vector4.zero);

        // Journal extras: page dot (white, tinted in code), folded page corner, Wren's ink doodles.
        Save("UI_Dot", Stamp(new[] { ".WWW.", "WWWWW", "WWWWW", "WWWWW", ".WWW." }), Vector4.zero);
        Save("UI_PageCurl", Stamp(new[]
        {
            "OOOOOOOOOOOO", "OLLLLLLLLLOK", "OLMMMMMMMOKK", "OLMMMMMMOKKK", "OLMMMMMOKKKK", "OLMMMMOKKKKK",
            "OLMMMOKKKKKK", "OLMMOKKKKKKK", "OLMOKKKKKKKK", "OLOKKKKKKKKK", "OOKKKKKKKKKK", "OKKKKKKKKKKK",
        }), Vector4.zero);
        Save("UI_Doodle_Crate", Stamp(new[]
        {
            "....OOOOOOOOOOO.", "....O.O.....O.O.", "OO..O..O...O..O.", "....O...O.O...O.", ".OOOO....O....O.",
            "....O...O.O...O.", "OO..O..O...O..O.", "....O.O.....O.O.", "....OOOOOOOOOOO.",
        }), Vector4.zero);
        Save("UI_Doodle_Sparkle", Stamp(new[] { "....O....", "....O....", "...O.O...", "OOO...OOO", "...O.O...", "....O....", "....O...." }), Vector4.zero);

        AssetDatabase.Refresh();
        Debug.Log("UI Kit: sprites written to " + KitFolder + ".");
    }

    // Parchment page (9-slice, centre TILED, not stretched):
    //  - centre: flat paper with sparse, low-contrast 1-px flecks (tiles seamlessly);
    //  - border: aged edges that darken toward the outline (dithered), rounded corners;
    //  - bottom: a few stacked page edges, so the page has thickness.
    private static Color32[,] Page(int w, int h, int seed)
    {
        Color32[,] p = new Color32[w, h];
        System.Random rng = new System.Random(seed);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                double r = rng.NextDouble();
                Color32 c = r < 0.025 ? PaperD : r < 0.06 ? PaperL : PaperM;      // sparse soft flecks
                if (d <= 2) c = PaperEdge;                                          // burnt edge
                else if (d <= 4) c = PaperD;
                else if (d <= 6) c = (x + y) % 2 == 0 ? PaperD : PaperM;            // dithered fade
                else if (d <= 9 && (x + y) % 4 == 0) c = PaperD;                    // faint fade
                p[x, y] = c;
            }
        for (int x = 1; x < w - 1; x++) { p[x, 1] = PaperEdge; p[x, 2] = PaperL; p[x, 3] = PaperEdge; p[x, 4] = PaperL; } // page stack
        Outline(p, Ink);
        RoundCorners(p);
        return p;
    }

    // Note card: light paper with a soft edge and a built-in 2-px drop shadow (bottom-right), so it sits on the page.
    private static Color32[,] Card(int w, int h, int seed)
    {
        Color32[,] p = Fill(w, h, Clear);
        System.Random rng = new System.Random(seed);
        Color32 shadow = new Color32(60, 35, 20, 70);
        for (int y = 0; y < h - 2; y++) { p[w - 1, y] = shadow; p[w - 2, y] = shadow; }
        for (int x = 2; x < w; x++) { p[x, 0] = shadow; p[x, 1] = shadow; }
        for (int y = 2; y < h; y++)
            for (int x = 0; x < w - 2; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, w - 3 - x), Mathf.Min(y - 2, h - 1 - y));
                double r = rng.NextDouble();
                Color32 c = r < 0.03 ? PaperM : PaperL;
                if (d == 0) c = PaperEdge;
                else if (d <= 2 && (x + y) % 2 == 0) c = PaperM;
                p[x, y] = c;
            }
        return p;
    }

    // Leather book cover behind the pages: deep green leather, dark rim, gold stitching, brass corners.
    private static Color32[,] Cover(int w, int h)
    {
        Color32 leather = Hex("35502f"), leatherD = Hex("26391f"), leatherL = Hex("456641");
        Color32[,] p = new Color32[w, h];
        System.Random rng = new System.Random(11);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                Color32 c = rng.NextDouble() < 0.08 ? leatherL : leather;
                if (d <= 2) c = leatherD;
                if (d == 5 && (x + y) % 3 != 0) c = GoldD; // stitching
                p[x, y] = c;
            }
        foreach ((int cx, int cy) in new[] { (0, 0), (w - 6, 0), (0, h - 6), (w - 6, h - 6) }) // brass corners
            for (int y = 0; y < 6; y++) for (int x = 0; x < 6; x++)
                p[cx + x, cy + y] = (x == 2 && y == 3) || (x == 3 && y == 2) ? GoldL : (x == 0 || y == 0 || x == 5 || y == 5 ? GoldD : Gold);
        Outline(p, Ink);
        RoundCorners(p);
        return p;
    }

    // Soft drop shadow (stepped alpha), 9-slice: placed behind pages and ribbons, offset down-right.
    private static Color32[,] Shadow(int w, int h)
    {
        Color32[,] p = Fill(w, h, Clear);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                byte a = d >= 6 ? (byte)110 : d >= 3 ? (byte)70 : (byte)35;
                p[x, y] = new Color32(20, 12, 6, a);
            }
        return p;
    }

    // Spine shadow: transparent to dark, left to right (flip it for the other page). Stretched to the page height.
    private static Color32[,] Gutter()
    {
        int w = 16, h = 4;
        Color32[,] p = Fill(w, h, Clear);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                int a = x * x / 3; // steeper near the spine
                p[x, y] = new Color32(70, 40, 20, (byte)Mathf.Min(120, a));
            }
        return p;
    }

    private static Color32[,] Tape()
    {
        int w = 20, h = 8;
        Color32[,] p = Fill(w, h, new Color32(245, 236, 210, 190));
        for (int y = 0; y < h; y++) { if (y % 2 == 0) { p[0, y] = Clear; p[w - 1, y] = Clear; } }
        for (int x = 1; x < w - 1; x++) { p[x, h - 1] = new Color32(255, 250, 235, 215); p[x, 0] = new Color32(200, 180, 140, 170); }
        return p;
    }

    // Diagram object: a stone block (16x16) with ink outline and top-left light.
    private static Color32[,] Block()
    {
        Color32 stone = Hex("8a8f99"), stoneL = Hex("aeb3bc"), stoneD = Hex("6a6f78");
        Color32[,] p = Fill(16, 16, stone);
        for (int i = 1; i < 15; i++) { p[i, 14] = stoneL; p[1, i] = stoneL; p[i, 1] = stoneD; p[14, i] = stoneD; }
        p[5, 9] = stoneD; p[6, 9] = stoneD; p[10, 5] = stoneD; p[9, 11] = stoneL; // a few chips
        Outline(p, Ink);
        RoundCorners(p);
        return p;
    }

    // Arrow parts in white (tinted per force by the diagram): shaft (9-slice horizontally) and head (points right).
    private static Color32[,] ArrowShaft()
    {
        Color32[,] p = Fill(8, 5, White);
        for (int x = 0; x < 8; x++) { p[x, 0] = Hex("d8d0c0"); p[x, 4] = Hex("d8d0c0"); }
        return p;
    }

    private static Color32[,] ArrowHead()
    {
        Color32[,] p = Fill(9, 9, Clear);
        for (int x = 0; x < 9; x++)
            for (int y = 0; y < 9; y++)
                if (Mathf.Abs(y - 4) <= 4 - x / 2) p[x, y] = Mathf.Abs(y - 4) == 4 - x / 2 ? Hex("d8d0c0") : White;
        return p;
    }

    // One metal binder ring (16x10): a loop over the spine with punched holes where it enters each page.
    // Placed one Image per ring by the journal builders (not tiled), so each ring reads as its own piece of metal.
    private static Color32[,] Rings()
    {
        string[] ring =
        {
            "....OOOOOOOO....",
            "..OOLLLLLLLLOO..",
            ".OLLMMMMMMMMLLO.",
            "OLMOOOOOOOOOOMLO",
            "HOMO........OMOH",
            "HOMO........OMOH",
            "OMDOOOOOOOOOODMO",
            ".ODDDDDDDDDDDDO.",
            "..OODDDDDDDDOO..",
            "....OOOOOOOO....",
        };
        Dictionary<char, Color32> key = new Dictionary<char, Color32>
            { { 'O', Ink }, { 'L', MetalL }, { 'M', Metal }, { 'D', Hex("5e5850") }, { 'H', Hex("2e1d12") } };
        int w = ring[0].Length, h = ring.Length;
        Color32[,] p = Fill(w, h, Clear);
        for (int row = 0; row < h; row++)
            for (int x = 0; x < w; x++)
                if (key.TryGetValue(ring[row][x], out Color32 c)) p[x, h - 1 - row] = c;
        return p;
    }

    // Title ribbon: green band with gold trim and forked ends.
    private static Color32[,] Ribbon(int w, int h)
    {
        Color32[,] p = Fill(w, h, Clear);
        for (int x = 0; x < w; x++)
            for (int y = 1; y < h - 1; y++)
            {
                Color32 c = y == 2 || y == h - 3 ? Gold : y == 1 || y == h - 2 ? Ink : (y == h - 4 ? GreenL : Green);
                if (y == 3) c = GreenD;
                p[x, y] = c;
            }
        int mid = h / 2;
        for (int k = 0; k < 6; k++) // forked notches (6 px deep)
            for (int y = mid - 3 + k / 2; y <= mid + 2 - k / 2; y++) { p[k, y] = Clear; p[w - 1 - k, y] = Clear; }
        return p;
    }

    private static Color32[,] Button(int w, int h, bool pressed)
    {
        Color32[,] p = Fill(w, h, pressed ? WoodD : Wood);
        for (int x = 1; x < w - 1; x++) { p[x, h - 2] = pressed ? Wood : WoodL; p[x, 1] = WoodD; }
        for (int x = 4; x < w - 4; x += 9) p[x, h / 2 - 1] = InkSoft; // wood grain
        p[3, h / 2] = Gold; p[w - 4, h / 2] = Gold; // nails
        Outline(p, Ink);
        RoundCorners(p);
        return p;
    }

    private static Color32[,] Divider()
    {
        int w = 16, h = 3;
        Color32[,] p = Fill(w, h, Clear);
        for (int x = 0; x < w; x += 2) p[x, 1] = InkSoft;
        return p;
    }

    // Wax seal with an embossed ring and a shard glyph.
    private static Color32[,] Seal()
    {
        int s = 24;
        Color32[,] p = Fill(s, s, Clear);
        Vector2 c = new Vector2(11.5f, 11.5f);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) + ((x * 7 + y * 3) % 5) * 0.12f; // slightly wobbly edge
                if (d <= 11.2f) p[x, y] = d > 10.2f ? RedD : (Mathf.Abs(d - 7.5f) < 0.6f ? RedL : Red);
            }
        string[] shard = { "..OO..", ".OLLO.", "OLLMMO", "OLMMMO", ".OMMO.", "..OO.." };
        for (int y = 0; y < shard.Length; y++)
            for (int x = 0; x < 6; x++)
            {
                char k = shard[y][x];
                if (k == '.') continue;
                p[9 + x, 14 - y] = k == 'O' ? RedD : k == 'L' ? GoldL : Gold;
            }
        return p;
    }

    // Bookmark tab: flat top, V-notch at the bottom, light left edge.
    private static Color32[,] Tab(Color32 body, Color32 light)
    {
        int w = 16, h = 28;
        Color32[,] p = Fill(w, h, body);
        for (int y = 0; y < h; y++) p[1, y] = light;
        for (int k = 0; k < 5; k++)
            for (int x = 8 - (5 - k); x < 8 + (5 - k); x++) p[x, k] = Clear; // notch at the bottom (y = 0 is bottom)
        Outline(p, Ink);
        return p;
    }

    // 12x12 icons, top row first. O ink, P paper, L line, G green, Y/W bulb, I metal, C glass, B handle, R red, E orange, T cyan.
    private static readonly Dictionary<string, string[]> Icons = new Dictionary<string, string[]>
    {
        { "Book", new[] { "............", ".OOOO..OOOO.", "OPPPPOOPPPPO", "OPLLPOOPLLPO", "OPPPPOOPPPPO", "OPLLPOOPLLPO",
                          "OPPPPOOPPPPO", "OPLLPOOPLLPO", "OPPPPOOPPPPO", "OGGGGOOGGGGO", ".OOOOOOOOOO.", "............" } },
        { "Bulb", new[] { "....OOOO....", "...OYYYYO...", "..OYWYYYYO..", "..OYWYYYYO..", "..OYYYYYYO..", "..OYYYYYYO..",
                          "...OYYYYO...", "....OYYO....", "....OIIO....", "....OIIO....", ".....OO.....", "............" } },
        { "Magnifier", new[] { "............", "...OOOO.....", "..OCCCCO....", ".OCWCCCCO...", ".OCCCCCCO...", ".OCCCCCCO...",
                               "..OCCCCO....", "...OOOOBB...", ".......BBB..", "........BBB.", ".........BB.", "............" } },
        { "Scale", new[] { "......O.....", ".OOOOOOOOOO.", ".O...OO...O.", "OOO..OO..OOO", "GGG..OO..GGG", ".....OO.....",
                           ".....OO.....", "....OOOO....", "...OOOOOO...", "............", "............", "............" } },
        { "Force", new[] { "............", "............", "........R...", "........RR..", ".RRRRRRRRRR.", ".RRRRRRRRRRR",
                           ".RRRRRRRRRR.", "........RR..", "........R...", "............", "............", "............" } },
        { "Burst", new[] { ".....O......", "..O..O..O...", "...OOOOO....", "..OOEEEOO...", "OOOEEWEEOOO.", "..OOEEEOO...",
                           "...OOOOO....", "..O..O..O...", ".....O......", "............", "............", "............" } },
        { "Tether", new[] { "............", "....TTTT....", "...T....T...", "..T..TT..T..", "..T.T..T.T..", "..T.T.TT.T..",
                            "..T..T...T..", "...T....T...", "TTTTTTTT....", "............", "............", "............" } },
        { "Lock", new[] { "....OOOO....", "...O....O...", "...O....O...", "..OOOOOOOO..", "..OYYYYYYO..", "..OYYOOYYO..",
                          "..OYYOOYYO..", "..OYYYYYYO..", "..OOOOOOOO..", "............", "............", "............" } },
    };

    private static Color32[,] Stamp(string[] rows)
    {
        Dictionary<char, Color32> key = new Dictionary<char, Color32>
        {
            { 'O', Ink }, { 'P', PaperL }, { 'L', PaperD }, { 'G', Green }, { 'Y', GoldL }, { 'W', White }, { 'I', Metal },
            { 'C', CyanL }, { 'B', Wood }, { 'R', Red }, { 'E', Orange }, { 'T', Cyan },
            { 'M', PaperM }, { 'K', Hex("26391f") }, // K = dark cover leather (seen through the folded corner)
        };
        int w = rows[0].Length, h = rows.Length;
        Color32[,] p = Fill(w, h, Clear);
        for (int row = 0; row < h; row++)
            for (int x = 0; x < w; x++)
                if (key.TryGetValue(rows[row][x], out Color32 c)) p[x, h - 1 - row] = c;
        return p;
    }

    // ------------------------------------------------------------------ 3. preview

    [MenuItem("Tools/Shards of Motion/UI Kit/3. Build Preview")]
    public static void BuildPreview()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Camera.main.backgroundColor = new Color(0.16f, 0.22f, 0.2f);
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

        Canvas canvas = new GameObject("Canvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        RectTransform root = (RectTransform)canvas.transform;

        // Notebook, back to front: drop shadow, leather cover, page shadows, pages (tiled paper),
        // spine shadows on each page's inner edge, then the ring binding over the gap.
        Panel(root, "Cover_Shadow", "UI_Shadow", new Vector2(18, -24), new Vector2(1800, 780));
        Panel(root, "Cover", "UI_Cover", Vector2.zero, new Vector2(1800, 780));
        Panel(root, "PageShadow_Left", "UI_Shadow", new Vector2(-428, -12), new Vector2(840, 680));
        Panel(root, "PageShadow_Right", "UI_Shadow", new Vector2(452, -12), new Vector2(840, 680));
        RectTransform left = Panel(root, "Page_Left", "UI_Page", new Vector2(-440, 0), new Vector2(840, 680));
        RectTransform right = Panel(root, "Page_Right", "UI_Page", new Vector2(440, 0), new Vector2(840, 680));
        left.GetComponent<Image>().type = Image.Type.Tiled;  // tile the paper, don't stretch it
        right.GetComponent<Image>().type = Image.Type.Tiled;
        Panel(left, "Gutter", "UI_Gutter", new Vector2(372, 0), new Vector2(96, 668));
        RectTransform rightGutter = Panel(right, "Gutter", "UI_Gutter", new Vector2(-372, 0), new Vector2(96, 668));
        rightGutter.localScale = new Vector3(-1f, 1f, 1f);   // darker toward the spine on both pages
        for (int i = 0; i < 8; i++) // eight separate rings down the spine
            Panel(root, "Ring", "UI_Rings", new Vector2(0f, 262f - i * 75f), new Vector2(48, 30)).GetComponent<Image>().type = Image.Type.Simple;

        // Title ribbon across the top (with its own shadow).
        Panel(root, "Ribbon_Shadow", "UI_Shadow", new Vector2(10, 318), new Vector2(760, 64));
        RectTransform ribbon = Panel(root, "Ribbon", "UI_Ribbon", new Vector2(0, 330), new Vector2(760, 72));
        Label(ribbon, "1 · The First Exchange", font, 46, White, Vector2.zero, new Vector2(700, 60), TextAlignmentOptions.Center);

        // Left page: diagram area placeholder + caption.
        Label(left, "From the field journal of Wren Alder", font, 30, InkSoft, new Vector2(0, 250), new Vector2(720, 40), TextAlignmentOptions.Center);
        Panel(left, "Divider", "UI_Divider", new Vector2(0, 220), new Vector2(640, 9)).GetComponent<Image>().type = Image.Type.Tiled;
        RectTransform card = Panel(left, "Diagram_Card", "UI_Card", new Vector2(0, -20), new Vector2(620, 400));
        card.GetComponent<Image>().type = Image.Type.Tiled;
        Panel(card, "Tape", "UI_Tape", new Vector2(0, 200), new Vector2(120, 48));
        Label(card, "[ force diagram ]\nPush 12 N  →  ■  ←  Friction 19.6 N", font, 34, Ink, Vector2.zero, new Vector2(560, 200), TextAlignmentOptions.Center);
        Icon(left, "UI_Icon_Burst", new Vector2(-300, -280));
        Icon(left, "UI_Icon_Tether", new Vector2(-240, -280));
        Label(left, "P1 shoves. P2 pulls.", font, 30, InkSoft, new Vector2(40, -280), new Vector2(460, 40), TextAlignmentOptions.Left);

        // Right page: Big Idea card, notes, Look closer, fun fact, page indicator.
        RectTransform idea = Panel(right, "BigIdea_Card", "UI_Card", new Vector2(0, 190), new Vector2(700, 170));
        Icon(idea, "UI_Icon_Bulb", new Vector2(-300, 40));
        Label(idea, "BIG IDEA", font, 30, Green, new Vector2(-120, 50), new Vector2(300, 40), TextAlignmentOptions.Left);
        Label(idea, "Things stay still until a net force moves them.", font, 38, Ink, new Vector2(20, -20), new Vector2(600, 90), TextAlignmentOptions.Left);
        Label(right, "The stone wouldn't budge, no matter how hard I pushed — 12 newtons is no match for 19.6 newtons of friction. " +
                     "Then my partner pushed too.", font, 32, Ink, new Vector2(0, 10), new Vector2(700, 150), TextAlignmentOptions.TopLeft);
        RectTransform closer = Panel(right, "LookCloser_Card", "UI_Card", new Vector2(0, -160), new Vector2(700, 140));
        Icon(closer, "UI_Icon_Magnifier", new Vector2(-300, 30));
        Label(closer, "LOOK CLOSER   f = μ·m·g = 19.6 N   Δv = J ÷ m = 5 m/s", font, 30, Ink, new Vector2(30, 0), new Vector2(580, 110), TextAlignmentOptions.Left);
        Panel(right, "Seal", "UI_Seal", new Vector2(320, -270), new Vector2(72, 72));
        Label(right, "1 / 2   >", font, 30, InkSoft, new Vector2(200, -280), new Vector2(200, 40), TextAlignmentOptions.Right);

        // Side tabs and buttons.
        string[] tabs = { "Green", "Gold", "Cyan", "Orange", "Red", "Locked" };
        for (int i = 0; i < tabs.Length; i++) Panel(root, "Tab_" + tabs[i], "UI_Tab_" + tabs[i], new Vector2(890, 240 - i * 96), new Vector2(48, 84));
        RectTransform button = Panel(root, "Button", "UI_Button", new Vector2(-600, -440), new Vector2(288, 72));
        Label(button, "Resume", font, 36, White, Vector2.zero, new Vector2(260, 60), TextAlignmentOptions.Center);
        RectTransform pressed = Panel(root, "Button_Pressed", "UI_Button_Pressed", new Vector2(-280, -440), new Vector2(288, 72));
        Label(pressed, "Restart Room", font, 36, GoldL, Vector2.zero, new Vector2(260, 60), TextAlignmentOptions.Center);
        string[] icons = { "Book", "Bulb", "Magnifier", "Scale", "Force", "Burst", "Tether", "Lock" };
        for (int i = 0; i < icons.Length; i++) Icon(root, "UI_Icon_" + icons[i], new Vector2(60 + i * 60, -440));

        Directory.CreateDirectory(Path.GetDirectoryName(PreviewScene));
        EditorSceneManager.SaveScene(scene, PreviewScene);
        Debug.Log("UI Kit: preview saved to " + PreviewScene + " (look at it in the Game view at 1920x1080).");
    }

    // ------------------------------------------------------------------ helpers

    private static RectTransform Panel(RectTransform parent, string name, string sprite, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{KitFolder}/{sprite}.png");
        img.type = img.sprite != null && img.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        if (sprite == "UI_Page" || sprite == "UI_Card" || sprite == "UI_Cover") img.type = Image.Type.Tiled; // textures tile, never stretch
        img.raycastTarget = false;
        return rt;
    }

    private static void Icon(RectTransform parent, string sprite, Vector2 pos) => Panel(parent, sprite, sprite, pos, new Vector2(36, 36));

    private static void Label(RectTransform parent, string text, TMP_FontAsset font, float size, Color32 color, Vector2 pos, Vector2 box, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
    }

    private static void Save(string name, Color32[,] pixels, Vector4 border)
    {
        int w = pixels.GetLength(0), h = pixels.GetLength(1);
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) tex.SetPixel(x, y, pixels[x, y]);
        string path = $"{KitFolder}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ArtPixelsPerUnit;
        importer.spriteBorder = border;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        TextureImporterSettings s = new TextureImporterSettings();
        importer.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect; // needed for sliced/tiled UI images
        importer.SetTextureSettings(s);
        importer.SaveAndReimport();
    }

    private static Color32[,] Fill(int w, int h, Color32 c)
    {
        Color32[,] p = new Color32[w, h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) p[x, y] = c;
        return p;
    }

    private static void Outline(Color32[,] p, Color32 c)
    {
        int w = p.GetLength(0), h = p.GetLength(1);
        for (int x = 0; x < w; x++) { if (p[x, 0].a > 0) p[x, 0] = c; if (p[x, h - 1].a > 0) p[x, h - 1] = c; }
        for (int y = 0; y < h; y++) { if (p[0, y].a > 0) p[0, y] = c; if (p[w - 1, y].a > 0) p[w - 1, y] = c; }
    }

    private static void RoundCorners(Color32[,] p)
    {
        int w = p.GetLength(0), h = p.GetLength(1);
        foreach ((int x, int y) in new[] { (0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1) }) p[x, y] = Clear;
        Color32 o = p[1, 0];
        foreach ((int x, int y) in new[] { (1, 1), (w - 2, 1), (1, h - 2), (w - 2, h - 2) }) p[x, y] = o;
    }

    private static Color32 Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
