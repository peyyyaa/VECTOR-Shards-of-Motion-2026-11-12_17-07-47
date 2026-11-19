using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

/// <summary>
/// Editor tool: Tools > Shards of Motion > Room 1 > Apply Visual Pass.
/// Dresses R1_Room01 with Village Props art (recolored per Docs/Region1_Art_Direction.md), inside the prefab.
/// Re-runnable: it removes what it made last time and rebuilds, so tweak and run again.
///
/// Physics are never touched: graybox colliders stay exactly as they are (their grey sprites are hidden),
/// and the terrain Tilemap is visual only (its tile colliders are switched off).
/// Coordinates are room-local (floor top y = -4.5, inside walls x -9..9).
/// </summary>
public static class Room1VisualPass
{
    private const string RoomPrefab = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room01.prefab";
    private const string RuinTexture = "Assets/_Project/Art/Recolors/Ground_RuinStone.png";
    private const string RuinTiles = "Assets/_Project/Art/Recolors/Palettes/Ground_RuinStone";
    private const string ForestTexture = "Assets/_Project/Art/Recolors/Props_Forest.png";
    private const string InteractiveTexture = "Assets/_Project/Art/Recolors/Props_Interactive.png";
    private const string VillageTexture = "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Village Props.png";
    private const string VisualTag = "_Visual"; // every object this tool creates ends with this

    // Ground tile numbers (identified by edge analysis of TX Tileset Ground).
    private const int TL = 0, T = 1, TR = 2, Le = 3, F = 4, R = 5, BL = 6, B = 7, BR = 8;
    private const int StripL = 157, StripM = 158, StripR = 164;

    private static Dictionary<string, Sprite> ruin, forest, interactive, village;

