using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Editor tool: Tools > Shards of Motion > Recolor.
///   1. Create Default Profiles  - the starting recolor rules (Art/Recolors/Profiles), editable in the Inspector
///   2. Build All                - writes the recolored textures (+ Tile Palette copies) to Art/Recolors
///   3. Create Lever Sprite      - a 16x32 lever drawn with Village Props' own colors; updates the Lever prefab
///   4. Build Preview Scene      - Scenes/Prototype/Art_Recolor_Preview: originals next to the recolors
/// Imported package files are never modified. Re-running Build All only overwrites the recolored images
/// (their GUIDs, sprites and palettes stay the same, so anything already painted updates automatically).
/// </summary>
public static class RecolorTool
{
    private const string Root = "Assets/_Project/Art/Recolors";
    private const string ProfilesFolder = Root + "/Profiles";
    private const string PalettesFolder = Root + "/Palettes";
    private const string Village = "Assets/Cainos/Pixel Art Platformer - Village Props";
    private const string GroundTexture = Village + "/Texture/TX Tileset Ground.png";
    private const string PropsTexture = Village + "/Texture/TX Village Props.png";
    private const string GroundPalette = Village + "/Tileset Palette/TP Ground.prefab";
    private const string LeverSpritePath = "Assets/_Project/Art/Sprites/Props/SP_Lever.png";
    private const string LeverPrefabPath = "Assets/_Project/Prefabs/Interactables/Lever.prefab";
    private const string PreviewScenePath = "Assets/_Project/Scenes/Prototype/Art_Recolor_Preview.unity";

    // ------------------------------------------------------------------ 1. profiles

    [MenuItem("Tools/Shards of Motion/Recolor/1. Create Default Profiles")]
    public static void CreateDefaultProfiles()
    {
        EnsureFolder(ProfilesFolder);
        Texture2D ground = AssetDatabase.LoadAssetAtPath<Texture2D>(GroundTexture);
        Texture2D props = AssetDatabase.LoadAssetAtPath<Texture2D>(PropsTexture);
        GameObject palette = AssetDatabase.LoadAssetAtPath<GameObject>(GroundPalette);

        // Cobble browns are hue ~28-32 deg; grass tops are olive, hue ~40-57 deg (measured from the texture).
        Make("Ground_RuinStone", ground, true, palette,
            Rule("Cobble -> cool grey-blue stone", 15, 38, 0.15f, 215, 0.30f, 1.35f),
            Rule("Grass -> deep moss", 38, 80, 0.2f, 100, 1.05f, 0.85f));
        Make("Ground_Wet", ground, true, palette,
            Rule("Cobble -> wet blue stone", 15, 38, 0.15f, 205, 0.45f, 1.45f),
            Rule("Grass -> teal water-moss", 38, 80, 0.2f, 170, 0.90f, 0.95f));
        Make("Ground_Mossy", ground, true, palette,
            Rule("Cobble -> moss-covered stone", 15, 38, 0.15f, 85, 0.45f, 1.15f),
            Rule("Grass -> thick bright moss", 38, 80, 0.2f, 105, 1.25f, 1.15f));
        Make("Props_Forest", props, false, null,
            Rule("Banner -> forest green", 340, 40, 0.3f, 120, 0.90f, 0.85f, "TX Village Props - Banner"),
            Rule("Foliage -> deeper green", 42, 95, 0.2f, 100, 1.15f, 0.92f));
        Make("Props_Interactive", props, false, null,
            Rule("Ladder rungs -> dark iron gate bars", 0, 60, 0f, 215, 0.30f, 0.70f,
                "TX Village Props - Ladder 01 T", "TX Village Props - Ladder 01 M", "TX Village Props - Ladder 01 B"),
            Rule("Fence rails -> brass pressure-plate slabs", 0, 60, 0f, 40, 1.35f, 1.25f,
                "TX Village Props - Fence 01 Rail 01", "TX Village Props - Fence 01 Rail 02", "TX Village Props - Fence 01 Rail 03"));

        AssetDatabase.SaveAssets();
        Debug.Log("Recolor: default profiles are in " + ProfilesFolder + " (existing ones were kept).");
    }

    private static void Make(string name, Texture2D source, bool palette, GameObject sourcePalette, params RecolorRule[] rules)
    {
        string path = $"{ProfilesFolder}/RP_{name}.asset";
        if (AssetDatabase.LoadAssetAtPath<RecolorProfile>(path) != null) return; // keep edits
        RecolorProfile p = ScriptableObject.CreateInstance<RecolorProfile>();
        p.source = source;
        p.outputName = name;
        p.copyTilePalette = palette;
        p.sourcePalette = sourcePalette;
        p.rules = rules.ToList();
        AssetDatabase.CreateAsset(p, path);
    }

