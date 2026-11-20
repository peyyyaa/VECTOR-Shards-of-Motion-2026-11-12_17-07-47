using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools > Shards of Motion > Map > Draw Region Map.
/// 1. Creates the Region 1 map data (Assets/_Project/Data/Map/Region1_Map.asset) the first time: the 8 rooms in path order
///    (names, 3 shards each, the two machine parts, main journal entries, marker positions) and the two locked regions.
///    On later runs it keeps your edits and only fills journal entries that are still empty (new rooms' entries).
/// 2. Draws the map art and its icons with MapPainter into Assets/_Project/Art/Map.
/// The map screen itself (MapPanel) is part of UI_Canvas: Create UI Prefabs builds it with BuildPanel below.
/// After moving markers or changing label sides in the asset: run Draw Region Map, then Create UI Prefabs.
/// </summary>
public static class MapTool
{
    private const string DataFolder = "Assets/_Project/Data/Map";
    private const string DataPath = DataFolder + "/Region1_Map.asset";
    private const string ArtFolder = "Assets/_Project/Art/Map";
    private const string JournalFolder = "Assets/_Project/Data/Journal";
    private const string Kit = "Assets/_Project/Art/Sprites/UI";
    private const int Scale = 3; // map pixels -> canvas pixels (the journal's art pixel = 3 canvas px)

    private static readonly Color32 Ink = MenuUIBuilder.Ink, InkSoft = new Color32(107, 74, 46, 255);

    [MenuItem("Tools/Shards of Motion/Map/Draw Region Map")]
    public static void DrawMenu()
    {
        RegionMapData data = EnsureData();
        Draw(data);
        Selection.activeObject = data;
        Debug.Log($"Map drawn into {ArtFolder}; data in {DataPath}. Run Create UI Prefabs to rebuild the map screen with it.");
    }

    // ------------------------------------------------------------------ data

    public static RegionMapData EnsureData()
    {
        RegionMapData data = AssetDatabase.LoadAssetAtPath<RegionMapData>(DataPath);
        if (data == null)
        {
            Directory.CreateDirectory(DataFolder);
            data = ScriptableObject.CreateInstance<RegionMapData>();
            Region1Defaults(data);
            AssetDatabase.CreateAsset(data, DataPath);
        }
        FillJournals(data);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        return data;
    }

    private static void Region1Defaults(RegionMapData d)
    {
        d.regionId = "Region1";
        d.regionName = "The Shattered Grounds";
        d.mapSize = new Vector2Int(480, 220);
        d.villageName = "Alderbrook";
        d.villagePosition = new Vector2(34f, 52f);
        const RegionMapData.LabelSide B = RegionMapData.LabelSide.Below, R = RegionMapData.LabelSide.Right;
        (string name, Vector2 at, RegionMapData.LabelSide side, string part, string partName)[] rooms =
        {
            ("The First Exchange",    new Vector2(88f, 40f),   B, null, null),
            ("Weight of Things",      new Vector2(150f, 56f),  B, null, null),
            ("Together We Move",      new Vector2(212f, 40f),  B, "R1_Part_Flywheel", "Flywheel"),
            ("Broken Crossing",       new Vector2(266f, 82f),  R, "R1_Part_Counterweight", "Counterweight"),
            ("Long Push",             new Vector2(220f, 122f), B, null, null),
            ("Weight of Cooperation", new Vector2(156f, 134f), B, null, null),
            ("Unstoppable Object",    new Vector2(92f, 170f),  B, null, null),
            ("Heart of the Forest",   new Vector2(200f, 170f), B, null, null),
        };
        d.rooms = new List<RegionMapData.RoomInfo>();
        for (int i = 0; i < rooms.Length; i++)
            d.rooms.Add(new RegionMapData.RoomInfo
            {
                roomObject = $"R1_Room{i + 1:00}", displayName = rooms[i].name, shards = 3,
                partId = rooms[i].part, partName = rooms[i].partName, mapPosition = rooms[i].at, label = rooms[i].side,
            });
        d.lockedRegions = new List<RegionMapData.LockedRegion>
        {
            new RegionMapData.LockedRegion { name = "The Gravity Divide", area = new Rect(342f, 116f, 128f, 94f) },
            new RegionMapData.LockedRegion { name = "The Mechanical Depths", area = new Rect(342f, 12f, 128f, 94f) },
        };
    }

