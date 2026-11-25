using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static RoomBuildKit;
using static FinaleKit;

/// <summary>
/// Builds the finale's PART 3, "The Great Shard", inside Prefabs/Rooms/Region1/R1_Room10.prefab. Re-runnable.
/// Needs Finale > 1. Add Finale Rooms To Scene.
/// Story: the engine's pulse has reached the chamber; the cracked Great Shard hangs over a bed of thorns. The way up is
/// two climbs, one each, to two pedestals; the shard heals only while both of you hold on together.
///
/// Layout (room-local; floor top y = -4.5):
///   entry floor (from part 2) | WEST CLIMB: ledge x -9.6..-8.4 at 2.5 m, a LIFT x -7.6..-6.4 riding 2.5 -> 5.8 m, the
///   WEST PEDESTAL ledge x -5.6..-3.4 at 5.9 m | THORNS x -3..3 on the floor | the GREAT SHARD over them (x 0, ~6.2 m) |
///   EAST CLIMB mirrored (a lift with a different rhythm) and the EAST PEDESTAL ledge x 3.4..5.6 at 5.9 m.
/// The lifts run only once the Heart Engine has woken (saved "r1_engine_awake", EventSignal): the engine powers the
/// chamber. Without them the pedestal ledges (5.9 m) are out of reach (jump 2.5 m + float).
/// The finale: stand on a pedestal each and HOLD Interact together for 3 s (ShardPedestal + GreatShard). Let go and the
/// light drains back. Freed: slow motion, the shard heals and rises, the region is saved as complete, the end-of-region
/// cards play (every room and what it taught, Wren's words), then home to the village.
/// Extras: 1 shard hanging over the thorns (dash across from a lift at the right moment).
/// </summary>
public static class Room10Builder
{
    private const string Room10Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room10.prefab";
    private const string CrystalPath = "Assets/_Project/Art/Village/Shrine_Crystal.png";
    private const float PedestalTop = Floor + 5.9f;

    private static readonly string[] Built =
    {
        "Thorns", "Ledge_West", "Ledge_East", "Lift_West", "Lift_East", "Power_West", "Power_East", "Pedestal_West", "Pedestal_East",
        "Pad_West", "Pad_East", "Beam_West", "Beam_East", "Great_Shard", "Shrine_Room10", "Shard_Thorns",
    };

    // What each room taught, for the end-of-region cards (same order as the map's rooms).
    private static readonly string[] Lessons =
    {
        "Nothing moves until a force makes it. Balanced forces: no change at all.",
        "The same push moves a lighter thing faster: a = F ÷ m.",
        "Two pushes together add up: the net force is what counts.",
        "An impulse becomes momentum: J = Δp. Enough of it breaks a wall.",
        "Friction takes momentum away, a little every second.",
        "Weight is a force, W = m·g. Balance it, and you can lift each other.",
        "The heavier and the faster, the harder it is to stop.",
        "Gravity gives every mass the same speed; the heavier cart brings more momentum. Together, you woke the heart of the forest.",
    };

    private const string Closing =
        "\"You did what I never could alone.\nThe forest is breathing again. Come home, you two.\"\n<size=70%>- Wren</size>";

