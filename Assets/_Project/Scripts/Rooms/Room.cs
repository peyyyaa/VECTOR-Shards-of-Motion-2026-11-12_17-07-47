using UnityEngine;

/// <summary>
/// Marks one screen-sized area of the world as a "room". The camera shows exactly one room at a
/// time, and a player who leaves the current room's area (e.g., falls into a pit) respawns.
///
/// All rooms of a region live side by side in ONE scene, so everything (blocks, open doors)
/// stays where the players left it — no save system needed.
///
/// Setup: put this on an empty GameObject placed at the room's CENTER. The room's area is drawn
/// as a yellow box in the Scene view. Default size = one screen:
///   640 × 360 pixels at 32 pixels per unit = 20 × 11.25 units.
/// Rooms next to each other should touch edge to edge (e.g., centers 20 units apart sideways).
/// </summary>
public class Room : MonoBehaviour
{
    [Tooltip("Room area in units, centered on this GameObject. Keep the default (one screen) unless the camera changes.")]
    [SerializeField] private Vector2 size = new Vector2(20f, 11.25f);

    [Header("Default Entry Points")]
    [Tooltip("Where Player 1 appears if the game starts in this room, or respawns before entering through an exit.")]
    [SerializeField] private Transform player1Entry;
    [Tooltip("Where Player 2 appears if the game starts in this room, or respawns before entering through an exit.")]
    [SerializeField] private Transform player2Entry;

    [Tooltip("Leaving through the exit to the next room counts as solving this room (map, solved banner). Off for the " +
             "first parts of a multi-room level (the finale): it is solved at its end instead.")]
    [SerializeField] private bool solvedOnExit = true;

    public Vector2 Center => transform.position;
    public bool SolvedOnExit => solvedOnExit;
    public Vector2 Size => size;
    public Transform Player1Entry => player1Entry;
    public Transform Player2Entry => player2Entry;

    /// <summary>True if the point is inside the room's area, grown by 'margin' units on every side.</summary>
    public bool Contains(Vector2 point, float margin = 0f)
    {
        Vector2 half = size * 0.5f + new Vector2(margin, margin);
        Vector2 offset = point - Center;
        return Mathf.Abs(offset.x) <= half.x && Mathf.Abs(offset.y) <= half.y;
    }

    /// <summary>
    /// Returns every resettable object that belongs to this room to its starting state
    /// (see IRoomResettable for how membership is decided). Players are handled by RoomManager.
    /// </summary>
    public void ResetRoom()
    {
        foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (behaviour is IRoomResettable resettable && BelongsHere(behaviour, resettable))
            {
                resettable.ResetToStart();
            }
        }
    }

    private bool BelongsHere(MonoBehaviour behaviour, IRoomResettable resettable)
    {
        Room parentRoom = behaviour.GetComponentInParent<Room>();
        if (parentRoom != null) return parentRoom == this;
        return Contains(resettable.HomePoint);
    }

    // Always drawn (not just when selected) so the room layout is visible while building levels.
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(transform.position, size);

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.5f);
        if (player1Entry != null) Gizmos.DrawWireSphere(player1Entry.position, 0.3f);
        if (player2Entry != null) Gizmos.DrawWireSphere(player2Entry.position, 0.3f);
    }
}
