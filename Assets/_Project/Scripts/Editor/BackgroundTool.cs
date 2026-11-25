using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool: Tools > Shards of Motion > Backgrounds.
///   1. Process Backgrounds - turns each source image in Art/Backgrounds into clean pixel art:
///      shrinks it so one room screen (20 x 11.25 units) is exactly 320 x 180 pixels (imported at 16 PPU,
///      so each background pixel = 2 x 2 game pixels), reduces it to a small palette (removes the noise
///      and soft gradients), and cuts room-sized crops: Art/Backgrounds/Processed/BG_&lt;name&gt;_&lt;A|B|C&gt;.png.
///      The source images are left untouched.
///   2. Apply To Rooms - puts the assigned crop behind each listed room (inside its prefab), with a tint.
/// Both steps can be re-run.
/// </summary>
public static class BackgroundTool
{
    private const string SourceFolder = "Assets/_Project/Art/Backgrounds";
    private const string OutFolder = SourceFolder + "/Processed";
    private const string RoomsFolder = "Assets/_Project/Prefabs/Rooms/Region1";
    private const int CropWidth = 320, CropHeight = 180, PixelsPerUnit = 16, PaletteSize = 48;
    private const string BackgroundName = "Room_Background";

    // Which crop goes behind which room. Only Room 1 for now (the visual benchmark);
    // the planned progression for the other rooms is listed below, ready to enable.
    private static readonly (string room, string crop, Color tint)[] Assignments =
    {
        ("R1_Room01", "BG_MountainVista_A", new Color(0.88f, 0.92f, 0.95f)),
        ("R1_Room02", "BG_MountainVista_A", new Color(0.84f, 0.88f, 0.93f)),  // same vista, a touch dimmer: deeper in
        ("R1_Room03", "BG_MistyRuins_A", new Color(0.85f, 0.88f, 0.9f)),
        ("R1_Room04", "BG_MistyRuins_B", new Color(0.85f, 0.88f, 0.9f)),     // swap with A/C if another crop shows the waterfall better
        ("R1_Room05", "BG_MistyRuins_C", new Color(0.78f, 0.83f, 0.86f)),   // deeper in: each room a little darker
        ("R1_Room06", "BG_ForestCanopy_A", new Color(0.7f, 0.74f, 0.77f)),
        ("R1_Room07", "BG_ForestCanopy_B", new Color(0.62f, 0.67f, 0.72f)),
        ("R1_Room08", "BG_ForestCanopy_C", new Color(0.5f, 0.56f, 0.66f)),   // the finale, part 1: the Heartwood begins
        ("R1_Room09", "BG_ForestCanopy_A", new Color(0.38f, 0.45f, 0.56f)),  // part 2: the engine hall
        ("R1_Room10", "BG_ForestCanopy_B", new Color(0.34f, 0.42f, 0.55f)),  // part 3: the Great Shard's chamber
    };

    // Short names for the source files (by their start).
    private static readonly (string prefix, string shortName)[] Sources =
    {
        ("Misty Pixel Mountain Vista", "MountainVista"),
        ("Misty Forest Ruins and Waterfall", "MistyRuins"),
        ("Misty Pixel-Art Forest Canopy", "ForestCanopy"),
        ("Mossy Ruins in a Lush Forest", "MossyRuins"),
    };

