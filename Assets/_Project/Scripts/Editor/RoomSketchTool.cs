using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Shards of Motion > Journal > Draw Room Sketches.
/// Draws "Wren's sketch" of each built room from its prefab: an ink schematic on transparent background (shown on the
/// journal's paper card), 300 x 170 px (15 px per metre), shown at 2x. It shows HOW THINGS CONNECT, not the solution:
///   stone (hatched), acid (green), plates with the weight they need, crates with their mass ("?" where the room hides
///   it), movers with an orange arrow along their path, levers, cyan arrows from each plate/lever to what it powers
///   (tagged ALL / ANY / FLIP for how a relay combines them), and the exit (at its real height, read from the open
///   Region 1 scene's doorways). Shards and machine parts are left out on purpose.
/// The sketch and a caption are stored on the room's JournalEntry (sketch, sketchCaption); JournalStone adds the page.
/// Re-run after changing a room.
/// </summary>
public static class RoomSketchTool
{
    private const string Folder = "Assets/_Project/Art/Journal";
    private const string JournalFolder = "Assets/_Project/Data/Journal";
    private const int W = 300, H = 170;
    private const float Ppu = 15f, HalfW = 10f, HalfH = 5.625f;

    private static readonly (int room, string prefab, string entry)[] Rooms =
    {
        (1, "Assets/_Project/Prefabs/Rooms/Region1/R1_Room01.prefab", "R1_01_TheFirstExchange"),
        (2, "Assets/_Project/Prefabs/Rooms/Region1/R1_Room02.prefab", "R1_02_WeightOfThings"),
        (3, "Assets/_Project/Prefabs/Rooms/Region1/R1_Room03.prefab", "R1_03_TogetherWeMove"),
    };

    private static readonly Color32 Ink = new Color32(74, 47, 28, 255), InkSoft = new Color32(74, 47, 28, 110), Hatch = new Color32(74, 47, 28, 45);
    private static readonly Color32 Acid = new Color32(94, 160, 60, 170), AcidTop = new Color32(62, 120, 40, 255);
    private static readonly Color32 Gold = new Color32(170, 120, 30, 255), CrateFill = new Color32(176, 130, 80, 120);
    private static readonly Color32 Link = new Color32(30, 125, 150, 255), Move = new Color32(200, 104, 30, 255), Exit = new Color32(40, 140, 60, 255);

    private static Color32[] px;

    [MenuItem("Tools/Shards of Motion/Journal/Draw Room Sketches")]
    public static void DrawAll()
    {
        Directory.CreateDirectory(Folder);
        foreach ((int room, string prefab, string entryName) in Rooms)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(prefab);
            JournalEntry entry = AssetDatabase.LoadAssetAtPath<JournalEntry>($"{JournalFolder}/{entryName}.asset");
            if (root == null || entry == null) { Debug.LogWarning($"Room sketches: Room {room} prefab or entry {entryName} not found; skipped."); continue; }
            HashSet<string> modes = new HashSet<string>();
            Sprite sprite = Save($"Sketch_R1_{room:00}", Draw(root.transform, room, modes));
            entry.sketch = sprite;
            entry.sketchCaption = Caption(modes);
            EditorUtility.SetDirty(entry);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Room sketches drawn into " + Folder + " and added to the journal entries.");
    }

    private static string Caption(HashSet<string> modes)
    {
        List<string> parts = new List<string>();
        if (modes.Contains("ALL")) parts.Add("ALL: every arrow at once");
        if (modes.Contains("ANY")) parts.Add("ANY: one is enough");
        if (modes.Contains("FLIP")) parts.Add("FLIP: each one reverses it");
        return parts.Count > 0 ? string.Join(". ", parts) + "." : "Arrows: what powers what.";
    }

    // ------------------------------------------------------------------ drawing the room

