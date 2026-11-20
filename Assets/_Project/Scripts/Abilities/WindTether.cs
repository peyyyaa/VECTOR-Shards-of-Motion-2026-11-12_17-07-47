using UnityEngine;

/// <summary>
/// Wind Tether (Player 2): hold the Ability button to latch onto one physics object in front
/// of you and pull it toward you with a steady force.
///
/// How it works:
/// - On press: picks the NEAREST valid target within Max Range, inside the front cone,
///   with a clear line of sight. Valid = a Dynamic Rigidbody2D on Target Layers that isn't a player.
/// - While held: applies a continuous force (ForceMode2D.Force) toward the player, every physics step.
/// - Stops when: the button is released, the target is farther than Max Range, or line of sight
///   is blocked. Pressing again picks a new target.
/// - The pull pauses (but stays attached) while the target is within Hold Distance, so the
///   object isn't dragged into the player.
/// - Never moves objects by setting their position: only forces.
///
/// Physics:
/// - Newton's 2nd law with friction: the object only starts moving if the pull beats friction.
///     F_net = F_pull − μ·m·g    a = F_net / m
///   2 kg block, μ = 1/3, g = 29.43:  friction ≈ 19.6 N;  24 N pull → F_net ≈ 4.4 N,  a ≈ 2.2 m/s².
/// - The force acts along the line from the object to the player, like a rope. If the player is
///   higher than the object, part of the pull is upward, which reduces the normal force and
///   therefore the friction — just like pulling a sled with an angled rope.
///
/// Simplification: a real rope would pull the player toward the object just as hard (Newton's 3rd law).
/// We assume the player is braced (feet gripping the ground), so no reaction force is applied to them.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
public class WindTether : MonoBehaviour
{
    // Triggered by this player's "Ability" button (read via PlayerInputReader), held down.

    [Header("Targeting")]
    [Tooltip("Maximum tether length, center to center, in units (meters).")]
    [SerializeField] private float maxRange = 4f;
    [Tooltip("Total width of the front cone used to pick a target, in degrees.")]
    [SerializeField, Range(1f, 180f)] private float targetConeAngle = 120f;
    [Tooltip("Layers that can be tethered (PhysicsObject). Players are always ignored.")]
    [SerializeField] private LayerMask targetLayers;
    [Tooltip("Layers that block the tether's line of sight (e.g., Ground, PhysicsObject, Interactable).")]
    [SerializeField] private LayerMask lineOfSightBlockers;

    [Header("Force")]
    [Tooltip("Continuous pull in newtons while the button is held.")]
    [SerializeField] private float pullForce = 24f;
    [Tooltip("Within this distance the pull pauses (tether stays attached). 0 = always pull.")]
    [SerializeField] private float holdDistance = 1f;

    [Header("Visual (optional)")]
    [Tooltip("A LineRenderer drawn from the player to the target while tethered.")]
    [SerializeField] private LineRenderer tetherLine;

    private Rigidbody2D body;
    private PlayerMovement movement;
    private PlayerInputReader input;

    private Rigidbody2D target;
    private bool acquireRequested;  // set in Update, handled in FixedUpdate

    private readonly Collider2D[] overlapResults = new Collider2D[16];
    private readonly RaycastHit2D[] lineResults = new RaycastHit2D[8];

    /// <summary>The object currently tethered, or null.</summary>
    public Rigidbody2D Target => target;

    public float MaxRange => maxRange;

