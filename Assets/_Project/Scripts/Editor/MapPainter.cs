using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws the map art in the same ink style as Wren's room sketches (RoomSketchTool): a transparent pixel drawing that
/// sits on the parchment page. Only plain drawing here (no assets, no editor calls); MapTool saves the results.
///   Map:   a double ink frame, a compass, Alderbrook's houses, the dashed path through the room markers, scattered trees
///          and rocks, the great tree of the finale room, and fogged locked regions (cliffs, gears) with a faint trail to them.
///   Icons: the room marker (ring), a shard and a machine part, for the map's UI.
/// Text (names, counts) is not drawn here: the map screen shows it with the UI font.
/// </summary>
public static class MapPainter
{
    public static readonly Color32 Ink = new Color32(74, 47, 28, 255);
    private static readonly Color32 InkMid = new Color32(74, 47, 28, 150), InkSoft = new Color32(74, 47, 28, 85), InkFaint = new Color32(74, 47, 28, 45);
    private static readonly Color32 Leaf = new Color32(92, 112, 52, 120), Roof = new Color32(150, 70, 50, 170), Paper = new Color32(255, 251, 240, 255);
    private static readonly Color32 Crystal = new Color32(90, 190, 210, 255), CrystalLight = new Color32(200, 245, 250, 255);
    private static readonly Color32 Brass = new Color32(196, 150, 60, 255);

    private static Color32[] px;
    private static int W, H;

    public struct Label { public Vector2 at; public RegionMapData.LabelSide side; }

    // ------------------------------------------------------------------ the map

    public static Color32[] DrawMap(int width, int height, Vector2 village, IList<Vector2> rooms, IList<Label> labels, IList<Rect> fog, int seed = 7)
    {
        Begin(width, height);
        System.Random rng = new System.Random(seed);

        // Path: a smooth curve from the village through every room, then a faint trail off into the first locked region.
        List<Vector2> points = new List<Vector2> { village };
        points.AddRange(rooms);
        List<Vector2> path = Spline(points, 6);
        List<Vector2> trail = new List<Vector2>();
        if (fog.Count > 0 && rooms.Count > 0)
        {
            Vector2 last = rooms[rooms.Count - 1];
            Vector2 into = new Vector2(fog[0].xMin + 14f, fog[0].center.y);
            trail = Spline(new List<Vector2> { last, Vector2.Lerp(last, into, 0.5f) + new Vector2(0f, 4f), into }, 10);
            for (int i = 1; i < fog.Count; i++)
                trail.AddRange(Spline(new List<Vector2> { new Vector2(fog[i - 1].center.x, fog[i - 1].yMin + 10f),
                    new Vector2(fog[i].center.x + 10f, (fog[i - 1].yMin + fog[i].yMax) * 0.5f), new Vector2(fog[i].center.x, fog[i].yMax - 10f) }, 10));
        }

        // Places scenery must keep clear of.
        List<Rect> keepClear = new List<Rect>
        {
            new Rect(village.x - 22f, village.y - 26f, 44f, 42f),   // houses + the village label under them
            new Rect(0f, height - 34f, 40f, 34f),                     // compass
        };
        foreach (Label l in labels) keepClear.Add(LabelRect(l));
        foreach (Rect f in fog) keepClear.Add(new Rect(f.x - 8f, f.y - 8f, f.width + 16f, f.height + 16f));
        Vector2 bigTree = rooms.Count > 0 ? rooms[rooms.Count - 1] + new Vector2(0f, 20f) : Vector2.zero; // stands over the last room
        if (rooms.Count > 0) keepClear.Add(new Rect(bigTree.x - 18f, bigTree.y - 12f, 36f, 32f));

        bool Clear(Vector2 p, float pathGap)
        {
            if (p.x < 10f || p.y < 8f || p.x > width - 10f || p.y > height - 12f) return false;
            foreach (Rect r in keepClear) if (r.Contains(p)) return false;
            foreach (Vector2 q in path) if ((q - p).sqrMagnitude < pathGap * pathGap) return false;
            foreach (Vector2 q in trail) if ((q - p).sqrMagnitude < 7f * 7f) return false;
            foreach (Vector2 r in rooms) if ((r - p).sqrMagnitude < 15f * 15f) return false;
            return true;
        }

        // Locked regions first (under the fog): what's waiting there, drawn faintly.
        for (int i = 0; i < fog.Count; i++)
        {
            if (i % 2 == 0) Cliffs(fog[i], rng); else Gears(fog[i], rng);
            Fog(fog[i], rng);
        }

        // Scenery: trees in loose clumps, a few rocks.
        List<Vector2> placed = new List<Vector2>();
        for (int tries = 0; tries < 4000 && placed.Count < 70; tries++)
        {
            Vector2 p = new Vector2(rng.Next(0, width), rng.Next(0, height));
            if (!Clear(p, 10f)) continue;
            bool crowded = false;
            foreach (Vector2 q in placed) if ((q - p).sqrMagnitude < 10f * 10f) { crowded = true; break; }
            if (crowded) continue;
            placed.Add(p);
            int kind = rng.Next(0, 10);
            if (kind < 5) Pine(Vector2Int.RoundToInt(p), rng.Next(0, 2) == 0 ? 3 : 4);
            else if (kind < 9) RoundTree(Vector2Int.RoundToInt(p), rng.Next(2, 4));
            else Rock(Vector2Int.RoundToInt(p));
        }

        // The path on top of the scenery: ink dashes, then the faint trail.
        Dashed(path, Ink, 4, 3);
        Dashed(trail, InkSoft, 2, 4);

        if (rooms.Count > 0) GreatTree(Vector2Int.RoundToInt(bigTree));
        Village(Vector2Int.RoundToInt(village));
        Compass(new Vector2Int(20, height - 20));
        Frame();
        return px;
    }

