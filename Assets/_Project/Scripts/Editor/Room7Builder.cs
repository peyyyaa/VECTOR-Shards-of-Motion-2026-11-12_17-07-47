using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static RoomBuildKit;

/// <summary>
/// Builds the Room 7 graybox ("Unstoppable Object": momentum and braking) inside Prefabs/Rooms/Region1/R1_Room07.prefab.
/// Re-runnable. Players arrive on the doorway landing at 6 m (west, from Room 6); the exit to Room 8 is on the floor (east).
///
/// Layout (room-local; floor top y = -4.5):
///   landing x -10..-6.5 (6 m, the doorway's) | MID SHELF x -6.5..3 (3 m, solid to the floor) topped with WET STONE
///   (mu 0.05; an edge collider, the block 2 cm lower) | a CULVERT over x -4.5..2 (inside 1.3 m: crates fit, players don't;
///   its roof is 4.6 m up) with the BRAKE PLATE inside (x -0.6..0.8) and the STOP BLOCK slot at its east edge (x 0.85..1.15) |
///   ACID PIT x 3..6 with a stepping pillar (x 4.2..4.8, 2 m) | east floor x 6..10, exit gate x 9..9.6.
/// Physics (3 kg ram on wet stone: friction 4.4 N, slows 1.47 m/s each second):
///   P1's Burst at the west end: p = 15 kg·m/s, v = 5 m/s. It reaches the stop block at ~3 m/s (~1.3 s); left alone it slides
///   ~8.5 m in all, off the shelf's end and into the acid (crates in acid go back to their start).
/// The chain:
///   1. Launch: P1 Bursts the ram into the culvert.
///   2. Catch: P2, on the culvert roof by the lever, raises the stop block for ~1.5 s (timed lever + SignalTimer, lamps blink).
///      Up in time: the ram slams into it and stops on the plate (J = F·dt: a huge force for a split second). Too early: it's
///      back down; too late: the ram is past. One player can't launch and reach the lever in time.
///   3. Exit: the plate (80 N, crates only) raises the exit gate. Jump the acid (or use the pillar) and leave.
/// No skipping: the plate is inside the culvert (no player fits), players don't count on it, and the ram is the only crate.
/// Extras: 3 shards (over the culvert roof; over the acid pillar; high over the exit gate: jump + float + dash from the roof).
/// </summary>
public static class Room7Builder
{
    private const string Room7Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room07.prefab";
    private const float Floor = -4.5f;
    private const float ShelfTop = Floor + 3f, ShelfLeft = -6.5f, ShelfRight = 3f, RoofBottom = ShelfTop + 1.3f, RoofTop = RoofBottom + 0.3f;
    private const float CulvertLeft = -4.5f, CulvertRight = 2f, StopX = 0.85f, PitLeft = 3f, PitRight = 6f;

    private static readonly string[] Built =
    {
        "Floor_Right", "Pit_Bottom", "Pit_Acid", "Pillar", "Shelf", "Surface_Wet", "Culvert_Roof",
        "Plate_Brake_80N", "Gate_Stop", "Lever_Stop", "Timer_Stop", "Lamp_Lever", "Lamp_Stop", "Gate_Exit",
        "Crate_Ram_3kg", "Shrine_Room07", "Journal_Room07", "Shard_Roof", "Shard_Pillar", "Shard_Gate",
    };

