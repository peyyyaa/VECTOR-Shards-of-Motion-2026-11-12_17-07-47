using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the player character sprites from text pixel-art sources (Assets/_Project/Art/Characters/Source/*_Art.json).
///
/// A source file holds: a palette (one letter per colour, "." = empty), body parts drawn as rows of letters,
/// recolours (e.g. "back" limbs are drawn darker), poses (a part plus an x offset), and animations. Each
/// animation lists its layers in draw order (cape, back arm, back leg, front leg, torso, head, front arm) with
/// a base position, and each frame picks a pose per layer plus a body bob / head lean. A one-pixel dark outline
/// is added automatically around the whole figure. Characters face right; the game flips them for left.
///
/// Output: one PNG per frame in Assets/_Project/Art/Characters/{Name}/ ({Name}_{Anim}_{i}.png), 32 PPU, point
/// filtered, with the pivot set so the feet sit on the bottom of the player's body (read from Player_Base:
/// its 1x1 collider is scaled 0.8 x 1.5, so the feet are 24 px below the player's position).
/// Colliders and physics are not touched. When the sprites go on a player, put them on a child object that
/// cancels the player's 0.8 x 1.5 scale, or they will be stretched.
/// </summary>
public static class CharacterSpriteTool
{
    private const string SourceFolder = "Assets/_Project/Art/Characters/Source";
    private const string OutFolder = "Assets/_Project/Art/Characters";
    private const string PreviewScene = "Assets/_Project/Scenes/Prototype/Art_Character_Preview.unity";
    private const string RoomPrefab = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room01.prefab";
    private const int PixelsPerUnit = 32;
    private const string PlayerPrefab = "Assets/_Project/Prefabs/Player/Player_Base.prefab";

    // How far the bottom of the player's body is below its position, in pixels, read from Player_Base
    // (BoxCollider2D size/offset x the object's scale; currently 1 x 1 scaled 0.8 x 1.5 -> 24 px).
    private static float FeetBelowCenterPx
    {
        get
        {
            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
            BoxCollider2D col = player != null ? player.GetComponent<BoxCollider2D>() : null;
            if (col == null)
            {
                Debug.LogWarning("Character sprites: no BoxCollider2D on Player_Base; assuming a 1.5-unit-tall body.");
                return 0.75f * PixelsPerUnit;
            }
            float scaleY = player.transform.localScale.y;
            return (col.size.y * 0.5f - col.offset.y) * scaleY * PixelsPerUnit;
        }
    }
    private const float RoomFloorY = -4.5f;      // top of Room 1's ground (props stand at y = -4.5)

    [MenuItem("Tools/Shards of Motion/Characters/1. Build Character Sprites")]
    public static void BuildAll()
    {
        string[] sources = Directory.GetFiles(SourceFolder, "*_Art.json");
        if (sources.Length == 0) { Debug.LogError("Character sprites: no *_Art.json files in " + SourceFolder); return; }
        foreach (string src in sources) Build(src.Replace('\\', '/'));
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Shards of Motion/Characters/2. Build Preview Scene")]
    public static void BuildPreview()
    {
        Dictionary<string, Sprite[]> anims = LoadAnims("P1");
        if (!anims.ContainsKey("Idle") || !anims.ContainsKey("Run"))
        {
            Debug.LogError("Character preview: P1 Idle/Run sprites not found. Run 1. Build Character Sprites first.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.625f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
        PixelPerfectCamera ppc = camGo.AddComponent<PixelPerfectCamera>();
        ppc.assetsPPU = PixelsPerUnit;
        ppc.refResolutionX = 640;
        ppc.refResolutionY = 360;
        ppc.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;

        GameObject lightGo = new GameObject("Global Light 2D");
        Light2D light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;

        // Room 1 as scenery only: a copy with every script removed and physics switched off.
        GameObject roomAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefab);
        if (roomAsset != null)
        {
            GameObject room = (GameObject)PrefabUtility.InstantiatePrefab(roomAsset);
            PrefabUtility.UnpackPrefabInstance(room, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            room.name = "R1_Room01 (scenery copy)";
            for (int pass = 0; pass < 4; pass++) // several passes: some scripts require others
                foreach (MonoBehaviour mb in room.GetComponentsInChildren<MonoBehaviour>(true))
                    if (mb != null && IsGameplayScript(mb)) Object.DestroyImmediate(mb);
            foreach (Rigidbody2D rb in room.GetComponentsInChildren<Rigidbody2D>(true)) rb.simulated = false;
        }
        else Debug.LogWarning("Character preview: R1_Room01 prefab not found; showing the characters only.");

        float feet = RoomFloorY + FeetBelowCenterPx / PixelsPerUnit;
        float bigFeet = RoomFloorY + 2f * FeetBelowCenterPx / PixelsPerUnit;
        float x = -7f, bigX = -1f;
        foreach (string who in new[] { "P1", "P2" })
        {
            Dictionary<string, Sprite[]> a = who == "P1" ? anims : LoadAnims(who);
            if (!a.ContainsKey("Idle") || !a.ContainsKey("Run")) { Debug.LogWarning($"Character preview: {who} sprites not found; skipped."); continue; }
            Character($"{who}_Idle", a["Idle"], Fps(who, "Idle", 6f), new Vector3(x, feet, 0f), 1);
            Character($"{who}_Run", a["Run"], Fps(who, "Run", 12f), new Vector3(x + 1.5f, feet, 0f), 1);
            // 2x copies (whole-number scale keeps the pixels square) to inspect the detail.
            Character($"{who}_Idle (2x)", a["Idle"], Fps(who, "Idle", 6f), new Vector3(bigX, bigFeet, 0f), 2);
            Character($"{who}_Run (2x)", a["Run"], Fps(who, "Run", 12f), new Vector3(bigX + 3f, bigFeet, 0f), 2);
            x += 3f;
            bigX += 6f;
        }

        EditorSceneManager.SaveScene(scene, PreviewScene);
        Debug.Log("Character preview: saved " + PreviewScene + ". Press Play to see the animations.");
    }

    // ------------------------------------------------------------------ building

    private static void Build(string sourcePath)
    {
        Dictionary<string, object> art = (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(sourcePath));
        string name = (string)art["name"];
        int w = Int(art["width"]), h = Int(art["height"]);
        Color32 outline = Hex((string)art["outline"]);
        Dictionary<char, Color32> palette = new Dictionary<char, Color32>();
        foreach (KeyValuePair<string, object> kv in (Dictionary<string, object>)art["palette"]) palette[kv.Key[0]] = Hex((string)kv.Value);
        Dictionary<string, object> parts = (Dictionary<string, object>)art["parts"];
        Dictionary<string, object> poses = (Dictionary<string, object>)art["poses"];
        Dictionary<string, object> recolors = (Dictionary<string, object>)art["recolors"];
        Dictionary<string, object> anims = (Dictionary<string, object>)art["anims"];

        string folder = $"{OutFolder}/{name}";
        Directory.CreateDirectory(folder);
        float pivotY = -1f;
        List<string> written = new List<string>();

        foreach (KeyValuePair<string, object> anim in anims)
        {
            Dictionary<string, object> a = (Dictionary<string, object>)anim.Value;
            List<object> layers = (List<object>)a["layers"];
            List<object> frames = (List<object>)a["frames"];
            for (int i = 0; i < frames.Count; i++)
            {
                Dictionary<string, object> f = (Dictionary<string, object>)frames[i];
                Color32?[,] px = new Color32?[w, h]; // [x, row from top]
                foreach (object lo in layers)
                {
                    Dictionary<string, object> l = (Dictionary<string, object>)lo;
                    string key = (string)l["key"];
                    if (f.TryGetValue(key, out object chosen) && chosen == null) continue; // layer hidden this frame
                    string poseName = chosen as string ?? (string)l["default"];
                    string partName = poseName;
                    int dx = 0, dy = 0;
                    if (poses.TryGetValue(poseName, out object po))
                    {
                        Dictionary<string, object> p = (Dictionary<string, object>)po;
                        partName = (string)p["part"];
                        if (p.TryGetValue("dx", out object pdx)) dx = Int(pdx);
                        if (p.TryGetValue("dy", out object pdy)) dy = Int(pdy);
                    }
                    if (!parts.TryGetValue(partName, out object rowsObj)) { Debug.LogError($"{name}: no part '{partName}'"); return; }
                    bool bobs = !l.TryGetValue("bob", out object lb) || !(lb is bool b) || b;
                    int bob = bobs && f.TryGetValue("bob", out object fb) ? Int(fb) : 0;
                    int lean = l.TryGetValue("lean", out object ll) && ll is bool lv && lv && f.TryGetValue("lean", out object fl) ? Int(fl) : 0;
                    Dictionary<string, object> map = l.TryGetValue("recolor", out object rc) ? (Dictionary<string, object>)recolors[(string)rc] : null;
                    Stamp(px, (List<object>)rowsObj, Int(l["x"]) + dx + lean, Int(l["y"]) + dy + bob, palette, map, name, partName);
                }
                Color32[,] img = Outline(px, outline);
                if (pivotY < 0f) pivotY = (h - 1 - LowestRow(img) + FeetBelowCenterPx) / h; // from the first frame: feet on the collider bottom
                string path = $"{folder}/{name}_{anim.Key}_{i}.png";
                Save(img, path);
                written.Add(path);
            }
        }

        foreach (string path in written) Import(path, new Vector2(0.5f, pivotY));
        Debug.Log($"Character sprites: {name} built ({written.Count} frames) in {folder}.");
    }

    private static void Stamp(Color32?[,] px, List<object> rows, int x0, int y0, Dictionary<char, Color32> palette,
        Dictionary<string, object> map, string who, string part)
    {
        int w = px.GetLength(0), h = px.GetLength(1);
        for (int r = 0; r < rows.Count; r++)
        {
            string row = (string)rows[r];
            for (int c = 0; c < row.Length; c++)
            {
                char ch = row[c];
                if (ch == '.') continue;
                if (map != null && map.TryGetValue(ch.ToString(), out object to)) ch = ((string)to)[0];
                if (!palette.TryGetValue(ch, out Color32 col)) { Debug.LogError($"{who}: part '{part}' uses '{ch}', which is not in the palette."); continue; }
                int x = x0 + c, y = y0 + r;
                if (x >= 0 && x < w && y >= 0 && y < h) px[x, y] = col;
            }
        }
    }

    // Every empty pixel that touches a filled one (up/down/left/right) becomes the outline colour.
    private static Color32[,] Outline(Color32?[,] px, Color32 outline)
    {
        int w = px.GetLength(0), h = px.GetLength(1);
        Color32[,] img = new Color32[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (px[x, y].HasValue) { img[x, y] = px[x, y].Value; continue; }
                bool edge = (x > 0 && px[x - 1, y].HasValue) || (x < w - 1 && px[x + 1, y].HasValue)
                         || (y > 0 && px[x, y - 1].HasValue) || (y < h - 1 && px[x, y + 1].HasValue);
                img[x, y] = edge ? outline : new Color32(0, 0, 0, 0);
            }
        return img;
    }

    private static int LowestRow(Color32[,] img)
    {
        for (int y = img.GetLength(1) - 1; y >= 0; y--)
            for (int x = 0; x < img.GetLength(0); x++)
                if (img[x, y].a > 0) return y;
        return img.GetLength(1) - 1;
    }

    private static void Save(Color32[,] img, string path)
    {
        int w = img.GetLength(0), h = img.GetLength(1);
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) tex.SetPixel(x, h - 1 - y, img[x, y]); // rows are top-down
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void Import(string path, Vector2 pivot)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        TextureImporterSettings s = new TextureImporterSettings();
        importer.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = pivot;
        s.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(s);
        importer.SaveAndReimport();
    }

    // ------------------------------------------------------------------ preview helpers

    private static Dictionary<string, Sprite[]> LoadAnims(string name)
    {
        Dictionary<string, List<(int, Sprite)>> found = new Dictionary<string, List<(int, Sprite)>>();
        foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { $"{OutFolder}/{name}" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string[] bits = Path.GetFileNameWithoutExtension(path).Split('_'); // Name_Anim_i
            if (bits.Length != 3 || !int.TryParse(bits[2], out int index)) continue;
            if (!found.ContainsKey(bits[1])) found[bits[1]] = new List<(int, Sprite)>();
            found[bits[1]].Add((index, AssetDatabase.LoadAssetAtPath<Sprite>(path)));
        }
        return found.ToDictionary(kv => kv.Key, kv => kv.Value.OrderBy(t => t.Item1).Select(t => t.Item2).ToArray());
    }

    private static float Fps(string name, string anim, float fallback)
    {
        string path = $"{SourceFolder}/{name}_Art.json";
        if (!File.Exists(path)) return fallback;
        Dictionary<string, object> art = (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(path));
        Dictionary<string, object> a = (Dictionary<string, object>)((Dictionary<string, object>)art["anims"])[anim];
        return a.TryGetValue("fps", out object fps) ? (float)(double)fps : fallback;
    }

    private static void Character(string objName, Sprite[] frames, float fps, Vector3 pos, int scale)
    {
        GameObject go = new GameObject(objName);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Player";
        go.AddComponent<SpriteFlipbook>().EditorSetup(frames, fps);
    }

    // The project's own scripts have no namespace; Unity components (lights, shadow casters) do and are kept.
    private static bool IsGameplayScript(MonoBehaviour mb)
    {
        string ns = mb.GetType().Namespace;
        return string.IsNullOrEmpty(ns) || ns.StartsWith("Cainos");
    }

    private static int Int(object o) => (int)System.Math.Round((double)o);

    private static Color32 Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    // ------------------------------------------------------------------ tiny JSON reader (objects, arrays, strings, numbers, bools, null)

    private static class MiniJson
    {
        public static object Parse(string json) { int i = 0; object v = Value(json, ref i); return v; }

        private static object Value(string s, ref int i)
        {
            Skip(s, ref i);
            char c = s[i];
            if (c == '{')
            {
                Dictionary<string, object> d = new Dictionary<string, object>();
                i++;
                while (true)
                {
                    Skip(s, ref i);
                    if (s[i] == '}') { i++; return d; }
                    string key = Str(s, ref i);
                    Skip(s, ref i); i++; // ':'
                    d[key] = Value(s, ref i);
                    Skip(s, ref i);
                    if (s[i] == ',') i++;
                }
            }
            if (c == '[')
            {
                List<object> list = new List<object>();
                i++;
                while (true)
                {
                    Skip(s, ref i);
                    if (s[i] == ']') { i++; return list; }
                    list.Add(Value(s, ref i));
                    Skip(s, ref i);
                    if (s[i] == ',') i++;
                }
            }
            if (c == '"') return Str(s, ref i);
            if (s.Substring(i).StartsWith("true")) { i += 4; return true; }
            if (s.Substring(i).StartsWith("false")) { i += 5; return false; }
            if (s.Substring(i).StartsWith("null")) { i += 4; return null; }
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(start, i - start), CultureInfo.InvariantCulture);
        }

        private static string Str(string s, ref int i)
        {
            StringBuilder sb = new StringBuilder();
            i++; // opening quote
            while (s[i] != '"')
            {
                if (s[i] == '\\')
                {
                    i++;
                    char e = s[i];
                    if (e == 'n') sb.Append('\n');
                    else if (e == 't') sb.Append('\t');
                    else if (e == 'u') { sb.Append((char)System.Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; }
                    else sb.Append(e);
                }
                else sb.Append(s[i]);
                i++;
            }
            i++; // closing quote
            return sb.ToString();
        }

        private static void Skip(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }
    }
}
