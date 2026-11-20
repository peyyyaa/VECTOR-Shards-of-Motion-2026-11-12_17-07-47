using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the Room 2 graybox ("Weight of Things") inside Prefabs/Rooms/Region1/R1_Room02.prefab.
/// Re-running it removes what it built before and builds again (other objects in the room are kept).
///
/// Puzzle: three unmarked crates (1, 2, 3 kg; mass labels off) and three LOCKED pedestals (25 / 55 / 85 N) that only
/// show the weight they need and light up when it's reached, never the weight on them. One TEST SCALE by the entrance
/// reads any weight. The exit gate opens while all three pedestals are pressed. Each crate can only be used once, so the
/// only solution is 1 kg -> 25 N, 2 kg -> 55 N, 3 kg -> 85 N. Pedestals ignore players (cargo scales), so a player
/// (29.4 N) can't stand in for the 1 kg crate. Players tell the crates apart by weighing or pushing:
///   friction mu*m*g with mu = 1/3, g = 29.43:  1 kg 9.8 N, 2 kg 19.6 N, 3 kg 29.4 N
///   one push 12 N moves only 1 kg; two pushes 24 N move 1 and 2 kg; 3 kg needs the Kinetic Burst.
/// Roles:
///   P2 - the 1 kg crate sits in a low niche above the left doorway (1.2 m clearance: no player fits). From the
///        high step, 3.7 m away, the Wind Tether pulls it out (24 N vs 9.8 N friction); the Burst can't reach.
///   both - the 2 kg crate starts up on the low step: climb up together and push it off (24 N vs 19.6 N).
///   P1 - the 3 kg crate starts 5 m from its pedestal: each burst gives dv = 15/3 = 5 m/s and a slide of
///        v^2/(2 mu g) = 1.27 m, so it takes 4 bursts. A 5th sends it into the acid pit just past the pedestal,
///        and it returns to its start. A burst on the 1 kg crate (15 m/s, ~11.5 m) also tends to end in the pit.
///
/// Room-local coordinates: floor top y = -4.5, ceiling y = 5.625, doorways at x = -10 and 10 (opening up to y = -0.5).
/// Parts are cloned from Room 1's graybox objects, so they keep the same layers, colliders and settings.
/// </summary>
public static class Room2Builder
{
    private const string Room1Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room01.prefab";
    private const string Room2Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room02.prefab";
    private const string Interactables = "Assets/_Project/Prefabs/Interactables";

    private static readonly string[] Built =
    {
        "Niche_Shelf", "Niche_Roof", "Step_Low", "Step_High", "Gate_Lintel", "Gate_Exit", "Relay_Exit",
        "Scale_25N", "Scale_55N", "Scale_85N", "Crate_A_1kg", "Crate_B_2kg", "Crate_C_3kg",
        "Floor_Right", "Pit_Acid", "Pit_Bottom", "Test_Scale",
        "Shrine_Room02", "Journal_Room02", "Shard_Roof", "Shard_Air", "Shard_Corner",
    };

