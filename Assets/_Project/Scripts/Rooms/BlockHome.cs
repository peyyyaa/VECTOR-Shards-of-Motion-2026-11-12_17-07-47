using UnityEngine;

/// <summary>
/// Gives a physics block a "home": the position it started in.
///
/// - Automatic recovery: if the block ends up outside EVERY room's area (e.g., it fell down a
///   pit), it returns home. Blocks that go through a doorway on purpose are still inside a
///   room, so they're left alone.
/// - Room reset / hazards: ReturnHome() is called by the room reset and by HazardZone.
///
/// Returning home is an instant move (like a respawn), not a physics motion: the block's
/// speed and spin are cleared.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BlockHome : MonoBehaviour, IRoomResettable
{
    [Tooltip("How far (units) outside all rooms the block can go before it returns home.")]
    [SerializeField, Min(0f)] private float lostMargin = 1f;

    private Rigidbody2D body;
    private Vector2 homePosition;
    private float homeRotation;
    private Room[] rooms;

    public Vector2 HomePoint => homePosition;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        homePosition = transform.position;
        homeRotation = transform.eulerAngles.z;
    }

    private void Start()
    {
        rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
    }

    private void FixedUpdate()
    {
        if (rooms == null || rooms.Length == 0) return;

        Vector2 p = body.position;
        foreach (Room room in rooms)
        {
            if (room != null && room.Contains(p, lostMargin)) return; // still in some room
        }
        ReturnHome();
    }

    public void ResetToStart() => ReturnHome();

    /// <summary>Instantly puts the block back where it started, at rest.</summary>
    public void ReturnHome()
    {
        transform.SetPositionAndRotation(homePosition, Quaternion.Euler(0f, 0f, homeRotation));
        body.position = homePosition;
        body.rotation = homeRotation;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 home = Application.isPlaying ? (Vector3)homePosition : transform.position;
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(home, Vector3.one);
    }
}