    private static Color32[] Draw(Transform root, int roomIndex, HashSet<string> modes)
    {
        px = new Color32[W * H];
        Vector2 L(Vector3 world) => root.InverseTransformPoint(world);

        Rect LocalRect(BoxCollider2D box)
        {
            Transform t = box.transform;
            Vector2 a = L(t.TransformPoint(box.offset - box.size * 0.5f)), b = L(t.TransformPoint(box.offset + box.size * 0.5f));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        bool IsSpecial(GameObject go) => go.GetComponentInParent<Rigidbody2D>() != null || go.GetComponent<PressurePlate>() != null ||
            go.GetComponent<HazardZone>() != null || go.GetComponent<ResetShrine>() != null || go.GetComponent<JournalStone>() != null ||
            go.GetComponent<Lever>() != null || go.GetComponent<QuestItem>() != null || go.GetComponent<MotionShard>() != null;

        // Stone and floors.
        foreach (BoxCollider2D box in root.GetComponentsInChildren<BoxCollider2D>(true))
            if (!box.isTrigger && !IsSpecial(box.gameObject) && !PrefabUtility.IsPartOfPrefabInstance(box.gameObject))
                Stone(LocalRect(box));

        // Doorways (scene objects): raised landings and the exit marker.
        DoorwayInfo(roomIndex - 1, roomIndex, out bool leftExists, out float leftBottom, out _);
        DoorwayInfo(roomIndex, roomIndex + 1, out bool rightExists, out float rightBottom, out float rightHeight);
        if (leftExists && leftBottom > 0.01f) Stone(Rect.MinMaxRect(-HalfW, -5.5f, -HalfW + 3.5f, -4.5f + leftBottom));
        if (rightExists && rightBottom > 0.01f) Stone(Rect.MinMaxRect(HalfW - 3.5f, -5.5f, HalfW, -4.5f + rightBottom));
        if (rightExists) ExitMarker(-4.5f + rightBottom + Mathf.Min(rightHeight, 4f) * 0.5f);

        // Acid.
        foreach (HazardZone hz in root.GetComponentsInChildren<HazardZone>(true))
            if (hz.TryGetComponent(out BoxCollider2D box) && !PrefabUtility.IsPartOfPrefabInstance(hz.gameObject)) AcidArea(LocalRect(box));

        // Movers: outline, ghost at the far end, path arrow.
        foreach (Mover m in root.GetComponentsInChildren<Mover>(true))
        {
            if (!m.TryGetComponent(out BoxCollider2D box)) continue;
            Rect r = LocalRect(box);
            Vector2 travel = new SerializedObject(m).FindProperty("travel").vector2Value;
            bool gate = m.name.StartsWith("Gate");
            if (gate) { for (float x = r.xMin + 0.15f; x < r.xMax; x += 0.3f) Line(P(x, r.yMin), P(x, r.yMax), Ink); }
            Outline(r, Ink);
            Outline(new Rect(r.position + travel, r.size), InkSoft, dotted: true);
            // Path arrow: just above a sideways mover, just beside an up/down one.
            Vector2 off = Mathf.Abs(travel.x) >= Mathf.Abs(travel.y) ? new Vector2(0f, 0.6f) : new Vector2(r.width * 0.5f + 0.4f, 0f);
            Vector2 from = r.center + off, to = r.center + travel + off;
            Arrow(P(from.x, from.y), P(to.x, to.y), Move, dotted: true);
        }

        // Plates with the weight they need ("cargo" plates ignore players: marked with a small box).
        foreach (PressurePlate plate in root.GetComponentsInChildren<PressurePlate>(true))
        {
            Rect r = LocalRect(plate.GetComponent<BoxCollider2D>());
            Fill(Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, r.yMin + 0.15f), Gold);
            SerializedObject so = new SerializedObject(plate);
            float need = so.FindProperty("requiredWeight").floatValue;
            if (need > 999f) Text(P(r.center.x, r.yMin + 0.9f), "SCALE", Gold);
            else Text(P(r.center.x, r.yMin + 0.9f), $"{need:0}N", Gold);
        }

        // Crates (mass, or "?" where this room hides it) and levers.
        foreach (Rigidbody2D rb in root.GetComponentsInChildren<Rigidbody2D>(true))
        {
            if (rb.bodyType != RigidbodyType2D.Dynamic || !rb.TryGetComponent(out BoxCollider2D box)) continue;
            Rect r = LocalRect(box);
            Fill(r, CrateFill);
            Outline(r, Ink);
            bool hidden = rb.TryGetComponent(out MassLabel ml) && !ml.enabled;
            Text(P(r.center.x, r.center.y + 0.15f), hidden ? "?" : $"{rb.mass:0}KG", Ink);
        }
        foreach (Lever lever in root.GetComponentsInChildren<Lever>(true))
        {
            Vector2 p = L(lever.transform.position);
            Vector2Int b = P(p.x, p.y - 0.3f), t = P(p.x + 0.3f, p.y + 0.35f);
            Line(b, t, Ink);
            Fill(Rect.MinMaxRect(p.x - 0.25f, p.y - 0.45f, p.x + 0.25f, p.y - 0.3f), Ink);
            Disc(t, 2, Ink);
        }

        // Connections: arrow from each plate/lever to the mover it powers.
        foreach (Mover m in root.GetComponentsInChildren<Mover>(true))
        {
            SignalSource signal = new SerializedObject(m).FindProperty("signal").objectReferenceValue as SignalSource;
            if (signal == null) continue;
            List<SignalSource> sources = new List<SignalSource>();
            string tag = null;
            if (signal is PuzzleRelay relay)
            {
                SerializedObject rs = new SerializedObject(relay);
                SerializedProperty inputs = rs.FindProperty("inputs");
                for (int i = 0; i < inputs.arraySize; i++)
                    if (inputs.GetArrayElementAtIndex(i).objectReferenceValue is SignalSource s) sources.Add(s);
                int mode = rs.FindProperty("mode").enumValueIndex;
                if (sources.Count > 1) tag = mode == 0 ? "ALL" : mode == 1 ? "ANY" : "FLIP";
            }
            else sources.Add(signal);
            if (tag != null) modes.Add(tag);

            Vector2 target = m.TryGetComponent(out BoxCollider2D mb) ? LocalRect(mb).center : (Vector2)L(m.transform.position);
            foreach (SignalSource s in sources)
            {
                if (s.transform.IsChildOf(m.transform)) continue; // built into it (the ferry's deck plate)
                Vector2 from = L(s.transform.position) + Vector2.up * 0.4f;
                Arrow(P(from.x, from.y), P(target.x, target.y), Link, dotted: true);
            }
            if (tag != null) Text(P(target.x, target.y - 0.9f), tag, Link);
        }
        return px;
    }

