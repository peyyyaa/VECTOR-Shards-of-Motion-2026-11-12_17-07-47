using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static RoomBuildKit;

/// <summary>
/// Builds the Room 8 graybox ("Heart of the Forest", the finale) inside Prefabs/Rooms/Region1/R1_Room08.prefab. Re-runnable.
/// The Momentum Engine is rebuilt from the region's three machine parts, and its chain reaction frees the Great Shard.
///
/// Layout (room-local; floor top y = -4.5), west to east:
///   entry floor (from Room 7) | PERCH x -5.6..-4.6 (2 m) over a WET TRACK x -6.6..-2.6 with the 2 kg drive crate |
///   the ENGINE (paddle x -2.6..-2.3, flywheel centred x -1.2, 1.5 m radius) | HEART GATE x 2..2.6 (5.2 m tall) |
///   behind it the counterweight lift: pan B x 3..4.8 (floor, brake lever), pulley tower x 4.8..6.6 (from 5.5 m up),
///   pan A x 6.6..8.4 (5 m up, caged counterweight) | DAIS x 0.4..2.9 at 6 m with the Great Shard.
/// The chain:
///   0. Install: Interact with the engine. All three parts must have been found (Flywheel, Counterweight, Bearing); the
///      engine names what's missing and where. Installed: the flywheel and bearing appear, the counterweight drops into its cage.
///   1. Spin up (impulse, momentum): P1 Bursts the drive crate along the wet track into the paddle: it arrives with
///      ~13.6 kg·m/s, all of it goes into the flywheel. P2 on the perch tethers it back to reload. Friction drains 1.5 kg·m/s
///      per second (low: the Bearing). Full at 50 (~5 hits).
///   2. Run (friction as a timer): at 35 or more the Heart Gate is open: from 50 that's (50 - 35) / 1.5 = 10 s.
///   3. Ride (weight, equilibrium): both on pan B, pull the lever: the counterweight (88.3 N) beats you two (58.9 N), its pan
///      sinks and yours rises to 5 m; step onto the dais (6 m).
///   4. Free the Great Shard: both players beside it, Interact. Region complete; back to the village.
/// No skipping: the dais is reachable only from pan B at the top (the tower blocks the counterweight side); the gate is too tall
/// to jump; the lift does nothing useful until the counterweight is installed.
/// Extras: 3 shards (over the perch; on the east floor behind the gate; high over the dais).
/// </summary>
public static class Room8Builder
{
    private const string Room8Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room08.prefab";
    private const string FxLibraryPath = "Assets/_Project/Art/FX/FX_Library.asset";
    private const string WheelSpritePath = "Assets/_Project/Art/Sprites/Props/SP_PulleyWheel.png";
    private const string BearingSpritePath = "Assets/_Project/Art/Sprites/Props/SP_Part_Bearing.png";
    private const string CrystalPath = "Assets/_Project/Art/Village/Shrine_Crystal.png";
    private const float Floor = -4.5f, Travel = 5f, PanThickness = 0.25f, High = Floor + Travel;
    private const float TrackLeft = -6.6f, TrackRight = -2.6f, WheelX = -1.2f, WheelY = Floor + 2.2f;
    private const float GateLeft = 2f, GateRight = 2.6f, PanBLeft = 3f, PanBRight = 4.8f, PanALeft = 6.6f, PanARight = 8.4f;

    private static readonly string[] Built =
    {
        "Floor_West", "Floor_East", "Floor_Track", "Surface_Wet", "Perch", "Paddle", "Engine", "Engine_Frame", "Flywheel_Ghost",
        "Gate_Heart", "Pan_A", "Pan_B", "Cage_West", "Cage_East", "Cage_Top", "Scale_A", "Scale_B", "Lever_Brake", "Balance_Lift",
        "Pulley_Tower", "Dais", "Great_Shard", "Crate_Drive_2kg", "Crate_Counterweight_3kg",
        "Shrine_Room08", "Journal_Room08", "Shard_Perch", "Shard_East", "Shard_Dais",
    };

