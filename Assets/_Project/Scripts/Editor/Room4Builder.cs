using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static RoomBuildKit;

/// <summary>
/// Builds the Room 4 graybox ("Broken Crossing": momentum) inside Prefabs/Rooms/Region1/R1_Room04.prefab. Re-runnable.
///
/// Layout (room-local; floor top y = -4.5). Players arrive from Room 3 on the raised doorway landing (x -10..-6.5,
/// top y -1.5), which continues as the old aqueduct ledge (x -6.5..-1.5) with the SLUICE lever on it; a step
/// (x -1.5..-0.5, top y -2.5) climbs back up from the floor. A water channel runs along the floor (x -6.5..7):
/// with the sluice open it pushes crates (not players) 25 N to the right. Two rubble walls, floor to ceiling:
///   Wall 1 at x 2..3 needs 12 kg·m/s.  Wall 2 at x 7..8 needs 16 kg·m/s. The exit is past Wall 2 (floor level).
/// Physics (mu = 1/3, g = 29.43, Burst 15 N·s -> every crate gets p = 15 kg·m/s; friction removes mu*m*g per second):
///   Wall 1: 1 kg burst from 1-3 m away arrives with ~14.3-12.9 (breaks); 3 kg (starts 1 m from it) arrives with ~7 (no).
///   Wall 2 (4.5 m on): 1 kg burst alone arrives ~12.5 (no); current alone ~11 (no); burst + current ~18.2 (breaks).
///   2 kg: burst + current ~17.3 (breaks). 3 kg can't: friction 29.4 N > current 25 N.
/// Extras: machine part 2 (Counterweight) on a nook past Wall 2 (stand on a crate to reach it); 3 shards.
/// </summary>
public static class Room4Builder
{
    private const string Room4Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room04.prefab";
    private const string FxLibraryPath = "Assets/_Project/Art/FX/FX_Library.asset";
    private const string VillageTexture = "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Village Props.png";
    private const float Floor = -4.5f, Ceiling = 5.625f;

    private static readonly string[] Built =
    {
        "Aqueduct_Ledge", "Step", "Nook", "Wall_Rubble_12", "Wall_Rubble_16", "Channel_Current", "Lever_Sluice", "Sign_Sluice",
        "Crate_Light_1kg", "Crate_Mid_2kg", "Crate_Heavy_3kg", "Shrine_Room04", "Journal_Room04",
        "Shard_Step", "Shard_Air", "Shard_Between", "Part_PendulumBob", "Part_Counterweight",
    };