    /// <summary>Area a room's name label takes on the map (map pixels), so scenery stays out of it.</summary>
    public static Rect LabelRect(Label l)
    {
        Vector2 p = l.at;
        switch (l.side)
        {
            case RegionMapData.LabelSide.Above: return new Rect(p.x - 30f, p.y + 6f, 60f, 26f);
            case RegionMapData.LabelSide.Left: return new Rect(p.x - 72f, p.y - 14f, 64f, 28f);
            case RegionMapData.LabelSide.Right: return new Rect(p.x + 8f, p.y - 14f, 64f, 28f);
            default: return new Rect(p.x - 30f, p.y - 32f, 60f, 26f);
        }
    }

    // ------------------------------------------------------------------ icons (shown at 3x by the map screen)

    /// <summary>Room marker, 13 x 13: an ink ring with a paper centre (the map tints it faint for unreached rooms).</summary>
    public static Color32[] DrawMarker(out int w, out int h)
    {
        Begin(w = 13, h = 13);
        Vector2Int c = new Vector2Int(6, 6);
        Disc(c, 6, Ink);
        Disc(c, 4, Paper);
        Disc(c, 2, Ink);
        return px;
    }

    /// <summary>Motion Shard, 7 x 9: a cyan crystal with an ink outline.</summary>
    public static Color32[] DrawShard(out int w, out int h)
    {
        Begin(w = 7, h = 9);
        for (int y = 0; y < 9; y++)
        {
            int half = y <= 4 ? y * 3 / 4 : (8 - y) * 3 / 4; // diamond, a little taller than wide
            for (int x = 3 - half; x <= 3 + half; x++) Set(x, y, Crystal);
            Set(3 - half, y, Ink); Set(3 + half, y, Ink);
        }
        Set(3, 0, Ink); Set(3, 8, Ink);
        Set(2, 5, CrystalLight); Set(2, 4, CrystalLight); Set(3, 6, CrystalLight);
        return px;
    }

