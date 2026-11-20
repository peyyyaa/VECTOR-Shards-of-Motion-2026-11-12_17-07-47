using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static RoomBuildKit;

/// <summary>
/// Builds the Room 5 graybox ("Long Push": friction) inside Prefabs/Rooms/Region1/R1_Room05.prefab. Re-runnable.
///
/// Layout (room-local; floor top y = -4.5). Three floors, each its own surface (an EdgeCollider2D on Ground, linked to its
/// neighbours with adjacent points so a sliding crate doesn't snag on the joins; the room's grey Floor box is lowered 2 cm
/// underneath as a safety net and no longer touches anything):
///   stone x -10..-7 (entry) | MOSS x -7..-4.5 (Moss: mu 0.6, combine Maximum), all of it the exit plate (80 N) |
///   WET STONE x -4.5..4 (Wet_Stone: mu 0.05, combine Minimum) | stone x 4..10 (alcove, exit).
///   Culvert: a low slab over x -3.5..1.5 (inside 1.3 m tall): crates slide under, players (1.5 m) don't fit and climb
///   over its roof (top y -2.5, 2 m: an easy jump).
///   Alcove block x 4..6.6 (top y -2.3): an alcove 1.6 m deep and 1.8 m tall opens west; the 2 kg crate sits against its back
///   wall, with only 0.6 m in front of it (a player is 0.8 m wide), so nobody can get behind it. Players arriving from
///   Room 6 land at x 7-7.8, on open floor (keep the block west of that). Gate to Room 6 at x 9..9.6.
///   A fallen column (x -9.7..-9.1, 0.65 m) by the west doorway stops an overshooting crate before it slides into Room 4.
/// Physics (crate materials combine Minimum, moss's Maximum wins over it; friction slows any crate at mu*g):
///   2 kg: friction stone 19.6 N, wet stone 2.9 N, moss 35.3 N; Burst dv = 7.5 m/s slides 2.9 / 19 / 1.6 m.
///   1 kg: Burst dv = 15 m/s (Room 4: dv = J/m) -> four times the distance (moss 6.4 m): it crosses the 2.5 m moss bed and hits
///   the column. Walked along and let go at the culvert (~6 m/s) it glides through and stops ~0.5 m into the moss.
/// Solution: the plate needs 80 N = both crates (58.9 + 29.4 = 88 N; one crate + a player also reaches it, but then that
///   player can't leave). 1) Push the 1 kg crate (it also blocks the tether's line of sight) at a run and let it glide through
///   the culvert. 2) P2 tethers the 2 kg crate out of the alcove (24 N > 19.6 N). 3) P1 bursts it west; the moss stops it.
///   Recovery: an overshooting 1 kg crate rests against the column; P2 tethers it back onto the moss (24 N > 17.7 N).
/// Extras: machine part 3 (Bearing) above the 2 kg crate at the back of the alcove (reachable once it's out); 3 shards
///   (above the culvert roof; on the west ledge x -8.4..-7, 3.6 m up, 3.5 m from the roof's west end; above the alcove block).
/// Keep things out of x -10..-9 up to 4 m (the Room 4 doorway's exit trigger) and above 4 m there (its wall); the column is
/// low enough to hop and only stops crates.
/// </summary>
public static class Room5Builder
{
    private const string Room5Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room05.prefab";
    private const string FxLibraryPath = "Assets/_Project/Art/FX/FX_Library.asset";
    private const string MossPath = "Assets/_Project/PhysicsMaterials/Moss.physicsMaterial2D";
    private const string WetPath = "Assets/_Project/PhysicsMaterials/Wet_Stone.physicsMaterial2D";
    private const string BearingSpritePath = "Assets/_Project/Art/Sprites/Props/SP_Part_Bearing.png";
    private const float Floor = -4.5f;

    // Surface boundaries (west to east).
    private const float MossLeft = -7f, WetLeft = -4.5f, StoneEastLeft = 4f;
    // Alcove block: hollow x 4..5.6 (1.6 m deep), back wall x 5.6..6.6. Must stay west of Room 6's arrival spots (x 7, 7.8).
    public const float AlcoveLeft = StoneEastLeft, AlcoveBack = 5.6f, AlcoveRight = 6.6f;

    private static readonly string[] Built =
    {
        "Surface_Stone_West", "Surface_Moss", "Surface_Wet", "Surface_Stone_East", "Strip_Moss", "Strip_Wet",
        "Culvert", "Alcove_Roof", "Alcove_Back", "Ledge_West", "Column_Fallen", "Gate_Exit", "Plate_Moss_45N", "Plate_Moss_80N",
        "Crate_2kg", "Crate_1kg",
        "Shrine_Room05", "Journal_Room05", "Shard_Roof", "Shard_Ledge", "Shard_Alcove", "Part_Bearing",
    };

