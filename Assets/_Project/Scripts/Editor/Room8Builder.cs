using UnityEditor;
using UnityEngine;
using static RoomBuildKit;
using static FinaleKit;

/// <summary>
/// Builds the finale's PART 1, "The Descent", inside Prefabs/Rooms/Region1/R1_Room08.prefab (Room 8 = the three-part
/// "Heart of the Forest"; run Finale > 1. Add Finale Rooms To Scene first). Re-runnable.
/// Story: Wren's note waits at the entrance. The Heart Cart must go down the old cart chute to the engine hall below,
/// but a wall of roots blocks the rail.
///
/// Layout (room-local; floor top y = -4.5), west to east:
///   entry floor (from Room 7) under the LOADING DECK x -8..-3 at 3 m (wet top; step slab x -8.9..-8.1 at 1.8 m to climb
///   up) with the Heart Cart (2 kg) behind a CHOCK at its east end, the ORE CHUTE above the cart and its handle on the deck;
///   under the deck the CHOCK PLATE (20 N: one player) | the RAMP x -3..4 (wet, 3 m down) | wet floor x 4..6.5 with the
///   ROOT WALL (x 4.6..5.2, floor to ceiling, breaks at 42 kg·m/s) and the CART CHUTE (x 5.4..6.4, a slab over it at 1.9 m)
///   | the raised doorway landing to part 2 (x 6.5..10 at 3.4 m) with the exit gate.
/// The chain (Newton's 2nd law, impulse and momentum, gravity):
///   1. Load: pull the chute handle twice: 2 kg -> 4 kg (a heavier cart: same Burst, less speed; same slope, MORE momentum).
///   2. Release: one player stands on the chock plate (under the deck) while the other launches the cart from the deck.
///      Nobody can do both: the plate is 3 m below and the chock drops the moment they step off.
///   3. Break: down the ramp the cart reaches ~12.5 m/s. Empty: p ≈ 25 kg·m/s (≈ 29 with a Burst): it bounces off.
///      Loaded: p ≈ 50 kg·m/s: the roots give way (the wall absorbs its momentum: it stops there).
///   4. Deliver: Burst it the last metre into the cart chute: it drops to the engine hall (saved), the exit gate opens.
/// Extras: 1 shard over the ramp (jump + float + dash from the deck's edge).
/// </summary>
public static class Room8Builder
{
    private const string Room8Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room08.prefab";
    private const float DeckTop = Floor + 3f, DeckLeft = -8f, DeckRight = -3f, RampFoot = 4f, Landing = FinaleSetup.Part1To2Doorway;

    private static readonly string[] Built =
    {
        // this version
        "Floor_Track", "Surface_Wet", "Deck", "Deck_Wet", "Step", "Ramp", "Gate_Chock", "Plate_Chock_20N", "Lamp_Chock",
        "Ore_Chute", "Wall_Roots", "Cart_Chute", "Slab_Chute", "Gate_Exit", "Heart_Cart", "Shrine_Room08", "Journal_Room08", "Shard_Ramp",
        // the single-room Room 8 (now parts 2 and 3)
        "Floor_West", "Floor_East", "Perch", "Paddle", "Engine", "Engine_Frame", "Flywheel_Ghost", "Engine_Body", "Engine_Runes", "Striker",
        "Gate_Heart", "Pan_A", "Pan_B", "Cage_West", "Cage_East", "Cage_Top", "Scale_A", "Scale_B", "Lever_Brake", "Balance_Lift",
        "Pulley_Tower", "Dais", "Great_Shard", "Crate_Drive_2kg", "Crate_Counterweight_3kg", "Shard_Perch", "Shard_East", "Shard_Dais",
    };

