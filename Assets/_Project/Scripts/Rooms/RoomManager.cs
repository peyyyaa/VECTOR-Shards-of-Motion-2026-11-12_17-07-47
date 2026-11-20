using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Keeps track of which room the players are in, slides the camera between rooms, and
/// respawns a player who leaves the current room (e.g., falls into a pit).
///
/// - Room change: called by a RoomExit when both players are in its zone. Both players are
///   placed at the target room's entry points and the camera slides over (Slide Duration).
/// - Respawn: if a player ends up outside the current room's area (plus Respawn Margin), only
///   that player goes back to the point where they entered this room. The other player isn't reset.
///
/// The camera never follows the players inside a room: it shows the whole room, so both
/// players are always on screen.
///
/// Setup: one per scene (e.g., on an empty "RoomManager" GameObject). Assign Starting Room and the
/// Coop Spawner. Camera Transform defaults to the Main Camera.
/// </summary>
public class RoomManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The room the camera shows when the scene starts (where the players spawn).")]
    [SerializeField] private Room startingRoom;
    [Tooltip("Provides the spawned players.")]
    [SerializeField] private LocalCoopSpawner spawner;
    [Tooltip("The camera to move between rooms. Leave empty to use the Main Camera.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Transition")]
    [Tooltip("Seconds the camera takes to slide to the next room.")]
    [SerializeField, Min(0f)] private float slideDuration = 0.4f;

    [Header("Respawn")]
    [Tooltip("How far (units) outside the room's area a player can go before respawning.")]
    [SerializeField, Min(0f)] private float respawnMargin = 1f;

    [Header("Saving")]
    [Tooltip("Name of this region in the save file (progress = furthest room reached here).")]
    [SerializeField] private string regionId = "Region1";
    [Tooltip("Off for places that aren't region rooms (the village), so they don't count as progress.")]
    [SerializeField] private bool saveProgress = true;

    /// <summary>Set before loading a region scene (e.g. Continue): start in this room number instead of the first. 0 = normal.</summary>
    public static int RequestedStartRoom;

    private Room[] roomsInOrder = new Room[0];

    private Room currentRoom;
    private Transform entryPlayer1;   // where each player entered the current room
    private Transform entryPlayer2;

    private bool transitioning;
    private float slideTimer;
    private Vector3 slideFrom;
    private Vector3 slideTo;

    public Room CurrentRoom => currentRoom;
    public bool IsTransitioning => transitioning;

    /// <summary>The region these rooms belong to, or null in places that aren't a region (the village). Used by the map.</summary>
    public string RegionId => saveProgress ? regionId : null;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        if (startingRoom == null || spawner == null || cameraTransform == null)
        {
            Debug.LogError("RoomManager: assign Starting Room and Spawner (and make sure there's a Main Camera).", this);
            return;
        }

        currentRoom = startingRoom;
        entryPlayer1 = startingRoom.Player1Entry;
        entryPlayer2 = startingRoom.Player2Entry;
        cameraTransform.position = CameraPositionFor(startingRoom);

        roomsInOrder = FindObjectsByType<Room>(FindObjectsSortMode.None);
        System.Array.Sort(roomsInOrder, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        if (saveProgress) SaveSystem.ReachedRoom(regionId, RoomNumber(startingRoom));

        int requested = RequestedStartRoom;
        RequestedStartRoom = 0;
        if (requested > 1) StartCoroutine(GoToNextFrame(requested));
    }

    // Wait one frame so the players have spawned, then jump.
    private System.Collections.IEnumerator GoToNextFrame(int roomNumber)
    {
        yield return null;
        GoToRoomNumber(roomNumber);
    }

    /// <summary>Rooms in this region, left to right (room 1 first).</summary>
    public IReadOnlyList<Room> Rooms => roomsInOrder;

    /// <summary>1-based number of a room (left to right), or 0 if unknown.</summary>
    public int RoomNumber(Room room) => System.Array.IndexOf(roomsInOrder, room) + 1;

    /// <summary>
    /// Moves both players to room n (1-based), arriving exactly where they would when walking in: at the arrival
    /// points of the doorway from the previous room (Doorway_R{n-1}_R{n}), else at the room's own entry points.
    /// </summary>
    public void GoToRoomNumber(int n)
    {
        if (n < 1 || n > roomsInOrder.Length) return;
        Room room = roomsInOrder[n - 1];
        GameObject doorway = n > 1 ? GameObject.Find($"Doorway_R{n - 1}_R{n}") : null;
        Transform a1 = doorway != null ? doorway.transform.Find("Arrive_Right_P1") : null;
        Transform a2 = doorway != null ? doorway.transform.Find("Arrive_Right_P2") : null;
        TransitionTo(room, a1 != null ? a1 : room.Player1Entry, a2 != null ? a2 : room.Player2Entry);
    }

    /// <summary>True if exits of 'room' may be used right now (it's the current room, not mid-slide).</summary>
    public bool CanLeave(Room room) => !transitioning && room != null && room == currentRoom;

    /// <summary>Moves both players into 'target' and slides the camera there.</summary>
    public void TransitionTo(Room target, Transform arrivalPlayer1, Transform arrivalPlayer2)
    {
        if (transitioning || target == null || target == currentRoom || cameraTransform == null) return;

        currentRoom = target;
        entryPlayer1 = arrivalPlayer1 != null ? arrivalPlayer1 : target.Player1Entry;
        entryPlayer2 = arrivalPlayer2 != null ? arrivalPlayer2 : target.Player2Entry;

        IReadOnlyList<PlayerInput> players = spawner.Players;
        for (int i = 0; i < players.Count; i++)
        {
            PlacePlayer(players[i], EntryFor(i));
        }

        slideFrom = cameraTransform.position;
        slideTo = CameraPositionFor(target);
        slideTimer = 0f;
        transitioning = true;

        if (saveProgress) SaveSystem.ReachedRoom(regionId, RoomNumber(target)); // autosave: progress
    }

    /// <summary>Sends one player back to where they entered the current room (e.g., touched a hazard).</summary>
    public void RespawnPlayer(Rigidbody2D playerBody)
    {
        if (playerBody == null || spawner == null || currentRoom == null) return;

        IReadOnlyList<PlayerInput> players = spawner.Players;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null && players[i].GetComponent<Rigidbody2D>() == playerBody)
            {
                PlacePlayer(players[i], EntryFor(i));
                return;
            }
        }
    }

    /// <summary>
    /// Resets the current room: its blocks, levers, relays and doors return to their starting
    /// state, and both players return to where they entered the room.
    /// </summary>
    public void ResetCurrentRoom()
    {
        if (transitioning || currentRoom == null || spawner == null) return;

        currentRoom.ResetRoom();

        IReadOnlyList<PlayerInput> players = spawner.Players;
        for (int i = 0; i < players.Count; i++)
        {
            PlacePlayer(players[i], EntryFor(i));
        }
    }

    private void FixedUpdate()
    {
        if (transitioning || currentRoom == null || spawner == null) return;

        IReadOnlyList<PlayerInput> players = spawner.Players;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == null) continue;
            Rigidbody2D rb = players[i].GetComponent<Rigidbody2D>();
            if (rb != null && !currentRoom.Contains(rb.position, respawnMargin))
            {
                PlacePlayer(players[i], EntryFor(i));
            }
        }
    }

    // Camera moves in LateUpdate, after everything else this frame, so it doesn't jitter.
    private void LateUpdate()
    {
        if (!transitioning) return;

        slideTimer += Time.deltaTime;
        float t = slideDuration > 0f ? Mathf.Clamp01(slideTimer / slideDuration) : 1f;
        cameraTransform.position = Vector3.Lerp(slideFrom, slideTo, Mathf.SmoothStep(0f, 1f, t));

        if (t >= 1f) transitioning = false;
    }

    private Transform EntryFor(int playerListIndex) => playerListIndex == 0 ? entryPlayer1 : entryPlayer2;

    private Vector3 CameraPositionFor(Room room)
    {
        // Keep the camera's own z (it sits in front of the 2D world, e.g., z = -10).
        return new Vector3(room.Center.x, room.Center.y, cameraTransform.position.z);
    }

    // Moves a player to a point and stops them. Used only for room changes and respawns:
    // gameplay movement always uses physics.
    private static void PlacePlayer(PlayerInput player, Transform point)
    {
        if (player == null || point == null) return;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        Vector3 p = point.position;
        player.transform.position = new Vector3(p.x, p.y, player.transform.position.z);
        if (rb != null)
        {
            rb.position = p;
            rb.linearVelocity = Vector2.zero;
        }
    }
}