    // Raised for TetherEffects (read-only): latched onto an object, let go, or pressed with nothing to grab.
    public event System.Action<Rigidbody2D> Attached;
    public event System.Action<Rigidbody2D> Released;
    public event System.Action Missed;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        input = GetComponent<PlayerInputReader>();
        SetLineVisible(false);
    }

    private void OnDisable()
    {
        Release();
    }

    private void Update()
    {
        if (input.AbilityPressedThisFrame && target == null)
        {
            acquireRequested = true;
        }

        if (!input.AbilityHeld)
        {
            acquireRequested = false;
            Release();
        }
    }

    private void FixedUpdate()
    {
        if (acquireRequested)
        {
            acquireRequested = false;
            target = FindTarget();
            if (target != null) Attached?.Invoke(target);
            else Missed?.Invoke();
        }

        if (target == null) return;

        if (!IsStillValid(target))
        {
            Release();
            return;
        }

        Vector2 toPlayer = body.position - target.worldCenterOfMass;
        float distance = toPlayer.magnitude;

        if (distance > holdDistance && distance > 0.0001f)
        {
            // Continuous force in newtons, applied every physics step while held.
            target.AddForce(toPlayer / distance * pullForce, ForceMode2D.Force);
        }
    }

    // Drawn in LateUpdate so the line follows the (interpolated) on-screen positions smoothly.
    private void LateUpdate()
    {
        if (target == null || tetherLine == null) return;

        tetherLine.SetPosition(0, transform.position);
        tetherLine.SetPosition(1, target.transform.position);
    }

    private Rigidbody2D FindTarget()
    {
        Vector2 origin = body.position;
        Vector2 facing = new Vector2(movement.FacingDirection, 0f);
        float halfAngle = targetConeAngle * 0.5f;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(targetLayers);
        filter.useTriggers = false;

        int count = Physics2D.OverlapCircle(origin, maxRange, filter, overlapResults);

        Rigidbody2D best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Rigidbody2D candidate = overlapResults[i].attachedRigidbody;
            if (!IsValidBody(candidate)) continue;

            Vector2 toCandidate = candidate.worldCenterOfMass - origin;
            float distance = toCandidate.magnitude;
            if (distance > maxRange || distance < 0.0001f) continue;
            if (Vector2.Angle(facing, toCandidate) > halfAngle) continue; // not in front
            if (!HasLineOfSight(candidate)) continue;

            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        if (best != null) SetLineVisible(true);
        return best;
    }

    private bool IsStillValid(Rigidbody2D rb)
    {
        if (!IsValidBody(rb)) return false;
        if (Vector2.Distance(body.position, rb.worldCenterOfMass) > maxRange) return false;
        return HasLineOfSight(rb);
    }

    // Dynamic, active, not this player, and not any player.
    private bool IsValidBody(Rigidbody2D rb)
    {
        return rb != null
            && rb != body
            && rb.gameObject.activeInHierarchy
            && rb.bodyType == RigidbodyType2D.Dynamic
            && rb.GetComponent<PlayerMovement>() == null;
    }

    // Clear if nothing on Line Of Sight Blockers lies between us and the target
    // (our own colliders and the target's colliders don't count).
    private bool HasLineOfSight(Rigidbody2D rb)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(lineOfSightBlockers);
        filter.useTriggers = false;

        int count = Physics2D.Linecast(body.position, rb.worldCenterOfMass, filter, lineResults);
        for (int i = 0; i < count; i++)
        {
            Rigidbody2D hitBody = lineResults[i].rigidbody;
            if (hitBody == body || hitBody == rb) continue;
            return false; // something else is in the way
        }
        return true;
    }

    private void Release()
    {
        Rigidbody2D was = target;
        target = null;
        if (was != null) Released?.Invoke(was);
        SetLineVisible(false);
    }

    private void SetLineVisible(bool visible)
    {
        if (tetherLine == null) return;
        tetherLine.positionCount = 2;
        tetherLine.enabled = visible;
    }

    // Scene view: tether range and front cone (cyan), and the current tether (white).
    private void OnDrawGizmosSelected()
    {
        PlayerMovement pm = movement != null ? movement : GetComponent<PlayerMovement>();
        int dir = (pm != null && Application.isPlaying) ? pm.FacingDirection : 1;

        Vector3 origin = transform.position;
        Vector3 facing = new Vector3(dir, 0f, 0f);
        float halfAngle = targetConeAngle * 0.5f;

        Gizmos.color = new Color(0f, 0.9f, 1f, 0.8f);
        Gizmos.DrawWireSphere(origin, maxRange);
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, 0f, halfAngle) * facing * maxRange);
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, 0f, -halfAngle) * facing * maxRange);

        if (target != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(origin, target.worldCenterOfMass);
        }
    }
}