    private static RecolorRule Rule(string label, float hueFrom, float hueTo, float minSat, float targetHue, float satMul, float valMul, params string[] sprites)
    {
        return new RecolorRule { label = label, hueFrom = hueFrom, hueTo = hueTo, minSaturation = minSat,
            targetHue = targetHue, saturationMultiply = satMul, valueMultiply = valMul, onlySprites = sprites };
    }

    // ------------------------------------------------------------------ 2. build

    [MenuItem("Tools/Shards of Motion/Recolor/2. Build All")]
    public static void BuildAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:RecolorProfile", new[] { ProfilesFolder });
        if (guids.Length == 0) CreateDefaultProfiles();
        guids = AssetDatabase.FindAssets("t:RecolorProfile", new[] { ProfilesFolder });
        foreach (string g in guids) Build(AssetDatabase.LoadAssetAtPath<RecolorProfile>(AssetDatabase.GUIDToAssetPath(g)));
        AssetDatabase.Refresh();
        Debug.Log($"Recolor: built {guids.Length} profiles into {Root}.");
    }

    public static void Build(RecolorProfile p)
    {
        if (p == null || p.source == null) return;
        string sourcePath = AssetDatabase.GetAssetPath(p.source);

        // Read the original PNG file directly (works even if the texture isn't marked readable).
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(sourcePath));
        Color32[] pixels = tex.GetPixels32();
        int width = tex.width;

        // Sprite rectangles, for rules limited to named sprites (rects use the same bottom-left origin as the pixels).
        Dictionary<string, Rect> spriteRects = new Dictionary<string, Rect>();
        foreach (Sprite s in AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<Sprite>())
            if (!spriteRects.ContainsKey(s.name)) spriteRects.Add(s.name, s.rect);
        List<Rect>[] ruleAreas = p.rules.Select(r => r.onlySprites == null || r.onlySprites.Length == 0 ? null
            : r.onlySprites.Where(spriteRects.ContainsKey).Select(n => spriteRects[n]).ToList()).ToArray();

        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 c = pixels[i];
            if (c.a == 0) continue;
            Color.RGBToHSV(c, out float h, out float s, out float v);
            float hue = h * 360f;
            int x = i % width, y = i / width;

            for (int r = 0; r < p.rules.Count; r++)
            {
                RecolorRule rule = p.rules[r];
                if (ruleAreas[r] != null && !ruleAreas[r].Any(a => a.Contains(new Vector2(x + 0.5f, y + 0.5f)))) continue;
                bool inHue = rule.hueFrom <= rule.hueTo ? hue >= rule.hueFrom && hue <= rule.hueTo : hue >= rule.hueFrom || hue <= rule.hueTo;
                if (!inHue || s < rule.minSaturation) continue;

                Color n = Color.HSVToRGB(rule.targetHue / 360f, Mathf.Clamp01(s * rule.saturationMultiply), Mathf.Clamp01(v * rule.valueMultiply));
                pixels[i] = new Color32((byte)(n.r * 255f + 0.5f), (byte)(n.g * 255f + 0.5f), (byte)(n.b * 255f + 0.5f), c.a);
                break; // first matching rule wins
            }
        }
        tex.SetPixels32(pixels);

        EnsureFolder(Root);
        string outPath = $"{Root}/{p.outputName}.png";
        string sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
        if (!File.Exists(outPath + ".meta"))
        {
            // First build: copy the original's import settings and sprite slicing, under a new GUID.
            string meta = File.ReadAllText(sourcePath + ".meta").Replace("guid: " + sourceGuid, "guid: " + NewGuid());
            File.WriteAllText(outPath + ".meta", meta);
        }
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

        if (p.copyTilePalette && p.sourcePalette != null) CopyPalette(p, sourceGuid, AssetDatabase.AssetPathToGUID(outPath));
    }

    // Copies the Tile Palette prefab and its tile assets; the copies point at the recolored texture.
    private static void CopyPalette(RecolorProfile p, string sourceTexGuid, string outTexGuid)
    {
        string palettePath = AssetDatabase.GetAssetPath(p.sourcePalette);
        string paletteDir = Path.GetDirectoryName(palettePath).Replace('\\', '/');
        string tilesDir = $"{paletteDir}/{Path.GetFileNameWithoutExtension(palettePath)}";
        string destDir = $"{PalettesFolder}/{p.outputName}";
        string destPalette = $"{PalettesFolder}/TP {p.outputName}.prefab";
        if (File.Exists(destPalette)) return; // already made: it follows the recolored texture automatically

        EnsureFolder(destDir);
        Dictionary<string, string> guidMap = new Dictionary<string, string> { { sourceTexGuid, outTexGuid } };
        List<(string from, string to)> copies = new List<(string, string)>();
        foreach (string file in Directory.GetFiles(tilesDir).Where(f => !f.EndsWith(".meta")))
        {
            string to = $"{destDir}/{Path.GetFileName(file)}";
            guidMap[ReadGuid(file)] = NewGuid();
            copies.Add((file.Replace('\\', '/'), to));
        }
        guidMap[ReadGuid(palettePath)] = NewGuid();
        copies.Add((palettePath, destPalette));

        foreach ((string from, string to) in copies)
        {
            File.WriteAllText(to, ReplaceGuids(File.ReadAllText(from), guidMap));
            File.WriteAllText(to + ".meta", ReplaceGuids(File.ReadAllText(from + ".meta"), guidMap));
        }
        AssetDatabase.Refresh();
    }

    // ------------------------------------------------------------------ 3. lever sprite

    [MenuItem("Tools/Shards of Motion/Recolor/3. Create Lever Sprite")]
    public static void CreateLeverSprite()
    {
        // Colors sampled from TX Village Props (outline, crate wood, anvil iron, golden-chest brass).
        Dictionary<char, Color32> palette = new Dictionary<char, Color32>
        {
            { 'O', Hex("472a15") }, { 'D', Hex("533d26") }, { 'W', Hex("79603f") }, { 'L', Hex("8f7852") },
            { 'I', Hex("685f54") }, { 'i', Hex("979083") }, { 'B', Hex("906237") }, { 'b', Hex("ba8e55") },
        };
        // 16 x 32, top row first. Handle leans left ("off"); the Lever flips it to the right when on.
        string[] rows =
        {
            "................",
            "..OOOO..........",
            ".OBbbBO.........",
            ".ObbbBO.........",
            ".OBbBBO.........",
            "..OOWO..........",
            "...OLWO.........",
            "...OLWO.........",
            "....OLWO........",
            "....OLWO........",
            "....OLWO........",
            ".....OLWO.......",
            ".....OLWO.......",
            ".....OLWO.......",
            "......OLWO......",
            "......OLWO......",
            "......OLWO......",
            ".......OLWO.....",
            ".......OLWO.....",
            "......OOiIOO....",
            ".....OIiiiIO....",
            ".....OIiIiIO....",
            "..OOOOOOOOOOOO..",
            "..OLLLLLLLLLLO..",
            "..OWWWWWWWWWWO..",
            "..OWDWWWWWWDWO..",
            "..OWWWWWWWWWWO..",
            "..ObWWWWWWWWbO..",
            "..ODDDDDDDDDDO..",
            "..ODDDDDDDDDDO..",
            "..ODDDDDDDDDDO..",
            "..OOOOOOOOOOOO..",
        };

        Texture2D tex = new Texture2D(16, 32, TextureFormat.RGBA32, false);
        for (int row = 0; row < rows.Length; row++)
            for (int x = 0; x < 16; x++)
            {
                char k = rows[row][x];
                tex.SetPixel(x, 31 - row, k == '.' ? new Color32(0, 0, 0, 0) : palette[k]);
            }
        EnsureFolder(Path.GetDirectoryName(LeverSpritePath).Replace('\\', '/'));
        File.WriteAllBytes(LeverSpritePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(LeverSpritePath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(LeverSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.BottomCenter; // stands on the floor
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        // Lever prefab: real sprite at 1:1 (16 x 32 px = 0.5 x 1 unit), trigger fitted to it, no color tint.
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LeverSpritePath);
        GameObject root = PrefabUtility.LoadPrefabContents(LeverPrefabPath);
        root.transform.localScale = Vector3.one;
        SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        BoxCollider2D col = root.GetComponent<BoxCollider2D>();
        col.size = new Vector2(0.5f, 1f);
        col.offset = new Vector2(0f, 0.5f);
        SerializedObject lever = new SerializedObject(root.GetComponent<Lever>());
        lever.FindProperty("offColor").colorValue = Color.white;
        lever.FindProperty("onColor").colorValue = new Color(0.85f, 1f, 1f); // faint cyan when on
        lever.FindProperty("flipSpriteWhenOn").boolValue = true;
        lever.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, LeverPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("Recolor: lever sprite created (" + LeverSpritePath + ") and the Lever prefab now uses it. Pivot: bottom-centre (place it on the floor).");
    }

    // ------------------------------------------------------------------ 4. preview

    [MenuItem("Tools/Shards of Motion/Recolor/4. Build Preview Scene")]
    public static void BuildPreviewScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camObject = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera cam = camObject.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 22f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.192f, 0.302f, 0.475f);
        camObject.AddComponent<UniversalAdditionalCameraData>();
        Light2D light = new GameObject("Global Light 2D").AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;

        // Row 1: tile palettes (original + each recolor), laid out as in the Tile Palette window.
        List<(string label, string path)> palettes = new List<(string, string)> { ("Original", GroundPalette) };
        foreach (string p in Directory.Exists(PalettesFolder) ? Directory.GetFiles(PalettesFolder, "*.prefab") : new string[0])
            palettes.Add((Path.GetFileNameWithoutExtension(p).Replace("TP ", ""), p.Replace('\\', '/')));
        float x = 0f;
        foreach ((string label, string path) in palettes)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Tilemap tm = inst.GetComponentInChildren<Tilemap>();
            tm.CompressBounds();
            Bounds b = tm.localBounds;
            inst.transform.position = new Vector3(x - b.min.x, -b.min.y, 0f);
            Caption(label, new Vector3(x + b.size.x * 0.5f, b.size.y + 1.5f, 0f));
            x += b.size.x + 2f;
        }

        // Row 2: props, original (top) vs recolored (bottom).
        string[] spriteNames =
        {
            "TX Village Props - Tree 01", "TX Village Props - Bush 02", "TX Village Props - Grass 02",
            "TX Village Props - Banner", "TX Village Props - Ladder 01 M", "TX Village Props - Fence 01 Rail 01",
        };
        Dictionary<string, Sprite> original = SpritesByName(PropsTexture);
        Dictionary<string, Sprite> forest = SpritesByName($"{Root}/Props_Forest.png");
        Dictionary<string, Sprite> interactive = SpritesByName($"{Root}/Props_Interactive.png");
        float px = 0f;
        foreach (string n in spriteNames)
        {
            Place(original, n, new Vector3(px, -6f, 0f));
            Dictionary<string, Sprite> recolored = n.Contains("Ladder") || n.Contains("Fence") ? interactive : forest;
            Place(recolored, n, new Vector3(px, -14f, 0f));
            px += 6f;
        }
        Sprite leverSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LeverSpritePath);
        if (leverSprite != null) Place(new Dictionary<string, Sprite> { { "lever", leverSprite } }, "lever", new Vector3(px, -14f, 0f));
        Caption("Props: original (top) / recolored (bottom)", new Vector3(px * 0.5f, -1.5f, 0f));

        camObject.transform.position = new Vector3(Mathf.Max(x, px) * 0.5f, 2f, -10f);
        EnsureFolder(Path.GetDirectoryName(PreviewScenePath).Replace('\\', '/'));
        EditorSceneManager.SaveScene(scene, PreviewScenePath);
        Debug.Log("Recolor: preview scene saved: " + PreviewScenePath);
    }

    private static void Place(Dictionary<string, Sprite> sprites, string name, Vector3 position)
    {
        if (!sprites.TryGetValue(name, out Sprite sprite)) return;
        SpriteRenderer r = new GameObject(name).AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.transform.position = position;
    }

    private static void Caption(string text, Vector3 position)
    {
        GameObject go = new GameObject("Caption " + text);
        go.transform.position = position;
        WorldSign sign = go.AddComponent<WorldSign>();
        SerializedObject so = new SerializedObject(sign);
        so.FindProperty("text").stringValue = text;
        so.FindProperty("width").floatValue = 16f;
        so.FindProperty("showDistance").floatValue = 0f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------ helpers

    private static Dictionary<string, Sprite> SpritesByName(string texturePath)
    {
        Dictionary<string, Sprite> d = new Dictionary<string, Sprite>();
        foreach (Sprite s in AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>())
            if (!d.ContainsKey(s.name)) d.Add(s.name, s);
        return d;
    }

    private static string ReadGuid(string assetPath)
    {
        foreach (string line in File.ReadAllLines(assetPath + ".meta"))
            if (line.StartsWith("guid: ")) return line.Substring(6).Trim();
        return "";
    }

    private static string ReplaceGuids(string text, Dictionary<string, string> map)
    {
        foreach (KeyValuePair<string, string> kv in map) text = text.Replace(kv.Key, kv.Value);
        return text;
    }

    private static string NewGuid() => System.Guid.NewGuid().ToString("N");

    private static Color32 Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    private static void EnsureFolder(string path)
    {
        path = path.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
