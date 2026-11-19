using UnityEngine;

/// <summary>
/// A deadly area (acid, lava, poisonous water, spikes).
/// - A player who touches it respawns where they entered the current room.
/// - A block (with BlockHome) that touches it returns to where it started.
/// Anything else is ignored.
///
/// Setup: a Collider2D with Is Trigger ON. On a Pixel Water object it can share the water's
/// own trigger collider. The water's layer must collide with Player and PhysicsObject in the
/// Layer Collision Matrix (trigger messages need that).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HazardZone : MonoBehaviour
{
    [SerializeField] private bool respawnPlayers = true;
    [SerializeField] private bool returnBlocks = true;

    private RoomManager manager;

    private void Awake()
    {
        manager = FindFirstObjectByType<RoomManager>();
        if (manager == null) Debug.LogError($"{name}: HazardZone needs a RoomManager in the scene.", this);
    }

    private void OnTriggerEnter2D(Collider2D other) => Handle(other);

    // Also on Stay, in case something is already inside when the zone turns on.
    private void OnTriggerStay2D(Collider2D other) => Handle(other);

    private void Handle(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        if (respawnPlayers && rb.GetComponent<PlayerMovement>() != null)
        {
            if (manager != null) manager.RespawnPlayer(rb);
        }
        else if (returnBlocks && rb.TryGetComponent(out BlockHome home))
        {
            home.ReturnHome();
        }
    }
}