    [MenuItem("Tools/Shards of Motion/Room 8/1. Build Graybox")]
    public static void Build()
    {
        Transform ledge = Template("Ledge_Heavy"), liftT = Template("Lift"), plateT = Template("Plate_Heavy_80N"), gateT = Template("Gate_Exit"),
                  shrineT = Template("Shrine_Room01"), journalT = Template("Journal_Room01");
        if (ledge == null || liftT == null || plateT == null || gateT == null || shrineT == null || journalT == null) return;
        GameObject b2 = Load($"{Interactables}/Block_2kg.prefab"), b3 = Load($"{Interactables}/Block_3kg.prefab"),
                   shrineP = Load($"{Interactables}/ResetShrine.prefab"), journalP = Load($"{Interactables}/JournalStone.prefab"),
                   shardP = Load($"{Interactables}/MotionShard.prefab"), leverP = Load($"{Interactables}/Lever.prefab");
        PhysicsMaterial2D wet = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/_Project/PhysicsMaterials/Wet_Stone.physicsMaterial2D");
        Sprite wheelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WheelSpritePath), bearingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BearingSpritePath),
               crystal = AssetDatabase.LoadAssetAtPath<Sprite>(CrystalPath);
        if (b2 == null || b3 == null || shrineP == null || journalP == null || shardP == null || leverP == null || wet == null)
        {
            Debug.LogError("Room 8: a prefab in Prefabs/Interactables or the Wet_Stone material is missing. Nothing was changed.");
            return;
        }
        if (wheelSprite == null || bearingSprite == null)
            Debug.LogWarning("Room 8: run Room 6 > 2. Apply Visual Pass (draws the wheel) and Room 5 > 1. Build Graybox (draws the bearing) for the engine's art.");
        FxLibrary fx = AssetDatabase.LoadAssetAtPath<FxLibrary>(FxLibraryPath);
        JournalEntry entry = JournalTool.SaveRoom8Entry();

        GameObject room = PrefabUtility.LoadPrefabContents(Room8Path);
        try
        {
            Transform layout = Find(room.transform, "Layout"), puzzle = Find(room.transform, "Puzzle"), objects = Find(room.transform, "Objects"),
                      info = Find(room.transform, "Info"), floor = Find(room.transform, "Floor");
            if (layout == null || puzzle == null || objects == null || info == null || floor == null)
            {
                Debug.LogError("Room 8: R1_Room08 is missing Layout, Puzzle, Objects, Info or Floor. Nothing was changed.");
                return;
            }
            RemoveBuilt(room.transform, Built);

            // ---- Floor: stone west and east, the track's box 2 cm low under its wet edge (the edge carries the crate).
            SetRect(floor, -10f, Floor - 1f, TrackLeft, Floor);
            GameObject east = Object.Instantiate(floor.gameObject, layout); east.name = "Floor_East";
            SetRect(east.transform, TrackRight, Floor - 1f, 10f, Floor);
            GameObject trackBox = Object.Instantiate(floor.gameObject, layout); trackBox.name = "Floor_Track";
            SetRect(trackBox.transform, TrackLeft, Floor - 1.02f, TrackRight, Floor - 0.02f);
            GameObject surface = new GameObject("Surface_Wet") { layer = LayerMask.NameToLayer("Ground") };
            surface.transform.SetParent(layout, false);
            EdgeCollider2D edge = surface.AddComponent<EdgeCollider2D>();
            edge.points = new[] { new Vector2(TrackLeft, Floor), new Vector2(TrackRight, Floor) };
            edge.sharedMaterial = wet;
            edge.useAdjacentStartPoint = true; edge.adjacentStartPoint = new Vector2(TrackLeft - 1f, Floor);
            edge.useAdjacentEndPoint = true; edge.adjacentEndPoint = new Vector2(TrackRight + 1f, Floor);
            Solid(ledge, layout, "Perch", -5.6f, Floor + 1.7f, -4.6f, Floor + 2f);                // P2 reloads from here (~3.4 m to the paddle)
            Solid(ledge, layout, "Pulley_Tower", PanBRight, Floor + 5.5f, PanALeft, 5.7f);        // blocks the counterweight side
            Solid(ledge, layout, "Dais", 0.4f, Floor + 5.6f, 2.9f, Floor + 6f);                  // the Great Shard's dais (6 m)
            SetEntries(room.transform, new Vector2(-8.4f, Floor + 0.75f), new Vector2(-7.6f, Floor + 0.75f));

            // ---- The engine: install trigger, paddle, flywheel (+ ghost while empty), bearing, lamp.
            GameObject engineGo = new GameObject("Engine") { layer = LayerMask.NameToLayer("Interactable") };
            engineGo.transform.SetParent(puzzle, false);
            engineGo.transform.localPosition = new Vector3(WheelX, WheelY, 0f);
            BoxCollider2D trigger = engineGo.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(3.4f, 4.4f);
            MomentumEngine engine = engineGo.AddComponent<MomentumEngine>();

            GameObject frame = Clone(ledge, layout, "Engine_Frame"); // visual only: the engine's dark housing
            foreach (Collider2D c in frame.GetComponents<Collider2D>()) Object.DestroyImmediate(c);
            SetRect(frame.transform, WheelX - 1.7f, Floor, WheelX + 1.7f, WheelY + 1.9f);
            if (frame.TryGetComponent(out SpriteRenderer frameSr)) { frameSr.color = new Color(0.32f, 0.27f, 0.24f); frameSr.sortingLayerName = "BackDecor"; frameSr.sortingOrder = 20; }

            GameObject paddle = Solid(ledge, puzzle, "Paddle", TrackRight, Floor, TrackRight + 0.3f, Floor + 1.2f);
            paddle.AddComponent<EnginePaddle>().EditorSetup(engine);

            SpriteRenderer ghost = SpriteAt(puzzle, "Flywheel_Ghost", wheelSprite, new Vector3(WheelX, WheelY, 0f), 2f, "Props", 3);
            ghost.color = new Color(1f, 1f, 1f, 0.25f); // the empty slot: a faint outline of the missing flywheel
            SpriteRenderer wheel = SpriteAt(engineGo.transform, "Flywheel", wheelSprite, new Vector3(WheelX, WheelY, 0f), 2f, "Props", 4);
            SpriteRenderer bearing = SpriteAt(wheel.transform, "Bearing", bearingSprite, new Vector3(WheelX, WheelY, 0f), 1f, "Props", 5);
            Light2D lamp = new GameObject("Lamp").AddComponent<Light2D>();
            lamp.transform.SetParent(engineGo.transform, false);
            lamp.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            lamp.lightType = Light2D.LightType.Point;
            lamp.color = new Color(0.45f, 0.95f, 1f);
            lamp.intensity = 1.4f;
            lamp.pointLightOuterRadius = 3f;
            lamp.pointLightInnerRadius = 0.3f;
            lamp.enabled = false;

            // ---- The Heart Gate: open while the flywheel holds 35+ kg·m/s.
            Platform(gateT, puzzle, "Gate_Heart", GateLeft, Floor, GateRight, Floor + 5.2f, new Vector2(0f, 5.3f), 6f, engine, stopIfBlocked: true);

            // ---- The counterweight lift: pan A up (caged counterweight once installed), pan B on the floor, braked.
            Rigidbody2D panA = Pan(liftT, puzzle, "Pan_A", PanALeft, PanARight, High);
            Rigidbody2D panB = Pan(liftT, puzzle, "Pan_B", PanBLeft, PanBRight, Floor);
            Attach(Solid(ledge, puzzle, "Cage_West", PanALeft, High, PanALeft + 0.15f, High + 1.35f), panA);
            Attach(Solid(ledge, puzzle, "Cage_East", PanARight - 0.15f, High, PanARight, High + 1.35f), panA);
            Attach(Solid(ledge, puzzle, "Cage_Top", PanALeft, High + 1.2f, PanARight, High + 1.35f), panA);
            PressurePlate scaleA = Scale(plateT, puzzle, "Scale_A", PanALeft + 0.15f, PanARight - 0.15f, High, panA.transform);
            PressurePlate scaleB = Scale(plateT, puzzle, "Scale_B", PanBLeft, PanBRight, Floor, panB.transform);
            GameObject lever = Instance(leverP, puzzle, "Lever_Brake", new Vector2(PanBRight - 0.35f, Floor + 0.5f));
            StandOn(lever, Floor);
            lever.transform.SetParent(panB.transform, true);
            GameObject liftGo = new GameObject("Balance_Lift");
            liftGo.transform.SetParent(puzzle, false);
            liftGo.transform.localPosition = new Vector3((PanBLeft + PanARight) * 0.5f, Floor + 3f, 0f);
            BalanceLift lift = liftGo.AddComponent<BalanceLift>();
            lift.EditorSetup(panA, panB, scaleA, scaleB, Travel, 1f, lever.GetComponent<Lever>()); // start: pan A at the top
            lift.EditorSetKind(BalanceLift.Kind.Lift);

            // ---- Objects
            Crate(b2, objects, "Crate_Drive_2kg", -4.6f, Floor + 0.5f);
            GameObject counter = Crate(b3, objects, "Crate_Counterweight_3kg", (PanALeft + PanARight) * 0.5f, High + 0.5f);
            counter.SetActive(false); // appears in its cage when the engine's parts are installed
            engine.EditorSetup(new[] { "R1_Part_Flywheel", "R1_Part_Counterweight", "R1_Part_Bearing" },
                               new[] { "Flywheel (Room 3: Together We Move)", "Counterweight (Room 4: Broken Crossing)", "Bearing (Room 5: Long Push)" },
                               new[] { wheel.gameObject, counter }, wheel.transform, lamp, fx);
            wheel.gameObject.SetActive(false);

            // The Great Shard on the dais.
            GameObject shardGo = new GameObject("Great_Shard") { layer = LayerMask.NameToLayer("Interactable") };
            shardGo.transform.SetParent(puzzle, false);
            shardGo.transform.localPosition = new Vector3(1.5f, Floor + 6f, 0f);
            SpriteRenderer shardSr = shardGo.AddComponent<SpriteRenderer>();
            shardSr.sprite = crystal;
            shardSr.sortingLayerName = "Interactables";
            shardSr.sortingOrder = 5;
            BoxCollider2D shardTrigger = shardGo.AddComponent<BoxCollider2D>();
            shardTrigger.isTrigger = true;
            shardTrigger.offset = new Vector2(0f, 1f);
            shardTrigger.size = new Vector2(2.4f, 2f);
            Light2D shardGlow = new GameObject("Glow").AddComponent<Light2D>();
            shardGlow.transform.SetParent(shardGo.transform, false);
            shardGlow.transform.localPosition = new Vector3(0f, 1f, 0f);
            shardGlow.lightType = Light2D.LightType.Point;
            shardGlow.color = new Color(0.5f, 0.92f, 1f);
            shardGlow.pointLightOuterRadius = 4f;
            shardGlow.pointLightInnerRadius = 0.4f;
            shardGo.AddComponent<GreatShard>().EditorSetup(shardGlow, room.name);

            float shrineLift = shrineT.localPosition.y - Floor, journalLift = journalT.localPosition.y - Floor;
            Instance(shrineP, info, "Shrine_Room08", new Vector2(1.1f, Floor + shrineLift)); // west of the Heart Gate
            GameObject journal = Instance(journalP, info, "Journal_Room08", new Vector2(-8.6f, Floor + journalLift));
            if (journal.TryGetComponent(out JournalStone stone)) { stone.EditorSetEntry(entry); EditorUtility.SetDirty(stone); }

            Instance(shardP, objects, "Shard_Perch", new Vector2(-5.1f, Floor + 4.3f)); // jump from the perch
            Instance(shardP, objects, "Shard_East", new Vector2(6f, Floor + 2.3f));   // behind the Heart Gate
            Instance(shardP, objects, "Shard_Dais", new Vector2(1.7f, Floor + 8.3f)); // jump from the dais

            PrefabUtility.SaveAsPrefabAsset(room, Room8Path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(room);
        }
        Debug.Log("Room 8: graybox built in R1_Room08 (Heart of the Forest). Play it from Dev > Play From Room > Room 8.");
    }

    private static SpriteRenderer SpriteAt(Transform parent, string name, Sprite sprite, Vector3 localPos, float scale, string layer, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = localPos; // the prefab's root sits at the origin: room-local = world while editing it
        go.transform.localScale = Vector3.one * scale / Mathf.Max(0.0001f, parent.lossyScale.x); // true size under a scaled parent
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return sr;
    }

    private static Rigidbody2D Pan(Transform liftT, Transform parent, string name, float left, float right, float top)
    {
        GameObject go = Solid(liftT, parent, name, left, top - PanThickness, right, top);
        if (go.TryGetComponent(out Mover mover)) Object.DestroyImmediate(mover);
        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        return rb;
    }

    private static void Attach(GameObject part, Rigidbody2D pan) => part.transform.SetParent(pan.transform, true);

    private static PressurePlate Scale(Transform plateT, Transform parent, string name, float left, float right, float top, Transform pan)
    {
        PressurePlate plate = Plate(plateT, parent, name, (left + right) * 0.5f, top, 9999f, PlateReadout.Display.ReadingOnly, true, right - left);
        SerializedObject so = new SerializedObject(plate);
        so.FindProperty("maxRestingSpeed").floatValue = 4f;
        so.ApplyModifiedPropertiesWithoutUndo();
        plate.transform.SetParent(pan, true);
        return plate;
    }

    private static void StandOn(GameObject go, float groundY)
    {
        SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        go.transform.position += new Vector3(0f, groundY - sr.bounds.min.y, 0f);
    }
}
