using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A doorway from one room to the next. When BOTH players stand inside this zone, the camera
/// slides to the target room and the players are placed at that room's entry points.
///
/// Why both: the screen is shared, so one player can't go ahead without the other. The first
/// player to arrive simply waits at the edge (same rule as the exit door).
///
/// Setup:
/// - Put this on a GameObject with a BoxCollider2D, "Is Trigger" ON, covering the opening
///   on the CURRENT room's side. Make it a child of that room so "From Room" fills in itself.
/// - Target Entry points go inside the target room, OUTSIDE that room's own exit zones
///   (otherwise the players would bounce straight back).
/// - Behind the zone, block the opening with a "player barrier": a collider on the PlayerBarrier
///   layer, which collides with players only. Players can't walk off-screen, but blocks can
///   still be pushed or burst through into the next room.
/// - For two-way travel, the target room needs its own RoomExit pointing back.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RoomExit : MonoBehaviour
{
    [Header("Rooms")]
    [Tooltip("The room this exit belongs to. If empty: the parent Room, or else the Room whose area contains this exit.")]
    [SerializeField] private Room fromRoom;
    [Tooltip("The room the players go to. If empty: the Room whose area contains Target Entry Player 1.")]
    [SerializeField] private Room targetRoom;

    [Header("Arrival Points (inside the target room)")]
    [SerializeField] private Transform targetEntryPlayer1;
    [SerializeField] private Transform targetEntryPlayer2;

    [Header("Co-op")]
    [Tooltip("How many players must be in the zone to go through.")]
    [SerializeField, Min(1)] private int requiredPlayers = 2;

    private Collider2D zone;
    private RoomManager manager;
    private readonly Collider2D[] overlapResults = new Collider2D[8];
    private readonly HashSet<PlayerMovement> playersFound = new HashSet<PlayerMovement>();

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
        fromRoom = GetComponentInParent<Room>();
    }

    private void Awake()
    {
        zone = GetComponent<Collider2D>();
        if (fromRoom == null) fromRoom = GetComponentInParent<Room>();
        if (fromRoom == null) fromRoom = FindRoomContaining(transform.position);
        if (targetRoom == null && targetEntryPlayer1 != null) targetRoom = FindRoomContaining(targetEntryPlayer1.position);
        manager = FindFirstObjectByType<RoomManager>();

        if (manager == null) Debug.LogError($"{name}: no RoomManager in the scene.", this);
        if (fromRoom == null) Debug.LogError($"{name}: 'From Room' isn't set (make this a child of a Room).", this);
        if (targetRoom == null) Debug.LogError($"{name}: 'Target Room' isn't set, and no Room contains its arrival point.", this);
    }

    private void FixedUpdate()
    {
        if (manager == null || targetRoom == null) return;
        if (!manager.CanLeave(fromRoom)) return; // only the current room's exits work

        if (CountPlayersInside() >= requiredPlayers)
        {
            manager.TransitionTo(targetRoom, targetEntryPlayer1, targetEntryPlayer2);
        }
    }

    // Fallback when this exit isn't a child of a Room: use the room whose area contains it.
    private static Room FindRoomContaining(Vector2 point)
    {
        foreach (Room room in FindObjectsByType<Room>(FindObjectsSortMode.None))
        {
            if (room.Contains(point)) return room;
        }
        return null;
    }

    // Asks the physics engine who's overlapping the zone right now (instead of counting
    // enter/exit events), so it's always correct, even right after players are moved.
    private int CountPlayersInside()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;

        int count = zone.Overlap(filter, overlapResults);
        playersFound.Clear();
        for (int i = 0; i < count; i++)
        {
            Rigidbody2D rb = overlapResults[i].attachedRigidbody;
            PlayerMovement player = rb != null ? rb.GetComponent<PlayerMovement>() : null;
            if (player != null) playersFound.Add(player);
        }
        return playersFound.Count;
    }

    // Scene view: the zone (green) and lines to where the players will arrive.
    private void OnDrawGizmos()
    {
        Collider2D col = zone != null ? zone : GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);

        if (targetEntryPlayer1 != null) Gizmos.DrawLine(col.bounds.center, targetEntryPlayer1.position);
        if (targetEntryPlayer2 != null) Gizmos.DrawLine(col.bounds.center, targetEntryPlayer2.position);
    }
}
