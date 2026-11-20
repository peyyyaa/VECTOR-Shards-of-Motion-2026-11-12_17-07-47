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

    /// <summary>Creates (or rewrites) the Room 2 entry asset; Room2Builder assigns it to the room's journal stone.</summary>
    public static JournalEntry SaveRoom2Entry()
    {
        Directory.CreateDirectory(Folder);
        return Save("R1_02_WeightOfThings", WeightOfThings());
    }

    // ------------------------------------------------------------------ content

    private static JournalEntry WeightOfThings()
    {
        JournalEntry e = ScriptableObject.CreateInstance<JournalEntry>();
        e.title = "2 · Weight of Things";

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
        EditorUtility.CopySerialized(content, existing); // keep the asset's GUID so references survive
        existing.name = name;
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
