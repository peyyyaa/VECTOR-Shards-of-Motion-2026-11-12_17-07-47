using UnityEngine;

/// <summary>
/// Anything that can return to how it started when its room is reset
/// (blocks, levers, relays, doors).
///
/// Which room an object belongs to: its parent Room if it's a child of one; otherwise the Room
/// whose area contains its HomePoint. (A block's HomePoint is where it started, so a block that
/// was pushed into the next room still belongs to, and resets with, its original room.)
/// </summary>
public interface IRoomResettable
{
    /// <summary>Where this object "lives" (its start position).</summary>
    Vector2 HomePoint { get; }

    /// <summary>Put this object back in its starting state.</summary>
    void ResetToStart();
}