    /// <summary>Machine part, 9 x 9: a brass cog.</summary>
    public static Color32[] DrawPart(out int w, out int h)
    {
        Begin(w = 9, h = 9);
        Vector2Int c = new Vector2Int(4, 4);
        foreach (Vector2Int t in new[] { new Vector2Int(0, 4), new Vector2Int(4, 0), new Vector2Int(-4, 0), new Vector2Int(0, -4) })
        { Set(c.x + t.x, c.y + t.y, Ink); Set(c.x + t.x * 3 / 4, c.y + t.y * 3 / 4, Ink); }
        foreach (Vector2Int t in new[] { new Vector2Int(3, 3), new Vector2Int(-3, 3), new Vector2Int(3, -3), new Vector2Int(-3, -3) })
            Set(c.x + t.x, c.y + t.y, Ink);
        Disc(c, 3, Ink);
        Disc(c, 2, Brass);
        Set(c.x, c.y, Ink);
        Set(c.x - 1, c.y + 1, new Color32(240, 210, 130, 255));
        return px;
    }

    // ------------------------------------------------------------------ pieces of the map

    private static void Frame()
    {
        for (int x = 1; x < W - 1; x++) { Set(x, 1, Ink); Set(x, H - 2, Ink); Set(x, 4, InkSoft); Set(x, H - 5, InkSoft); }
        for (int y = 1; y < H - 1; y++) { Set(1, y, Ink); Set(W - 2, y, Ink); Set(4, y, InkSoft); Set(W - 5, y, InkSoft); }
        foreach (Vector2Int c in new[] { new Vector2Int(4, 4), new Vector2Int(W - 5, 4), new Vector2Int(4, H - 5), new Vector2Int(W - 5, H - 5) })
        { Disc(c, 2, Ink); Set(c.x, c.y, Paper); }
    }

    private static void Compass(Vector2Int c)
    {
        Line(new Vector2Int(c.x, c.y - 9), new Vector2Int(c.x, c.y + 9), InkMid);
        Line(new Vector2Int(c.x - 7, c.y), new Vector2Int(c.x + 7, c.y), InkSoft);
        for (int i = 0; i <= 3; i++) { Line(new Vector2Int(c.x - i, c.y + 9 - i * 2), new Vector2Int(c.x + i, c.y + 9 - i * 2), Ink); }
        Disc(c, 1, Ink);
        // "N" above the needle (3 x 5)
        string[] n = { "#.#", "###", "###", "#.#", "#.#" };
        for (int row = 0; row < 5; row++) for (int col = 0; col < 3; col++) if (n[row][col] == '#') Set(c.x - 1 + col, c.y + 16 - row, Ink);
    }

    private static void Village(Vector2Int c)
    {
        House(new Vector2Int(c.x - 11, c.y - 2), 9, 5);
        House(new Vector2Int(c.x + 1, c.y + 3), 11, 6);
        House(new Vector2Int(c.x + 12, c.y - 3), 8, 5);
        // the lab's little observatory dome, next to the middle house
        Vector2Int dome = new Vector2Int(c.x - 3, c.y + 11);
        for (int y = 0; y < 5; y++) { Set(dome.x - 2, dome.y - y, Ink); Set(dome.x + 2, dome.y - y, Ink); }
        for (int x = -2; x <= 2; x++) Set(dome.x + x, dome.y + (x == 0 ? 2 : Mathf.Abs(x) == 1 ? 2 : 1), Ink);
        Set(dome.x + 2, dome.y + 3, Ink); Set(dome.x + 3, dome.y + 4, Ink); // telescope
        // a short stretch of road leaving the village
        for (int x = -18; x <= 18; x += 3) Set(c.x + x, c.y - 9, InkSoft);
    }

    private static void House(Vector2Int baseLeft, int w, int wallH)
    {
        int x0 = baseLeft.x, y0 = baseLeft.y;
        for (int x = 0; x < w; x++) { Set(x0 + x, y0, Ink); }
        for (int y = 0; y <= wallH; y++) { Set(x0, y0 + y, Ink); Set(x0 + w - 1, y0 + y, Ink); }
        int peak = w / 2 + 1;
        for (int i = 0; i <= peak; i++)
        {
            int y = y0 + wallH + peak - i;
            for (int x = x0 - 1 + i; x <= x0 + w - i; x++) if (x == x0 - 1 + i || x == x0 + w - i) Set(x, y, Ink); else Set(x, y, Roof);
        }
        Set(x0 + w / 2, y0 + 1, Ink); Set(x0 + w / 2, y0 + 2, Ink); // door
        Set(x0 + 2, y0 + wallH - 1, InkMid);                         // window
    }

