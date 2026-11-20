using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Effects tooling (Tools > Shards of Motion > Effects):
/// 1. Build Effect Sprites: draws the pixel effect sprites into Assets/_Project/Art/FX/ (32 PPU, point filtered)
///    and fills FX_Library.asset. The Kinetic Burst shockwave is drawn at the burst's real Range and Cone Angle,
///    read from the Player1 prefab, so what players see is exactly what the burst reaches. Re-run after changing them.
/// 2. Set Up Player Effects: adds BurstEffects (Player1), TetherEffects (Player2) and MovementEffects
///    (Player_Base, coloured per player). Purely visual; no physics values are touched.
/// </summary>
public static class FxSpriteTool
{
    private const string Folder = "Assets/_Project/Art/FX";
    private const string LibraryPath = Folder + "/FX_Library.asset";
    private const string PlayerFolder = "Assets/_Project/Prefabs/Player";
    private const int PPU = 32;

    private static readonly Color32 Clear = new Color32(0, 0, 0, 0), White = new Color32(255, 255, 255, 255);
    private static readonly Color32 Hot = new Color32(255, 241, 194, 255), Orange = new Color32(232, 137, 58, 255);

    [MenuItem("Tools/Shards of Motion/Effects/1. Build Effect Sprites")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder);
        float range = 2.5f, cone = 60f;
        GameObject p1 = AssetDatabase.LoadAssetAtPath<GameObject>($"{PlayerFolder}/Player1.prefab");
        KineticBurst kb = p1 != null ? p1.GetComponent<KineticBurst>() : null;
        if (kb != null) { range = kb.Range; cone = kb.ConeAngle; }
        else Debug.LogWarning("Effect sprites: KineticBurst not found on Player1; drawing the shockwave for 2.5 m / 60°.");

        FxLibrary lib = AssetDatabase.LoadAssetAtPath<FxLibrary>(LibraryPath);
        if (lib == null) { lib = ScriptableObject.CreateInstance<FxLibrary>(); AssetDatabase.CreateAsset(lib, LibraryPath); }

