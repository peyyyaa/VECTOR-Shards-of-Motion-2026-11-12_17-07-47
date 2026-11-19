using UnityEngine;

/// <summary>
/// Base class for anything that can be "on" or "off" in a puzzle: pressure plates, levers,
/// and relays (which combine other sources). A PuzzleRelay reads IsActive from its inputs,
/// so any SignalSource can be dragged into a relay's Inputs list.
/// </summary>
public abstract class SignalSource : MonoBehaviour
{
    /// <summary>True while this source is on (plate pressed, lever on, relay conditions met).</summary>
    public abstract bool IsActive { get; }
}
