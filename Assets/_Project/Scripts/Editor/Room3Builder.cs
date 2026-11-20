using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static RoomBuildKit;

/// <summary>
/// Builds the Room 3 graybox ("Together We Move") inside Prefabs/Rooms/Region1/R1_Room03.prefab. Re-runnable.
///
/// Layout (room-local; floor top y = -4.5):
///   left bank x -10..-1 | acid x -1..6.5, under a low stone arch for x -1..4 (bottom y -2.7, top y 0) | exit landing.
///   The arch leaves only 1.8 m above the floor, so no jump arc fits under it: jump + float + dash reaches ~6 m
///   there (it would be ~12 m in the open), and the acid can only be crossed on the FERRY.
/// Ferry: a 2.5 m kinematic platform with a cargo plate built into its deck (85 N, people don't count). It docks
///   at the left bank (x -1..1.5) and at the far end (x 4..6.5), right against the exit landing, in the open.
///   It waits 1.5 s after its signal changes (Mover Signal Delay), with a blinking light, so players can hop on/off.
///   Relay mode Switch (odd number of inputs on = sail): the cargo plate, a lever on the deck and a lever on the
///   exit landing. Loading the crate sends it; flipping either lever reverses it (to come back from Room 4).
/// Puzzle (one crate, two uses):
///   1. Get the 3 kg crate onto the ferry: friction 29.4 N beats one push (12 N) and the tether (24 N), but
///      push + tether together (36 N) move it: a = (36 - 29.4) / 3 = 2.2 m/s^2. (Careful bursts also work.)
///   2. Ride across together (inertia: the crate lags when the ferry starts, slides ~0.2 m when it stops), then
///      use the crate on the deck as a step: Doorway_R3_R4 is raised 3 m (landing top y -1.5) - too high to jump
///      from the deck (2.38 m), reachable from the crate's top (+1 m). No unloading needed.
/// Extras: 3 shards; machine part 1 (Flywheel) on top of the arch, reachable only from the exit landing.
/// </summary>
public static class Room3Builder
{
    private const string Room3Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room03.prefab";
    private const string FxLibraryPath = "Assets/_Project/Art/FX/FX_Library.asset";
    private const string VillageTexture = "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Village Props.png";
    private const float Floor = -4.5f;

    private static readonly string[] Built =
    {
        "Floor_Right", "Pit_Acid", "Pit_Bottom", "Causeway_Arch", "Bridge", "Ferry", "Plate_Bridge_85N", "Plate_Ferry_85N",
        "Relay_Bridge", "Relay_Ferry", "Lever_Recall", "Lever_Deck", "Lever_Call", "Sign_Recall", "Crate_Stone_3kg", "Crate_Cargo_2kg",
        "Shrine_Room03", "Journal_Room03", "Shard_Tunnel", "Shard_Arch", "Shard_High", "Part_Flywheel",
    };