    [MenuItem("Tools/Shards of Motion/Room 5/1. Build Graybox")]
    public static void Build()
    {
        Transform ledge = Template("Ledge_Heavy"), plateT = Template("Plate_Heavy_80N"), gateT = Template("Gate_Exit"),
                  shrineT = Template("Shrine_Room01"), journalT = Template("Journal_Room01");
        if (ledge == null || plateT == null || gateT == null || shrineT == null || journalT == null) return;
        GameObject b1 = Load($"{Interactables}/Block_1kg.prefab"), b2 = Load($"{Interactables}/Block_2kg.prefab"), shrineP = Load($"{Interactables}/ResetShrine.prefab"),
                   journalP = Load($"{Interactables}/JournalStone.prefab"), shardP = Load($"{Interactables}/MotionShard.prefab");
        PhysicsMaterial2D moss = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(MossPath), wet = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(WetPath);
        if (b1 == null || b2 == null || shrineP == null || journalP == null || shardP == null || moss == null || wet == null)
        {
            Debug.LogError("Room 5: a prefab in Prefabs/Interactables or a physics material (Moss, Wet_Stone) is missing. Nothing was changed.");
            return;
        }
        FxLibrary fx = AssetDatabase.LoadAssetAtPath<FxLibrary>(FxLibraryPath);
        Sprite bearing = BearingSprite();
        JournalEntry entry = JournalTool.SaveRoom5Entry();

        GameObject room = PrefabUtility.LoadPrefabContents(Room5Path);
        try
        {
            Transform layout = Find(room.transform, "Layout"), puzzle = Find(room.transform, "Puzzle"),
                      objects = Find(room.transform, "Objects"), info = Find(room.transform, "Info");
            if (layout == null || puzzle == null || objects == null || info == null)
            {
                Debug.LogError("Room 5: R1_Room05 is missing Layout, Puzzle, Objects or Info. Nothing was changed.");
                return;
            }
            RemoveBuilt(room.transform, Built);

            // ---- Floors: the grey box drops 2 cm (safety net only); the surfaces carry the friction.
            Transform floorBox = Find(room.transform, "Floor");
            if (floorBox != null) SetRect(floorBox, -10f, Floor - 1.02f, 10f, Floor - 0.02f);
            float[] x = { -10f, MossLeft, WetLeft, StoneEastLeft, 10f };
            string[] names = { "Surface_Stone_West", "Surface_Moss", "Surface_Wet", "Surface_Stone_East" };
            PhysicsMaterial2D[] mats = { null, moss, wet, null };
            for (int i = 0; i < names.Length; i++)
                Surface(layout, names[i], x[i], x[i + 1], mats[i], i > 0 ? x[i - 1] : (float?)null, i + 2 < x.Length ? x[i + 2] : (float?)null);
            Strip(ledge, layout, "Strip_Moss", MossLeft, WetLeft, new Color(0.35f, 0.6f, 0.25f));
            Strip(ledge, layout, "Strip_Wet", WetLeft, StoneEastLeft, new Color(0.45f, 0.62f, 0.8f));

            // ---- Culvert, alcove block, shard ledge
            Solid(ledge, layout, "Culvert", -3.5f, Floor + 1.3f, 1.5f, Floor + 2f);
            Solid(ledge, layout, "Alcove_Roof", AlcoveLeft, Floor + 1.8f, AlcoveRight, Floor + 2.2f);
            Solid(ledge, layout, "Alcove_Back", AlcoveBack, Floor, AlcoveRight, Floor + 1.8f);
            Solid(ledge, layout, "Column_Fallen", -9.7f, Floor, -9.1f, Floor + 0.65f); // stops crates, not people (hop it)
            Solid(ledge, layout, "Ledge_West", -8.4f, Floor + 3.2f, -7f, Floor + 3.6f); // 3.6 m: from the culvert roof only.
            // Keep it east of x -9: the Room 4 doorway's wall reaches 1 m into the room from 4 m up, and its exit trigger
            // fills x -10..-9 up to 4 m.
            SetEntries(room.transform, new Vector2(-8.6f, Floor + 0.75f), new Vector2(-7.8f, Floor + 0.75f)); // on the entry stone

            // ---- Puzzle: the moss plate opens the exit gate
            // The whole moss bed is the plate (2.5 m): both crates must rest on it (80 N; 2 kg + 1 kg = 88 N).
            PressurePlate plate = Plate(plateT, puzzle, "Plate_Moss_80N", (MossLeft + WetLeft) * 0.5f, Floor, 80f, PlateReadout.Display.TargetOnly, true,
                                        WetLeft - MossLeft);
            Platform(gateT, puzzle, "Gate_Exit", 9f, Floor, 9.6f, Floor + 4.2f, new Vector2(0f, 4.3f), 4f, plate, false);

            // ---- Objects
            Crate(b2, objects, "Crate_2kg", AlcoveBack - 0.5f, Floor + 0.5f); // against the alcove's back wall
            Crate(b1, objects, "Crate_1kg", 2.7f, Floor + 0.5f);                // on the wet stone, between the culvert and the alcove
            float shrineLift = shrineT.localPosition.y - Floor, journalLift = journalT.localPosition.y - Floor;
            Instance(shrineP, info, "Shrine_Room05", new Vector2(8.3f, Floor + shrineLift)); // by the exit, clear of the crates' path
            GameObject journal = Instance(journalP, info, "Journal_Room05", new Vector2(-8.3f, Floor + journalLift));
            if (journal.TryGetComponent(out JournalStone stone)) { stone.EditorSetEntry(entry); EditorUtility.SetDirty(stone); }

            Instance(shardP, objects, "Shard_Roof", new Vector2(-1f, -0.3f));     // jump from the culvert roof (top -2.5)
            Instance(shardP, objects, "Shard_Ledge", new Vector2(-7.7f, -0.3f));  // on the west ledge: jump (+ float) from the roof's west end
            Instance(shardP, objects, "Shard_Alcove", new Vector2(AlcoveBack - 0.3f, 0f)); // jump from the alcove block (top -2.3)
            MachinePart(objects, shardP.layer, "Part_Bearing", new Vector2(AlcoveBack - 0.5f, Floor + 1.4f), "R1_Part_Bearing",
                        "Bearing (machine part 3)", bearing, fx);

            PrefabUtility.SaveAsPrefabAsset(room, Room5Path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(room);
        }
        Debug.Log("Room 5: graybox built in R1_Room05 (Long Push). Play it from Dev > Play From Room > Room 5.");
    }

    // A floor surface: an edge along the floor top, on Ground, with its material. Adjacent points continue the line into the
    // neighbouring surfaces, so a crate sliding from one to the next doesn't catch on the join.
    private static void Surface(Transform parent, string name, float left, float right, PhysicsMaterial2D material, float? prevLeft, float? nextRight)
    {
        GameObject go = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        go.transform.SetParent(parent, false);
        EdgeCollider2D edge = go.AddComponent<EdgeCollider2D>();
        edge.points = new[] { new Vector2(left, Floor), new Vector2(right, Floor) };
        edge.sharedMaterial = material;
        if (prevLeft.HasValue) { edge.useAdjacentStartPoint = true; edge.adjacentStartPoint = new Vector2(prevLeft.Value, Floor); }
        if (nextRight.HasValue) { edge.useAdjacentEndPoint = true; edge.adjacentEndPoint = new Vector2(nextRight.Value, Floor); }
    }

    // Graybox colour band on top of the floor (sprite only, no collider) showing where a surface is.
    private static void Strip(Transform ledge, Transform parent, string name, float left, float right, Color color)
    {
        GameObject go = Clone(ledge, parent, name);
        foreach (Collider2D c in go.GetComponents<Collider2D>()) Object.DestroyImmediate(c);
        SetRect(go.transform, left, Floor - 0.25f, right, Floor);
        if (go.TryGetComponent(out SpriteRenderer sr)) { sr.color = color; sr.sortingOrder += 1; }
    }

    private static void MachinePart(Transform parent, int layer, string name, Vector2 pos, string id, string displayName, Sprite sprite, FxLibrary fx)
    {
        GameObject go = new GameObject(name) { layer = layer };
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Interactables";
        sr.sortingOrder = 5;
        CircleCollider2D c = go.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = 0.3f; // stays out of reach from the alcove mouth while the crate is inside
        go.AddComponent<QuestItem>().EditorSetup(id, displayName, fx);
        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(go.transform, false);
        Light2D light = glow.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(1f, 0.84f, 0.47f);
        light.intensity = 0.7f;
        light.pointLightOuterRadius = 1.4f;
        light.pointLightInnerRadius = 0.2f;
    }

    /// <summary>The Bearing (machine part 3): a 16 x 16 steel ring with balls inside, drawn here (32 PPU, Point).</summary>
    public static Sprite BearingSprite()
    {
        const int n = 16;
        Color32 clear = new Color32(0, 0, 0, 0), outline = new Color32(38, 42, 52, 255), steel = new Color32(150, 160, 172, 255),
                light = new Color32(214, 222, 230, 255), dark = new Color32(96, 104, 116, 255), ball = new Color32(232, 214, 160, 255);
        Color32[] px = new Color32[n * n];
        Vector2 c = new Vector2(7.5f, 7.5f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                Color32 col = clear;
                if (d <= 7.6f) col = outline;
                if (d <= 6.7f) col = y > 8 ? light : y < 6 ? dark : steel;   // outer race, lit from above
                if (d <= 5.4f) col = outline;
                if (d <= 4.6f) col = new Color32(70, 60, 50, 255);            // the gap where the balls run
                if (d <= 2.9f) col = outline;
                if (d <= 2.2f) col = steel;                                   // inner race
                if (d <= 1.1f) col = clear;                                   // the shaft hole
                px[y * n + x] = col;
            }
        for (int i = 0; i < 6; i++) // six balls between the races
        {
            float a = i * Mathf.PI / 3f + 0.3f;
            int bx = Mathf.RoundToInt(c.x + Mathf.Cos(a) * 3.7f), by = Mathf.RoundToInt(c.y + Mathf.Sin(a) * 3.7f);
            px[by * n + bx] = ball;
            if (bx + 1 < n) px[by * n + bx + 1] = ball;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(BearingSpritePath));
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        File.WriteAllBytes(BearingSpritePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(BearingSpritePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(BearingSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(BearingSpritePath);
    }
}
