using UnityEngine;

/// <summary>
/// An exit door. Closed: a solid wall. Open: the solid collider is switched off,
/// the sprite fades, and the player walking into the doorway completes the room.
///
/// Setup: the door GameObject needs TWO BoxCollider2D components:
///   1. A solid one (Is Trigger OFF) — assign it to "Solid Collider". Blocks the player while closed.
///   2. A trigger one (Is Trigger ON) — detects the player in the doorway.
/// Call Open() / Close() from another script or a UnityEvent (e.g., PressurePlate's On Pressed / On Released).
/// </summary>
public class ExitDoor : MonoBehaviour
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

    private bool isOpen;
    private bool roomCompleted;

    public bool IsOpen => isOpen;

    private void Start()
    {
        SetOpen(startOpen);
    }

    public void Open() => SetOpen(true);

    public void Close() => SetOpen(false);

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

    // "Stay" (not "Enter") so it still works if the player is already
    // standing in the doorway at the moment the door opens.
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isOpen || roomCompleted) return;

        Rigidbody2D rb = other.attachedRigidbody;
        if (rb != null && rb.GetComponent<PlayerMovement>() != null)
        {
            roomCompleted = true;
            Debug.Log("Room complete!");
        }
    }
}