    [MenuItem("Tools/Shards of Motion/Room 2/1. Build Graybox")]
    public static void Build()
    {
        GameObject r1 = AssetDatabase.LoadAssetAtPath<GameObject>(Room1Path);
        if (r1 == null) { Debug.LogError("Room 2: R1_Room01 not found (its graybox parts are used as templates)."); return; }
        Dictionary<string, Transform> t = new Dictionary<string, Transform>();
        foreach (string n in new[] { "Ledge_Heavy", "Plate_Heavy_80N", "Gate_Exit", "Gate_Lintel", "Relay_Exit", "Shrine_Room01", "Journal_Room01" })
        {
            Transform found = Find(r1.transform, n);
            if (found == null) { Debug.LogError($"Room 2: template '{n}' not found in R1_Room01. Nothing was changed."); return; }
            t[n] = found;
        }
        GameObject[] blocks = { Load($"{Interactables}/Block_1kg.prefab"), Load($"{Interactables}/Block_2kg.prefab"), Load($"{Interactables}/Block_3kg.prefab") };
        GameObject shrinePrefab = Load($"{Interactables}/ResetShrine.prefab");
        GameObject journalPrefab = Load($"{Interactables}/JournalStone.prefab");
        GameObject shardPrefab = Load($"{Interactables}/MotionShard.prefab");
        if (blocks.Any(b => b == null) || shrinePrefab == null || journalPrefab == null || shardPrefab == null)
        {
            Debug.LogError("Room 2: a prefab in Prefabs/Interactables is missing (Block_1kg/2kg/3kg, ResetShrine, JournalStone, MotionShard). Nothing was changed.");
            return;
        }
        JournalEntry entry = JournalTool.SaveRoom2Entry();

        GameObject room = PrefabUtility.LoadPrefabContents(Room2Path);
        try
        {
            Transform layout = Find(room.transform, "Layout"), puzzle = Find(room.transform, "Puzzle"),
                      objects = Find(room.transform, "Objects"), info = Find(room.transform, "Info");
            if (layout == null || puzzle == null || objects == null || info == null)
            {
                Debug.LogError("Room 2: R1_Room02 is missing its Layout / Puzzle / Objects / Info groups. Nothing was changed.");
                return;
            }
            foreach (string n in Built)
            {
                Transform old = Find(room.transform, n);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            // ---- Layout (rectangles: left, bottom, right, top)
            Solid(t["Ledge_Heavy"], layout, "Niche_Shelf", -10f, -1f, -7.5f, -0.5f);   // above the left doorway (opening ends at -0.5)
            Solid(t["Ledge_Heavy"], layout, "Niche_Roof", -10f, 0.7f, -7.5f, 1.2f);     // 1.2 m gap: a crate fits, a player (1.5 m) doesn't
            Solid(t["Ledge_Heavy"], layout, "Step_Low", -3f, -2.9f, -0.5f, -2.5f);      // 2 m up; 1.6 m under it, so crates and players pass
            Solid(t["Ledge_Heavy"], layout, "Step_High", -4.75f, -1f, -3f, -0.5f);      // 2 m above Step_Low: P2's tether spot (level with the niche)
            Solid(t["Gate_Lintel"], layout, "Gate_Lintel", 8.5f, 1.5f, 9.5f, 5.625f);   // nothing goes over the gate

            // Acid pit (x 7.25..8.5) just past the 85 N pedestal: the room's floor is split around it.
            Transform floor = Find(layout, "Floor");
            if (floor == null) { Debug.LogError("Room 2: Layout/Floor not found. Nothing was changed."); return; }
            GameObject floorRight = Object.Instantiate(floor.gameObject, layout);
            floorRight.name = "Floor_Right";
            SetRect(floor, -10f, -5.5f, 7.25f, -4.5f);
            SetRect(floorRight.transform, 8.5f, -5.5f, 10f, -4.5f);
            Solid(t["Ledge_Heavy"], layout, "Pit_Bottom", 7.25f, -7f, 8.5f, -6.5f);    // safety net below the acid
            Transform hazards = Find(room.transform, "Hazards") ?? puzzle;
            GameObject acid = Clone(t["Ledge_Heavy"], hazards, "Pit_Acid");
            acid.layer = 0; // Default: collides with players and blocks (trigger messages need that)
            SetRect(acid.transform, 7.25f, -6.5f, 8.5f, -5f);
            acid.GetComponent<BoxCollider2D>().isTrigger = true;
            SpriteRenderer acidSr = acid.GetComponent<SpriteRenderer>();
            acidSr.color = new Color(0.45f, 0.95f, 0.25f, 0.85f);
            acidSr.sortingLayerName = "Water";
            acid.AddComponent<HazardZone>(); // players respawn, crates return to where they started

            // ---- Puzzle: three locked pedestals (target only, ignore players) + one test scale (reading only)
            PressurePlate s25 = Scale(t["Plate_Heavy_80N"], puzzle, "Scale_25N", -0.25f, 25f, PlateReadout.Display.TargetOnly, false);
            PressurePlate s55 = Scale(t["Plate_Heavy_80N"], puzzle, "Scale_55N", 2.75f, 55f, PlateReadout.Display.TargetOnly, false);
            PressurePlate s85 = Scale(t["Plate_Heavy_80N"], puzzle, "Scale_85N", 6.25f, 85f, PlateReadout.Display.TargetOnly, false);
            Scale(t["Plate_Heavy_80N"], puzzle, "Test_Scale", -6.25f, 9999f, PlateReadout.Display.ReadingOnly, true); // weighs anything, never "pressed"

            GameObject relay = Clone(t["Relay_Exit"], puzzle, "Relay_Exit");
            relay.transform.localPosition = new Vector3(8f, 1f, 0f);
            PuzzleRelay relayComp = relay.GetComponent<PuzzleRelay>();
            SerializedObject r = new SerializedObject(relayComp);
            SerializedProperty inputs = r.FindProperty("inputs");
            inputs.arraySize = 3;
            inputs.GetArrayElementAtIndex(0).objectReferenceValue = s25;
            inputs.GetArrayElementAtIndex(1).objectReferenceValue = s55;
            inputs.GetArrayElementAtIndex(2).objectReferenceValue = s85;
            r.FindProperty("mode").enumValueIndex = 0;  // All
            r.FindProperty("latch").boolValue = false;  // closes again if a crate is taken off
            r.ApplyModifiedPropertiesWithoutUndo();

            GameObject gate = Clone(t["Gate_Exit"], puzzle, "Gate_Exit");
            gate.transform.localPosition = new Vector3(9f, -1.5f, 0f); // x 8.5..9.5, floor to y 1.5: blocks the right doorway
            SerializedObject g = new SerializedObject(gate.GetComponent<Mover>());
            g.FindProperty("signal").objectReferenceValue = relayComp;
            g.ApplyModifiedPropertiesWithoutUndo();

            // ---- Objects: three unmarked crates (mass labels off in this room: weigh them!)
            Crate(blocks[0], objects, "Crate_A_1kg", -8f, 0f);     // in the niche, front face flush with the opening
            Crate(blocks[1], objects, "Crate_B_2kg", -1f, -2f);    // up on the low step's right end: both climb up and push it off
            Crate(blocks[2], objects, "Crate_C_3kg", 1.25f, -4f);  // 5 m left of the 85 N pedestal: 4 bursts of ~1.27 m; a 5th reaches the acid

            GameObject shrine = (GameObject)PrefabUtility.InstantiatePrefab(shrinePrefab, info);
            shrine.name = "Shrine_Room02";
            shrine.transform.localPosition = new Vector3(-9.2f, t["Shrine_Room01"].localPosition.y, 0f);

            GameObject journal = (GameObject)PrefabUtility.InstantiatePrefab(journalPrefab, info);
            journal.name = "Journal_Room02";
            journal.transform.localPosition = new Vector3(-5.2f, t["Journal_Room01"].localPosition.y, 0f);
            JournalStone stone = journal.GetComponent<JournalStone>();
            if (stone != null) { stone.EditorSetEntry(entry); EditorUtility.SetDirty(stone); }

            foreach ((string name, Vector2 pos) in new[]
            {
                ("Shard_Roof", new Vector2(-8.75f, 2f)),    // on the niche roof: jump + float over from the high step
                ("Shard_Air", new Vector2(2.5f, 1.5f)),     // mid-air above the scales: jump from the high step and float right
                ("Shard_Corner", new Vector2(-8.2f, 4f)),   // explorer: jump up from the niche roof
            })
            {
                GameObject s = (GameObject)PrefabUtility.InstantiatePrefab(shardPrefab, objects);
                s.name = name;
                s.transform.localPosition = pos;
            }

            PrefabUtility.SaveAsPrefabAsset(room, Room2Path);
            Debug.Log("Room 2: graybox built in R1_Room02 (Weight of Things). Open Region1_ShatteredGrounds and play from Room 2.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(room);
        }
    }

    // ------------------------------------------------------------------ helpers

    // A copy of a Room 1 graybox object without its art children (art is added later by the dressing pass).
    private static GameObject Clone(Transform template, Transform parent, string name)
    {
        GameObject go = Object.Instantiate(template.gameObject, parent);
        go.name = name;
        for (int i = go.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true; // graybox shape visible again (Room 1's art pass hid it)
        return go;
    }

    private static void Solid(Transform template, Transform parent, string name, float left, float bottom, float right, float top)
    {
        GameObject go = Clone(template, parent, name);
        go.transform.localPosition = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0f);
        go.transform.localScale = new Vector3(right - left, top - bottom, 1f);
    }

    private static void SetRect(Transform tr, float left, float bottom, float right, float top)
    {
        tr.localPosition = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0f);
        tr.localScale = new Vector3(right - left, top - bottom, 1f);
    }

    private static PressurePlate Scale(Transform template, Transform parent, string name, float x, float newtons,
        PlateReadout.Display display, bool countPlayers)
    {
        GameObject go = Clone(template, parent, name);
        go.transform.localPosition = new Vector3(x, -4.45f, 0f); // 1.5 x 0.1 trigger resting on the floor
        PressurePlate plate = go.GetComponent<PressurePlate>();
        SerializedObject p = new SerializedObject(plate);
        p.FindProperty("requiredWeight").floatValue = newtons;
        p.FindProperty("plateRenderer").objectReferenceValue = go.GetComponent<SpriteRenderer>();
        p.FindProperty("releasedColor").colorValue = new Color(0.6f, 0.6f, 0.2f);
        p.FindProperty("pressedColor").colorValue = new Color(0.2f, 0.9f, 0.3f);
        p.FindProperty("slab").objectReferenceValue = null; // the sinking slab comes with the art pass
        p.FindProperty("countPlayers").boolValue = countPlayers; // pedestals: off, so a player can't stand in for the 1 kg crate
        p.ApplyModifiedPropertiesWithoutUndo();
        PlateReadout readout = go.GetComponent<PlateReadout>();
        if (readout != null)
        {
            SerializedObject ro = new SerializedObject(readout);
            ro.FindProperty("display").enumValueIndex = (int)display;
            ro.ApplyModifiedPropertiesWithoutUndo();
        }
        return plate;
    }

    private static void Crate(GameObject prefab, Transform parent, string name, float x, float y)
    {
        GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        c.name = name;
        c.transform.localPosition = new Vector3(x, y, 0f);
        MassLabel label = c.GetComponent<MassLabel>();
        if (label != null) label.enabled = false; // unmarked: the scales tell them apart
    }

    private static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

    private static Transform Find(Transform root, string name) =>
        root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name);
}