    private static void Pine(Vector2Int c, int size)
    {
        for (int i = 0; i <= size * 2; i++)
        {
            int half = i / 2;
            int y = c.y + size * 2 - i;
            Set(c.x - half, y, Ink); Set(c.x + half, y, Ink);
            for (int x = c.x - half + 1; x < c.x + half; x++) if ((x + y) % 2 == 0) Set(x, y, Leaf);
        }
        Set(c.x, c.y - 1, Ink);
    }

    // A leafy tree: ink crown outline, the lower half of the crown shaded, a short trunk.
    private static void RoundTree(Vector2Int c, int r)
    {
        Vector2Int crown = new Vector2Int(c.x, c.y + r + 1);
        for (int y = -r; y <= r; y++) for (int x = -r; x <= r; x++)
        {
            int d = x * x + y * y;
            if (d > r * r + r) continue;
            bool edge = d > (r - 1) * (r - 1) + (r - 1);
            if (edge) Set(crown.x + x, crown.y + y, Ink);
            else if (y < 0) Set(crown.x + x, crown.y + y, Leaf);
        }
        Set(c.x, c.y, Ink); Set(c.x, c.y + 1, Ink);
    }

    private static void Rock(Vector2Int c)
    {
        for (int x = -3; x <= 3; x++) Set(c.x + x, c.y, Ink);
        Set(c.x - 3, c.y + 1, Ink); Set(c.x - 2, c.y + 2, Ink); Set(c.x - 1, c.y + 3, Ink); Set(c.x, c.y + 3, Ink);
        Set(c.x + 1, c.y + 3, Ink); Set(c.x + 2, c.y + 2, Ink); Set(c.x + 3, c.y + 1, Ink);
        Set(c.x + 1, c.y + 1, InkSoft); Set(c.x + 2, c.y + 1, InkSoft);
    }

    // The finale's great tree ("Heart of the Forest"), its roots reaching down to the last room's marker.
    private static void GreatTree(Vector2Int c)
    {
        int r = 10;
        Vector2Int crown = new Vector2Int(c.x, c.y + 6);
        foreach (Vector2Int o in new[] { new Vector2Int(-6, -1), new Vector2Int(6, -1), new Vector2Int(0, 4), new Vector2Int(0, -3) })
            for (int y = -r + 3; y <= r - 3; y++) for (int x = -r + 3; x <= r - 3; x++)
                if (x * x + y * y <= (r - 3) * (r - 3)) Set(crown.x + o.x + x, crown.y + o.y + y, (x * 3 + y) % 4 == 0 ? Leaf : new Color32(0, 0, 0, 0));
        // outline: pixels of the crown next to empty ones
        bool In(int x, int y)
        {
            foreach (Vector2Int o in new[] { new Vector2Int(-6, -1), new Vector2Int(6, -1), new Vector2Int(0, 4), new Vector2Int(0, -3) })
            { int dx = x - (crown.x + o.x), dy = y - (crown.y + o.y); if (dx * dx + dy * dy <= (r - 3) * (r - 3)) return true; }
            return false;
        }
        for (int y = crown.y - r - 2; y <= crown.y + r + 2; y++)
            for (int x = crown.x - r - 6; x <= crown.x + r + 6; x++)
                if (In(x, y) && (!In(x + 1, y) || !In(x - 1, y) || !In(x, y + 1) || !In(x, y - 1))) Set(x, y, Ink);
        // trunk and roots
        for (int y = c.y - 10; y < c.y - 2; y++) { Set(c.x - 2, y, Ink); Set(c.x + 2, y, Ink); }
        Line(new Vector2Int(c.x - 2, c.y - 10), new Vector2Int(c.x - 6, c.y - 12), Ink);
        Line(new Vector2Int(c.x + 2, c.y - 10), new Vector2Int(c.x + 6, c.y - 12), Ink);
        Set(c.x, c.y - 6, InkSoft);
    }