    // A room's main entry is the one titled "<room number> · ..." (Room 1's extra "Float" entry is not its main one).
    private static void FillJournals(RegionMapData d)
    {
        if (!AssetDatabase.IsValidFolder(JournalFolder)) return;
        JournalEntry[] entries = AssetDatabase.FindAssets("t:JournalEntry", new[] { JournalFolder })
            .Select(g => AssetDatabase.LoadAssetAtPath<JournalEntry>(AssetDatabase.GUIDToAssetPath(g))).Where(e => e != null).ToArray();
        for (int i = 0; i < d.rooms.Count; i++)
        {
            if (d.rooms[i].journal != null) continue;
            string prefix = $"R1_{i + 1:00}_";
            d.rooms[i].journal = entries.FirstOrDefault(e => e.name.StartsWith(prefix) && e.title.StartsWith($"{i + 1} "));
        }
    }

    // ------------------------------------------------------------------ art

    public static void Draw(RegionMapData d)
    {
        Directory.CreateDirectory(ArtFolder);
        List<Vector2> rooms = d.rooms.Select(r => r.mapPosition).ToList();
        List<MapPainter.Label> labels = d.rooms.Select(r => new MapPainter.Label { at = r.mapPosition, side = r.label }).ToList();
        List<Rect> fog = d.lockedRegions.Select(l => l.area).ToList();
        Color32[] px = MapPainter.DrawMap(d.mapSize.x, d.mapSize.y, d.villagePosition, rooms, labels, fog);
        d.mapArt = Save($"Map_{d.regionId}", px, d.mapSize.x, d.mapSize.y);

        Save("Map_Marker", MapPainter.DrawMarker(out int w, out int h), w, h);
        Save("Map_Shard", MapPainter.DrawShard(out w, out h), w, h);
        Save("Map_Part", MapPainter.DrawPart(out w, out h), w, h);
        EditorUtility.SetDirty(d);
        AssetDatabase.SaveAssets();
    }

    private static Sprite Save(string name, Color32[] pixels, int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        string path = $"{ArtFolder}/{name}.png";
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

    private static Sprite ArtSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{name}.png");
    private static Sprite KitSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Kit}/{name}.png");

    // ------------------------------------------------------------------ the map screen (part of UI_Canvas)

    /// <summary>Builds the map screen under the canvas (hidden). Called by UIPrefabBuilder.</summary>
    public static MapPanel BuildPanel(Transform canvas)
    {
        RegionMapData data = EnsureData();
        if (data.mapArt == null || ArtSprite("Map_Marker") == null) Draw(data);

        RectTransform root = MenuUIBuilder.Fullscreen(canvas, "MapPanel", new Color(0f, 0f, 0f, 0.6f));
        RectTransform card = MenuUIBuilder.Card(root, "Card", Vector2.zero, new Vector2(1640f, 1000f), "UI_Page");
        MenuUIBuilder.Text(card, "Heading", data.regionName, 56f, MenuUIBuilder.Green, new Vector2(0f, 445f), new Vector2(1000f, 70f));
        TMP_Text totals = MenuUIBuilder.Text(card, "Totals", "", 30f, Ink, new Vector2(0f, 395f), new Vector2(1300f, 44f));

        Vector2 size = (Vector2)data.mapSize * Scale;
        RectTransform mapRt = MenuUIBuilder.Rect(card, "Map", new Vector2(0f, 20f), size);
        Image art = mapRt.gameObject.AddComponent<Image>();
        art.sprite = data.mapArt;
        art.raycastTarget = false;
        Vector2 M(Vector2 p) => (p - (Vector2)data.mapSize * 0.5f) * Scale;

        MenuUIBuilder.Text(mapRt, "Village", data.villageName, 26f, Ink, M(data.villagePosition) + new Vector2(0f, -66f), new Vector2(240f, 36f));

        foreach (RegionMapData.LockedRegion region in data.lockedRegions)
        {
            RectTransform g = MenuUIBuilder.Rect(mapRt, region.name, M(region.area.center), region.area.size * Scale);
            Img(g, "Lock", KitSprite("UI_Icon_Lock"), new Vector2(0f, 42f), new Vector2(36f, 36f));
            MenuUIBuilder.Text(g, "Name", region.name, 30f, Ink, Vector2.zero, new Vector2(g.sizeDelta.x, 40f));
            MenuUIBuilder.Text(g, "Soon", "coming soon", 24f, InkSoft, new Vector2(0f, -32f), new Vector2(g.sizeDelta.x, 32f));
        }

        MapMarker[] markers = new MapMarker[data.rooms.Count];
        for (int i = 0; i < data.rooms.Count; i++) markers[i] = Marker(mapRt, data.rooms[i], M(data.rooms[i].mapPosition), i);

        TMP_Text info = MenuUIBuilder.Text(card, "Info", "", 28f, Ink, new Vector2(0f, -355f), new Vector2(1500f, 40f));
        Button back = MenuUIBuilder.Button(card, "Back", "Back", new Vector2(-160f, -430f), 260f);
        TMP_Text hint = MenuUIBuilder.Text(card, "Hint", "", 26f, InkSoft, new Vector2(200f, -430f), new Vector2(400f, 40f), TextAlignmentOptions.Left);

        MapPanel panel = root.gameObject.AddComponent<MapPanel>();
        panel.EditorSetup(data, markers, totals, info, hint, back);
        root.gameObject.SetActive(false);
        return panel;
    }

