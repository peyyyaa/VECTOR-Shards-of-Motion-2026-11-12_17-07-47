using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static RoomBuildKit;
using static FinaleKit;

/// <summary>
/// Builds the finale's PART 2, "The Heart Engine", inside Prefabs/Rooms/Region1/R1_Room09.prefab. Re-runnable.
/// Needs: Finale > 1. Add Finale Rooms To Scene, and Room 8 > 0. Import Engine Art.
/// Story: the delivered cart waits in the old engine hall. The Momentum Engine fills the hall (the team's art at 2x).
///
/// Layout (room-local; floor top y = -4.5):
///   doorway landing from part 1 (x -10..-6.5 at 3.4 m; the ALCOVE platform in it) | the RAMP x -6.5..-0.5 (wet, 3.4 m down)
///   | P1's BALCONY over the ramp (x -3.8..-0.05, top 4.9 m: the ore hopper and the hammer release lever), reached from the
///   landing; its east end is a beam up to the roof, so nobody walks off onto the cart's rail by accident
///   | the LOW RAIL x -1.6..0.2 (2.8 -> 3 m: the cart's way off the lift, nobody's station) | the CHAIN LIFT shaft x 0.2..1.8
///   (its loop returns behind, x 2.9) | the STRIKER x 1.8..2.1 | the WINCH HAMMER hanging at x 2.15 beside the low stop, its
///   ratchet drum at the pivot | the ENGINE (flywheel x 3.5, 4.9 m up) | P2's station on the floor: the WINCH ring (x 9 -> 6.6),
///   its rope running up the engine's right post and along its top beam to the drum | exit gate x 9.3..9.8.
/// Every run: the cart hits the striker (its momentum goes into the flywheel), rests on the chain's floor platform and is
/// carried up to the LOW stop under the ore chute. P1 may drop ore into it from the balcony (heavier cart). P2 winds the
/// hammer (tether the winch ring: 6:1 pulley, ratchet); P1 releases it: it swings down and knocks the cart off the rail
/// (a 1D collision, e = 0.9: the heavier the cart, up to the hammer's 4 kg, the more of the hammer's momentum it takes).
/// The cart flies onto the ramp (higher with a bigger hit) and comes back down into the striker.
/// Engine stages (EngineStage): 30 = the hall lamps; 60 = the alcove platform rises (to a shard up under the roof, out of
/// reach from the balcony); 100 = THE AWAKENING (saved "r1_engine_awake"): the exit gate opens for good and the Great
/// Shard's chamber (part 3) comes alive.
/// The engine must first be assembled (all three parts found; the assembly screen).
/// Art: Room 8 Part 2 > 0. Build Heart Engine Art (runs automatically if missing); the look: 2. Apply Visual Pass.
/// </summary>
public static class Room9Builder
{
    private const string Room9Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room09.prefab";
    public const float Landing = FinaleSetup.Part1To2Doorway, RampFoot = -0.5f;
    public const float LowStop = Floor + 3f, HighStop = Floor + 5.5f, RailLeft = -1.6f; // HighStop: unused since the road switch was removed
    public const float ShaftLeft = 0.2f, ShaftRight = 1.8f, ShaftX = 1f, ReturnX = 2.9f, WheelX = 3.5f, EngineScale = 2f;
    public const float HammerX = 2.15f, HammerChain = 2.5f, PivotY = LowStop + 0.4f + HammerChain;
    public const float ChainTop = Floor + 6.4f, ChainBottom = Floor - 1.5f; // the chain loop's deck-top heights
    // P1's balcony. Its top (4.9 m) keeps the alcove shard (9.4 m) out of reach from it (jump 2.5 m + body 1.5 m + the
    // shard's 0.32 m radius = 4.3 m: 9.2 < 9.4), while the risen alcove platform (6 m) reaches it.
    public const float BalconyTop = Floor + 4.9f, BalconyLeft = -3.8f, PostLeft = -0.25f, PostRight = -0.05f, HopperX = -0.8f, CatchX = -2.4f;
    public const float ChuteMouthX = 1.15f, ChuteMouthY = LowStop + 1.9f;
    private const string LeverSprite = "Assets/_Project/Art/Sprites/Props/SP_Lever.png";
    public const float MaxMomentum = 100f, Stage1 = 30f, Stage2 = 60f;
    public static readonly float[] Notches = { Stage1 / MaxMomentum, Stage2 / MaxMomentum, 1f };

