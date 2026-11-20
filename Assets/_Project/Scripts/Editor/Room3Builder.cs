using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static RoomBuildKit;

/// <summary>
/// Builds the Room 3 graybox ("Together We Move") inside Prefabs/Rooms/Region1/R1_Room03.prefab. Re-runnable.
///
/// Layout (room-local; floor top y = -4.5):
///   left bank x -10..-2 | acid pit x -2..5.5 under a low stone arch (bottom y -2.7, top y 0) | right bank x 5.5..10.
///   The arch leaves only 1.8 m above the floor, so no jump arc fits under it: jump + float + dash reaches ~6 m
///   there (it would be ~12 m in the open), and the 7.5 m of acid can only be crossed on the BRIDGE.
/// Bridge: a 2.5 m kinematic platform carrying the 2 kg crate. It travels 5 m to the right bank (2 m/s) while
///   the bridge plate (85 N, crates only) is pressed, and back when it isn't. A lever on the right bank also
///   calls it (relay: Any), so players coming back from Room 4 aren't stranded.
/// Puzzle: get the 3 kg crate onto the plate. Friction 29.4 N beats one push (12 N) and the tether (24 N), but
///   push + tether together (36 N) move it: a = (36 - 29.4) / 3 = 2.2 m/s^2. Or careful bursts (1.27 m each).
///   The bridge leaves as soon as the plate is pressed, so both players must be on it: the neat solution is
///   P1 bursting the crate onto the plate FROM the bridge (crate at x -2.6 slides 1.27 m to the plate at -3.87).
/// Exit: Doorway_R3_R4 raised 3 m (set in the Region 1 scene if it's open; landing top y -1.5). Too high to jump
///   (2.38 m), so push the 2 kg crate off the bridge against the landing, stand on it (+1 m) and jump up.
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
        "Floor_Right", "Pit_Acid", "Pit_Bottom", "Causeway_Arch", "Bridge", "Plate_Bridge_85N", "Relay_Bridge", "Lever_Recall",
        "Crate_Stone_3kg", "Crate_Cargo_2kg", "Shrine_Room03", "Journal_Room03",
        "Shard_Tunnel", "Shard_Arch", "Shard_High", "Part_Flywheel",
    };

    [MenuItem("Tools/Shards of Motion/Room 3/1. Build Graybox")]
    public static void Build()
    {
        Transform ledge = Template("Ledge_Heavy"), plateT = Template("Plate_Heavy_80N"), liftT = Template("Lift"),
                  relayT = Template("Relay_Exit"), shrineT = Template("Shrine_Room01"), journalT = Template("Journal_Room01");
        if (ledge == null || plateT == null || liftT == null || relayT == null || shrineT == null || journalT == null) return;
        GameObject block2 = Load($"{Interactables}/Block_2kg.prefab"), block3 = Load($"{Interactables}/Block_3kg.prefab");
        GameObject shrineP = Load($"{Interactables}/ResetShrine.prefab"), journalP = Load($"{Interactables}/JournalStone.prefab");
        GameObject shardP = Load($"{Interactables}/MotionShard.prefab"), leverP = Load($"{Interactables}/Lever.prefab");
        if (block2 == null || block3 == null || shrineP == null || journalP == null || shardP == null || leverP == null)
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

            // ---- Layout: split floor, acid pit, the low arch over it
            GameObject floorRight = Object.Instantiate(floor.gameObject, layout);
            floorRight.name = "Floor_Right";
            SetRect(floor, -10f, -5.5f, -2f, Floor);
            SetRect(floorRight.transform, 5.5f, -5.5f, 10f, Floor);
            Solid(ledge, layout, "Pit_Bottom", -2f, -7f, 5.5f, -6.5f);
            Solid(ledge, layout, "Causeway_Arch", -2f, -2.7f, 5.5f, 0f); // 1.8 m headroom: no jumping across
            GameObject acid = Clone(ledge, hazards, "Pit_Acid");
            acid.layer = 0;
            SetRect(acid.transform, -2f, -6.5f, 5.5f, -4.8f);
            acid.GetComponent<BoxCollider2D>().isTrigger = true;
            SpriteRenderer acidSr = acid.GetComponent<SpriteRenderer>();
            acidSr.color = new Color(0.45f, 0.95f, 0.25f, 0.85f);
            acidSr.sortingLayerName = "Water";
            acid.AddComponent<HazardZone>();

            // ---- Puzzle: plate + recall lever -> relay (Any) -> bridge
            PressurePlate plate = Plate(plateT, puzzle, "Plate_Bridge_85N", -3.87f, Floor, 85f, PlateReadout.Display.ReadingAndTarget, countPlayers: false);
            GameObject lever = Instance(leverP, puzzle, "Lever_Recall", new Vector2(6.1f, Floor + 0.5f));
            StandOnFloor(lever, Floor);
            PuzzleRelay relay = Relay(relayT, puzzle, "Relay_Bridge", new Vector2(-3.9f, 0.5f), 1, plate, lever.GetComponent<Lever>());
            Platform(liftT, puzzle, "Bridge", -2f, -4.8f, 0.5f, Floor, new Vector2(5f, 0f), 2f, relay, stopIfBlocked: false);

            // ---- Objects
            Crate(block3, objects, "Crate_Stone_3kg", -5.6f, -4f);   // between the arrival points and the plate
            Crate(block2, objects, "Crate_Cargo_2kg", -0.75f, -4f);  // on the bridge: the step up to the exit
            Instance(shrineP, info, "Shrine_Room03", new Vector2(-9.3f, shrineT.localPosition.y));
            GameObject journal = Instance(journalP, info, "Journal_Room03", new Vector2(-8.4f, journalT.localPosition.y));
            if (journal.TryGetComponent(out JournalStone stone)) { stone.EditorSetEntry(entry); EditorUtility.SetDirty(stone); }

            Instance(shardP, objects, "Shard_Tunnel", new Vector2(1.75f, -3.6f));   // collected while riding the bridge
            Instance(shardP, objects, "Shard_Arch", new Vector2(3.5f, 0.75f));      // on the arch: from the exit landing
            Instance(shardP, objects, "Shard_High", new Vector2(-5.6f, -0.9f));     // stand on the 3 kg crate and jump
            MachinePart(objects, shardP.layer, "Part_Flywheel", new Vector2(-1.4f, 0.8f), "R1_Part_Flywheel", "Flywheel (machine part 1)");

            PrefabUtility.SaveAsPrefabAsset(room, Room3Path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(room);
        }
        RaiseExitDoorway();
        Debug.Log("Room 3: graybox built in R1_Room03 (Together We Move).");
    }

    // Lever art stands on the floor whatever the prefab's pivot.
    private static void StandOnFloor(GameObject go, float floorY)
    {
        SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        float bottom = sr.bounds.min.y;
        go.transform.position += new Vector3(0f, floorY - bottom, 0f);
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
