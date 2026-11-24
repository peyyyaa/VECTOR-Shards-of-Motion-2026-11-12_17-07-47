using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates the Room 1 Motion Journal entries (Wren Alder's field notes) as JournalEntry assets in
/// Assets/_Project/Data/Journal/ and assigns them to Journal_Room01 and Journal_Float inside R1_Room01.
/// Running it again rewrites the entries (edit the text here, or edit the assets afterwards and don't re-run).
/// Numbers match the game: g = 29.43 m/s², block μ = 1/3, push 12 N per player, tether 24 N, burst 15 N·s,
/// player 1 kg, keystone 2 kg, crate 3 kg.
/// </summary>
public static class JournalTool
{
    private const string Folder = "Assets/_Project/Data/Journal";
    private const string RoomPrefab = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room01.prefab";

    [MenuItem("Tools/Shards of Motion/Journal/Create Room 1 Entries")]
    public static void CreateRoom1()
    {
        Directory.CreateDirectory(Folder);
        JournalEntry first = Save("R1_01_TheFirstExchange", FirstExchange());
        JournalEntry flt = Save("R1_01_Float", Float());

        GameObject root = PrefabUtility.LoadPrefabContents(RoomPrefab);
        try
        {
            bool ok = Assign(root.transform, "Journal_Room01", first) & Assign(root.transform, "Journal_Float", flt);
            if (!ok) { Debug.LogError("Journal tool: a Journal stone is missing in R1_Room01. Nothing was assigned."); return; }
            PrefabUtility.SaveAsPrefabAsset(root, RoomPrefab);
            Debug.Log("Journal tool: Room 1 entries created in " + Folder + " and assigned to R1_Room01.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>Rewrites every room's entry asset from this file (same assets, so journal stones keep their links).</summary>
    [MenuItem("Tools/Shards of Motion/Journal/Update All Entries")]
    public static void UpdateAll()
    {
        Directory.CreateDirectory(Folder);
        Save("R1_01_TheFirstExchange", FirstExchange());
        Save("R1_01_Float", Float());
        SaveRoom2Entry();
        SaveRoom3Entry();
        SaveRoom4Entry();
        SaveRoom5Entry();
        SaveRoom6Entry();
        SaveRoom7Entry();
        Debug.Log("Journal tool: all entries updated in " + Folder + ".");
    }

    public static JournalEntry SaveRoom4Entry()
    {
        Directory.CreateDirectory(Folder);
        return Save("R1_04_BrokenCrossing", BrokenCrossing());
    }

    private static JournalEntry BrokenCrossing()
    {
        JournalEntry e = ScriptableObject.CreateInstance<JournalEntry>();
        e.title = "4 · Broken Crossing";
        e.tip = "Walls only break for crates, and only for enough momentum. The heavy crate loses its momentum too fast; the light one " +
                "keeps it. For the second wall, open the sluice so the current helps your Burst.";

        e.pages.Add(new JournalPage
        {
            bigIdea = "Momentum is mass times velocity: p = m·v. It's how much \"oomph\" a moving thing carries.",
            notes = "When the aqueduct collapsed, its rubble sealed the way. Pushing won't move it, but a crate that hits hard enough " +
                    "will smash through. Not just fast, not just heavy: both. Each wall shows the momentum it needs.",
            lookCloser = $"p = m × v   (kg·m/s)\n" +
                         $"A Burst is an impulse, and impulse = change in momentum: J = Δp\n" +
                         $"So every crate gets {Imp("15 kg·m/s")}: 1 kg at 15 m/s, 3 kg at 5 m/s.",
            funFact = "A tennis ball and a bowling ball can have the same momentum. The tennis ball just has to be much, much faster.",
            marginP1 = "Same Burst. Same momentum. Huh.",
            marginP2 = "But the light one goes way faster.",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Crate\n1 kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Burst", newtons = 15f, unit = "N·s", direction = DiagramForce.Direction.Right, kind = DiagramForce.Kind.Impulse },
                },
                caption = "Same impulse, same momentum: 15 kg·m/s for any crate.",
            },
        });

        e.pages.Add(new JournalPage
        {
            bigIdea = "Friction takes momentum away every second. Heavy crates lose it faster.",
            notes = "Here's the catch: the heavy crate leaves with the same 15 kg·m/s, but friction grips it three times harder, " +
                    "so it's stopped before it reaches the rubble. The light crate keeps sliding.",
            lookCloser = $"Friction removes μ·m·g of momentum every second:\n" +
                         $"1 kg {Fric("9.8")}   2 kg {Fric("19.6")}   3 kg {Fric("29.4")}  kg·m/s each second\n" +
                         "After a Burst: 3 kg stops in 15 ÷ 29.4 ≈ 0.5 s, 1 kg lasts 15 ÷ 9.8 ≈ 1.5 s.",
            funFact = "This is why a heavy truck needs good brakes: the brakes must take away a lot of momentum.",
            marginP1 = "The big one just... gave up.",
            marginP2 = "Friction won. Use the small one.",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Crate\n3 kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Friction", newtons = 29.4f, direction = DiagramForce.Direction.Left, kind = DiagramForce.Kind.Friction },
                },
                caption = "Friction: −29.4 kg·m/s every second for the heavy crate.",
            },
        });

        e.pages.Add(new JournalPage
        {
            bigIdea = "Momentum adds up: push something that's already moving and the impulses stack.",
            notes = "The sluice still works. Its current pushes gently but never stops, and a small force for a long time is an " +
                    "impulse too: J = F × t. Ride a Burst on top of the current and the crate arrives with far more.",
            lookCloser = $"Current {Pull("25 N")} on 1 kg, friction {Fric("9.8 N")} → net 15.2 N\n" +
                         "→ the crate gains 15.2 kg·m/s every second it rides the current.\n" +
                         $"Burst {Imp("15")} + current over 3.5 m ≈ 18 kg·m/s; Burst alone ≈ 12.",
            funFact = "Rockets work this way: a modest push, kept up for minutes, adds up to enormous momentum.",
            marginP1 = "I kick it, the water carries it.",
            marginP2 = "Opening the sluice!",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Crate\n1 kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Current", newtons = 25f, direction = DiagramForce.Direction.Right, kind = DiagramForce.Kind.Pull },
                    new DiagramForce { label = "Friction", newtons = 9.8f, direction = DiagramForce.Direction.Left, kind = DiagramForce.Kind.Friction },
                },
                caption = "Net 15.2 N: +15.2 kg·m/s every second.",
            },
        });
        return e;
    }

    /// <summary>Creates (or rewrites) the Room 5 entry asset; Room5Builder assigns it.</summary>
    public static JournalEntry SaveRoom5Entry()
    {
        Directory.CreateDirectory(Folder);
        return Save("R1_05_LongPush", LongPush());
    }

    private static JournalEntry LongPush()
    {
        JournalEntry e = ScriptableObject.CreateInstance<JournalEntry>();
        e.title = "5 · Long Push";
        e.tip = "Both crates must end on the moss. Run the light one along and let it glide; a Burst sends it flying past. " +
                "Nobody can get behind the heavy one in the alcove: pull it out first, then Burst it west. The moss will catch it.";

        e.pages.Add(new JournalPage
        {
            bigIdea = "Friction pushes back against sliding. How hard depends on the surface and on how hard it's pressed.",
            notes = "The old causeway is three floors in one: dry stone, slick wet stone and thick moss. Same crate, same weight, " +
                    "very different grip. The surface's roughness is a number called μ (\"mu\").",
            lookCloser = $"Friction f = μ × N, and on a flat floor N = m·g = 2 × 29.43 = {Wt("58.9 N")}\n" +
                         $"Stone μ = 1/3 → {Fric("19.6 N")}    Wet stone μ = 0.05 → {Fric("2.9 N")}\n" +
                         $"Moss μ = 0.6 → {Fric("35.3 N")}   (one push is {Push("12 N")}, the tether {Pull("24 N")})",
            funFact = "Curling players sweep the ice in front of the stone: a thin layer of melt lowers μ, so the stone slides farther.",
            marginP1 = "On the wet stone I can push it alone!",
            marginP2 = "On the moss, not even both of us.",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Crate\n2 kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Push", newtons = 12f, direction = DiagramForce.Direction.Right, kind = DiagramForce.Kind.Push },
                    new DiagramForce { label = "Friction", newtons = 19.6f, direction = DiagramForce.Direction.Left, kind = DiagramForce.Kind.Friction },
                },
                caption = "On stone: 12 N can't beat 19.6 N of friction.",
            },
        });

        e.pages.Add(new JournalPage
        {
            bigIdea = "Less friction, longer slide. Friction is what stops a sliding thing.",
            notes = "A Burst gives any 2 kg crate the same speed. After that, only friction slows it down, so the floor decides how " +
                    "far it goes. On wet stone it barely slows at all; on moss it stops almost at once.",
            lookCloser = $"Burst: Δv = {Imp("7.5 m/s")}. Stopping distance d = v² ÷ (2·μ·g):\n" +
                         $"Stone ≈ 2.9 m    Wet stone ≈ 19 m    Moss ≈ 1.6 m\n" +
                         "(Real surfaces grip a little harder to start than to slide; this world uses one μ for both.)",
            funFact = "On dry roads car tyres have μ close to 1. On ice it can drop below 0.1, which is why braking distances explode.",
            marginP1 = "One kick and it's gone!",
            marginP2 = "And the moss is the brake.",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Crate\n2 kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Friction", newtons = 2.9f, direction = DiagramForce.Direction.Left, kind = DiagramForce.Kind.Friction },
                },
                caption = "On wet stone: only 2.9 N slows it down.",
            },
        });

        e.pages.Add(new JournalPage
        {
            bigIdea = "Same push, different crate. A Burst gives every crate the same momentum, so the light one leaves twice as fast.",
            notes = "The old gate wants 80 N on the moss, so both crates must rest there: 58.9 + 29.4 = 88 N. But burst the light crate " +
                    "and it shoots straight across the moss. It needs a gentler start: run it along and let it go.",
            lookCloser = $"Δv = J ÷ m:  2 kg → {Imp("7.5 m/s")}    1 kg → {Imp("15 m/s")}\n" +
                         "Friction slows ANY crate by μ·g each second (moss: 17.7 m/s every second).\n" +
                         "Twice the speed → four times the distance: on moss 1.6 m vs 6.4 m.",
            funFact = "Stopping distance grows with the square of speed. That's why driving twice as fast needs about four times the room to stop.",
            marginP1 = "Gently?? I only know one setting.",
            marginP2 = "Then I'll push this one. You kick the heavy one.",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Crate\n1 kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Burst", newtons = 15f, unit = "N·s", direction = DiagramForce.Direction.Left, kind = DiagramForce.Kind.Impulse },
                },
                caption = "Same impulse, half the mass: Δv = 15 ÷ 1 = 15 m/s.",
            },
        });
        return e;
    }

    /// <summary>Creates (or rewrites) the Room 6 entry asset; Room6Builder assigns it.</summary>
    public static JournalEntry SaveRoom6Entry()
    {
        Directory.CreateDirectory(Folder);
        return Save("R1_06_WeightOfCooperation", WeightOfCooperation());
    }

    private static JournalEntry WeightOfCooperation()
    {
        JournalEntry e = ScriptableObject.CreateInstance<JournalEntry>();
        e.title = "6 · Weight of Cooperation";
        e.tip = "One of you stands on the plate so the other can ride the west lift up to the gallery. Load BOTH crates onto the " +
                "pan up there, then both of you get on the low pan and pull its lever. Equal weights won't move.";

        e.pages.Add(new JournalPage
        {
            bigIdea = "Weight is a force: W = m·g. On a balance lift the heavier side goes down and lifts the other.",
            notes = "Two pans on one rope over the old pulley. Whatever one pan gains in height, the other loses. The brake holds " +
                    "them until the lever is pulled. To go UP, the other side must weigh more than you.",
            lookCloser = $"Each of you: {Wt("29.4 N")}    1 kg crate: {Wt("29.4 N")}    2 kg crate: {Wt("58.9 N")}\n" +
                         "The lift is pushed by the difference: net force = W(other side) − W(your side).",
            funFact = "Old mine lifts and many modern elevators hang a counterweight on the other end of the rope, so the motor " +
                      "only has to lift the difference.",
            marginP1 = "The crates have to go DOWN for us to go up?",
            marginP2 = "Same rope. Down there means up here.",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Crate\n2 kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Weight", newtons = 58.9f, direction = DiagramForce.Direction.Down, kind = DiagramForce.Kind.Weight },
                    new DiagramForce { label = "Normal", newtons = 58.9f, direction = DiagramForce.Direction.Up, kind = DiagramForce.Kind.Normal },
                },
                caption = "Resting on a pan: it pushes up exactly as hard as the weight pulls down.",
            },
        });

        e.pages.Add(new JournalPage
        {
            bigIdea = "Equal weights balance. Balanced forces mean nothing changes: that's equilibrium.",
            notes = "Both of you on one pan weigh exactly as much as the big crate on the other. Pull the lever and... nothing. " +
                    "Not stuck: balanced. To lift yourselves, the other side must be heavier than both of you together.",
            lookCloser = $"Both of you: 2 × 29.4 = {Wt("58.9 N")}    2 kg crate: {Wt("58.9 N")}  → net force 0, no motion\n" +
                         $"Add the 1 kg crate: 88.3 − 58.9 = {Push("29.4 N")} net, so your side rises.",
            funFact = "A bridge, a table, a sleeping cat: anything that isn't speeding up has balanced forces on it.",
            marginP1 = "We pulled the lever. It just... sat there.",
            marginP2 = "Because we weigh the same as the crate!",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Lift",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Crate side", newtons = 58.9f, direction = DiagramForce.Direction.Left, kind = DiagramForce.Kind.Pull },
                    new DiagramForce { label = "Your side", newtons = 58.9f, direction = DiagramForce.Direction.Right, kind = DiagramForce.Kind.Pull },
                },
                caption = "The rope is pulled 58.9 N each way: balanced, so it stays still.",
            },
        });

        e.pages.Add(new JournalPage
        {
            bigIdea = "The pan pushes up on you: the normal force. On a moving lift it changes, so you feel heavier or lighter.",
            notes = "Standing still, the pan pushes up with exactly your weight. When the lift starts upward it must also speed you " +
                    "up, so it pushes harder: you feel heavier. As it eases to a stop at the top, you feel lighter for a moment.",
            lookCloser = "Lift: a = net force ÷ total mass = (29.4 − 4 friction) ÷ (3 + 2 + 2 pans) ≈ 3.6 m/s²\n" +
                         $"Riding up, each of you: N = m·(g + a) = 1 × (29.43 + 3.6) ≈ {Sup("33 N")}, more than your {Wt("29.4 N")} weight.",
            funFact = "That heavy feeling as an elevator starts upward is the floor pushing you harder than gravity pulls.",
            marginP1 = "I feel heavier. Is that a thing?",
            marginP2 = "It's the pan pushing harder. Physics!",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "You\n1 kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Weight", newtons = 29.4f, direction = DiagramForce.Direction.Down, kind = DiagramForce.Kind.Weight },
                    new DiagramForce { label = "Normal", newtons = 33f, direction = DiagramForce.Direction.Up, kind = DiagramForce.Kind.Normal },
                },
                caption = "Speeding up upward: the normal force wins by m·a.",
            },
        });
        return e;
    }

    /// <summary>Creates (or rewrites) the Room 7 entry asset; Room7Builder assigns it.</summary>
    public static JournalEntry SaveRoom7Entry()
    {
        Directory.CreateDirectory(Folder);
        return Save("R1_07_UnstoppableObject", UnstoppableObject());
    }

    private static JournalEntry UnstoppableObject()
    {
        JournalEntry e = ScriptableObject.CreateInstance<JournalEntry>();
        e.title = "7 · Unstoppable Object";
        e.tip = "One of you sends the stone into the culvert with a Burst; the other waits by the lever on the culvert roof. " +
                "The stop block only stays up for a moment: pull the lever just before the stone gets there.";

        e.pages.Add(new JournalPage
        {
            bigIdea = "Momentum p = m·v. A moving thing keeps its momentum until something takes it away.",
            notes = "On the wet stone almost nothing pushes back, so the stone just keeps going: through the culvert, off the end, " +
                    "into the acid. Friction is trying, but 4.4 N is far too gentle to stop 15 kg·m/s in time.",
            lookCloser = $"Burst: {Imp("15 kg·m/s")} → the 3 kg stone moves at 5 m/s.\n" +
                         $"Wet-stone friction {Fric("4.4 N")} takes away 4.4 kg·m/s each second:\n" +
                         "15 ÷ 4.4 ≈ 3.4 s and about 8.5 m to stop. The shelf isn't that long.",
            funFact = "Out in space there is no friction at all: a thrown spanner would keep going forever.",
            marginP1 = "I barely touched it and it's GONE.",
            marginP2 = "That's momentum for you.",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Stone\n3 kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Friction", newtons = 4.4f, direction = DiagramForce.Direction.Left, kind = DiagramForce.Kind.Friction },
                },
                caption = "Moving right at 5 m/s; only 4.4 N pushes back.",
            },
        });

        e.pages.Add(new JournalPage
        {
            bigIdea = "Stopping takes an impulse: J = F·Δt = Δp. Stop it fast and the force is huge.",
            notes = "Friction would take the stone's 15 kg·m/s away slowly, over seconds. The stop block takes all of it in an " +
                    "instant. Same change in momentum, so a tiny time means an enormous force. That's why the block is solid stone.",
            lookCloser = $"Friction: {Fric("4.4 N")} × 3.4 s ≈ 15 N·s\n" +
                         "Stop block: 15 N·s in about 0.02 s → about 750 N!\n" +
                         "Same impulse, very different force.",
            funFact = "Crumple zones and airbags work the other way round: they make the stop take longer, so the force on you is smaller.",
            marginP1 = "Pull it NOW? Now?! NOW!",
            marginP2 = "Just before it gets there. Timing!",
            diagram = new ForceDiagram { show = false },
        });
        return e;
    }

    /// <summary>Creates (or rewrites) the Room 2 entry asset; Room2Builder assigns it to the room's journal stone.</summary>
    public static JournalEntry SaveRoom2Entry()
    {
        Directory.CreateDirectory(Folder);
        return Save("R1_02_WeightOfThings", WeightOfThings());
    }

    /// <summary>Creates (or rewrites) the Room 3 entry asset; Room3Builder assigns it.</summary>
    public static JournalEntry SaveRoom3Entry()
    {
        Directory.CreateDirectory(Folder);
        return Save("R1_03_TogetherWeMove", TogetherWeMove());
    }

    private static JournalEntry TogetherWeMove()
    {
        JournalEntry e = ScriptableObject.CreateInstance<JournalEntry>();
        e.title = "3 · Together We Move";
        e.tip = "The ferry sails when it carries at least 85 N of cargo (people don't count), and waits a moment so you can hop on. " +
                "The stone crate is too heavy for one of you: push and pull it together. At the far side, climb the crate.";

        e.pages.Add(new JournalPage
        {
            bigIdea = "Forces on the same object add up. Two small forces can beat one big friction.",
            notes = "The causeway collapsed long ago; only the old cargo ferry still crosses the acid. It sails when its deck carries " +
                    "a heavy load. The stone crate is too heavy for either of us alone. But a push and a pull at the same time, " +
                    "in the same direction, add together.",
            lookCloser = $"Friction on 3 kg: μ·m·g = {Fric("29.4 N")}\n" +
                         $"Push {Push("12 N")} alone: no.   Tether {Pull("24 N")} alone: no.\n" +
                         $"Push + tether = {Push("12")} + {Pull("24")} = 36 N → a = (36 − 29.4) ÷ 3 = 2.2 m/s²",
            funFact = "Engineers call adding forces like this a \"vector sum\": direction matters as much as size.",
            marginP1 = "I push, you pull. Same way!",
            marginP2 = "Pulling from the front still counts.",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Crate\n3 kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Tether", newtons = 24f, direction = DiagramForce.Direction.Right, kind = DiagramForce.Kind.Pull },
                    new DiagramForce { label = "Push", newtons = 12f, direction = DiagramForce.Direction.Right, kind = DiagramForce.Kind.Push },
                    new DiagramForce { label = "Friction", newtons = 29.4f, direction = DiagramForce.Direction.Left, kind = DiagramForce.Kind.Friction },
                },
                caption = "12 + 24 = 36 N forward against 29.4 N: it moves.",
            },
        });

        e.pages.Add(new JournalPage
        {
            bigIdea = "Inertia rides along: things keep doing what they were doing until a force changes it.",
            notes = "Watch the crate on the ferry. When the ferry starts, the crate lags behind for a moment; when it stops, " +
                    "the crate keeps sliding forward. Only friction from the deck drags it along. Keep cargo away from the edges!",
            lookCloser = $"Friction on the 3 kg crate: up to {Fric("29.4 N")}, so a = 29.4 ÷ 3 = 9.8 m/s²\n" +
                         "Ferry speed 2 m/s: the crate catches up in 2 ÷ 9.8 ≈ 0.2 s\n" +
                         "and slips about v² ÷ (2a) = 4 ÷ 19.6 ≈ 0.2 m each time.",
            funFact = "It's why a bus pushes you back when it starts and forward when it brakes. You aren't pushed: you just keep going.",
            marginP1 = "The crate didn't move. The ferry did.",
            marginP2 = "Then the crate wanted to keep going.",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Crate\n3 kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Friction", newtons = 29.4f, direction = DiagramForce.Direction.Right, kind = DiagramForce.Kind.Friction },
                },
                caption = "As the ferry starts right, friction is the only force pulling the crate along.",
            },
        });
        return e;
    }

    // ------------------------------------------------------------------ content

    private static JournalEntry WeightOfThings()
    {
        JournalEntry e = ScriptableObject.CreateInstance<JournalEntry>();
        e.title = "2 · Weight of Things";
        e.tip = "Not sure which crate is which? Weigh it on the test scale by the door, or push it: if one of you can move it alone, " +
                "it's the light one. The light crate waits in the niche above the door; the Tether can reach it from the high step.";

        e.pages.Add(new JournalPage
        {
            bigIdea = "Mass is how much stuff something has. Weight is how hard gravity pulls on it: W = m·g.",
            notes = "This hall was a testing room. Three crates, the same size, no markings. The three pedestals by the gate only light up " +
                    "when they get the weight they want, and they ignore people. The old test scale by the door still reads any weight. " +
                    "Size tells you nothing here. Mass does.",
            lookCloser = $"W = m × 29.43 m/s²\n" +
                         $"1 kg → {Wt("29.4 N")}     2 kg → {Wt("58.9 N")}     3 kg → {Wt("88.3 N")}\n" +
                         "The three scales need 25 N, 55 N and 85 N.",
            funFact = "On the Moon your mass is exactly the same. Your weight is only about 1/6.",
            marginP1 = "They all look the same to me.",
            marginP2 = "Then let's weigh them.",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Crate\n? kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Support", newtons = 58.9f, direction = DiagramForce.Direction.Up, kind = DiagramForce.Kind.Normal },
                    new DiagramForce { label = "Weight", newtons = 58.9f, direction = DiagramForce.Direction.Down, kind = DiagramForce.Kind.Weight },
                },
                caption = "On a scale, the support force equals the weight. That's what it reads.",
            },
        });

        e.pages.Add(new JournalPage
        {
            bigIdea = "The same force moves a light object more: a = net force ÷ m.",
            notes = "A quick test without a scale: push it. Friction grips each crate with μ·m·g, so heavier crates grip harder. " +
                    "If one of you can move it alone, it's the light one.",
            lookCloser = $"Friction μ·m·g:  1 kg {Fric("9.8 N")}   2 kg {Fric("19.6 N")}   3 kg {Fric("29.4 N")}\n" +
                         $"One push {Push("12 N")}: moves only 1 kg  (a = 2.2 ÷ 1 = 2.2 m/s²)\n" +
                         $"Two pushes {Push("24 N")}: moves 2 kg too  (a = 4.4 ÷ 2 = 2.2 m/s²). 3 kg: no.",
            funFact = "Newton's second law fits in three letters: F = ma.",
            marginP1 = "If it won't budge, it's my turn.",
            marginP2 = "My tether pulls 24 N on its own.",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Crate\n2 kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Two pushes", newtons = 24f, direction = DiagramForce.Direction.Right, kind = DiagramForce.Kind.Push },
                    new DiagramForce { label = "Friction", newtons = 19.6f, direction = DiagramForce.Direction.Left, kind = DiagramForce.Kind.Friction },
                },
                caption = "Net force 4.4 N, so a = 4.4 ÷ 2 = 2.2 m/s².",
            },
        });

        e.pages.Add(new JournalPage
        {
            bigIdea = "One burst, different speeds: Δv = J ÷ m. Lighter crates go faster and slide further.",
            notes = "Careful near the acid. A burst that nudges the heavy crate sends the light one flying across the hall. " +
                    "Friction slows every sliding crate at the same rate, μ·g ≈ 9.8 m/s², so speed decides the distance. Count your bursts.",
            lookCloser = $"Δv = {Imp("15 N·s")} ÷ m:   3 kg → 5 m/s     1 kg → 15 m/s\n" +
                         "Slide on flat ground:  d = v² ÷ (2·μ·g) = v² ÷ 19.6\n" +
                         "5 m/s → about 1.3 m      15 m/s → about 11.5 m",
            funFact = "Mass doesn't change how fast friction slows a slide: heavy and light crates both lose 9.8 m/s every second.",
            marginP1 = "Gentle is not my style.",
            marginP2 = "Then I'll fetch the small one.",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Crate\n3 kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Burst", newtons = 15f, unit = "N·s", direction = DiagramForce.Direction.Right, kind = DiagramForce.Kind.Impulse },
                },
                caption = "Δv = 15 ÷ 3 = 5 m/s, then it slides about 1.3 m.",
            },
        });
        return e;
    }

    private static JournalEntry FirstExchange()
    {
        JournalEntry e = ScriptableObject.CreateInstance<JournalEntry>();
        e.title = "1 · The First Exchange";
        e.tip = "One of you stands on the small plate to hold the lift up while the other rides to the high perch. " +
                "Up there, slide the keystone onto its plate. The heavy crate on the ledge won't move for pushing: give it a Burst.";

        e.pages.Add(new JournalPage
        {
            bigIdea = "Things stay still (or keep moving) until a net force changes them.",
            notes = "Day one at the ruins. That crate has not moved in a hundred years, and two of us leaning on it " +
                    $"didn't change its mind. Not because it's \"heavy\", exactly: {Fric("friction")} pushes back just as hard as " +
                    $"we {Push("push")}, so the forces cancel. Net force zero, motion zero. Newton's First Law, in stone.",
            lookCloser = $"Friction can grip with up to  f = μ·m·g = (1/3) × 3 kg × 29.43 m/s² ≈ {Fric("29.4 N")}\n" +
                         $"Two pushes: 12 N + 12 N = {Push("24 N")} < {Fric("29.4 N")}, so friction wins.\n" +
                         $"The tether alone: {Pull("24 N")}. Same story.",
            funFact = "\"Inertia\" comes from the Latin for idleness. Objects really are lazy.",
            marginP1 = "Pushing harder? We ARE pushing harder.",
            marginP2 = "Then we need a different kind of force...",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Crate\n3 kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Two pushes", newtons = 24f, direction = DiagramForce.Direction.Right, kind = DiagramForce.Kind.Push },
                    new DiagramForce { label = "Friction", newtons = 24f, direction = DiagramForce.Direction.Left, kind = DiagramForce.Kind.Friction },
                },
                caption = "Balanced: friction matches the push (up to 29.4 N). Net force = 0.",
            },
        });

        e.pages.Add(new JournalPage
        {
            bigIdea = "An impulse is a big force for a short moment. It changes velocity right away.",
            notes = "The Kinetic Burst doesn't lean on the crate, it SLAMS it. All of the push arrives in an instant, " +
                    "so the crate jumps straight to a new speed before friction can settle the argument. " +
                    "Heavier things get less speed from the same burst.",
            lookCloser = "Impulse  J = F·Δt = m·Δv,  so  Δv = J ÷ m\n" +
                         $"Crate:  {Imp("15 N·s")} ÷ 3 kg = 5 m/s\n" +
                         $"Keystone:  {Imp("15 N·s")} ÷ 2 kg = 7.5 m/s",
            funFact = "A tennis racket touches the ball for about 5 thousandths of a second. That's an impulse.",
            marginP1 = "Short and sharp. That's my style.",
            marginP2 = "I'll take steady and from a distance.",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Crate\n3 kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Burst", newtons = 15f, unit = "N·s", direction = DiagramForce.Direction.Right, kind = DiagramForce.Kind.Impulse },
                },
                caption = "One burst: Δv = 15 ÷ 3 = 5 m/s, all at once.",
            },
        });

        e.pages.Add(new JournalPage
        {
            bigIdea = "Weight is the pull of gravity: W = m·g. Plates feel weight, not size.",
            notes = "The old pressure plates are scales. Each one waits for a certain weight before it clicks. " +
                    "Gravity here is three times stronger than back home, so everything weighs three times more. " +
                    "A plate counts everything stacked on it, so a player on the keystone adds up.",
            lookCloser = "W = m × 29.43 m/s²\n" +
                         $"Player 1 kg → {Wt("29.4 N")}     Keystone 2 kg → {Wt("58.9 N")}\n" +
                         $"Crate 3 kg → {Wt("88.3 N")}   (the plate pushes back {Sup("88.3 N")})",
            funFact = "On Earth, g is 9.81 m/s². This world's 29.43 is exactly three times that.",
            marginP1 = "So I'm a 29.4 N hero. Noted.",
            marginP2 = "Stand on the keystone. Instant 88.3 N.",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Crate\n3 kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Support", newtons = 88.3f, direction = DiagramForce.Direction.Up, kind = DiagramForce.Kind.Normal },
                    new DiagramForce { label = "Weight", newtons = 88.3f, direction = DiagramForce.Direction.Down, kind = DiagramForce.Kind.Weight },
                },
                caption = "The plate pushes up exactly as hard as the crate weighs.",
            },
        });
        return e;
    }

    private static JournalEntry Float()
    {
        JournalEntry e = ScriptableObject.CreateInstance<JournalEntry>();
        e.title = "Float";
        e.pages.Add(new JournalPage
        {
            bigIdea = "Less gravity means a slower fall, so you stay in the air longer and go further.",
            notes = "Hold Jump while falling and the shard's glow takes most of your weight away, for one second per jump. " +
                    "It never lifts you, it only slows the fall. (Real people can't switch gravity off, of course: " +
                    "that part is shard magic. The rest is honest physics.)",
            lookCloser = $"Float gravity: 29.43 ÷ 6 ≈ 4.9 m/s², so weight {Wt("29.4 N")} → {Wt("4.9 N")}. Fall capped at 2 m/s, 1 s per jump.\n" +
                         "Jump alone: about 4.9 m across.   With float: about 9.6 m.",
            funFact = "The Moon's gravity is about 1/6 of Earth's. Floating here feels like a Moon walk.",
            marginP1 = "Float, then Burst from the air!",
            marginP2 = "Hang time = aiming time.",
            diagram = new ForceDiagram
            {
                show = true,
                objectLabel = "Player\n1 kg",
                forces = new List<DiagramForce>
                {
                    new DiagramForce { label = "Weight (float)", newtons = 4.9f, direction = DiagramForce.Direction.Down, kind = DiagramForce.Kind.Weight },
                },
                caption = "Normally 29.4 N. While floating, only 4.9 N.",
            },
        });
        return e;
    }

    // ------------------------------------------------------------------ helpers

    // Key numbers are inked in the same colour as their arrow in the diagram (a little darker, for paper).
    private static string Push(string t) => $"<color=#a26029>{t}</color>";
    private static string Pull(string t) => $"<color=#267682>{t}</color>";
    private static string Fric(string t) => $"<color=#7d3029>{t}</color>";
    private static string Wt(string t) => $"<color=#8a5a2e>{t}</color>";
    private static string Sup(string t) => $"<color=#42612b>{t}</color>";
    private static string Imp(string t) => $"<color=#896923>{t}</color>";

    private static JournalEntry Save(string name, JournalEntry content)
    {
        string path = $"{Folder}/{name}.asset";
        JournalEntry existing = AssetDatabase.LoadAssetAtPath<JournalEntry>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(content, path);
            return content;
        }
        Sprite sketch = existing.sketch;           // drawn by Draw Room Sketches: keep it when the text is rewritten
        string sketchCaption = existing.sketchCaption;
        EditorUtility.CopySerialized(content, existing); // keep the asset's GUID so references survive
        existing.name = name;
        existing.sketch = sketch;
        existing.sketchCaption = sketchCaption;
        EditorUtility.SetDirty(existing);
        AssetDatabase.SaveAssets();
        Object.DestroyImmediate(content);
        return existing;
    }

    private static bool Assign(Transform root, string stoneName, JournalEntry entry)
    {
        foreach (JournalStone stone in root.GetComponentsInChildren<JournalStone>(true))
        {
            if (stone.name != stoneName) continue;
            stone.EditorSetEntry(entry);
            EditorUtility.SetDirty(stone);
            return true;
        }
        Debug.LogError("Journal tool: no JournalStone named " + stoneName + " in R1_Room01.");
        return false;
    }
}