    private static readonly string[] Built =
    {
        // this version
        "Floor_Track", "Floor_East", "Shaft_Cover", "Sign_Crossing", "Cart_Stop", "Surface_Wet", "Ramp", "Rail_Low", "Balcony", "Balcony_Post", "Chain_Lift",
        "Winch_Hammer", "Hammer_Catch", "Cord_Winch", "Slot_Winch",
        "Paddle", "Striker", "Engine", "Engine_Body", "Engine_Runes", "Ore_Chute", "Momentum_Column", "Hall_Lamps",
        "Stage_Lamps", "Stage_Awake", "Engine_Milestones", "Platform_Alcove", "Gate_Exit", "Heart_Cart", "Shrine_Room09", "Journal_Room09", "Shard_Alcove",
        // earlier versions (the road switch: sky rail, points gate and cord, road lever)
        "Rail_Sky", "Gate_Points", "Lever_Road", "Cord_Points", "Slot_Points",
        "Bumper", "Rail_Return", "Rail_Wet", "Lift_Return", "Lever_Lift", "Lift_Pit", "Gate_Guard", "Lift_Sensor", "Lift_Away", "Paternoster",
        "Shard_Bumper", "Shard_Lift",
    };

    [MenuItem("Tools/Shards of Motion/Room 8 Part 2/1. Build Graybox (The Heart Engine)")]
    public static void Build()
    {
        Transform ledge = Template("Ledge_Heavy"), liftT = Template("Lift"), gateT = Template("Gate_Exit");
        if (ledge == null || liftT == null || gateT == null) return;
        GameObject b1 = Load($"{Interactables}/Block_1kg.prefab"), b2 = Load($"{Interactables}/Block_2kg.prefab"),
                   shrineP = Load($"{Interactables}/ResetShrine.prefab"), journalP = Load($"{Interactables}/JournalStone.prefab"),
                   shardP = Load($"{Interactables}/MotionShard.prefab");
        if (b1 == null || b2 == null || shrineP == null || journalP == null || shardP == null || Wet == null)
        {
            Debug.LogError("Room 8 part 2: a prefab in Prefabs/Interactables or the Wet_Stone material is missing. Nothing was changed.");
            return;
        }
        Sprite bodySprite = EngineArtTool.Load("Engine_Base"), loadedSprite = EngineArtTool.Load("Engine_Base_Loaded"),
               wheelArt = EngineArtTool.Load("Engine_Wheel"), striker = EngineArtTool.Load("Striker");
        Sprite[] runeSprites = new Sprite[EngineArtTool.RuneSegments];
        for (int i = 0; i < runeSprites.Length; i++) runeSprites[i] = EngineArtTool.Load($"Engine_Rune_{i:00}");
        if (bodySprite == null || loadedSprite == null || wheelArt == null || striker == null || System.Array.Exists(runeSprites, r => r == null))
        {
            Debug.LogError("Room 8 part 2: the engine art is missing. Run Room 8 > 0. Import Engine Art first. Nothing was changed.");
            return;
        }
        if (Load(Room9Path) == null) { Debug.LogError("Room 8 part 2: R1_Room09 doesn't exist yet. Run Finale > 1. Add Finale Rooms To Scene."); return; }
        FinaleArt.Ensure(Notches);
        HeartEngineArt.Ensure();
        Sprite leverSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LeverSprite);
        Material chainMat = HeartEngineArt.Mat("Line_Chain"), ropeMat = HeartEngineArt.Mat("Line_Rope");
        FxLibrary fx = FxLib;
        JournalEntry entry = JournalTool.SaveRoom8Entry();
        float shrineLift = ShrineLift, journalLift = JournalLift;