    [MenuItem("Tools/Shards of Motion/Room 1/Apply Visual Pass")]
    public static void Apply()
    {
        ruin = Sprites(RuinTexture);
        forest = Sprites(ForestTexture);
        interactive = Sprites(InteractiveTexture);
        village = Sprites(VillageTexture);
        if (ruin.Count == 0 || forest.Count == 0 || interactive.Count == 0)
        {
            Debug.LogError("Room 1 Visual Pass: run Tools > Shards of Motion > Recolor > Build All first.");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(RoomPrefab);
        try
        {
            // Check everything we dress exists before changing anything (clear message instead of a crash).
            string[] required = { "Layout", "Grid", "TM_Terrain", "TM_BackWall", "Floor", "Wall_Left", "Perch", "Niche_Roof",
                "Ledge_Heavy", "Gate_Lintel", "Float_Ledge", "Lift", "Gate_Exit", "Plate_Lift_25N", "Plate_Key_45N",
                "Plate_Heavy_80N", "Relay_Exit", "Shrine_Room01", "Journal_Room01", "Journal_Float" };
            string[] missing = required.Where(n => Find(root.transform, n) == null).ToArray();
            if (missing.Length > 0)
            {
                Debug.LogError("Room 1 Visual Pass: R1_Room01 is missing " + string.Join(", ", missing) + ". Nothing was changed.");
                return;
            }

            Clear(root.transform);
            Build(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, RoomPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        DressDoorway();
        Debug.Log("Room 1 Visual Pass applied to " + RoomPrefab + " (and the Doorway_Side prefab). Open the Region 1 scene to see it.");
    }

    // The doorway prefab's wall above the opening (2 x 6.5 units, from 4 m above the floor) in Ruin Stone.
    // Applies to every doorway in every scene, since they all use this prefab.
    private const string DoorwayPrefab = "Assets/_Project/Prefabs/Rooms/Doorway_Side.prefab";

    private static void DressDoorway()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(DoorwayPrefab);
        try
        {
            Transform wall = Find(root.transform, "Wall_Above");
            if (wall == null) { Debug.LogError("Room 1 Visual Pass: Doorway_Side has no Wall_Above."); return; }
            Clear(root.transform);
            if (wall.TryGetComponent(out SpriteRenderer sr)) sr.enabled = false;
            Bounds b = new Bounds(wall.localPosition, wall.localScale); // collider is the 1x1 box, scaled
            Block(root.transform, "Wall_Above", b.min.x, b.min.y, b.max.x, b.max.y, grounded: false);
            PrefabUtility.SaveAsPrefabAsset(root, DoorwayPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ------------------------------------------------------------------ build

    private static void Build(Transform room)
    {
        Transform layout = Find(room, "Layout");
        Transform visuals = NewChild(layout, "Visuals" + VisualTag);

        // --- Graybox sprites off (colliders untouched)
        foreach (string n in new[] { "Floor", "Wall_Left", "Perch", "Niche_Roof", "Ledge_Heavy", "Gate_Lintel", "Float_Ledge",
                                     "Lift", "Gate_Exit", "Plate_Lift_25N", "Plate_Key_45N", "Plate_Heavy_80N", "Shrine_Room01",
                                     "Journal_Room01", "Journal_Float" })
        {
            Transform t = Find(room, n);
            if (t != null && t.TryGetComponent(out SpriteRenderer sr)) sr.enabled = false;
        }

        // --- Terrain tilemaps (visual only). Grid offset +0.5 in y so cells line up with the floor top (-4.5).
        Transform grid = Find(layout, "Grid");
        grid.localPosition = new Vector3(0f, 0.5f, 0f);
        Tilemap terrain = Find(grid, "TM_Terrain").GetComponent<Tilemap>();
        foreach (Collider2D c in terrain.GetComponents<Collider2D>()) c.enabled = false; // tiles carry rounded colliders: not ours
        terrain.ClearAllTiles();
        for (int x = -9; x <= 9; x++) { terrain.SetTile(Cell(x, -6), Tile(T)); terrain.SetTile(Cell(x, -7), Tile(F)); }
        for (int y = -7; y <= 5; y++) terrain.SetTile(Cell(-10, y), Tile(y >= -5 ? R : F)); // left wall, face to the right

        // Back wall: a few broken fragments (not a solid wall), tinted a little darker and cooler so they sit
        // behind the play space. The background image provides the rest of the depth.
        Tilemap back = Find(grid, "TM_BackWall").GetComponent<Tilemap>();
        back.ClearAllTiles();
        back.color = new Color(0.62f, 0.68f, 0.76f);
        // Pillar at the start area (x -8..-7, 3 tiles tall).
        back.SetTile(Cell(-8, -3), Tile(T)); back.SetTile(Cell(-8, -4), Tile(F)); back.SetTile(Cell(-8, -5), Tile(F));
        // Low broken wall behind the shrine (x -3..-1, stepped).
        back.SetTile(Cell(-3, -4), Tile(TL)); back.SetTile(Cell(-3, -5), Tile(Le));
        back.SetTile(Cell(-2, -5), Tile(TR));
        // Stub by the doorway (x 7..8).
        back.SetTile(Cell(7, -5), Tile(T));

        // --- Ledges and walls as stretched sprites, sized exactly like the graybox.
        Block(visuals, "Ledge_Heavy", -1f, -4.5f, 2.5f, -2.5f, grounded: true);
        Block(visuals, "Gate_Lintel", 5f, 1.5f, 6f, 6f, grounded: false);
        Strip(visuals, "Perch", -9f, -4.5f, 1.0f, alignTop: true, leftCap: false, rightCap: true);
        Strip(visuals, "Niche_Roof", -9f, -7f, 2.7f, alignTop: true, leftCap: false, rightCap: true); // top is walkable
        Strip(visuals, "Float_Ledge", 3f, 4.5f, 0.5f, alignTop: true, leftCap: true, rightCap: true);

        // --- Interactive objects (visual children; they move with their objects)
        Transform lift = Find(room, "Lift");
        SpriteRenderer plank = Visual(lift, "Platform", village["TX Village Props - Elevator Platform"], "Interactables", 0);
        // The Elevator Platform sprite is 10 px tall, but only its bottom 7 rows are plank (the top 3 rows are
        // just the chain rings). Put the plank's top on the collider's top so players stand ON the wood.
        float liftTop = BoxEdgeY(lift, top: true);
        PlaceBottom(plank, new Vector2(lift.position.x, liftTop - 7f / 32f));
        foreach (float side in new[] { -0.85f, 0.85f })
        {
            // Chains run UP from the lift's top edge (past the top of the screen).
            SpriteRenderer chain = Visual(lift, "Chain", village["TX Village Props - Chain 01"], "Interactables", -1);
            Tiled(chain, new Vector2(3f / 32f, 11f));
            PlaceBottom(chain, new Vector2(lift.position.x + side, lift.position.y + 0.15f));
        }

        Transform gate = Find(room, "Gate_Exit");
        SpriteRenderer bars = Visual(gate, "Bars", interactive["TX Village Props - Ladder 01 M"], "Interactables", 0);
        Tiled(bars, new Vector2(1f, 6f));
        PlaceCenter(bars, gate.position); // floor to the gate's top, whatever the sprite's pivot

        foreach (string plateName in new[] { "Plate_Lift_25N", "Plate_Key_45N", "Plate_Heavy_80N" })
        {
            Transform plate = Find(room, plateName);
            SpriteRenderer slab = Visual(plate, "Slab", interactive["TX Village Props - Fence 01 Rail 01"], "Interactables", 0);
            Tiled(slab, new Vector2(1.5f, 9f / 32f));
            // The slab sits in a recess: drawn just behind the ground tiles, with its top 4 px above the floor.
            // PressurePlate sinks it up to 3 px as weight is added (visual only).
            slab.sortingLayerName = "BackDecor";
            slab.sortingOrder = 20;
            float floor = BoxEdgeY(plate, top: false);
            PlaceBottom(slab, new Vector2(plate.position.x, floor + 4f / 32f - 9f / 32f));
            SerializedObject p = new SerializedObject(plate.GetComponent<PressurePlate>());
            p.FindProperty("plateRenderer").objectReferenceValue = slab;
            p.FindProperty("slab").objectReferenceValue = slab.transform;
            p.FindProperty("releasedColor").colorValue = Color.white;
            p.FindProperty("pressedColor").colorValue = new Color(0.75f, 1f, 0.8f);
            p.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- Shrine (Stone of Recall + glow + cyan light) and Journal signs
        Transform shrine = Find(room, "Shrine_Room01");
        SpriteRenderer stone = Visual(shrine, "Stone", village["TX Village Props - Stone of Recall"], "Interactables", 0);
        PlaceBottom(stone, new Vector2(shrine.position.x, -4.5f));
        SpriteRenderer glow = Visual(shrine, "Glow", village["TX Village Props - Stone of Recall - Glow"], "Interactables", 1);
        glow.transform.position = stone.bounds.center;
        PointLight(shrine, "Light", stone.bounds.center, new Color(0.4f, 0.9f, 1f), 0.8f, 2.5f);

        // The start-area Journal goes by the left wall, so its prompt doesn't overlap Plate_Lift's readout.
        Transform startJournal = Find(room, "Journal_Room01");
        startJournal.position = new Vector3(-8.7f, startJournal.position.y, startJournal.position.z);

        foreach (string journalName in new[] { "Journal_Room01", "Journal_Float" })
        {
            Transform j = Find(room, journalName);
            SpriteRenderer sign = Visual(j, "Sign", village["TX Village Props - Sign 01"], "Props", 0);
            float groundY = journalName == "Journal_Float" ? 0.5f : -4.5f;
            PlaceBottom(sign, new Vector2(j.position.x, groundY));
        }

        // --- Decoration (sparse; nothing on puzzle paths)
        Transform decor = NewChild(visuals, "Decor" + VisualTag);
        Prop(decor, forest, "TX Village Props - Bush 01", -8.6f, -4.5f, "Props", -1);
        Prop(decor, forest, "TX Village Props - Bush 02", 8.5f, -4.5f, "Props", -1);
        Prop(decor, forest, "TX Village Props - Grass 02", -5.0f, -4.5f, "Props", 0);
        Prop(decor, forest, "TX Village Props - Grass 05", 0.3f, -2.5f, "Props", 0);   // on the crate ledge, left of the crate
        Prop(decor, forest, "TX Village Props - Grass 01", 4.2f, 0.5f, "Props", 0);    // float ledge
        Prop(decor, forest, "TX Village Props - Grass 04", -7.6f, 1.0f, "Props", 0);   // perch
        Hanging(decor, "TX Village Props - Grass 08", -6.3f, 0.5f);                    // under the perch
        Hanging(decor, "TX Village Props - Grass 07", 3.7f, 0.0f);                     // under the float ledge
        Prop(decor, village, "TX Village Props - Rock 02", 8.6f, -4.5f, "Props", 0);
        Prop(decor, village, "TX Village Props - Rock 03", -2.2f, -4.5f, "Props", 0);
        // Banner on its own post (Banner Pillar) near the exit, instead of hanging on a wall.
        SpriteRenderer post = Prop(decor, village, "TX Village Props - Banner Pillar", 6.9f, -4.5f, "Props", -2);
        SpriteRenderer banner = Visual(decor, "Banner", forest["TX Village Props - Banner"], "Props", -1, false);
        Bounds pb = post.bounds;
        PlaceBottom(banner, new Vector2(pb.center.x, pb.max.y - 0.35f - banner.sprite.bounds.size.y));

        // Lamp post between the heavy plate and the gate, warm light at the top.
        SpriteRenderer pillar = Prop(decor, village, "TX Village Props - Road Lamp Pillar", 4.55f, -4.5f, "Props", 0);
        Vector3 lampTop = new Vector3(pillar.bounds.center.x, pillar.bounds.max.y, 0f);
        SpriteRenderer lamp = Prop(decor, village, "TX Village Props - Road Lamp Light On", lampTop.x, lampTop.y - 0.3f, "Props", 1);
        PointLight(decor, "Lamp Light", lamp.bounds.center, new Color(1f, 0.78f, 0.5f), 1.1f, 5f);

        // Exit glow: a cyan light at the doorway that turns on while the exit gate is open.
        Light2D exitLight = PointLight(decor, "Exit Light", new Vector3(8f, -2.5f, 0f), new Color(0.35f, 0.9f, 1f), 1.2f, 3.5f);
        exitLight.enabled = false;
        SignalActivator activator = exitLight.gameObject.AddComponent<SignalActivator>();
        SerializedObject a = new SerializedObject(activator);
        a.FindProperty("signal").objectReferenceValue = Find(room, "Relay_Exit").GetComponent<PuzzleRelay>();
        SerializedProperty list = a.FindProperty("behaviours");
        list.arraySize = 1;
        list.GetArrayElementAtIndex(0).objectReferenceValue = exitLight;
        a.ApplyModifiedPropertiesWithoutUndo();
    }

    // A rectangular block in 3x3 tile pieces. "grounded" blocks use side tiles at the bottom (no rounded underside).
    private static void Block(Transform parent, string name, float xMin, float yMin, float xMax, float yMax, bool grounded)
    {
        Transform group = NewChild(parent, name + VisualTag);
        float w = xMax - xMin, h = yMax - yMin;
        bool narrow = w < 2f;
        (float a, float b, int col)[] cols = narrow
            ? new[] { (xMin, xMax, 1) }
            : new[] { (xMin, xMin + 1f, 0), (xMin + 1f, xMax - 1f, 1), (xMax - 1f, xMax, 2) };
        List<(float a, float b, int row)> rows = new List<(float, float, int)> { (yMax - 1f, yMax, 0) };
        if (h > 2f) rows.Add((yMin + 1f, yMax - 1f, 1));
        rows.Add((yMin, yMin + 1f, grounded ? 1 : 2));

        int[,] pieces = { { TL, T, TR }, { Le, F, R }, { BL, B, BR } };
        foreach ((float x0, float x1, int col) in cols)
            foreach ((float y0, float y1, int row) in rows)
            {
                if (x1 - x0 <= 0f || y1 - y0 <= 0f) continue;
                SpriteRenderer sr = Visual(group, "Piece", Ground(pieces[row, col]), "Midground", 0, false);
                Tiled(sr, new Vector2(x1 - x0, y1 - y0));
                PlaceCenter(sr, new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f));
            }
    }

    // A thin platform from the 1-tile strip pieces, aligned to the collider's top (floors) or bottom (ceilings).
    private static void Strip(Transform parent, string name, float xMin, float xMax, float edgeY, bool alignTop, bool leftCap, bool rightCap)
    {
        Transform group = NewChild(parent, name + VisualTag);
        float y = alignTop ? edgeY - 0.5f : edgeY + 0.5f;
        float midA = leftCap ? xMin + 1f : xMin, midB = rightCap ? xMax - 1f : xMax;
        if (midB > midA)
        {
            SpriteRenderer mid = Visual(group, "Middle", Ground(StripM), "Midground", 0, false);
            Tiled(mid, new Vector2(midB - midA, 1f));
            PlaceCenter(mid, new Vector2((midA + midB) * 0.5f, y));
        }
        if (leftCap) Visual(group, "Left", Ground(StripL), "Midground", 0, false).transform.position = new Vector3(xMin + 0.5f, y, 0f);
        if (rightCap) Visual(group, "Right", Ground(StripR), "Midground", 1, false).transform.position = new Vector3(xMax - 0.5f, y, 0f);
    }

    private static SpriteRenderer Prop(Transform parent, Dictionary<string, Sprite> sheet, string sprite, float x, float groundY, string layer, int order)
    {
        SpriteRenderer sr = Visual(parent, sprite.Replace("TX Village Props - ", ""), sheet[sprite], layer, order, false);
        PlaceBottom(sr, new Vector2(x, groundY));
        return sr;
    }

    // Grass tuft flipped upside down under a ledge: reads as hanging moss.
    private static void Hanging(Transform parent, string sprite, float x, float ceilingY)
    {
        SpriteRenderer sr = Visual(parent, "Hanging " + sprite.Replace("TX Village Props - ", ""), forest[sprite], "Props", 1, false);
        sr.flipY = true;
        // Flipping mirrors the sprite around its pivot: its top ends up at (extents.y - center.y) above the pivot.
        Bounds b = sr.sprite.bounds;
        sr.transform.position = new Vector3(x - b.center.x, ceilingY + b.center.y - b.extents.y, 0f);
    }

    private static Light2D PointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float radius)
    {
        GameObject go = new GameObject(name + VisualTag);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        Light2D l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.pointLightOuterRadius = radius;
        l.pointLightInnerRadius = radius * 0.2f;
        return l;
    }

    // ------------------------------------------------------------------ helpers

    // A sprite child. counterScale undoes the parent's scale so the art shows at its true pixel size.
    private static SpriteRenderer Visual(Transform parent, string name, Sprite sprite, string layer, int order, bool counterScale = true)
    {
        GameObject go = new GameObject(name + VisualTag);
        go.transform.SetParent(parent, false);
        if (counterScale)
        {
            Vector3 s = parent.lossyScale;
            go.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f);
        }
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return sr;
    }

    private static void Tiled(SpriteRenderer sr, Vector2 size)
    {
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;
    }

    // The sprite's drawn area relative to its transform, using the sprite's real pivot
    // (Village sprites often pivot at the bottom or top, not the centre; Tiled sprites stretch from the pivot).
    private static Bounds LocalBounds(SpriteRenderer sr)
    {
        if (sr.drawMode == SpriteDrawMode.Simple) return sr.sprite.bounds;
        Vector2 pivot01 = new Vector2(sr.sprite.pivot.x / sr.sprite.rect.width, sr.sprite.pivot.y / sr.sprite.rect.height);
        Vector2 center = new Vector2((0.5f - pivot01.x) * sr.size.x, (0.5f - pivot01.y) * sr.size.y);
        return new Bounds(center, sr.size);
    }

    // Put a sprite so its bottom edge sits on bottomCenter.y, centred on bottomCenter.x.
    private static void PlaceBottom(SpriteRenderer sr, Vector2 bottomCenter)
    {
        Bounds local = LocalBounds(sr);
        Vector3 scale = sr.transform.lossyScale;
        sr.transform.position = new Vector3(bottomCenter.x - local.center.x * scale.x,
                                            bottomCenter.y - (local.center.y - local.extents.y) * scale.y, 0f);
    }

    // Put a sprite so the centre of its drawn area is at center.
    private static void PlaceCenter(SpriteRenderer sr, Vector2 center)
    {
        Bounds local = LocalBounds(sr);
        Vector3 scale = sr.transform.lossyScale;
        sr.transform.position = new Vector3(center.x - local.center.x * scale.x, center.y - local.center.y * scale.y, 0f);
    }

    // World y of a BoxCollider2D's top or bottom edge (from its size/offset, so it works inside an edited prefab).
    private static float BoxEdgeY(Transform t, bool top)
    {
        BoxCollider2D box = t.GetComponent<BoxCollider2D>();
        if (box == null) return t.position.y;
        Vector2 local = box.offset + new Vector2(0f, (top ? 0.5f : -0.5f) * box.size.y);
        return t.TransformPoint(local).y;
    }

    private static void Clear(Transform room)
    {
        foreach (Transform t in room.GetComponentsInChildren<Transform>(true).Where(t => t != null && t.name.EndsWith(VisualTag)).ToArray())
            if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    private static Transform NewChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static Transform Find(Transform root, string name) =>
        root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

    private static Vector3Int Cell(int x, int y) => new Vector3Int(x, y, 0);
    private static Sprite Ground(int n) => ruin[$"TX Tileset Ground_{n}"];
    private static TileBase Tile(int n) => AssetDatabase.LoadAssetAtPath<TileBase>($"{RuinTiles}/TX Tileset Ground_{n}.asset");

    private static Dictionary<string, Sprite> Sprites(string path)
    {
        Dictionary<string, Sprite> d = new Dictionary<string, Sprite>();
        foreach (Sprite s in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
            if (!d.ContainsKey(s.name)) d.Add(s.name, s);
        return d;
    }
}