    [MenuItem("Tools/Shards of Motion/Room 3/1. Build Graybox")]
    public static void Build()
    {
        Transform ledge = Template("Ledge_Heavy"), plateT = Template("Plate_Heavy_80N"), liftT = Template("Lift"),
                  relayT = Template("Relay_Exit"), shrineT = Template("Shrine_Room01"), journalT = Template("Journal_Room01");
        if (ledge == null || plateT == null || liftT == null || relayT == null || shrineT == null || journalT == null) return;
        GameObject block3 = Load($"{Interactables}/Block_3kg.prefab");
        GameObject shrineP = Load($"{Interactables}/ResetShrine.prefab"), journalP = Load($"{Interactables}/JournalStone.prefab");
        GameObject shardP = Load($"{Interactables}/MotionShard.prefab"), leverP = Load($"{Interactables}/Lever.prefab");
        if (block3 == null || shrineP == null || journalP == null || shardP == null || leverP == null)
        {
            Debug.LogError("Room 3: a prefab in Prefabs/Interactables is missing. Nothing was changed.");
            return;
        }
        JournalEntry entry = JournalTool.SaveRoom3Entry();

        GameObject room = PrefabUtility.LoadPrefabContents(Room3Path);
        try
        {
            Transform layout = Find(room.transform, "Layout"), puzzle = Find(room.transform, "Puzzle"),
                      objects = Find(room.transform, "Objects"), info = Find(room.transform, "Info"),
                      hazards = Find(room.transform, "Hazards"), floor = layout != null ? Find(layout, "Floor") : null;
            if (layout == null || puzzle == null || objects == null || info == null || floor == null)
            {
                Debug.LogError("Room 3: R1_Room03 is missing Layout/Floor, Puzzle, Objects or Info. Nothing was changed.");
                return;
            }
            if (hazards == null) hazards = puzzle;
            RemoveBuilt(room.transform, Built);

            // ---- Layout: floor split around the acid, the low arch over its left part
            GameObject floorRight = Object.Instantiate(floor.gameObject, layout);
            floorRight.name = "Floor_Right";
            SetRect(floor, -10f, -5.5f, -1f, Floor);
            SetRect(floorRight.transform, 6.5f, -5.5f, 10f, Floor); // under the exit landing
            Solid(ledge, layout, "Pit_Bottom", -1f, -7f, 6.5f, -6.5f);
            Solid(ledge, layout, "Causeway_Arch", -1f, -2.7f, 4f, 0f); // 1.8 m headroom over the acid: no jumping across
            GameObject acid = Clone(ledge, hazards, "Pit_Acid");
            acid.layer = 0;
            SetRect(acid.transform, -1f, -6.5f, 6.5f, -5.0f); // surface 0.2 m below the ferry's hull: the water never touches it
            acid.GetComponent<BoxCollider2D>().isTrigger = true;
            SpriteRenderer acidSr = acid.GetComponent<SpriteRenderer>();
            acidSr.color = new Color(0.45f, 0.95f, 0.25f, 0.85f);
            acidSr.sortingLayerName = "Water";
            acid.AddComponent<HazardZone>();

            // ---- Puzzle: cargo plate on the deck + two levers -> relay (Switch: odd count = sail) -> ferry
            Mover ferry = Platform(liftT, puzzle, "Ferry", -1f, -4.8f, 1.5f, Floor, new Vector2(5f, 0f), 2f, null, stopIfBlocked: false);
            PressurePlate plate = Plate(plateT, puzzle, "Plate_Ferry_85N", 0.25f, Floor, 85f, PlateReadout.Display.ReadingAndTarget,
                                        countPlayers: false, width: 2.3f);
            if (plate.TryGetComponent(out PlateReadout readout))
            {
                SerializedObject ro = new SerializedObject(readout);
                ro.FindProperty("belowPlate").boolValue = false;               // under the deck would be in the acid
                ro.FindProperty("offset").vector2Value = new Vector2(0f, 1.6f); // above the cargo
                ro.ApplyModifiedPropertiesWithoutUndo();
            }
            plate.transform.SetParent(ferry.transform, true); // the plate rides on the deck

            GameObject deckLever = Instance(leverP, puzzle, "Lever_Deck", new Vector2(1.15f, Floor + 0.5f));
            StandOnFloor(deckLever, Floor);
            deckLever.transform.SetParent(ferry.transform, true); // rides along: "send it back" from on board
            GameObject callLever = Instance(leverP, puzzle, "Lever_Call", new Vector2(7.2f, -1f));
            StandOnFloor(callLever, -1.5f); // on the exit landing: "call it over" when coming back from Room 4

            PuzzleRelay relay = Relay(relayT, puzzle, "Relay_Ferry", new Vector2(0.25f, 0.5f), 2, // 2 = Switch
                plate, deckLever.GetComponent<Lever>(), callLever.GetComponent<Lever>());
            SerializedObject fm = new SerializedObject(ferry);
            fm.FindProperty("signal").objectReferenceValue = relay;
            fm.FindProperty("signalDelay").floatValue = 1.5f; // time to hop on / off
            fm.ApplyModifiedPropertiesWithoutUndo();

            GameObject sign = new GameObject("Sign_Recall");
            sign.transform.SetParent(info, false);
            sign.transform.localPosition = new Vector3(7.2f, 0.6f, 0f);
            WorldSign ws = sign.AddComponent<WorldSign>();
            SerializedObject so = new SerializedObject(ws);
            so.FindProperty("text").stringValue = "Ferry switch";
            so.FindProperty("width").floatValue = 3f;
            so.FindProperty("fontSizePx").floatValue = 22f;
            so.FindProperty("showDistance").floatValue = 3f;
            so.ApplyModifiedPropertiesWithoutUndo();

            // ---- Objects
            Crate(block3, objects, "Crate_Stone_3kg", -4.4f, -4f);   // on the left bank, 2.9 m from the ferry
            Instance(shrineP, info, "Shrine_Room03", new Vector2(-9.3f, shrineT.localPosition.y));
            GameObject journal = Instance(journalP, info, "Journal_Room03", new Vector2(-8.4f, journalT.localPosition.y));
            if (journal.TryGetComponent(out JournalStone stone)) { stone.EditorSetEntry(entry); EditorUtility.SetDirty(stone); }

            Instance(shardP, objects, "Shard_Tunnel", new Vector2(1.5f, -3.6f));    // collected while riding the ferry
            Instance(shardP, objects, "Shard_Arch", new Vector2(2.0f, 0.75f));      // on the arch: jump over from the exit landing
            Instance(shardP, objects, "Shard_High", new Vector2(-4.4f, -0.9f));     // stand on the 3 kg crate and jump
            MachinePart(objects, shardP.layer, "Part_Flywheel", new Vector2(-0.4f, 0.8f), "R1_Part_Flywheel", "Flywheel (machine part 1)");

            PrefabUtility.SaveAsPrefabAsset(room, Room3Path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(room);
        }
        RaiseExitDoorway();
        Debug.Log("Room 3: graybox built in R1_Room03 (Together We Move, cargo ferry). Run Room 3 > 2. Apply Visual Pass next.");
    }

    // Lever art stands on the floor whatever the prefab's pivot.
    private static void StandOnFloor(GameObject go, float floorY)
    {
        SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        go.transform.position += new Vector3(0f, floorY - sr.bounds.min.y, 0f);
    }

    // Machine part: a glowing wheel that bobs (QuestItem), on the shards' layer so players can pick it up.
    private static void MachinePart(Transform parent, int layer, string name, Vector2 pos, string id, string displayName)
    {
        GameObject go = new GameObject(name) { layer = layer };
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(VillageTexture))
            if (o is Sprite s && s.name == "TX Village Props - Wheelbarrow Wheel") sr.sprite = s;
        sr.color = new Color(1f, 0.9f, 0.6f);
        sr.sortingLayerName = "Interactables";
        sr.sortingOrder = 5;
        CircleCollider2D c = go.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = 0.45f;
        go.AddComponent<QuestItem>().EditorSetup(id, displayName, AssetDatabase.LoadAssetAtPath<FxLibrary>(FxLibraryPath));
        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(go.transform, false);
        Light2D light = glow.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(1f, 0.84f, 0.47f);
        light.intensity = 0.7f;
        light.pointLightOuterRadius = 1.6f;
        light.pointLightInnerRadius = 0.2f;
    }

    // The exit is high: Doorway_R3_R4 (in the Region 1 scene) gets a 3 m opening bottom, if that scene is open.
    private static void RaiseExitDoorway()
    {
        GameObject d = GameObject.Find("Doorway_R3_R4");
        Doorway doorway = d != null ? d.GetComponent<Doorway>() : null;
        if (doorway == null)
        {
            Debug.LogWarning("Room 3: open Region1_ShatteredGrounds (after Rooms > Set Up Adjustable Doorway) and run this again, " +
                             "or set Doorway_R3_R4's Opening Bottom to 3 and press Apply Opening.");
            return;
        }
        Undo.RegisterFullObjectHierarchyUndo(d, "Raise Room 3 exit");
        SerializedObject s = new SerializedObject(doorway);
        s.FindProperty("openingBottom").floatValue = 3f;
        s.ApplyModifiedProperties();
        doorway.Apply();
        foreach (Component c in d.GetComponentsInChildren<Component>(true))
            if (c != null && PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        EditorSceneManager.MarkSceneDirty(d.scene);
        Debug.Log("Room 3: Doorway_R3_R4 raised to 3 m. Save the scene.");
    }
}
