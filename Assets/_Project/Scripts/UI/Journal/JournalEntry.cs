using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One Motion Journal entry (the field journal of Wren Alder): a title and one or more pages.
/// Create with Assets > Create > Shards of Motion > Journal Entry, fill it in the Inspector, and drag it
/// into a JournalStone's Entry field. Players turn pages with Interact.
///
/// Writing guide: Big Idea = one plain sentence anyone can follow. Notes = Wren's voice, first person.
/// Look Closer = the formulas with this room's real numbers (one per line). Keep each page short.
/// </summary>
[CreateAssetMenu(menuName = "Shards of Motion/Journal Entry")]
public class JournalEntry : ScriptableObject
{
    public string title = "Journal";
    public List<JournalPage> pages = new List<JournalPage>();
}

[Serializable]
public class JournalPage
{
    [TextArea(2, 3)] public string bigIdea;
    [TextArea(3, 6)] public string notes;
    [Tooltip("Formulas with this room's numbers, one per line.")]
    [TextArea(2, 5)] public string lookCloser;
    [TextArea(1, 3)] public string funFact;
    [Tooltip("A short note from Player 1 (Kinetic Burst), shown in orange.")]
    public string marginP1;
    [Tooltip("A short note from Player 2 (Wind Tether), shown in cyan.")]
    public string marginP2;
    public ForceDiagram diagram = new ForceDiagram();
}

/// <summary>A free-body diagram: one object and the forces acting on it, drawn to scale on the page.</summary>
[Serializable]
public class ForceDiagram
{
    public bool show = false;
    [Tooltip("Written on the object, e.g. \"Stone 2 kg\".")]
    public string objectLabel = "2 kg";
    public List<DiagramForce> forces = new List<DiagramForce>();
    [TextArea(1, 3)] public string caption;
}

[Serializable]
public class DiagramForce
{
    public enum Direction { Right, Left, Up, Down }
    public enum Kind { Push, Pull, Friction, Weight, Normal, Impulse }

    public string label = "Push";
    [Tooltip("Size of the force in newtons (sets the arrow length). For an impulse, N·s.")]
    public float newtons = 10f;
    public Direction direction = Direction.Right;
    [Tooltip("Push = orange (P1), Pull = cyan (P2), Friction = red, Weight = brown, Normal = green, Impulse = gold.")]
    public Kind kind = Kind.Push;
    [Tooltip("Unit shown after the number.")]
    public string unit = "N";
}