        GameObject room = PrefabUtility.LoadPrefabContents(Room9Path);
        try
        {
            Transform layout = Find(room.transform, "Layout"), puzzle = Find(room.transform, "Puzzle"), objects = Find(room.transform, "Objects"),
                      info = Find(room.transform, "Info"), floor = Find(room.transform, "Floor");
            if (layout == null || puzzle == null || objects == null || info == null || floor == null)
            {
                Debug.LogError("Room 8 part 2: R1_Room09 is missing Layout, Puzzle, Objects, Info or Floor. Nothing was changed.");
                return;
            }
            RemoveBuilt(room.transform, Built);

            // ---- Layout: floor split around the chain's shaft; wet run from the ramp's foot to the striker; the two rails.
            SetRect(floor, -10f, Floor - 1f, RampFoot, Floor);
            GameObject track = Object.Instantiate(floor.gameObject, layout); track.name = "Floor_Track";
            SetRect(track.transform, RampFoot, Floor - 1.02f, ShaftLeft, Floor - 0.02f);
            GameObject east = Object.Instantiate(floor.gameObject, layout); east.name = "Floor_East";
            SetRect(east.transform, ShaftRight, Floor - 1f, 10f, Floor);
            // A grate over the lift shaft, flush with the track (2 cm under the floor line): players can always walk across,
            // even while the floor platform is up the shaft with the cart. The platforms are kinematic, so they rise through it
            // (a static collider) and still lift the cart from the floor line; nothing ever needs to go down the shaft.
            GameObject cover = Object.Instantiate(floor.gameObject, layout); cover.name = "Shaft_Cover";
            SetRect(cover.transform, ShaftLeft, Floor - 1.02f, ShaftRight, Floor - 0.02f);
            foreach (Collider2D c in cover.GetComponents<Collider2D>()) c.sharedMaterial = Wet;
            WetEdge(layout, "Surface_Wet", RampFoot, ShaftLeft, Floor);
            Ramp(ledge, layout, "Ramp", new Vector2(-6.5f, Floor + Landing), new Vector2(RampFoot, Floor), Floor - 0.5f);
            // The rail is thin and slopes gently down to the west, so a cart pushed off the chain always rolls on, and a cart
            // coming down the ramp (it stays level: ~1.4 m tall on the slope) has room under it.
            Slab(ledge, layout, "Rail_Low", new Vector2(RailLeft, LowStop - 0.2f), new Vector2(ShaftLeft, LowStop), 0.15f);
            SetEntries(room.transform, new Vector2(-8.4f, Floor + Landing + 0.75f), new Vector2(-7.6f, Floor + Landing + 0.75f));

            // ---- P1's balcony over the ramp (cart below: its top ~3.95 m at the rail, the balcony's underside 4.6 m). The
            // beam at its east end runs up to the roof: P1 can't walk off onto the rail or the chain, only leave westward.
            Solid(ledge, layout, "Balcony", BalconyLeft, BalconyTop - 0.3f, PostRight, BalconyTop);
            Solid(ledge, layout, "Balcony_Post", PostLeft, BalconyTop, PostRight, Ceiling);

            // ---- The cart and the CHAIN LIFT (an endless loop: up the shaft, over, down behind, back under the floor).
            HeartCart cart = Cart(b2, objects, "Heart_Cart", 1.15f, Floor);
            GameObject chainGo = new GameObject("Chain_Lift");
            chainGo.transform.SetParent(puzzle, false);
            const float deck = 0.3f, bottom = ChainBottom, top = ChainTop;
            Rigidbody2D[] steps = new Rigidbody2D[6];
            for (int i = 0; i < steps.Length; i++)
            {
                Vector2 d = ChainLift.ParkedDeck(i, steps.Length, ShaftX, ReturnX, bottom, top, Floor);
                GameObject step = Solid(liftT, chainGo.transform, $"Step_{i + 1}", d.x - 0.8f, d.y - deck, d.x + 0.8f, d.y);
                if (step.TryGetComponent(out Mover mover)) Object.DestroyImmediate(mover);
                foreach (Collider2D c in step.GetComponents<Collider2D>()) c.sharedMaterial = Wet;
                Rigidbody2D rb = step.GetComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                steps[i] = rb;
                // The deck's look (the collider is unchanged): oak planks with iron ends, true pixel size under the scaled step.
                if (step.TryGetComponent(out SpriteRenderer stepSr)) stepSr.enabled = false;
                TrueSize(SpriteAt(step.transform, "Deck", HeartEngineArt.Get("Lift_Deck"), new Vector3(d.x, d.y - deck * 0.5f, 0f), 1f, "Midground", 5));
            }
            LineRenderer chainLine = Line(chainGo.transform, "Chain", Color.white, 6f / 32f, "BackDecor", 5);
            Textured(chainLine, chainMat);
            ChainLift lift = chainGo.AddComponent<ChainLift>();
            lift.EditorSetup(steps, cart, null, ShaftX, ReturnX, bottom, top, Floor, LowStop, HighStop, chainLine); // no road switch: always the low stop
            // The two top sprockets the chain runs over (turned by the chain: ChainGears).
            Transform[] sprockets =
            {
                SpriteAt(chainGo.transform, "Sprocket_Up", HeartEngineArt.Get("Sprocket"), new Vector3(ShaftX, top, 0f), 1f, "BackDecor", 4).transform,
                SpriteAt(chainGo.transform, "Sprocket_Down", HeartEngineArt.Get("Sprocket"), new Vector3(ReturnX, top, 0f), 1f, "BackDecor", 4).transform,
            };
            chainGo.AddComponent<ChainGears>().EditorSetup(lift, sprockets, 0.5f);

            // ---- P2's station on the floor: the winch ring (tether it, pull west). Its rope runs east to a floor pulley, up the
            // engine's right post, along under its top beam and down to the hammer's drum (not across the hall).
            CordHandle winch = Cord(b1, puzzle, "Cord_Winch", 9f, Vector2.left, 2.4f, 19.6f, new[]
            {
                new Vector2(9.2f, Floor + 0.25f), new Vector2(9.2f, Floor + 8.75f), new Vector2(HammerX, Floor + 8.75f), new Vector2(HammerX, PivotY + 0.4f),
            }, ropeMat);
            SlotMark(ledge, layout, "Slot_Winch", 6.6f, 9f);

            // ---- The Winch Hammer beside the low stop: head, chain, the ratchet drum at its pivot.
            GameObject hammerGo = new GameObject("Winch_Hammer");
            hammerGo.transform.SetParent(puzzle, false);
            SpriteRenderer head = SpriteAt(hammerGo.transform, "Head", HeartEngineArt.Get("Hammer_Head_Large"), new Vector3(HammerX, LowStop + 0.4f, 0f), 1f, "Props", 6);
            LineRenderer hammerChain = Line(hammerGo.transform, "Chain", Color.white, 6f / 32f, "Props", 5);
            Textured(hammerChain, chainMat);
            WinchHammer hammer = hammerGo.AddComponent<WinchHammer>();
            hammer.EditorSetup(head.transform, new Vector2(HammerX, PivotY), HammerChain, 1f, 4f, 6f, winch, cart, hammerChain);
            SpriteRenderer wheelSr = SpriteAt(hammerGo.transform, "Ratchet_Wheel", HeartEngineArt.Get("Ratchet_Wheel"), new Vector3(HammerX, PivotY, 0f), 1f, "Props", 7);
            SpriteRenderer pawlSr = SpriteAt(hammerGo.transform, "Ratchet_Pawl", HeartEngineArt.Get("Ratchet_Pawl"), new Vector3(HammerX - 0.75f, PivotY + 0.25f, 0f), 1f, "Props", 8);
            pawlSr.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            WinchRatchet ratchet = hammerGo.AddComponent<WinchRatchet>();
            ratchet.EditorSetup(hammer, wheelSr.transform, pawlSr.transform);
            SerializedObject rso = new SerializedObject(ratchet);
            rso.FindProperty("pawlRest").floatValue = -18f;
            rso.FindProperty("pawlLift").floatValue = 12f;
            rso.ApplyModifiedPropertiesWithoutUndo();

            // ---- Reading the hammer: a height ruler east of its swing (marks 0.5 / 1 / 1.5 m, a live marker), the dotted
            // path it will swing along, and the pulley's numbers by the winch ring (HammerGauge).
            const float rulerX = 5.15f;
            float restY = LowStop + 0.4f; // the head's centre at the bottom of the swing: height 0
            SpriteRenderer backing = SpriteAt(hammerGo.transform, "Ruler_Backing", FinaleArt.Get("Pixel"), new Vector3(rulerX + 0.45f, restY + 0.8f, 0f), 1f, "Props", 1); // ruler, ticks and labels
            backing.transform.localScale = new Vector3(1.35f * 32f, 2.05f * 32f, 1f); // a dark strip (the 1 px sprite scaled), so the ruler reads over the flywheel
            backing.color = new Color(0.05f, 0.06f, 0.09f, 0.55f);
            SpriteRenderer ruler = SpriteAt(hammerGo.transform, "Ruler", HeartEngineArt.Get("Ruler"), new Vector3(rulerX, restY + 0.78f, 0f), 1f, "Props", 2);
            ruler.drawMode = SpriteDrawMode.Tiled;
            ruler.size = new Vector2(6f / 32f, 1.75f);
            foreach (float h in new[] { 0f, 0.5f, 1f, 1.5f })
                SpriteAt(hammerGo.transform, $"Ruler_Tick_{h:0.0}", HeartEngineArt.Get("Ruler_Tick"), new Vector3(rulerX + 0.09f, restY + h, 0f), 1f, "Props", 3);
            SpriteRenderer heightMarker = SpriteAt(hammerGo.transform, "Ruler_Marker", HeartEngineArt.Get("Ruler_Marker"), new Vector3(rulerX - 0.4f, restY, 0f), 1f, "Props", 4);
            LineRenderer swingPath = Line(hammerGo.transform, "Swing_Path", Color.white, 4f / 32f, "Props", 9);
            Textured(swingPath, HeartEngineArt.Mat("Line_Dots"));
            swingPath.enabled = false;
            hammerGo.AddComponent<HammerGauge>().EditorSetup(hammer, winch, new Vector2(rulerX - 0.4f, restY), heightMarker.transform, swingPath,
                                                            new Vector2(7.8f, Floor + 1.8f));

            // ---- P1's hammer release lever on the balcony's west half: in view of the hammer and the cart below.
            GameObject catchGo = new GameObject("Hammer_Catch") { layer = LayerMask.NameToLayer("Interactable") };
            catchGo.transform.SetParent(puzzle, false);
            catchGo.transform.localPosition = new Vector3(CatchX, BalconyTop + 0.7f, 0f);
            BoxCollider2D catchZone = catchGo.AddComponent<BoxCollider2D>();
            catchZone.isTrigger = true;
            catchZone.size = new Vector2(0.9f, 1.4f);
            SpriteRenderer catchSr = SpriteAt(catchGo.transform, "Lever", leverSprite, Vector3.zero, 1f, "Interactables", 0);
            if (catchSr.sprite != null) catchSr.transform.position = new Vector3(CatchX, BalconyTop + catchSr.sprite.bounds.extents.y - catchSr.sprite.bounds.center.y, 0f);
            catchGo.AddComponent<HammerCatch>().EditorSetup(hammer);

            // ---- The engine at 2x: body, rune meter, striker, flywheel + blur, hub light, lamp. Drawn behind everything.
            float wheelY = Floor + EngineArtTool.WheelOffset.y * EngineScale;
            GameObject engineGo = new GameObject("Engine") { layer = LayerMask.NameToLayer("Interactable") };
            engineGo.transform.SetParent(puzzle, false);
            engineGo.transform.localPosition = new Vector3(WheelX, wheelY, 0f);
            BoxCollider2D trigger = engineGo.AddComponent<BoxCollider2D>(); // to install: stand by the engine's foot
            trigger.isTrigger = true;
            trigger.size = new Vector2(4f, 2.4f);
            trigger.offset = new Vector2(0f, Floor + 1.2f - wheelY);
            MomentumEngine engine = engineGo.AddComponent<MomentumEngine>();

            Vector3 bodyAt = new Vector3(WheelX - EngineArtTool.WheelOffset.x * EngineScale, Floor, 0f);
            SpriteRenderer body = SpriteAt(puzzle, "Engine_Body", bodySprite, bodyAt, EngineScale, "Background", 20);
            GameObject runeGroup = new GameObject("Engine_Runes");
            runeGroup.transform.SetParent(puzzle, false);
            SpriteRenderer[] runes = new SpriteRenderer[runeSprites.Length];
            for (int i = 0; i < runes.Length; i++)
            {
                runes[i] = SpriteAt(runeGroup.transform, $"Rune_{i:00}", runeSprites[i], bodyAt, EngineScale, "Background", 21);
                runes[i].color = Color.clear;
            }
            GameObject paddle = Solid(ledge, puzzle, "Paddle", ShaftRight, Floor, ShaftRight + 0.3f, Floor + 1.2f);
            paddle.AddComponent<EnginePaddle>().EditorSetup(engine);
            foreach (Collider2D c in paddle.GetComponents<Collider2D>()) c.sharedMaterial = Wet; // the cart rides up past it
            if (paddle.TryGetComponent(out SpriteRenderer paddleSr)) paddleSr.enabled = false;
            paddle.AddComponent<PlayerPassThrough>(); // players walk past the striker; the cart still hits it
            // An invisible cart stop on the shaft's east side, from the striker's top to above the cart's stop: the cart can't
            // be pushed off the lift to the east (it would be stuck behind the striker). Players pass through it too.
            GameObject cartStop = Solid(ledge, puzzle, "Cart_Stop", ShaftRight + 0.02f, Floor + 1.2f, ShaftRight + 0.12f, LowStop + 1f);
            foreach (Collider2D c in cartStop.GetComponents<Collider2D>()) c.sharedMaterial = Wet; // the cart slides up past it
            if (cartStop.TryGetComponent(out SpriteRenderer stopSr)) stopSr.enabled = false;
            cartStop.AddComponent<PlayerPassThrough>();
            SpriteAt(puzzle, "Striker", striker, new Vector3(ShaftRight + 0.15f, Floor, 0f), 1f, "Props", 4);

            SpriteRenderer wheel = SpriteAt(engineGo.transform, "Flywheel", wheelArt, new Vector3(WheelX, wheelY, 0f), EngineScale, "Background", 22);
            SpriteRenderer[] blur = new SpriteRenderer[2];
            for (int i = 0; i < blur.Length; i++)
            {
                blur[i] = SpriteAt(wheel.transform, $"Blur_{i + 1}", wheelArt, new Vector3(WheelX, wheelY, 0f), EngineScale, "Background", 23 + i);
                blur[i].transform.localRotation = Quaternion.Euler(0f, 0f, 9f * (i + 1));
                blur[i].color = Color.clear;
            }
            Light2D hub = new GameObject("Hub_Light").AddComponent<Light2D>();
            hub.transform.SetParent(engineGo.transform, false);
            hub.lightType = Light2D.LightType.Point;
            hub.color = new Color(0.45f, 0.95f, 1f);
            hub.pointLightInnerRadius = 0.3f;
            hub.enabled = false;
            Light2D lamp = Lamp(engineGo.transform, "Lamp", new Vector2(0f, 4.6f), new Color(0.45f, 0.95f, 1f), 4f, 1.4f, false);

            engine.EditorSetup(new[] { "R1_Part_Flywheel", "R1_Part_Counterweight", "R1_Part_Bearing" },
                               new[] { "Flywheel (Room 3: Together We Move)", "Counterweight (Room 4: Broken Crossing)", "Bearing (Room 5: Long Push)" },
                               new[] { wheel.gameObject }, wheel.transform, lamp, fx);
            engine.EditorSetupLook(body, loadedSprite, runes, blur, hub);
            engine.EditorSetupNumbers(MaxMomentum, 3f, Stage2, 5f, new Vector2(0f, 5f), "r1_heart_engine_installed"); // its own save flag (not the old Room 8 one)
            SerializedObject eso = new SerializedObject(engine);
            eso.FindProperty("showGauge").boolValue = false; // the milestone board (below) shows the momentum here
            eso.ApplyModifiedPropertiesWithoutUndo();
            wheel.gameObject.SetActive(false);

            // ---- Ore chute: its mouth over the low stop (the cart sits there under it); P1 works it from the hopper at the balcony's
            // east end, by the post (its chute runs from there down to the mouth: Room9Dressing).
            OreChute chute = Chute(ledge, puzzle, "Ore_Chute", cart, new Vector2(HopperX, BalconyTop + 0.8f), new Vector2(ChuteMouthX, ChuteMouthY));
            foreach (SpriteRenderer sr in chute.GetComponentsInChildren<SpriteRenderer>()) if (sr.gameObject != chute.gameObject) { sr.sortingLayerName = "Background"; sr.sortingOrder = 15; } // behind the engine
            if (chute.TryGetComponent(out SpriteRenderer handleSr) && HeartEngineArt.Get("Hopper") != null)
            {
                handleSr.enabled = false; // drawn as the hopper standing on the balcony (the trigger stays where it was)
                SpriteAt(chute.transform, "Hopper", HeartEngineArt.Get("Hopper"), new Vector3(HopperX, BalconyTop, 0f), 1f, "Interactables", 0);
            }

            // ---- The Momentum Column, tall above the landing (where players arrive: the first thing they see).
            GameObject column = new GameObject("Momentum_Column");
            column.transform.SetParent(puzzle, false);
            column.transform.localPosition = new Vector3(-9.3f, Floor + Landing + 0.25f, 0f); // the inside bottom of the glass
            SpriteRenderer glass = column.AddComponent<SpriteRenderer>();
            glass.sprite = FinaleArt.Get("Column_Glass");
            glass.sortingLayerName = "BackDecor";
            glass.sortingOrder = 32;
            SpriteRenderer fill = SpriteAt(column.transform, "Liquid", FinaleArt.Get("Pixel_Bottom"), column.transform.position, 1f, "BackDecor", 30);
            SpriteRenderer surface = SpriteAt(column.transform, "Surface", FinaleArt.Get("Pixel"), column.transform.position, 1f, "BackDecor", 31);
            surface.transform.localScale = new Vector3(FinaleArt.ColumnInnerWidth * 32f, 1f, 1f);
            Light2D columnGlow = Lamp(column.transform, "Glow", Vector2.zero, new Color(0.5f, 0.95f, 1f), 2.5f, 0.5f, false);
            column.AddComponent<MomentumColumn>().EditorSetup(engine, fill, surface, columnGlow, FinaleArt.ColumnInnerHeight, FinaleArt.ColumnInnerWidth, fx);

            // ---- Stages: hall lamps (30), the alcove (60 = the engine's own signal), the awakening (100).
            GameObject lampGroup = new GameObject("Hall_Lamps");
            lampGroup.transform.SetParent(puzzle, false);
            Light2D[] hallLamps = new Light2D[4];
            float[] lx = { -8f, -2.5f, 3.5f, 8.5f };
            for (int i = 0; i < 4; i++) hallLamps[i] = Lamp(lampGroup.transform, $"Hall_Lamp_{i + 1}", new Vector2(lx[i], Floor + 8.6f), new Color(1f, 0.8f, 0.5f), 6f, 0.9f, false);
            EngineStage lampsStage = MakeStage(puzzle, "Stage_Lamps", engine, Stage1, "", hallLamps, "The hall's old lamps flicker awake.", false);
            EngineStage awake = MakeStage(puzzle, "Stage_Awake", engine, MaxMomentum, "r1_engine_awake", new Light2D[0],
                                            "THE HEART ENGINE WAKES\n<size=60%>Its pulse runs through the roots, toward the Great Shard.</size>", true);
            // The milestone board: what the momentum is for (30 / 60 / 100), up in the hall's top-right corner.
            GameObject boardGo = new GameObject("Engine_Milestones");
            boardGo.transform.SetParent(puzzle, false);
            boardGo.AddComponent<EngineMilestones>().EditorSetup(engine, new[]
            {
                new EngineMilestones.Milestone { signal = lampsStage, threshold = Stage1, name = "Hall lamps", onWord = "lit" },
                new EngineMilestones.Milestone { signal = engine, threshold = Stage2, name = "Shard platform", onWord = "up", drainedWord = "sank: refill to 60" },
                new EngineMilestones.Milestone { signal = awake, threshold = MaxMomentum, name = "Awaken the Heart", onWord = "AWAKE" },
            }, new Vector2(6.9f, Floor + 8.0f), 3f);
            // The alcove: a platform set into the landing that rises at 60 (players standing on it ride up to the shard).
            Platform(liftT, puzzle, "Platform_Alcove", -7.6f, Floor + Landing - 0.25f, -6.6f, Floor + Landing, new Vector2(0f, 2.6f), 2f, engine, stopIfBlocked: false);
            Platform(gateT, puzzle, "Gate_Exit", 9.3f, Floor, 9.8f, Floor + 4.2f, new Vector2(0f, 4.3f), 3f, awake, false);

            // ---- Info and extras
            // A sign at the crossing (fades in near it): the way east is over the shaft's grate and a hop over the striker.
            GameObject signGo = new GameObject("Sign_Crossing");
            signGo.transform.SetParent(info, false);
            signGo.transform.localPosition = new Vector3(-0.2f, Floor + 2.1f, 0f);
            SerializedObject sign = new SerializedObject(signGo.AddComponent<WorldSign>());
            sign.FindProperty("text").stringValue = "To the winch and the exit: walk on past the striker  >";
            sign.FindProperty("width").floatValue = 4.5f;
            sign.FindProperty("fontSizePx").floatValue = 19f;
            sign.FindProperty("showDistance").floatValue = 3f;
            sign.ApplyModifiedPropertiesWithoutUndo();

            Instance(shrineP, info, "Shrine_Room09", new Vector2(6f, Floor + shrineLift));               // at P2's station
            GameObject journal = Instance(journalP, info, "Journal_Room09", new Vector2(8.4f, Floor + journalLift));
            if (journal.TryGetComponent(out JournalStone stone)) { stone.EditorSetEntry(entry); EditorUtility.SetDirty(stone); }
            // From the risen alcove platform (top 6 m: reach 6 + 4.3 = 10.3); not from the balcony (4.9 + 4.3 = 9.2) or the landing.
            Instance(shardP, objects, "Shard_Alcove", new Vector2(-7.1f, Floor + 9.4f));

            PrefabUtility.SaveAsPrefabAsset(room, Room9Path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(room);
        }
        Debug.Log("Room 8 part 2: graybox built in R1_Room09 (The Heart Engine). Next: Room 8 Part 3 > 1. Build Graybox.");
    }