    // Locked region contents, drawn faint before the fog goes over them.
    private static void Cliffs(Rect r, System.Random rng)
    {
        int baseY = Mathf.RoundToInt(r.yMin + r.height * 0.3f);
        int x = Mathf.RoundToInt(r.xMin + 8f);
        while (x < r.xMax - 16f)
        {
            int w = rng.Next(14, 24), h = rng.Next(16, Mathf.Max(18, Mathf.RoundToInt(r.height * 0.55f)));
            Line(new Vector2Int(x, baseY), new Vector2Int(x + w / 2, baseY + h), InkMid);
            Line(new Vector2Int(x + w / 2, baseY + h), new Vector2Int(x + w, baseY), InkMid);
            Line(new Vector2Int(x + w / 2, baseY + h), new Vector2Int(x + w / 2 + 2, baseY + h / 2), InkSoft);
            x += w - 4;
        }
        // the divide: a gap with a broken bridge
        int gx = Mathf.RoundToInt(r.center.x);
        for (int y = Mathf.RoundToInt(r.yMin + 6f); y < baseY; y++) { Set(gx - 6, y, InkMid); Set(gx + 6, y, InkMid); }
        Line(new Vector2Int(gx - 14, baseY), new Vector2Int(gx - 6, baseY), InkMid);
        Line(new Vector2Int(gx + 6, baseY), new Vector2Int(gx + 14, baseY), InkMid);
        Line(new Vector2Int(gx - 6, baseY), new Vector2Int(gx - 2, baseY - 3), InkMid);
    }

    private static void Gears(Rect r, System.Random rng)
    {
        Vector2Int[] centres = { new Vector2Int(Mathf.RoundToInt(r.xMin + r.width * 0.3f), Mathf.RoundToInt(r.center.y + 4f)),
                                 new Vector2Int(Mathf.RoundToInt(r.xMin + r.width * 0.6f), Mathf.RoundToInt(r.center.y - 10f)),
                                 new Vector2Int(Mathf.RoundToInt(r.xMin + r.width * 0.8f), Mathf.RoundToInt(r.center.y + 14f)) };
        int[] radii = { 14, 10, 7 };
        for (int g = 0; g < centres.Length; g++)
        {
            int rad = radii[g];
            Ring(centres[g], rad, InkMid);
            Ring(centres[g], rad / 3, InkMid);
            int teeth = rad;
            for (int t = 0; t < teeth; t++)
            {
                float a = t * Mathf.PI * 2f / teeth;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Line(Vector2Int.RoundToInt(centres[g] + d * rad), Vector2Int.RoundToInt(centres[g] + d * (rad + 3)), InkMid);
            }
        }
    }