    [MenuItem("Tools/Shards of Motion/Room 8 Part 3/1. Build Graybox (The Great Shard)")]
    public static void Build()
    {
        Transform ledge = Template("Ledge_Heavy"), liftT = Template("Lift");
        if (ledge == null || liftT == null) return;
        GameObject shrineP = Load($"{Interactables}/ResetShrine.prefab"), shardP = Load($"{Interactables}/MotionShard.prefab");
        Sprite crystal = AssetDatabase.LoadAssetAtPath<Sprite>(CrystalPath);
        if (shrineP == null || shardP == null) { Debug.LogError("Room 8 part 3: a prefab in Prefabs/Interactables is missing. Nothing was changed."); return; }
        if (Load(Room10Path) == null) { Debug.LogError("Room 8 part 3: R1_Room10 doesn't exist yet. Run Finale > 1. Add Finale Rooms To Scene."); return; }
        FinaleArt.Ensure(Room9Builder.Notches);
        RegionMapData map = MapTool.EnsureData();
        float shrineLift = ShrineLift;

        GameObject room = PrefabUtility.LoadPrefabContents(Room10Path);
        try
        {
            Transform layout = Find(room.transform, "Layout"), puzzle = Find(room.transform, "Puzzle"), objects = Find(room.transform, "Objects"),
                      info = Find(room.transform, "Info"), hazards = Find(room.transform, "Hazards"), floor = Find(room.transform, "Floor");
            if (layout == null || puzzle == null || objects == null || info == null || floor == null)
            {
                Debug.LogError("Room 8 part 3: R1_Room10 is missing Layout, Puzzle, Objects, Info or Floor. Nothing was changed.");
                return;
            }
            if (hazards == null) hazards = puzzle;
            RemoveBuilt(room.transform, Built);
            SetRect(floor, -10f, Floor - 1f, 10f, Floor);
            SetEntries(room.transform, new Vector2(-8.4f, Floor + 0.75f), new Vector2(-7.6f, Floor + 0.75f));

            // ---- Thorns (a hazard strip on the floor under the shard).
            GameObject thorns = Clone(ledge, hazards, "Thorns");
            thorns.layer = 0;
            SetRect(thorns.transform, -3f, Floor, 3f, Floor + 0.4f);
            thorns.GetComponent<BoxCollider2D>().isTrigger = true;
            if (thorns.TryGetComponent(out SpriteRenderer thornSr)) thornSr.color = new Color(0.55f, 0.25f, 0.55f);
            thorns.AddComponent<HazardZone>();

            // ---- The two climbs: ledge -> powered lift -> pedestal ledge.
            Solid(ledge, layout, "Ledge_West", -9.6f, Floor + 2.2f, -8.4f, Floor + 2.5f);
            Solid(ledge, layout, "Ledge_East", 8.4f, Floor + 2.2f, 9.6f, Floor + 2.5f);
            Solid(ledge, layout, "Pedestal_West", -5.6f, PedestalTop - 0.3f, -3.4f, PedestalTop);
            Solid(ledge, layout, "Pedestal_East", 3.4f, PedestalTop - 0.3f, 5.6f, PedestalTop);
            EventSignal powerW = Power(puzzle, "Power_West");
            EventSignal powerE = Power(puzzle, "Power_East");
            LoopLift(Platform(liftT, puzzle, "Lift_West", -7.6f, Floor + 2.2f, -6.4f, Floor + 2.5f, new Vector2(0f, 3.3f), 1.6f, powerW, false), 0.9f);
            LoopLift(Platform(liftT, puzzle, "Lift_East", 6.4f, Floor + 2.2f, 7.6f, Floor + 2.5f, new Vector2(0f, 3.3f), 2.3f, powerE, false), 0.5f);

            // ---- The Great Shard and its two pedestals.
            GameObject shardGo = new GameObject("Great_Shard") { layer = LayerMask.NameToLayer("Interactable") };
            shardGo.transform.SetParent(puzzle, false);
            shardGo.transform.localPosition = new Vector3(0f, Floor + 5.4f, 0f);
            SpriteRenderer shardSr = shardGo.AddComponent<SpriteRenderer>();
            shardSr.sprite = crystal;
            shardSr.sortingLayerName = "Interactables";
            shardSr.sortingOrder = 5;
            shardSr.color = new Color(0.75f, 0.85f, 0.95f); // dimmed: cracked, its light failing
            BoxCollider2D shardTrigger = shardGo.AddComponent<BoxCollider2D>();
            shardTrigger.isTrigger = true;
            shardTrigger.offset = new Vector2(0f, 1f);
            shardTrigger.size = new Vector2(2.4f, 2.4f);
            Light2D glow = Lamp(shardGo.transform, "Glow", new Vector2(0f, 1f), new Color(0.5f, 0.92f, 1f), 5f, 1.2f, true);

            ShardPedestal west = Pad(puzzle, "Pad_West", "Beam_West", new Vector2(-4.5f, PedestalTop), shardGo.transform, new Color(1f, 0.65f, 0.3f));
            ShardPedestal east = Pad(puzzle, "Pad_East", "Beam_East", new Vector2(4.5f, PedestalTop), shardGo.transform, new Color(0.35f, 0.85f, 0.95f));
            GreatShard great = shardGo.AddComponent<GreatShard>();
            great.EditorSetup(glow, "R1_Room08"); // the finale is solved as Room 8 (one entry on the map)
            great.EditorSetupFinale(new[] { west, east }, 3f, null, map, Lessons, Closing);

            // ---- Info and extras
            Instance(shrineP, info, "Shrine_Room10", new Vector2(-5.8f, Floor + shrineLift));
            Instance(shardP, objects, "Shard_Thorns", new Vector2(0f, Floor + 3.4f));

            PrefabUtility.SaveAsPrefabAsset(room, Room10Path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(room);
        }
        Debug.Log("Room 8 part 3: graybox built in R1_Room10 (The Great Shard).");
    }

    private static EventSignal Power(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        EventSignal s = go.AddComponent<EventSignal>();
        s.EditorSetup("r1_engine_awake", 0f, 0f);
        return s;
    }

    // Loop mode: rides up and down on its own while powered.
    private static void LoopLift(Mover m, float wait)
    {
        SerializedObject so = new SerializedObject(m);
        so.FindProperty("mode").enumValueIndex = (int)Mover.MoverMode.Loop;
        so.FindProperty("waitAtEnds").floatValue = wait;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static ShardPedestal Pad(Transform parent, string name, string beamName, Vector2 top, Transform shard, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = top;
        BoxCollider2D zone = go.AddComponent<BoxCollider2D>();
        zone.isTrigger = true;
        zone.size = new Vector2(1.6f, 1.4f);
        zone.offset = new Vector2(0f, 0.7f);
        Light2D rune = Lamp(go.transform, "Rune", new Vector2(0f, 0.4f), color, 2.2f, 0.4f, true);
        SpriteRenderer beam = SpriteAt(parent, beamName, FinaleArt.Get("Pixel"), Vector3.zero, 1f, "FX", 3);
        beam.enabled = false;
        ShardPedestal pad = go.AddComponent<ShardPedestal>();
        pad.EditorSetup(shard, beam, rune, color);
        return pad;
    }
}
