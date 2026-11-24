using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static RoomBuildKit;

/// <summary>
/// Builds the Room 6 graybox ("Weight of Cooperation", up and over: weight, equilibrium, normal force) inside
/// Prefabs/Rooms/Region1/R1_Room06.prefab. Re-runnable. With Region1_ShatteredGrounds open, it also raises the exit doorway
/// (Doorway_R6_R7) to 6 m; its landing and the exit ledge are the goal. Save the scene afterwards.
///
/// Layout (room-local; floor top y = -4.5, ceiling y = 5.625), using the room's height:
///   WEST LIFT x -9..-7.6: a platform at 2.1 m (a jump from the floor) that rises to the gallery (5 m) while the HOLD PLATE
///     (x -5, 25 N: one player) is pressed, and for 2.5 s after (SignalTimer: its lamps blink faster as time runs out).
///   GALLERY x -7.5..-2 at 5 m with the 2 kg and 1 kg crates; PAN A x -2..0.2 starts at the top, flush with it (rim east).
///   PULLEY TOWER x 0.2..1.4 from 5.5 m to the ceiling: solid, so nobody jumps from the gallery side to the exit side.
///   PAN B x 1.4..3.2 starts on the floor, with the brake lever. EXIT LEDGE x 3.4..10 at 6 m (doorway landing from 6.5).
/// The balance lift (BalanceLift, Lift mode: a = (W_B - W_A - 4 N friction) / total mass, easing into the ends), 5 m travel.
/// Solution (a chain, both players busy):
///   1. P2 stands on the hold plate: the west lift carries P1 up to the gallery (one player can't: it only rises while the
///      plate is held, and you board it at the bottom).
///   2. P1 Bursts the 2 kg crate onto pan A (slides 2.9 m) and walks the 1 kg crate on (the rim stops them).
///   3. P1 drops down; both stand on pan B and pull the lever (brake off): 88.3 N vs 58.9 N, pan A sinks and pan B lifts
///      both to 5 m; step up onto the exit ledge (6 m).
///   With only the 2 kg crate: 58.9 = 58.9, nothing moves (equilibrium). Empty pans + brake off: it winds back to the start.
/// Heights: the exit (6 m) is reachable only from pan B at the top (5 m); the gallery (5 m) only from the west lift's top.
/// Extras: 3 shards (on the west lift's way up; high above the gallery; high above pan B's top).
/// </summary>
public static class Room6Builder
{
    private const string Room6Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room06.prefab";
    private const float Floor = -4.5f;
    private const float Travel = 5f, PanThickness = 0.25f, High = Floor + Travel; // 5 m: gallery and the pans' top
    private const float PanALeft = -2f, PanARight = 0.2f, TowerRight = 1.4f, PanBRight = 3.2f;
    private const float GalleryLeft = -7.5f, LiftLeft = -9f, LiftRight = -7.6f, LiftLow = Floor + 2.1f;
    private const float ExitHeight = 6f, ExitLedgeLeft = 3.4f;

    private static readonly string[] Built =
    {
        // this version
        "Gallery", "Pulley_Tower", "Exit_Ledge", "Lift_West", "Plate_Hold_25N", "Timer_Hold", "Lamp_Hold", "Lamp_Lift",
        "Pan_A", "Pan_B", "Rim_A", "Scale_A", "Scale_B", "Lever_Brake", "Balance_Lift",
        "Crate_1kg", "Crate_2kg", "Shrine_Room06", "Journal_Room06", "Shard_Climb", "Shard_Gallery", "Shard_Summit",
        // earlier versions
        "Step", "Shelf", "Pan_Load", "Pan_Counter", "Rim_Load", "Cage_West", "Cage_East", "Cage_Top", "Scale_Load", "Scale_Counter",
        "Scale", "Relay_Scale", "Lamp_Balanced", "Bench", "Curb", "Gate_Sluice", "Channel_Current", "Plate_Drop_80N", "Gate_Exit",
        "Crate_Counterweight_3kg", "Crate_Stone_3kg", "Shard_Shelf", "Shard_Sluice", "Shard_Drop",
        "Ledge_Mid", "Pulley_Beam", "Shard_Pulley", "Shard_Ledge", "Shard_Ride",
    };