    private static void DoorwayInfo(int a, int b, out bool exists, out float bottom, out float height)
    {
        exists = a >= 1 && b <= 8; bottom = 0f; height = 4f;
        GameObject d = GameObject.Find($"Doorway_R{a}_R{b}");
        if (d == null || !d.TryGetComponent(out Doorway doorway)) return;
        SerializedObject so = new SerializedObject(doorway);
        bottom = so.FindProperty("openingBottom").floatValue;
        height = so.FindProperty("openingHeight").floatValue;
    }

    private static void ExitMarker(float y)
    {
        Vector2Int tip = P(HalfW - 0.15f, y);
        Arrow(P(HalfW - 2.2f, y), tip, Exit, dotted: false);
        Text(P(HalfW - 1.3f, y + 0.75f), "EXIT", Exit);
    }

    // ------------------------------------------------------------------ primitives (room-local metres -> pixels)

    private static Vector2Int P(float x, float y) => new Vector2Int(Mathf.RoundToInt((x + HalfW) * Ppu), Mathf.RoundToInt((y + HalfH) * Ppu));

    private static void Set(int x, int y, Color32 c)
    {
        if (x < 0 || y < 0 || x >= W || y >= H) return;
        int i = y * W + x;
        Color32 o = px[i];
        float a = c.a / 255f, oa = o.a / 255f, na = a + oa * (1f - a);
        if (na <= 0f) return;
        px[i] = new Color32((byte)((c.r * a + o.r * oa * (1f - a)) / na), (byte)((c.g * a + o.g * oa * (1f - a)) / na),
                            (byte)((c.b * a + o.b * oa * (1f - a)) / na), (byte)(na * 255f));
    }

    private static void Fill(Rect r, Color32 c)
    {
        Vector2Int a = P(r.xMin, r.yMin), b = P(r.xMax, r.yMax);
        for (int y = a.y; y < Mathf.Max(a.y + 1, b.y); y++) for (int x = a.x; x < Mathf.Max(a.x + 1, b.x); x++) Set(x, y, c);
    }

    private static void Outline(Rect r, Color32 c, bool dotted = false)
    {
        Vector2Int a = P(r.xMin, r.yMin), b = P(r.xMax, r.yMax);
        b = new Vector2Int(Mathf.Max(a.x + 1, b.x) - 1, Mathf.Max(a.y + 1, b.y) - 1);
        Line(a, new Vector2Int(b.x, a.y), c, dotted); Line(new Vector2Int(b.x, a.y), b, c, dotted);
        Line(b, new Vector2Int(a.x, b.y), c, dotted); Line(new Vector2Int(a.x, b.y), a, c, dotted);
    }

    // Stone: light diagonal hatching inside an ink outline.
    private static void Stone(Rect r)
    {
        Vector2Int a = P(r.xMin, r.yMin), b = P(r.xMax, r.yMax);
        for (int y = a.y; y < b.y; y++) for (int x = a.x; x < b.x; x++) if ((x + y) % 4 == 0) Set(x, y, Hatch);
        Outline(r, Ink);
    }

    private static void AcidArea(Rect r)
    {
        Vector2Int a = P(r.xMin, r.yMin), b = P(r.xMax, r.yMax);
        for (int y = Mathf.Max(0, a.y); y < b.y; y++) for (int x = a.x; x < b.x; x++) Set(x, y, Acid);
        for (int x = a.x; x < b.x; x++) Set(x, b.y - 1 + ((x / 3) % 2), AcidTop); // little waves on top
    }