    [MenuItem("Tools/Shards of Motion/Room 7/1. Build Graybox")]
    public static void Build()
    {
        Transform ledge = Template("Ledge_Heavy"), plateT = Template("Plate_Heavy_80N"), gateT = Template("Gate_Exit"),
                  shrineT = Template("Shrine_Room01"), journalT = Template("Journal_Room01");
        if (ledge == null || plateT == null || gateT == null || shrineT == null || journalT == null) return;
        GameObject b3 = Load($"{Interactables}/Block_3kg.prefab"), shrineP = Load($"{Interactables}/ResetShrine.prefab"),
                   journalP = Load($"{Interactables}/JournalStone.prefab"), shardP = Load($"{Interactables}/MotionShard.prefab"),
                   leverP = Load($"{Interactables}/Lever.prefab");
        PhysicsMaterial2D wet = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/_Project/PhysicsMaterials/Wet_Stone.physicsMaterial2D");
        if (b3 == null || shrineP == null || journalP == null || shardP == null || leverP == null || wet == null)
        {
            Debug.LogError("Room 7: a prefab in Prefabs/Interactables or the Wet_Stone material is missing. Nothing was changed.");
            return;
        }
        JournalEntry entry = JournalTool.SaveRoom7Entry();

        GameObject room = PrefabUtility.LoadPrefabContents(Room7Path);
        try
        {
            Transform layout = Find(room.transform, "Layout"), puzzle = Find(room.transform, "Puzzle"), hazards = Find(room.transform, "Hazards"),
                      objects = Find(room.transform, "Objects"), info = Find(room.transform, "Info"), floor = Find(room.transform, "Floor");
            if (layout == null || puzzle == null || objects == null || info == null || floor == null)
            {
                Debug.LogError("Room 7: R1_Room07 is missing Layout, Puzzle, Objects, Info or Floor. Nothing was changed.");
                return;
            }
            if (hazards == null) hazards = puzzle;
            RemoveBuilt(room.transform, Built);

            // ---- Layout: floor split around the acid pit; the mid shelf (wet top) and its culvert.
            GameObject floorRight = Object.Instantiate(floor.gameObject, layout);
            floorRight.name = "Floor_Right";
            SetRect(floor, -10f, Floor - 1f, PitLeft, Floor);
            SetRect(floorRight.transform, PitRight, Floor - 1f, 10f, Floor);
            Solid(ledge, layout, "Pit_Bottom", PitLeft, -7f, PitRight, -6.5f);
            GameObject acid = Clone(ledge, hazards, "Pit_Acid");
            acid.layer = 0;
            SetRect(acid.transform, PitLeft, -6.5f, PitRight, Floor - 0.5f);
            acid.GetComponent<BoxCollider2D>().isTrigger = true;
            SpriteRenderer acidSr = acid.GetComponent<SpriteRenderer>();
            acidSr.color = new Color(0.45f, 0.95f, 0.25f, 0.85f);
            acidSr.sortingLayerName = "Water";
            acid.AddComponent<HazardZone>();
            Solid(ledge, layout, "Pillar", 4.2f, -6.5f, 4.8f, Floor + 2f); // a stepping stone back up out of the east side

            Solid(ledge, layout, "Shelf", ShelfLeft, Floor, ShelfRight, ShelfTop - 0.02f); // 2 cm low: the wet edge carries everything
            GameObject surface = new GameObject("Surface_Wet") { layer = LayerMask.NameToLayer("Ground") };
            surface.transform.SetParent(layout, false);
            EdgeCollider2D edge = surface.AddComponent<EdgeCollider2D>();
            edge.points = new[] { new Vector2(ShelfLeft, ShelfTop), new Vector2(ShelfRight, ShelfTop) };
            edge.sharedMaterial = wet;
            Solid(ledge, layout, "Culvert_Roof", CulvertLeft, RoofBottom, CulvertRight, RoofTop);
            SetEntries(room.transform, new Vector2(-8.4f, Floor + 6f + 0.75f), new Vector2(-7.6f, Floor + 6f + 0.75f)); // on the landing

            // ---- Link 2: the catch. Timed lever on the roof -> timer (~1.5 s, blinking) -> the stop block rises at the plate's edge.
            PressurePlate plate = Plate(plateT, puzzle, "Plate_Brake_80N", 0.1f, ShelfTop, 80f, PlateReadout.Display.TargetOnly, false, 1.4f);
            GameObject lever = Instance(leverP, puzzle, "Lever_Stop", new Vector2(1.4f, RoofTop + 0.5f));
            StandOn(lever, RoofTop);
            SerializedObject lv = new SerializedObject(lever.GetComponent<Lever>());
            lv.FindProperty("mode").enumValueIndex = 1;            // Timed: a press, then it springs back
            lv.FindProperty("autoOffSeconds").floatValue = 0.3f;
            lv.ApplyModifiedPropertiesWithoutUndo();
            GameObject timerGo = new GameObject("Timer_Stop");
            timerGo.transform.SetParent(puzzle, false);
            timerGo.transform.localPosition = new Vector3(1.4f, RoofTop + 0.3f, 0f);
            SignalTimer timer = timerGo.AddComponent<SignalTimer>();
            Mover stop = Platform(gateT, puzzle, "Gate_Stop", StopX, ShelfTop - 1f, StopX + 0.3f, ShelfTop,
                                  new Vector2(0f, 1f), 8f, timer, stopIfBlocked: true); // hidden in the shelf; pops up 1 m
            Light2D lampLever = Lamp(puzzle, "Lamp_Lever", new Vector3(1.4f, RoofTop + 1.2f, 0f), new Color(1f, 0.75f, 0.35f));
            Light2D lampStop = Lamp(puzzle, "Lamp_Stop", new Vector3(StopX + 0.15f, ShelfTop + 0.6f, 0f), new Color(1f, 0.45f, 0.3f));
            timer.EditorSetup(lever.GetComponent<Lever>(), 1.2f, new[] { lampLever, lampStop });

            // ---- Link 3: the plate raises the exit gate.
            Platform(gateT, puzzle, "Gate_Exit", 9f, Floor, 9.6f, Floor + 4.2f, new Vector2(0f, 4.3f), 4f, plate, false);

            // ---- Objects
            Crate(b3, objects, "Crate_Ram_3kg", -5f, ShelfTop + 0.5f); // P1 stands west of it (x -6.4..-5.6) to Burst it east
            float shrineLift = shrineT.localPosition.y - Floor, journalLift = journalT.localPosition.y - Floor;
            Instance(shrineP, info, "Shrine_Room07", new Vector2(7.2f + 1.3f, Floor + shrineLift));          // east floor, by the exit
            GameObject journal = Instance(journalP, info, "Journal_Room07", new Vector2(-7f, Floor + 6f + journalLift)); // on the landing
            if (journal.TryGetComponent(out JournalStone stone)) { stone.EditorSetEntry(entry); EditorUtility.SetDirty(stone); }

            Instance(shardP, objects, "Shard_Roof", new Vector2(-2f, RoofTop + 2.2f));     // jump from the culvert roof
            Instance(shardP, objects, "Shard_Pillar", new Vector2(4.5f, Floor + 4.4f));    // jump from the pillar (2 m)
            Instance(shardP, objects, "Shard_Gate", new Vector2(9.3f, Floor + 5.4f));      // jump + float + dash from the roof's east end

            PrefabUtility.SaveAsPrefabAsset(room, Room7Path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(room);
        }
        Debug.Log("Room 7: graybox built in R1_Room07 (Unstoppable Object). Play it from Dev > Play From Room > Room 7.");
    }

    private static Light2D Lamp(Transform parent, string name, Vector3 at, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        Light2D light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = 1.2f;
        light.pointLightOuterRadius = 2f;
        light.pointLightInnerRadius = 0.2f;
        light.enabled = false;
        return light;
    }

    private static void StandOn(GameObject go, float groundY)
    {
        SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        go.transform.position += new Vector3(0f, groundY - sr.bounds.min.y, 0f);
    }
}