    // Fog: soft cloud blobs over the area, stippled, with a scalloped edge.
    private static void Fog(Rect r, System.Random rng)
    {
        List<(Vector2 c, float rad)> blobs = new List<(Vector2, float)>();
        for (float y = r.yMin + 12f; y <= r.yMax - 10f; y += 16f)
            for (float x = r.xMin + 12f; x <= r.xMax - 10f; x += 16f)
                blobs.Add((new Vector2(x + rng.Next(-3, 4), y + rng.Next(-3, 4)), 13f + rng.Next(0, 5)));
        bool InFog(int x, int y)
        {
            if (x < r.xMin - 4f || x > r.xMax + 4f || y < r.yMin - 4f || y > r.yMax + 4f) return false;
            foreach ((Vector2 c, float rad) in blobs) if ((new Vector2(x, y) - c).sqrMagnitude <= rad * rad) return true;
            return false;
        }
        int x0 = Mathf.FloorToInt(r.xMin) - 6, x1 = Mathf.CeilToInt(r.xMax) + 6, y0 = Mathf.FloorToInt(r.yMin) - 6, y1 = Mathf.CeilToInt(r.yMax) + 6;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                if (!InFog(x, y)) continue;
                bool edge = !InFog(x + 1, y) || !InFog(x - 1, y) || !InFog(x, y + 1) || !InFog(x, y - 1);
                if (edge) Set(x, y, InkMid);
                else
                {
                    Set(x, y, new Color32(255, 251, 240, 150));             // paper mist over what's beneath
                    if ((x * 3 + y * 5) % 7 == 0) Set(x, y, InkFaint);       // stipple
                }
            }
    }

    // ------------------------------------------------------------------ primitives

    private static void Begin(int width, int height)
    {
        W = width; H = height;
        px = new Color32[W * H];
    }

    private static List<Vector2> Spline(List<Vector2> pts, int stepsPerPixel)
    {
        List<Vector2> result = new List<Vector2>();
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector2 p0 = pts[Mathf.Max(0, i - 1)], p1 = pts[i], p2 = pts[i + 1], p3 = pts[Mathf.Min(pts.Count - 1, i + 2)];
            int steps = Mathf.Max(8, Mathf.RoundToInt((p2 - p1).magnitude * stepsPerPixel / 4f));
            for (int s = 0; s < steps; s++)
            {
                float t = s / (float)steps, t2 = t * t, t3 = t2 * t;
                result.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
            }
        }
        if (pts.Count > 0) result.Add(pts[pts.Count - 1]);
        return result;
    }

    // Walks the curve and inks 'on' pixels, skips 'off' pixels (measured along the curve).
    private static void Dashed(List<Vector2> curve, Color32 c, int on, int off)
    {
        float travelled = 0f;
        Vector2Int lastPx = new Vector2Int(int.MinValue, 0);
        for (int i = 0; i < curve.Count; i++)
        {
            if (i > 0) travelled += (curve[i] - curve[i - 1]).magnitude;
            Vector2Int p = Vector2Int.RoundToInt(curve[i]);
            if (p == lastPx) continue;
            lastPx = p;
            if ((int)travelled % (on + off) < on) Set(p.x, p.y, c);
        }
    }

    private static void Set(int x, int y, Color32 c)
    {
        if (x < 0 || y < 0 || x >= W || y >= H) return;
        int i = y * W + x;
        if (c.a == 0) return; // fully transparent: leave what's there
        Color32 o = px[i];
        float a = c.a / 255f, oa = o.a / 255f, na = a + oa * (1f - a);
        if (na <= 0f) return;
        px[i] = new Color32((byte)((c.r * a + o.r * oa * (1f - a)) / na), (byte)((c.g * a + o.g * oa * (1f - a)) / na),
                            (byte)((c.b * a + o.b * oa * (1f - a)) / na), (byte)(na * 255f));
    }

    private static void Line(Vector2Int a, Vector2Int b, Color32 c)
    {
        int dx = Mathf.Abs(b.x - a.x), dy = -Mathf.Abs(b.y - a.y), sx = a.x < b.x ? 1 : -1, sy = a.y < b.y ? 1 : -1, err = dx + dy;
        int x = a.x, y = a.y;
        while (true)
        {
            Set(x, y, c);
            if (x == b.x && y == b.y) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x += sx; }
            if (e2 <= dx) { err += dx; y += sy; }
        }
    }

    private static void Disc(Vector2Int c, int r, Color32 col)
    {
        for (int y = -r; y <= r; y++) for (int x = -r; x <= r; x++) if (x * x + y * y <= r * r + r) Set(c.x + x, c.y + y, col);
    }

    private static void Ring(Vector2Int c, int r, Color32 col)
    {
        for (int y = -r; y <= r; y++) for (int x = -r; x <= r; x++)
        {
            int d = x * x + y * y;
            if (d <= r * r + r && d > (r - 1) * (r - 1) + (r - 1)) Set(c.x + x, c.y + y, col);
        }
    }
}