    [MenuItem("Tools/Shards of Motion/Room 4/1. Build Graybox")]
    public static void Build()
    {
        Transform ledge = Template("Ledge_Heavy"), shrineT = Template("Shrine_Room01"), journalT = Template("Journal_Room01");
        if (ledge == null || shrineT == null || journalT == null) return;
        GameObject b1 = Load($"{Interactables}/Block_1kg.prefab"), b2 = Load($"{Interactables}/Block_2kg.prefab"), b3 = Load($"{Interactables}/Block_3kg.prefab");
        GameObject shrineP = Load($"{Interactables}/ResetShrine.prefab"), journalP = Load($"{Interactables}/JournalStone.prefab");
        GameObject shardP = Load($"{Interactables}/MotionShard.prefab"), leverP = Load($"{Interactables}/Lever.prefab");
        if (b1 == null || b2 == null || b3 == null || shrineP == null || journalP == null || shardP == null || leverP == null)
        {
            Debug.LogError("Room 4: a prefab in Prefabs/Interactables is missing. Nothing was changed.");
            return;
        }
        FxLibrary fx = AssetDatabase.LoadAssetAtPath<FxLibrary>(FxLibraryPath);
        JournalEntry entry = JournalTool.SaveRoom4Entry();

        GameObject room = PrefabUtility.LoadPrefabContents(Room4Path);
        try
        {
            Transform layout = Find(room.transform, "Layout"), puzzle = Find(room.transform, "Puzzle"),
                      objects = Find(room.transform, "Objects"), info = Find(room.transform, "Info");
            if (layout == null || puzzle == null || objects == null || info == null)
            {
                Debug.LogError("Room 4: R1_Room04 is missing Layout, Puzzle, Objects or Info. Nothing was changed.");
                return;
            }
            RemoveBuilt(room.transform, Built);

            // ---- Layout
            Solid(ledge, layout, "Aqueduct_Ledge", -6.5f, -2f, -1.5f, -1.5f); // continues the arrival landing (top -1.5)
            Solid(ledge, layout, "Step", -1.5f, -2.9f, -0.5f, -2.5f);          // floor -> step (2 m) -> ledge (1 m)
            Solid(ledge, layout, "Nook", 8f, -2.4f, 9.5f, -2f);                // 2.5 m up: reachable from a crate's top

            // ---- Rubble walls (floor to ceiling; break only for crates with enough momentum)
            Wall(ledge, layout, "Wall_Rubble_12", 2f, 3f, 12f, fx);   // 1 m thick; the left face (where crates hit) is what matters
            Wall(ledge, layout, "Wall_Rubble_16", 7f, 8f, 16f, fx);
            SetEntries(room.transform, new Vector2(-8f, -0.75f), new Vector2(-7f, -0.75f)); // on the arrival landing (top -1.5)

            // ---- Sluice: lever -> current (AreaEffector2D, crates only) along the floor channel
            GameObject lever = Instance(leverP, puzzle, "Lever_Sluice", new Vector2(-2.6f, -1f));
            StandOn(lever, -1.5f);
            GameObject current = Clone(ledge, puzzle, "Channel_Current");
            current.layer = 0;
            SetRect(current.transform, -6.5f, Floor, 7f, Floor + 1.5f);
            BoxCollider2D zone = current.GetComponent<BoxCollider2D>();
            zone.isTrigger = true;
            zone.usedByEffector = true;
            AreaEffector2D effector = current.AddComponent<AreaEffector2D>();
            effector.useColliderMask = true;
            effector.colliderMask = LayerMask.GetMask("PhysicsObject");
            effector.useGlobalAngle = true;
            effector.forceAngle = 0f;      // to the right
            effector.forceMagnitude = 25f; // newtons, like the Lab's water gate
            effector.enabled = false;      // the sluice starts closed
            SignalActivator activator = current.AddComponent<SignalActivator>();
            SerializedObject sa = new SerializedObject(activator);
            sa.FindProperty("signal").objectReferenceValue = lever.GetComponent<Lever>();
            SerializedProperty behaviours = sa.FindProperty("behaviours");
            behaviours.arraySize = 1;
            behaviours.GetArrayElementAtIndex(0).objectReferenceValue = effector;
            sa.ApplyModifiedPropertiesWithoutUndo();

            GameObject sign = new GameObject("Sign_Sluice");
            sign.transform.SetParent(info, false);
            sign.transform.localPosition = new Vector3(-2.6f, 0.4f, 0f);
            SerializedObject so = new SerializedObject(sign.AddComponent<WorldSign>());
            so.FindProperty("text").stringValue = "Sluice";
            so.FindProperty("width").floatValue = 3f;
            so.FindProperty("fontSizePx").floatValue = 22f;
            so.FindProperty("showDistance").floatValue = 3f;
            so.ApplyModifiedPropertiesWithoutUndo();

            // ---- Objects
            Crate(b1, objects, "Crate_Light_1kg", -3.5f, -4f);
            Crate(b2, objects, "Crate_Mid_2kg", -5f, -4f);
            Crate(b3, objects, "Crate_Heavy_3kg", 0.5f, -4f); // 1 m from Wall 1: the tempting wrong choice
            // Shrine and journal stand where players arrive (on the aqueduct ledge, top -1.5), not down on the floor.
            float shrineLift = shrineT.localPosition.y - Floor, journalLift = journalT.localPosition.y - Floor;
            Instance(shrineP, info, "Shrine_Room04", new Vector2(-5.9f, -1.5f + shrineLift));
            GameObject journal = Instance(journalP, info, "Journal_Room04", new Vector2(-4.8f, -1.5f + journalLift));
            if (journal.TryGetComponent(out JournalStone stone)) { stone.EditorSetEntry(entry); EditorUtility.SetDirty(stone); }

            Instance(shardP, objects, "Shard_Step", new Vector2(-1f, -1.7f));     // easy: on the step
            Instance(shardP, objects, "Shard_Air", new Vector2(1f, 0.6f));        // jump off the aqueduct ledge
            Instance(shardP, objects, "Shard_Between", new Vector2(5f, -1f));     // between the walls: stand on a crate
            MachinePart(objects, shardP.layer, "Part_Counterweight", new Vector2(8.75f, -1.2f), "R1_Part_Counterweight",
                        "Counterweight (machine part 2)", fx);

            PrefabUtility.SaveAsPrefabAsset(room, Room4Path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(room);
        }
        Debug.Log("Room 4: graybox built in R1_Room04 (Broken Crossing). Run Room 4 > 2. Apply Visual Pass next.");
    }

    private static void Wall(Transform ledge, Transform parent, string name, float left, float right, float momentum, FxLibrary fx)
    {
        GameObject w = Solid(ledge, parent, name, left, Floor, right, Ceiling);
        BreakableWall wall = w.AddComponent<BreakableWall>();
        wall.EditorSetup(momentum, fx);
        SerializedObject s = new SerializedObject(wall);
        float centreY = (Floor + Ceiling) * 0.5f;
        s.FindProperty("offset").vector2Value = new Vector2(0f, Floor + 2.2f - centreY); // label at head height
        s.FindProperty("showDistance").floatValue = 4f;
        s.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void StandOn(GameObject go, float groundY)
    {
        SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        go.transform.position += new Vector3(0f, groundY - sr.bounds.min.y, 0f);
    }

    private static void MachinePart(Transform parent, int layer, string name, Vector2 pos, string id, string displayName, FxLibrary fx)
    {
        GameObject go = new GameObject(name) { layer = layer };
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(VillageTexture))
            if (o is Sprite s && s.name == "TX Village Props - Anvil") sr.sprite = s; // a heavy counterweight
        sr.color = new Color(1f, 0.9f, 0.6f);
        sr.sortingLayerName = "Interactables";
        sr.sortingOrder = 5;
        CircleCollider2D c = go.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = 0.45f;
        go.AddComponent<QuestItem>().EditorSetup(id, displayName, fx);
        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(go.transform, false);
        Light2D light = glow.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(1f, 0.84f, 0.47f);
        light.intensity = 0.7f;
        light.pointLightOuterRadius = 1.6f;
        light.pointLightInnerRadius = 0.2f;
    }
}