    // A cord handle: a copy of the 1 kg block on the floor that slides along its slot (CordHandle), with its cord drawn.
    // Its body, collider and mass stay the block's (so the Wind Tether pulls it exactly as before); it is drawn as a brass ring.
    private static CordHandle Cord(GameObject block1kg, Transform parent, string name, float homeX, Vector2 direction, float travel, float tension,
                                   Vector2[] path, Material rope)
    {
        GameObject go = Crate(block1kg, parent, name, homeX, Floor + 0.5f, showMass: false);
        go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        go.transform.localPosition = new Vector3(homeX, Floor + 0.25f, 0f);
        foreach (Collider2D c in go.GetComponents<Collider2D>()) c.sharedMaterial = Wet;
        Sprite ring = HeartEngineArt.Get("Ring_Handle");
        if (ring != null)
        {
            if (go.TryGetComponent(out SpriteRenderer blockSr)) blockSr.enabled = false;
            SpriteAt(go.transform, "Ring", ring, new Vector3(homeX, Floor, 0f), 1f, "Interactables", 3); // bottom pivot: on the slot
        }
        LineRenderer line = Line(parent, name + "_Line", Color.white, 4f / 32f, "Props", 3);
        Textured(line, rope);
        line.transform.SetParent(go.transform, true);
        CordHandle handle = go.AddComponent<CordHandle>();
        handle.EditorSetup(direction, travel, tension, 0.8f, line, path);
        return handle;
    }

