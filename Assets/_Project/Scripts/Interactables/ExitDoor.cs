using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An exit door. Closed: a solid wall. Open: the solid collider is switched off,
/// the sprite fades, and the room completes once enough players (default: both)
/// are standing in the doorway.
///
/// Setup: the door GameObject needs TWO BoxCollider2D components:
///   1. A solid one (Is Trigger OFF) — assign it to "Solid Collider". Blocks the player while closed.
///   2. A trigger one (Is Trigger ON) — detects the player in the doorway.
/// Call Open() / Close() from another script or a UnityEvent (e.g., PressurePlate's On Pressed / On Released).
/// </summary>
public class ExitDoor : MonoBehaviour, IRoomResettable
{
    [Header("References")]
    [Tooltip("The non-trigger collider that blocks the doorway while closed.")]
    [SerializeField] private Collider2D solidCollider;
    [SerializeField] private SpriteRenderer doorRenderer;

    [Header("Appearance")]
    [Tooltip("Sprite opacity when open (0 = invisible, 1 = solid).")]
    [SerializeField, Range(0f, 1f)] private float openAlpha = 0.25f;

    [Header("State")]
    [SerializeField] private bool startOpen = false;
    [Tooltip("Once opened, the door stays open even if Close() is called (e.g., the block is taken off the plate).")]
    [SerializeField] private bool stayOpenOnceOpened = false;

    [Header("Co-op")]
    [Tooltip("How many players must be in the open doorway to complete the room.")]
    [SerializeField, Min(1)] private int requiredPlayers = 2;

    private bool isOpen;
    private bool roomCompleted;
    private readonly HashSet<PlayerMovement> playersInDoorway = new HashSet<PlayerMovement>();

    public bool IsOpen => isOpen;

    private void Start()
    {
        SetOpen(startOpen);
    }

    public Vector2 HomePoint => transform.position;

    /// <summary>Room reset: back to the starting state (also clears the stay-open latch).</summary>
    public void ResetToStart()
    {
        roomCompleted = false;
        SetOpen(startOpen);
    }

    public void Open() => SetOpen(true);

    public void Close()
    {
        if (stayOpenOnceOpened && isOpen) return; // latched open
        SetOpen(false);
    }

    private void SetOpen(bool open)
    {
        isOpen = open;

        if (solidCollider != null)
        {
            solidCollider.enabled = !open;
        }

        if (doorRenderer != null)
        {
            Color c = doorRenderer.color;
            c.a = open ? openAlpha : 1f;
            doorRenderer.color = c;
        }
    }

    // Track which players are inside the doorway trigger. Entering/leaving is tracked even
    // while the door is closed, so the room completes the moment it opens if everyone's there.
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement player = GetPlayer(other);
        if (player != null) playersInDoorway.Add(player);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerMovement player = GetPlayer(other);
        if (player != null) playersInDoorway.Remove(player);
    }

    private void FixedUpdate()
    {
        playersInDoorway.RemoveWhere(p => p == null); // destroyed players

        if (isOpen && !roomCompleted && playersInDoorway.Count >= requiredPlayers)
        {
            roomCompleted = true;
            Debug.Log("Room complete!");
        }
    }

    private static PlayerMovement GetPlayer(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        return rb != null ? rb.GetComponent<PlayerMovement>() : null;
    }
}