    private static void Line(Vector2Int a, Vector2Int b, Color32 c, bool dotted = false)
    {
        int dx = Mathf.Abs(b.x - a.x), dy = -Mathf.Abs(b.y - a.y), sx = a.x < b.x ? 1 : -1, sy = a.y < b.y ? 1 : -1, err = dx + dy, n = 0;
        int x = a.x, y = a.y;
        while (true)
        {
            if (!dotted || (n / 2) % 2 == 0) Set(x, y, c);
            n++;
            if (x == b.x && y == b.y) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x += sx; }
            if (e2 <= dx) { err += dx; y += sy; }
        }
    }

    private static void Arrow(Vector2Int a, Vector2Int b, Color32 c, bool dotted)
    {
        Line(a, b, c, dotted);
        Vector2 d = ((Vector2)(b - a)).normalized;
        if (d == Vector2.zero) return;
        Vector2 side = new Vector2(-d.y, d.x);
        for (int i = 1; i <= 4; i++)
        {
            Vector2 back = (Vector2)b - d * i;
            Line(Vector2Int.RoundToInt(back + side * i * 0.7f), Vector2Int.RoundToInt(back - side * i * 0.7f), c);
        }
    }

    private static void Disc(Vector2Int c, int r, Color32 col)
    {
        for (int y = -r; y <= r; y++) for (int x = -r; x <= r; x++) if (x * x + y * y <= r * r) Set(c.x + x, c.y + y, col);
    }

    // ------------------------------------------------------------------ tiny pixel font (3x5 glyphs, drawn at 2x), centred

    private static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
    {
        { '0', new[] { "###", "#.#", "#.#", "#.#", "###" } }, { '1', new[] { ".#.", "##.", ".#.", ".#.", "###" } },
        { '2', new[] { "###", "..#", "###", "#..", "###" } }, { '3', new[] { "###", "..#", "###", "..#", "###" } },
        { '4', new[] { "#.#", "#.#", "###", "..#", "..#" } }, { '5', new[] { "###", "#..", "###", "..#", "###" } },
        { '6', new[] { "###", "#..", "###", "#.#", "###" } }, { '7', new[] { "###", "..#", "..#", "..#", "..#" } },
        { '8', new[] { "###", "#.#", "###", "#.#", "###" } }, { '9', new[] { "###", "#.#", "###", "..#", "###" } },
        { 'A', new[] { ".#.", "#.#", "###", "#.#", "#.#" } }, { 'C', new[] { "###", "#..", "#..", "#..", "###" } },
        { 'E', new[] { "###", "#..", "##.", "#..", "###" } }, { 'F', new[] { "###", "#..", "##.", "#..", "#.." } },
        { 'G', new[] { "###", "#..", "#.#", "#.#", "###" } }, { 'I', new[] { "###", ".#.", ".#.", ".#.", "###" } },
        { 'K', new[] { "#.#", "#.#", "##.", "#.#", "#.#" } }, { 'L', new[] { "#..", "#..", "#..", "#..", "###" } },
        { 'N', new[] { "#.#", "###", "###", "#.#", "#.#" } }, { 'P', new[] { "##.", "#.#", "##.", "#..", "#.." } },
        { 'S', new[] { "###", "#..", "###", "..#", "###" } }, { 'T', new[] { "###", ".#.", ".#.", ".#.", ".#." } },
        { 'X', new[] { "#.#", "#.#", ".#.", "#.#", "#.#" } }, { 'Y', new[] { "#.#", "#.#", ".#.", ".#.", ".#." } },
        { '?', new[] { "###", "..#", ".#.", "...", ".#." } }, { '.', new[] { "...", "...", "...", "...", ".#." } },
    };

    private static void Text(Vector2Int centre, string text, Color32 c)
    {
        const int s = 2, gw = 3 * s, gh = 5 * s, gap = s;
        int width = text.Length * (gw + gap) - gap;
        int x0 = centre.x - width / 2, y0 = centre.y - gh / 2;
        foreach (char ch in text)
        {
            if (Glyphs.TryGetValue(char.ToUpperInvariant(ch), out string[] g))
                for (int row = 0; row < 5; row++)
                    for (int col = 0; col < 3; col++)
                        if (g[row][col] == '#')
                            for (int yy = 0; yy < s; yy++) for (int xx = 0; xx < s; xx++)
                                Set(x0 + col * s + xx, y0 + (4 - row) * s + yy, c);
            x0 += gw + gap;
        }
    }

    // ------------------------------------------------------------------ saving

    private static Sprite Save(string name, Color32[] pixels)
    {
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        string path = $"{Folder}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