    // One room: the ring (Button + MapMarker; pulses when selected), the pin, and the name + progress row.
    private static MapMarker Marker(RectTransform map, RegionMapData.RoomInfo room, Vector2 at, int index)
    {
        RectTransform root = MenuUIBuilder.Rect(map, $"Room{index + 1}", at, Vector2.zero);

        RectTransform ring = Img(root, "Marker", ArtSprite("Map_Marker"), Vector2.zero, new Vector2(13f, 13f) * Scale);
        Image ringImage = ring.GetComponent<Image>();
        ringImage.raycastTarget = true; // clickable
        Button button = ring.gameObject.AddComponent<Button>();
        button.targetGraphic = ringImage;
        ColorBlock c = button.colors;
        c.normalColor = Color.white;
        c.highlightedColor = new Color(1f, 0.9f, 0.7f);
        c.selectedColor = new Color(1f, 0.8f, 0.45f);
        c.pressedColor = new Color(0.85f, 0.75f, 0.6f);
        c.disabledColor = new Color(1f, 1f, 1f, 0.35f); // unreached: faint
        c.fadeDuration = 0.05f;
        button.colors = c;

        RectTransform pin = Img(root, "Pin", KitSprite("UI_Seal"), new Vector2(26f, 26f), new Vector2(48f, 48f));

        RectTransform details = MenuUIBuilder.Rect(root, "Details", Vector2.zero, Vector2.zero);
        Vector2 row; float start; Vector2 namePos, namePivot; TextAlignmentOptions align;
        switch (room.label)
        {
            case RegionMapData.LabelSide.Above:
                row = new Vector2(0f, 34f); start = -65f; namePos = new Vector2(0f, 50f); namePivot = new Vector2(0.5f, 0f); align = TextAlignmentOptions.Bottom; break;
            case RegionMapData.LabelSide.Right:
                row = new Vector2(26f, 12f); start = 0f; namePos = new Vector2(26f, -2f); namePivot = new Vector2(0f, 1f); align = TextAlignmentOptions.TopLeft; break;
            case RegionMapData.LabelSide.Left:
                row = new Vector2(-26f, 12f); start = -130f; namePos = new Vector2(-26f, -2f); namePivot = new Vector2(1f, 1f); align = TextAlignmentOptions.TopRight; break;
            default:
                row = new Vector2(0f, -34f); start = -65f; namePos = new Vector2(0f, -50f); namePivot = new Vector2(0.5f, 1f); align = TextAlignmentOptions.Top; break;
        }
        // Progress row: [shard] 2/3  [journal]  [part]   (130 px wide)
        Img(details, "Shard", ArtSprite("Map_Shard"), row + new Vector2(start + 10.5f, 0f), new Vector2(7f, 9f) * Scale);
        TMP_Text shards = MenuUIBuilder.Text(details, "Shards", "0/3", 24f, Ink, row + new Vector2(start + 47f, 0f), new Vector2(40f, 30f));
        Image journal = Img(details, "Journal", KitSprite("UI_Icon_Book"), row + new Vector2(start + 85f, 0f), new Vector2(24f, 24f)).GetComponent<Image>();
        Image part = Img(details, "Part", ArtSprite("Map_Part"), row + new Vector2(start + 116.5f, 0f), new Vector2(9f, 9f) * Scale).GetComponent<Image>();

        TMP_Text name = MenuUIBuilder.Text(details, "Name", room.displayName, 24f, Ink, namePos, new Vector2(210f, 56f), align);
        name.rectTransform.pivot = namePivot;
        name.rectTransform.anchoredPosition = namePos;

        MapMarker marker = ring.gameObject.AddComponent<MapMarker>();
        marker.EditorSetup(button, pin.gameObject, details.gameObject, name, shards, journal, part);
        return marker;
    }

    private static RectTransform Img(RectTransform parent, string name, Sprite sprite, Vector2 pos, Vector2 size)
    {
        RectTransform rt = MenuUIBuilder.Rect(parent, name, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return rt;
    }
}