    [MenuItem("Tools/Shards of Motion/Room 6/1. Build Graybox")]
    public static void Build()
    {
        Transform ledge = Template("Ledge_Heavy"), liftT = Template("Lift"), plateT = Template("Plate_Heavy_80N"),
                  shrineT = Template("Shrine_Room01"), journalT = Template("Journal_Room01");
        if (ledge == null || liftT == null || plateT == null || shrineT == null || journalT == null) return;
        GameObject b1 = Load($"{Interactables}/Block_1kg.prefab"), b2 = Load($"{Interactables}/Block_2kg.prefab"),
                   shrineP = Load($"{Interactables}/ResetShrine.prefab"), journalP = Load($"{Interactables}/JournalStone.prefab"),
                   shardP = Load($"{Interactables}/MotionShard.prefab"), leverP = Load($"{Interactables}/Lever.prefab");
        if (b1 == null || b2 == null || shrineP == null || journalP == null || shardP == null || leverP == null)
        {
            Debug.LogError("Room 6: a prefab in Prefabs/Interactables is missing. Nothing was changed.");
            return;
        }
        JournalEntry entry = JournalTool.SaveRoom6Entry();

        GameObject room = PrefabUtility.LoadPrefabContents(Room6Path);
        try
        {
            Transform layout = Find(room.transform, "Layout"), puzzle = Find(room.transform, "Puzzle"),
                      objects = Find(room.transform, "Objects"), info = Find(room.transform, "Info");
            if (layout == null || puzzle == null || objects == null || info == null)
            {
                Debug.LogError("Room 6: R1_Room06 is missing Layout, Puzzle, Objects or Info. Nothing was changed.");
                return;
            }
            RemoveBuilt(room.transform, Built);

            // ---- Layout: gallery, pulley tower, exit ledge (the doorway's own landing continues it from x 6.5).
            Solid(ledge, layout, "Gallery", GalleryLeft, High - 0.4f, PanALeft, High);
            Solid(ledge, layout, "Pulley_Tower", PanARight, Floor + 5.5f, TowerRight, 5.7f);
            Solid(ledge, layout, "Exit_Ledge", ExitLedgeLeft, Floor + ExitHeight - 0.4f, 6.5f, Floor + ExitHeight);
            SetEntries(room.transform, new Vector2(-8.4f, Floor + 0.75f), new Vector2(-7.6f, Floor + 0.75f));

            // ---- Link 1: the timed west lift. Hold plate -> timer (2.5 s after release, blinking) -> platform up to the gallery.
            PressurePlate hold = Plate(plateT, puzzle, "Plate_Hold_25N", -5f, Floor, 25f, PlateReadout.Display.TargetOnly, true, 1.2f);
            GameObject timerGo = new GameObject("Timer_Hold");
            timerGo.transform.SetParent(puzzle, false);
            timerGo.transform.localPosition = new Vector3(-5f, Floor + 0.3f, 0f); // at the plate (the sketch draws its arrow from here)
            SignalTimer timer = timerGo.AddComponent<SignalTimer>();
            Mover lift = Platform(liftT, puzzle, "Lift_West", LiftLeft, LiftLow - 0.3f, LiftRight, LiftLow,
                                  new Vector2(0f, High - LiftLow), 2.5f, timer, stopIfBlocked: true);
            Light2D lampHold = Lamp(puzzle, "Lamp_Hold", new Vector3(-5f, Floor + 1f, 0f));
            Light2D lampLift = Lamp(lift.transform, "Lamp_Lift", lift.transform.position + new Vector3(0f, 0.6f, 0f));
            timer.EditorSetup(hold, 2.5f, new[] { lampHold, lampLift });

            // ---- Link 2: the balance lift. Pan A up at the gallery, pan B on the floor, braked.
            Rigidbody2D panA = Pan(liftT, puzzle, "Pan_A", PanALeft, PanARight, High);
            Rigidbody2D panB = Pan(liftT, puzzle, "Pan_B", TowerRight, PanBRight, Floor);
            Solid(ledge, puzzle, "Rim_A", PanARight - 0.15f, High, PanARight, High + 0.4f).transform.SetParent(panA.transform, true);
            PressurePlate scaleA = Scale(plateT, puzzle, "Scale_A", PanALeft, PanARight - 0.15f, High, panA.transform);
            PressurePlate scaleB = Scale(plateT, puzzle, "Scale_B", TowerRight, PanBRight, Floor, panB.transform);
            GameObject lever = Instance(leverP, puzzle, "Lever_Brake", new Vector2(TowerRight + 0.35f, Floor + 0.5f));
            StandOn(lever, Floor);
            lever.transform.SetParent(panB.transform, true); // on board: release the brake from the pan you ride

            GameObject liftGo = new GameObject("Balance_Lift");
            liftGo.transform.SetParent(puzzle, false);
            liftGo.transform.localPosition = new Vector3((PanALeft + PanBRight) * 0.5f, Floor + 3f, 0f);
            BalanceLift balance = liftGo.AddComponent<BalanceLift>();
            balance.EditorSetup(panA, panB, scaleA, scaleB, Travel, 1f, lever.GetComponent<Lever>()); // start: pan A at the top
            balance.EditorSetKind(BalanceLift.Kind.Lift);

            // ---- Objects
            Crate(b2, objects, "Crate_2kg", -3f, High + 0.5f);   // a Burst from the west slides it 2.9 m onto pan A
            Crate(b1, objects, "Crate_1kg", -5f, High + 0.5f);   // walk it on (one push, 12 N > 9.8 N)
            float shrineLift = shrineT.localPosition.y - Floor, journalLift = journalT.localPosition.y - Floor;
            Instance(shrineP, info, "Shrine_Room06", new Vector2(-3.4f, Floor + shrineLift));
            GameObject journal = Instance(journalP, info, "Journal_Room06", new Vector2(-6.4f, Floor + journalLift));
            if (journal.TryGetComponent(out JournalStone stone)) { stone.EditorSetEntry(entry); EditorUtility.SetDirty(stone); }

            Instance(shardP, objects, "Shard_Climb", new Vector2(-8.3f, Floor + 3.6f));    // ride the west lift
            Instance(shardP, objects, "Shard_Gallery", new Vector2(-6.8f, Floor + 7.3f));  // jump from the gallery (5 m)
            Instance(shardP, objects, "Shard_Summit", new Vector2(2.3f, Floor + 7.4f));    // jump from pan B at the top (5 m)

            PrefabUtility.SaveAsPrefabAsset(room, Room6Path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(room);
        }

        SetExitHeight(ExitHeight);
        Debug.Log("Room 6: graybox built in R1_Room06 (Weight of Cooperation). Play it from Dev > Play From Room > Room 6.");
    }

    // A pan: Room 1's lift (kinematic body, Ground layer) without its Mover; BalanceLift drives it.
    private static Rigidbody2D Pan(Transform liftT, Transform parent, string name, float left, float right, float top)
    {
        GameObject go = Solid(liftT, parent, name, left, top - PanThickness, right, top);
        if (go.TryGetComponent(out Mover mover)) Object.DestroyImmediate(mover);
        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        return rb;
    }

    // A scale on a pan: a plate that never presses (it only weighs, players included), showing the reading, riding with the
    // pan. Anything riding the pan counts even while it moves (no "twitch" when crates lag a moment as it starts).
    private static PressurePlate Scale(Transform plateT, Transform parent, string name, float left, float right, float top, Transform pan)
    {
        PressurePlate plate = Plate(plateT, parent, name, (left + right) * 0.5f, top, 9999f, PlateReadout.Display.ReadingOnly, true, right - left);
        SerializedObject so = new SerializedObject(plate);
        so.FindProperty("maxRestingSpeed").floatValue = 4f;
        so.ApplyModifiedPropertiesWithoutUndo();
        plate.transform.SetParent(pan, true);
        return plate;
    }

    private static Light2D Lamp(Transform parent, string name, Vector3 worldPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = worldPos;
        Light2D light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(1f, 0.75f, 0.35f);
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

    // Sets Doorway_R6_R7's opening height above the floor (in the open Region 1 scene).
    private static void SetExitHeight(float bottom)
    {
        Doorway door = null;
        foreach (Doorway d in Object.FindObjectsByType<Doorway>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (d.name == "Doorway_R6_R7") door = d;
        if (door == null)
        {
            Debug.LogWarning($"Room 6: open Region1_ShatteredGrounds and run Build Graybox again to raise the exit doorway (Doorway_R6_R7) to {bottom} m.");
            return;
        }
        SerializedObject so = new SerializedObject(door);
        if (Mathf.Approximately(so.FindProperty("openingBottom").floatValue, bottom)) return;
        so.FindProperty("openingBottom").floatValue = bottom;
        so.ApplyModifiedProperties();
        door.Apply();
        EditorSceneManager.MarkSceneDirty(door.gameObject.scene);
        Debug.Log($"Room 6: Doorway_R6_R7 raised to {bottom} m. Save the scene (Ctrl+S).");
    }
}
