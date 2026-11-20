using UnityEngine;

/// <summary>
/// Moves an object back and forth between two points at a steady speed. One component for
/// moving platforms, lifts, extending bridges and sliding gates.
///
/// Point A = where the object starts. Point B = A + Travel. (Drawn in the Scene view.)
///
/// Modes:
/// - Follow Signal: goes to B while the Signal (plate, lever, relay) is on, back to A while off.
///   With no Signal, call MoveToA / MoveToB / Toggle from events instead.
/// - Loop: goes A → B → A ... on its own, waiting at each end. With a Signal, it only runs
///   while the signal is on ("a lever powers the platform").
///
/// Physics: uses a KINEMATIC Rigidbody2D moved by setting its velocity. A kinematic body isn't
/// pushed by forces or collisions (it acts as if infinitely heavy) but it pushes Dynamic bodies.
/// Because it has a real velocity, players riding it move along (PlayerMovement carries them)
/// and blocks are dragged along by friction.
///
/// Stop If Blocked: before each step, checks whether a player or block is directly in its path
/// and stops if so (waits, then carries on once the way is clear), so gates and lifts never crush
/// or shove anything. Riders standing on top of a rising lift are carried, not counted as blocking.
///
/// Setup: Rigidbody2D (Body Type Kinematic, Interpolate on) + a Collider2D, usually on the
/// Ground layer so players can stand and jump on it. Kinetic Burst and Wind Tether ignore it
/// (they only affect Dynamic bodies).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Mover : MonoBehaviour, IRoomResettable
{
    public enum MoverMode { FollowSignal, Loop }

    [Header("Path")]
    [Tooltip("Point B relative to the start position (point A), in units.")]
    [SerializeField] private Vector2 travel = new Vector2(0f, 3f);
    [Tooltip("Speed in units (meters) per second.")]
    [SerializeField, Min(0.01f)] private float speed = 2f;

    [Header("Control")]
    [SerializeField] private MoverMode mode = MoverMode.FollowSignal;
    [Tooltip("Follow Signal: at B while on, at A while off. Loop: only runs while on (empty = always runs).")]
    [SerializeField] private SignalSource signal;
    [SerializeField] private bool invertSignal = false;
    [Tooltip("Follow Signal: the signal must stay changed this long (seconds) before the mover reacts. " +
             "Gives players time to hop on or off a ferry. 0 = react at once.")]
    [SerializeField, Min(0f)] private float signalDelay = 0f;
    [Tooltip("Loop mode: seconds to wait at each end.")]
    [SerializeField, Min(0f)] private float waitAtEnds = 0.75f;

    [Header("Safety (gates)")]
    [SerializeField] private bool stopIfBlocked = false;
    [Tooltip("What counts as 'in the way'. Leave as Nothing for Player + PhysicsObject.")]
    [SerializeField] private LayerMask blockingLayers;

    private Rigidbody2D body;
    private Vector2 pointA;
    private Vector2 pointB;
    private bool targetB;          // Follow Signal without a signal: set by MoveToA/MoveToB/Toggle
    private bool loopGoingToB = true;
    private float waitTimer;
    private bool delayedPowered;    // the signal as the mover acts on it (after Signal Delay)
    private float delayTimer;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[8];

    public Vector2 HomePoint => pointA;

    /// <summary>True while the signal has changed but Signal Delay hasn't passed yet ("about to move").</summary>
    public bool IsAboutToMove { get; private set; }

    /// <summary>Developer testing only (DevTools F10): while true, the mover goes to point B whatever its signal.</summary>
    public bool DevHoldOpen { get; set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body.bodyType != RigidbodyType2D.Kinematic)
        {
            Debug.LogWarning($"{name}: Mover needs a Kinematic Rigidbody2D; switching it to Kinematic.", this);
            body.bodyType = RigidbodyType2D.Kinematic;
        }
        pointA = transform.position;
        pointB = pointA + travel;
        if (blockingLayers.value == 0) blockingLayers = LayerMask.GetMask("Player", "PhysicsObject");
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        bool powered = signal == null || (signal.IsActive != invertSignal);
        if (signalDelay > 0f && signal != null)
        {
            if (powered == delayedPowered) delayTimer = 0f;
            else if ((delayTimer += dt) >= signalDelay) { delayedPowered = powered; delayTimer = 0f; }
            IsAboutToMove = powered != delayedPowered;
            powered = delayedPowered;
        }

        Vector2 target;
        if (mode == MoverMode.FollowSignal)
        {
            if (signal != null) targetB = powered;
            target = targetB || DevHoldOpen ? pointB : pointA;
        }
        else
        {
            if (!powered || waitTimer > 0f)
            {
                if (powered) waitTimer -= dt;
                Stop();
                return;
            }
            target = loopGoingToB ? pointB : pointA;
        }

        Vector2 toTarget = target - body.position;
        float distance = toTarget.magnitude;
        if (distance < 0.0005f)
        {
            Stop();
            if (mode == MoverMode.Loop)
            {
                loopGoingToB = !loopGoingToB;
                waitTimer = waitAtEnds;
            }
            return;
        }

        float stepDistance = Mathf.Min(speed * dt, distance); // don't overshoot the end point
        Vector2 direction = toTarget / distance;

        if (stopIfBlocked && IsBlocked(direction, stepDistance))
        {
            Stop();
            return;
        }

        body.linearVelocity = direction * (stepDistance / dt);
    }

    private void Stop() => body.linearVelocity = Vector2.zero;

    // Something on Blocking Layers directly ahead, with its surface facing us.
    // - A rider on top while we move sideways has a sideways-facing contact, so it doesn't count.
    // - While moving UP, anything already touching our top is a rider being carried, not an obstacle;
    //   only things we haven't reached yet can block us. (Moving down, anything below us counts:
    //   a lift stops above a player instead of shoving them aside.)
    private const float RiderContactDistance = 0.05f;

    private bool IsBlocked(Vector2 direction, float distance)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(blockingLayers);
        filter.useTriggers = false;

        bool movingUp = direction.y > 0.5f;
        int count = body.Cast(direction, filter, hits, distance + 0.02f);
        for (int i = 0; i < count; i++)
        {
            if (Vector2.Dot(hits[i].normal, direction) >= -0.5f) continue; // not in our path
            if (movingUp && hits[i].distance <= RiderContactDistance) continue; // riding on top
            return true;
        }
        return false;
    }

    // For UnityEvents (Follow Signal mode with no Signal assigned).
    public void MoveToB() => targetB = true;
    public void MoveToA() => targetB = false;
    public void Toggle() => targetB = !targetB;
    public void SetAtB(bool atB) => targetB = atB;

    /// <summary>Room reset: instantly back to point A, stopped.</summary>
    public void ResetToStart()
    {
        body.position = pointA;
        transform.position = new Vector3(pointA.x, pointA.y, transform.position.z);
        Stop();
        targetB = false;
        loopGoingToB = true;
        waitTimer = 0f;
        delayedPowered = false;
        delayTimer = 0f;
        IsAboutToMove = false;
    }

    // Scene view: the path (A → B) and the object's outline at B.
    private void OnDrawGizmos()
    {
        Vector2 a = Application.isPlaying ? pointA : (Vector2)transform.position;
        Vector2 b = a + travel;
        Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.9f);
        Gizmos.DrawLine(a, b);

        Collider2D col = GetComponent<Collider2D>();
        Vector3 size = col != null ? col.bounds.size : Vector3.one * 0.5f;
        Vector3 offset = col != null ? col.bounds.center - transform.position : Vector3.zero;
        if (Application.isPlaying) offset = col != null ? col.bounds.center - (Vector3)body.position : Vector3.zero;
        Gizmos.DrawWireCube((Vector3)b + offset, size);
    }
}