    [MenuItem("Tools/Shards of Motion/Room 8/1. Build Graybox (Part 1: The Descent)")]
    public static void Build()
    {
        Transform ledge = Template("Ledge_Heavy"), plateT = Template("Plate_Heavy_80N"), gateT = Template("Gate_Exit");
        if (ledge == null || plateT == null || gateT == null) return;
        GameObject b2 = Load($"{Interactables}/Block_2kg.prefab"), shrineP = Load($"{Interactables}/ResetShrine.prefab"),
                   journalP = Load($"{Interactables}/JournalStone.prefab"), shardP = Load($"{Interactables}/MotionShard.prefab");
        if (b2 == null || shrineP == null || journalP == null || shardP == null || Wet == null)
        {
            Debug.LogError("Room 8: a prefab in Prefabs/Interactables or the Wet_Stone material is missing. Nothing was changed.");
            return;
        }
        FinaleArt.Ensure(Room9Builder.Notches);
        JournalEntry note = JournalTool.SaveWrensNote();
        float shrineLift = ShrineLift, journalLift = JournalLift;

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

            // ---- Layout
            SetRect(floor, -10f, Floor - 1f, RampFoot, Floor);
            GameObject track = Object.Instantiate(floor.gameObject, layout); track.name = "Floor_Track";
            SetRect(track.transform, RampFoot, Floor - 1.02f, 6.5f, Floor - 0.02f);         // 2 cm low under its wet edge
            WetEdge(layout, "Surface_Wet", RampFoot, 6.5f, Floor);
            Solid(ledge, layout, "Deck", DeckLeft, DeckTop - 0.32f, DeckRight, DeckTop - 0.02f);
            WetEdge(layout, "Deck_Wet", DeckLeft, DeckRight, DeckTop);
            Solid(ledge, layout, "Step", -8.9f, Floor + 1.6f, -8.1f, Floor + 1.8f);         // floor -> step -> deck
            Ramp(ledge, layout, "Ramp", new Vector2(DeckRight, DeckTop), new Vector2(RampFoot, Floor), Floor - 0.5f);
            Solid(ledge, layout, "Slab_Chute", 5.4f, Floor + 1.7f, 6.5f, Floor + 1.9f);   // over the chute: up to the landing
            SetEntries(room.transform, new Vector2(-8.4f, Floor + 0.75f), new Vector2(-7.6f, Floor + 0.75f));

            // ---- The cart, the ore chute, the chock and its plate
            HeartCart cart = Cart(b2, objects, "Heart_Cart", -6f, DeckTop);
            Chute(ledge, puzzle, "Ore_Chute", cart, new Vector2(-7.4f, DeckTop + 0.8f), new Vector2(-6f, Floor + 5f));
            PressurePlate plate = Plate(plateT, puzzle, "Plate_Chock_20N", -5.4f, Floor, 20f, PlateReadout.Display.TargetOnly, true, 1.2f);
            Platform(gateT, puzzle, "Gate_Chock", DeckRight - 0.55f, DeckTop, DeckRight - 0.25f, DeckTop + 0.9f,
                     new Vector2(0f, 1.1f), 6f, plate, stopIfBlocked: false); // lifts clear of the cart while the plate is held
            Lamp(puzzle, "Lamp_Chock", new Vector2(DeckRight - 0.4f, DeckTop + 2.3f), new Color(1f, 0.75f, 0.35f), 2f, 1.2f, true);

            // ---- Roots, chute, exit
            Wall(ledge, layout, "Wall_Roots", 4.6f, 5.2f, 42f);
            GameObject chuteGo = new GameObject("Cart_Chute");
            chuteGo.transform.SetParent(puzzle, false);
            chuteGo.transform.localPosition = new Vector3(5.95f, Floor + 0.5f, 0f);
            BoxCollider2D chuteTrigger = chuteGo.AddComponent<BoxCollider2D>();
            chuteTrigger.isTrigger = true;
            chuteTrigger.size = new Vector2(1.1f, 1f);
            CartChute chute = chuteGo.AddComponent<CartChute>();
            chute.EditorSetup(cart, "r1_cart_delivered");
            Platform(gateT, puzzle, "Gate_Exit", 8.45f, Floor + Landing, 8.95f, Floor + Landing + 4.2f, new Vector2(0f, 4.3f), 4f, chute, false);

            // ---- Info and extras
            Instance(shrineP, info, "Shrine_Room08", new Vector2(-4f, Floor + shrineLift));            // under the deck, by the ramp
            GameObject journal = Instance(journalP, info, "Journal_Room08", new Vector2(-6.9f, Floor + journalLift)); // Wren's note
            if (journal.TryGetComponent(out JournalStone stone)) { stone.EditorSetEntry(note); EditorUtility.SetDirty(stone); }
            Instance(shardP, objects, "Shard_Ramp", new Vector2(-1f, Floor + 5.3f));

            PrefabUtility.SaveAsPrefabAsset(room, Room8Path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(room);
        }
        Debug.Log("Room 8 part 1: graybox built in R1_Room08 (The Descent). Next: Room 8 Part 2 > 1. Build Graybox.");
    }
}