    // Undo a (non-uniformly) scaled parent's scale, so the sprite shows at its true pixel size.
    private static SpriteRenderer TrueSize(SpriteRenderer sr)
    {
        Vector3 s = sr.transform.parent != null ? sr.transform.parent.lossyScale : Vector3.one;
        sr.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f);
        return sr;
    }

    // A chain / rope line drawn with a repeating texture (one 32 px repeat per metre: the game's pixel size).
    private static void Textured(LineRenderer line, Material mat)
    {
        if (mat == null) return; // keeps the plain coloured line
        line.sharedMaterial = mat;
        line.textureMode = LineTextureMode.Tile;
    }

    // The slot a cord handle slides in: a dark groove on the floor (visual only).
    private static void SlotMark(Transform ledge, Transform parent, string name, float left, float right)
    {
        GameObject slot = Clone(ledge, parent, name);
        foreach (Collider2D c in slot.GetComponents<Collider2D>()) Object.DestroyImmediate(c);
        SetRect(slot.transform, left - 0.25f, Floor - 0.06f, right + 0.25f, Floor + 0.02f);
        if (slot.TryGetComponent(out SpriteRenderer sr)) { sr.color = new Color(0.2f, 0.18f, 0.16f); sr.sortingLayerName = "Props"; sr.sortingOrder = 1; }
    }

    private static EngineStage MakeStage(Transform parent, string name, MomentumEngine engine, float threshold, string latch, Light2D[] lights, string banner, bool big)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        EngineStage stage = go.AddComponent<EngineStage>();
        stage.EditorSetup(engine, threshold, latch, lights, banner, big);
        return stage;
    }
}