        lib.shockwave = new Sprite[4];
        float[] radius = { 0.35f, 0.6f, 0.85f, 1f };
        int[] thick = { 3, 3, 2, 1 };
        for (int i = 0; i < 4; i++) lib.shockwave[i] = Save($"FX_Shockwave_{i}", Shockwave(range * PPU, cone, radius[i], thick[i], i));
        lib.ring = new Sprite[4];
        int[] ringR = { 3, 5, 7, 8 };
        for (int i = 0; i < 4; i++) lib.ring[i] = Save($"FX_Ring_{i}", Ring(ringR[i], i == 3));
        lib.dust = new Sprite[3];
        for (int i = 0; i < 3; i++) lib.dust[i] = Save($"FX_Dust_{i}", Dust(i));
        lib.spark = Save("FX_Spark", Stamp(".W.", "WWW", ".W."));
        lib.wisp = Save("FX_Wisp", Stamp("WWW.", ".WWW"));
        lib.dot = Save("FX_Dot", Stamp("WW", "WW"));
        lib.arrow = Save("FX_Arrow", Stamp(
            "........W....",
            "........WW...",
            "WWWWWWWWWWW..",
            "WWWWWWWWWWWW.",
            "WWWWWWWWWWW..",
            "........WW...",
            "........W...."));
        lib.sparkle = Save("FX_Sparkle", Stamp("..W..", "..W..", "WWWWW", "..W..", "..W.."));

        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"Effect sprites: built in {Folder} (shockwave {range} m, {cone}°) and FX_Library.asset.");
    }

    [MenuItem("Tools/Shards of Motion/Effects/2. Set Up Player Effects")]
    public static void SetUpPlayers()
    {
        FxLibrary lib = AssetDatabase.LoadAssetAtPath<FxLibrary>(LibraryPath);
        if (lib == null) { Debug.LogError("Player effects: run Effects > 1. Build Effect Sprites first."); return; }

        Edit($"{PlayerFolder}/Player_Base.prefab", root =>
        {
            Transform visual = root.transform.Find("Visual");
            if (visual == null) { Debug.LogError("Player effects: Player_Base has no Visual child. Run Characters > 3. Set Up Player Visuals first."); return false; }
            MovementEffects m = Ensure<MovementEffects>(root);
            m.EditorSetup(lib, visual.GetComponent<SpriteRenderer>(), new Color32(232, 137, 58, 255), new Color32(255, 230, 150, 255));
            return true;
        });
        Edit($"{PlayerFolder}/Player1.prefab", root =>
        {
            if (root.GetComponent<KineticBurst>() == null) { Debug.LogError("Player effects: Player1 has no KineticBurst."); return false; }
            Ensure<BurstEffects>(root).EditorSetup(lib);
            SetMovementColors(root, lib, new Color32(232, 137, 58, 255), new Color32(255, 230, 150, 255));
            return true;
        });
        Edit($"{PlayerFolder}/Player2.prefab", root =>
        {
            if (root.GetComponent<WindTether>() == null) { Debug.LogError("Player effects: Player2 has no WindTether."); return false; }
            Ensure<TetherEffects>(root).EditorSetup(lib);
            SetMovementColors(root, lib, new Color32(73, 194, 208, 255), new Color32(191, 238, 242, 255));
            return true;
        });
        Debug.Log("Player effects: BurstEffects on Player1, TetherEffects on Player2, MovementEffects (dash/float) on both.");
    }

    private static void SetMovementColors(GameObject root, FxLibrary lib, Color tint, Color sparkle)
    {
        MovementEffects m = root.GetComponent<MovementEffects>();
        Transform visual = root.transform.Find("Visual");
        if (m != null && visual != null) m.EditorSetup(lib, visual.GetComponent<SpriteRenderer>(), tint, sparkle);
    }

    // Explicit null check: in the editor GetComponent returns a "fake null" that ?? does not treat as null.
    private static T Ensure<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static void Edit(string path, System.Func<GameObject, bool> change)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try { if (change(root)) PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // ------------------------------------------------------------------ drawing (x right, y up, origin bottom-left)

    // Arc of the burst cone at a fraction of its range, plus a faint fill and speed lines in the early frames.
    private static Color32[,] Shockwave(float rangePx, float coneDeg, float fraction, int thickness, int frame)
    {
        int half = Mathf.CeilToInt(rangePx) + 2, size = half * 2 + 1;
        Color32[,] p = new Color32[size, size];
        float r = rangePx * fraction, halfAngle = coneDeg * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - half, dy = y - half;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Abs(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
                if (ang > halfAngle || d < 4f) { p[x, y] = Clear; continue; }
                float edge = Mathf.Clamp01((halfAngle - ang) / 6f); // softer toward the cone's sides
                Color32 c = Clear;
                if (d <= r && d > r - thickness) c = d > r - 1f ? Hot : Orange;
                else if (frame <= 1 && d <= r - thickness) c = new Color32(232, 137, 58, 40);
                else if (frame == 1 || frame == 2)
                {
                    foreach (float line in new[] { -halfAngle * 0.6f, 0f, halfAngle * 0.6f })
                        if (Mathf.Abs(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg - line) < 1.2f && d > r * 0.45f && d < r * 0.85f)
                            c = new Color32(255, 241, 194, 160);
                }
                if (c.a > 0) c.a = (byte)(c.a * (frame == 3 ? 0.7f : 1f) * Mathf.Max(0.35f, edge));
                p[x, y] = c;
            }
        return p;
    }

    private static Color32[,] Ring(int r, bool broken)
    {
        int size = r * 2 + 3, c = r + 1;
        Color32[,] p = new Color32[size, size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                bool on = Mathf.Abs(d - r) < 0.55f && (!broken || ((x + y) % 3 != 0));
                p[x, y] = on ? White : Clear;
            }
        return p;
    }

    private static Color32[,] Dust(int frame)
    {
        Color32 body = new Color32(217, 203, 176, (byte)(220 - frame * 50)), rim = new Color32(176, 160, 132, (byte)(200 - frame * 50));
        float r = 2f + frame * 1.2f;
        int size = 11, c = 5;
        Color32[,] p = new Color32[size, size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c) * 1.4f);
                p[x, y] = d <= r - 1f ? body : d <= r ? rim : Clear;
            }
        return p;
    }

    // Rows top to bottom; 'W' = white, '.' = empty.
    private static Color32[,] Stamp(params string[] rows)
    {
        int w = rows[0].Length, h = rows.Length;
        Color32[,] p = new Color32[w, h];
        for (int row = 0; row < h; row++)
            for (int x = 0; x < w; x++)
                p[x, h - 1 - row] = rows[row][x] == 'W' ? White : Clear;
        return p;
    }

    private static Sprite Save(string name, Color32[,] px)
    {
        int w = px.GetLength(0), h = px.GetLength(1);
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) tex.SetPixel(x, y, px[x, y]);
        string path = $"{Folder}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PPU;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        TextureImporterSettings s = new TextureImporterSettings();
        importer.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Center;
        s.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(s);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
