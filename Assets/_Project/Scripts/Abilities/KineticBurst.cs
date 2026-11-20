using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kinetic Burst (Player 1): an instant push on physics objects in a cone in front of the player.
///
/// Physics:
/// - Impulse changes momentum:  J = Δp = m * Δv   →   Δv = J / m
///   Every object in the cone receives the same impulse J, so a light object
///   gains more speed than a heavy one. (Try changing the block's Mass.)
/// - Newton's 3rd law: if the player pushes an object with impulse J, the object
///   pushes back on the player with -J. Physically exact recoil = Recoil Multiplier 1.
///   The default is smaller (a deliberate game choice so the player stays controllable).
/// - No objects hit means nothing pushed back, so there is no recoil.
///
/// Never affects players (including the partner), even if the Player layer is ticked
/// in Affected Layers. Launching players is left to environmental equipment (spring pads, etc.).
///
/// Unity details: impulses use ForceMode2D.Impulse (an instant change in momentum).
/// After that, Unity's friction, collisions, and gravity take over; real-world results
/// would also depend on air resistance and surface details Unity simplifies.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
public class KineticBurst : MonoBehaviour
{
    // Triggered by this player's "Ability" button (read via PlayerInputReader).

    [Header("Burst Area")]
    [Tooltip("How far the burst reaches from the player's center, in units (meters).")]
    [SerializeField] private float range = 2.5f;
    [Tooltip("Total width of the cone in degrees (60 = 30° above and below facing direction).")]
    [SerializeField, Range(1f, 180f)] private float coneAngle = 60f;
    [Tooltip("Layers the burst can push (PhysicsObject). Players are always ignored.")]
    [SerializeField] private LayerMask affectedLayers;

    [Header("Force")]
    [Tooltip("Impulse given to each object hit, in newton-seconds (N·s = kg·m/s).")]
    [SerializeField] private float burstImpulse = 15f;
    [Tooltip("Fraction of the reaction impulse applied to the player. 1 = physically exact (Newton's 3rd law), 0 = no recoil.")]
    [SerializeField, Range(0f, 1f)] private float recoilMultiplier = 0.15f;
    [Tooltip("Seconds the player's normal steering is paused so the recoil is visible.")]
    [SerializeField] private float recoilControlLockTime = 0.15f;

    [Header("Timing")]
    [Tooltip("Seconds between bursts.")]
    [SerializeField] private float cooldown = 0.5f;

    private Rigidbody2D body;
    private PlayerMovement movement;
    private PlayerInputReader input;

    private bool burstRequested;      // set in Update, consumed in FixedUpdate
    private float cooldownRemaining;
    private float lockTimeRemaining;
    private bool holdingLock;         // true while this script owns ControlLocked

    private readonly HashSet<Rigidbody2D> alreadyHit = new HashSet<Rigidbody2D>();

    /// <summary>Time.time of the last burst (read by PlayerAnimator for the cast pose).</summary>
    public float LastFireTime { get; private set; } = float.NegativeInfinity;

    /// <summary>One object pushed by a burst: which body, the push direction, and the impulse (N·s).</summary>
    public struct Hit
    {
        public Rigidbody2D body;
        public Vector2 direction;
        public float impulse;
    }

    /// <summary>Raised after every burst (also when nothing was hit). Used by BurstEffects; read-only.</summary>
    public event System.Action<Vector2, int, IReadOnlyList<Hit>> Fired;

    public float Range => range;
    public float ConeAngle => coneAngle;

    private readonly List<Hit> lastHits = new List<Hit>();

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        input = GetComponent<PlayerInputReader>();
    }

    private void OnDisable()
    {
        ReleaseLock();
    }

    private void Update()
    {
        if (input.AbilityPressedThisFrame)
        {
            burstRequested = true;
        }
    }

    private void FixedUpdate()
    {
        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.fixedDeltaTime;
        }

        if (holdingLock)
        {
            lockTimeRemaining -= Time.fixedDeltaTime;
            if (lockTimeRemaining <= 0f)
            {
                ReleaseLock();
            }
        }

        // Don't fire during a dash or while another ability controls the player.
        if (burstRequested && cooldownRemaining <= 0f && !movement.ControlLocked)
        {
            Fire();
        }

        burstRequested = false;
    }

    private void Fire()
    {
        cooldownRemaining = cooldown;
        LastFireTime = Time.time;

        Vector2 origin = body.position;
        Vector2 facing = new Vector2(movement.FacingDirection, 0f);
        float halfAngle = coneAngle * 0.5f;

        Vector2 totalReaction = Vector2.zero;
        alreadyHit.Clear();
        lastHits.Clear();

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, affectedLayers);
        foreach (Collider2D hit in hits)
        {
            Rigidbody2D target = hit.attachedRigidbody;

            // Skip: no Rigidbody2D, the player itself, non-dynamic bodies, or a body already pushed
            // (one object can have several colliders).
            if (target == null || target == body || target.bodyType != RigidbodyType2D.Dynamic)
                continue;
            if (!alreadyHit.Add(target))
                continue;

            // Never push players (safety net in case the Player layer is ticked by mistake).
            if (target.GetComponent<PlayerMovement>() != null)
                continue;

            Vector2 toTarget = target.worldCenterOfMass - origin;
            if (toTarget.sqrMagnitude < 0.0001f)
                continue;

            if (Vector2.Angle(facing, toTarget) > halfAngle)
                continue; // outside the cone

            Vector2 pushDirection = toTarget.normalized;
            target.AddForce(pushDirection * burstImpulse, ForceMode2D.Impulse);

            totalReaction -= pushDirection * burstImpulse; // Newton's 3rd law: equal and opposite
            lastHits.Add(new Hit { body = target, direction = pushDirection, impulse = burstImpulse });
        }

        if (totalReaction != Vector2.zero && recoilMultiplier > 0f)
        {
            body.AddForce(totalReaction * recoilMultiplier, ForceMode2D.Impulse);

            // Pause normal steering briefly; otherwise PlayerMovement would overwrite
            // the recoil's horizontal velocity on the very next physics step.
            movement.ControlLocked = true;
            holdingLock = true;
            lockTimeRemaining = recoilControlLockTime;
        }

        Fired?.Invoke(origin, movement.FacingDirection, lastHits);
    }

    private void ReleaseLock()
    {
        if (holdingLock)
        {
            holdingLock = false;
            movement.ControlLocked = false;
        }
    }

    // Draws the burst range and cone edges in the Scene view when the player is selected.
    private void OnDrawGizmosSelected()
    {
        PlayerMovement pm = movement != null ? movement : GetComponent<PlayerMovement>();
        int dir = (pm != null && Application.isPlaying) ? pm.FacingDirection : 1;

        Vector3 origin = transform.position;
        Vector3 facing = new Vector3(dir, 0f, 0f);
        float halfAngle = coneAngle * 0.5f;

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
        Gizmos.DrawWireSphere(origin, range);
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, 0f, halfAngle) * facing * range);
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, 0f, -halfAngle) * facing * range);
    }
}