    [MenuItem("Tools/Shards of Motion/Backgrounds/1. Process Backgrounds")]
    public static void Process()
    {
        if (!AssetDatabase.IsValidFolder(OutFolder)) AssetDatabase.CreateFolder(SourceFolder, "Processed");
        int made = 0;
        foreach (string file in Directory.GetFiles(SourceFolder, "*.png"))
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            (string prefix, string shortName) source = Sources.FirstOrDefault(s => fileName.StartsWith(s.prefix));
            if (source.shortName == null) { Debug.LogWarning("Backgrounds: no short name for " + fileName + ", skipped."); continue; }

            Texture2D src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            src.LoadImage(File.ReadAllBytes(file));

            // 1. Shrink so the height is exactly 180 px (area-average: each new pixel = mean of the pixels it covers).
            int width = Mathf.RoundToInt(src.width * (float)CropHeight / src.height);
            Color[] small = Shrink(src, width, CropHeight);
            Object.DestroyImmediate(src);

            // 2. Limit to a small palette (k-means), so it reads as clean pixel art.
            Quantize(small, PaletteSize);

            // 3. Room-sized crops: left / middle / right (or just one if the image is about one room wide).
            int crops = width >= CropWidth * 1.5f ? 3 : 1;
            for (int c = 0; c < crops; c++)
            {
                int x0 = crops == 1 ? (width - CropWidth) / 2 : Mathf.RoundToInt((width - CropWidth) * c / 2f);
                Texture2D crop = new Texture2D(CropWidth, CropHeight, TextureFormat.RGBA32, false);
                for (int y = 0; y < CropHeight; y++)
                    for (int x = 0; x < CropWidth; x++)
                        crop.SetPixel(x, y, small[y * width + Mathf.Clamp(x0 + x, 0, width - 1)]);
                string outPath = $"{OutFolder}/BG_{source.shortName}_{(char)('A' + c)}.png";
                File.WriteAllBytes(outPath, crop.EncodeToPNG());
                Object.DestroyImmediate(crop);
                AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);
                ConfigureImport(outPath);
                made++;
            }
        }
        Debug.Log($"Backgrounds: {made} room-sized backgrounds written to {OutFolder} (320 x 180 px, 16 PPU, {PaletteSize} colors each source).");
    }

    [MenuItem("Tools/Shards of Motion/Backgrounds/2. Apply To Rooms")]
    public static void ApplyToRooms()
    {
        foreach ((string room, string crop, Color tint) in Assignments)
        {
            string prefabPath = $"{RoomsFolder}/{room}.prefab";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutFolder}/{crop}.png");
            if (sprite == null) { Debug.LogError($"Backgrounds: {crop} not found; run 1. Process Backgrounds first."); continue; }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null) { Debug.LogError("Backgrounds: missing " + prefabPath); continue; }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform layout = root.transform.Find("Layout") ?? root.transform;
                Transform old = layout.Find(BackgroundName);
                if (old != null) Object.DestroyImmediate(old.gameObject);

                // Centered on the room (= the camera's position in this room), filling the screen exactly.
                GameObject bg = new GameObject(BackgroundName);
                bg.transform.SetParent(layout, false);
                bg.transform.position = root.transform.position;
                SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = tint;
                sr.sortingLayerName = "Background";
                sr.sortingOrder = -100;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        Debug.Log("Backgrounds: applied to " + string.Join(", ", Assignments.Select(a => a.room)));
    }

    // ------------------------------------------------------------------ image processing

    private static Color[] Shrink(Texture2D src, int width, int height)
    {
        Color[] pixels = src.GetPixels();
        Color[] result = new Color[width * height];
        float sx = (float)src.width / width, sy = (float)src.height / height;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int x0 = Mathf.FloorToInt(x * sx), x1 = Mathf.Max(x0 + 1, Mathf.FloorToInt((x + 1) * sx));
                int y0 = Mathf.FloorToInt(y * sy), y1 = Mathf.Max(y0 + 1, Mathf.FloorToInt((y + 1) * sy));
                Color sum = Color.clear; int n = 0;
                for (int yy = y0; yy < y1 && yy < src.height; yy++)
                    for (int xx = x0; xx < x1 && xx < src.width; xx++) { sum += pixels[yy * src.width + xx]; n++; }
                Color c = sum / Mathf.Max(1, n);
                c.a = 1f;
                result[y * width + x] = c;
            }
        return result;
    }

    // k-means color reduction: start from colors spread across the image's brightness range, refine 10 times.
    private static void Quantize(Color[] pixels, int k)
    {
        Color[] sorted = pixels.OrderBy(c => c.r * 0.3f + c.g * 0.59f + c.b * 0.11f).ToArray();
        Vector3[] centers = new Vector3[k];
        for (int i = 0; i < k; i++) { Color c = sorted[(int)((i + 0.5f) / k * (sorted.Length - 1))]; centers[i] = new Vector3(c.r, c.g, c.b); }

        int[] assign = new int[pixels.Length];
        for (int iter = 0; iter < 10; iter++)
        {
            Vector3[] sums = new Vector3[k];
            int[] counts = new int[k];
            for (int p = 0; p < pixels.Length; p++)
            {
                Vector3 v = new Vector3(pixels[p].r, pixels[p].g, pixels[p].b);
                int best = 0; float bestD = float.MaxValue;
                for (int i = 0; i < k; i++) { float d = (v - centers[i]).sqrMagnitude; if (d < bestD) { bestD = d; best = i; } }
                assign[p] = best; sums[best] += v; counts[best]++;
            }
            for (int i = 0; i < k; i++) if (counts[i] > 0) centers[i] = sums[i] / counts[i];
        }
        for (int p = 0; p < pixels.Length; p++) { Vector3 c = centers[assign[p]]; pixels[p] = new Color(c.x, c.y, c.z, 1f); }
    }

    private static void ConfigureImport(string path)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit; // 320 x 180 px = 20 x 11.25 units = one room screen
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }
}
