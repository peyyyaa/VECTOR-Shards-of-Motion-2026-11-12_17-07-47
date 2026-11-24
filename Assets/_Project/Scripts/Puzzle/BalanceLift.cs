using UnityEngine;

/// <summary>
/// A balance lift (an Atwood machine): two pans hung from one pulley. The heavier side goes down and the lighter side goes
/// up; equal weights stay put (equilibrium). Each pan weighs its load with a PressurePlate built into it (stack weighing,
/// riders counted while the pan moves, since "resting" is measured relative to the pan).
///
/// Motion, Newton's second law for the whole system (pans and loads move together, the rope only turns the force around):
///   F_net = W_B - W_A (pan A rises when B is heavier) minus the pulley's friction,
///   a = F_net / (m_A + m_B + both pans' mass),
/// so a small difference moves it gently and a big one quickly. Pulley friction (a few newtons) holds it still when the
/// weights are equal or nearly so, and stops it after a push. Speed is capped, and it eases in to the ends of its travel.
///
/// Scale mode (a beam balance, e.g. Room 6): instead of accelerating, the pans settle at a tilt set by the weight
/// difference (a real balance has a restoring torque): fully tipped at Full Tip Difference or more, LEVEL when the weights
/// match. Players see the pans swing level as they reach the right load.
///
/// Brake: while the Release signal (e.g. a lever) is off, the pulley is braked and nothing moves, whatever the weights.
/// Ratchet: when both pans are empty and the brake is off, it slowly winds back to the start, so a wrong try never needs
/// the reset shrine.
/// Signal: it is a SignalSource that is ON while the lift is BALANCED: both pans carry weight, the weights match within the
/// pulley's friction, and that has held for Balance Hold seconds. A relay (with latch) can start the next mechanism.
/// Safety: if a player or crate is under a pan that's coming down, the lift stops (both pans: they share one rope) instead
/// of crushing it, and carries on once the way is clear.
///
/// Physics simplification: the rope and wheel are massless and the pans are kinematic bodies driven by this script (so they
/// carry riders and push crates like any moving platform). Readouts show weight (m*g), not the changing normal force.
///
/// Setup: two pans with a Kinematic Rigidbody2D + BoxCollider2D (Ground layer), each with a child PressurePlate (trigger on
/// its top, Count Players on). Place both pans at their start height; Travel is how far each pan moves from bottom to top;
/// Start sets where in that range the start height is (0 = pan A at its bottom).
/// </summary>
public class BalanceLift : SignalSource, IRoomResettable
{
    public enum Kind { Lift, Scale }

    [Tooltip("Lift: an Atwood machine you can ride (equal weights stay where they are). Scale: a beam balance (equal weights = level).")]
    [SerializeField] private Kind kind = Kind.Lift;

    [Header("Pans")]
    [SerializeField] private Rigidbody2D panA;
    [SerializeField] private Rigidbody2D panB;
    [SerializeField] private PressurePlate scaleA;
    [SerializeField] private PressurePlate scaleB;

    [Header("Travel")]
    [Tooltip("How far each pan moves between its bottom and top (m).")]
    [SerializeField, Min(0.1f)] private float travel = 4f;
    [Tooltip("Where pan A starts within its travel: 0 = bottom, 1 = top (pan B is the opposite).")]
    [SerializeField, Range(0f, 1f)] private float start = 0.5f;

    [Header("Physics")]
    [Tooltip("Mass of each empty pan (kg). Adds inertia; the two pans' weights cancel out.")]
    [SerializeField, Min(0.1f)] private float panMass = 1f;
    [Tooltip("Pulley friction (N): smaller weight differences don't move the lift.")]
    [SerializeField, Min(0f)] private float pulleyFriction = 4f;
    [SerializeField, Min(0.1f)] private float maxSpeed = 3f;
    [Tooltip("Lift mode: how hard it slows down approaching the ends of its travel (m/s^2), so it eases in instead of stopping dead.")]
    [SerializeField, Min(0.5f)] private float endEasing = 6f;

    [Header("Scale mode")]
    [Tooltip("Weight difference (N) that tips the scale all the way.")]
    [SerializeField, Min(1f)] private float fullTipDifference = 60f; // the 2 kg crate alone (29.4 N short) tips it halfway
    [Tooltip("How fast the pans swing to their new tilt (m/s).")]
    [SerializeField, Min(0.1f)] private float scaleSpeed = 1.5f;

    [Header("Balanced signal")]
    [Tooltip("Seconds the pans must stay balanced (equal weights, both loaded) before the signal turns on.")]
    [SerializeField, Min(0f)] private float balanceHold = 1f;

    [Header("Brake and ratchet")]
    [Tooltip("The brake is released while this signal is on (e.g. a lever on pan B). Empty = never braked.")]
    [SerializeField] private SignalSource release;
    [Tooltip("Speed (m/s) at which an empty lift winds back to its start.")]
    [SerializeField, Min(0f)] private float ratchetSpeed = 0.5f;

    private Vector2 bottomA, bottomB;
    private float height;   // pan A's height above its bottom (pan B's is travel - height)
    private float velocity; // of pan A, up = positive
    private bool ready;
    private float balancedFor;

    /// <summary>On while balanced: both pans loaded, weights equal within the pulley's friction, for Balance Hold seconds.</summary>
    public override bool IsActive => balancedFor >= balanceHold && balanceHold >= 0f && ready;

    public Vector2 HomePoint => transform.position;

    /// <summary>True while the brake holds the lift.</summary>
    public bool Braked => release != null && !release.IsActive;

    /// <summary>Pan A's height within its travel, 0..Travel (for visuals: chains, wheel).</summary>
    public float Height => height;
    public float Travel => travel;
    public Rigidbody2D PanA => panA;
    public Rigidbody2D PanB => panB;

    private void Awake()
    {
        if (panA == null || panB == null) { Debug.LogError("BalanceLift: assign both pans.", this); return; }
        height = start * travel;
        bottomA = panA.position - Vector2.up * height;
        bottomB = panB.position - Vector2.up * (travel - height);
        ready = true;
    }

    private void FixedUpdate()
    {
        if (!ready) return;
        float dt = Time.fixedDeltaTime, g = Mathf.Abs(Physics2D.gravity.y);
        float wA = scaleA != null ? scaleA.CurrentWeight : 0f, wB = scaleB != null ? scaleB.CurrentWeight : 0f;
        bool balanced = wA > 0.01f && wB > 0.01f && Mathf.Abs(wA - wB) <= pulleyFriction && Mathf.Abs(velocity) < 0.05f;
        balancedFor = balanced ? balancedFor + dt : 0f;

        if (Braked) velocity = 0f;
        else if (kind == Kind.Scale)
        {
            // Beam balance: the heavier side sits lower, in proportion to the difference; equal (within friction) = level.
            float diff = wA - wB;
            float tip = Mathf.Abs(diff) <= pulleyFriction ? 0f : Mathf.Clamp(diff / fullTipDifference, -1f, 1f);
            float target = travel * 0.5f * (1f - tip); // pan A's height: lower when A is heavier
            velocity = (Mathf.MoveTowards(height, target, scaleSpeed * dt) - height) / dt;
        }
        else if (wA <= 0.01f && wB <= 0.01f)
        {
            // Both pans empty: the ratchet winds it back to the start.
            float home = start * travel;
            velocity = Mathf.Abs(home - height) < 0.01f ? 0f : Mathf.Sign(home - height) * ratchetSpeed;
        }
        else
        {
            float net = wB - wA; // pushes pan A up
            if (Mathf.Abs(velocity) > 0.01f) net -= Mathf.Sign(velocity) * pulleyFriction;   // sliding friction
            else if (Mathf.Abs(net) <= pulleyFriction) net = 0f;                              // held by friction
            else net -= Mathf.Sign(net) * pulleyFriction;
            float mass = (wA + wB) / g + 2f * panMass;
            float before = velocity;
            velocity += net / mass * dt;
            if (before != 0f && Mathf.Sign(velocity) != Mathf.Sign(before) && Mathf.Abs(wB - wA) <= pulleyFriction) velocity = 0f; // friction stops, never reverses
            velocity = Mathf.Clamp(velocity, -maxSpeed, maxSpeed);
            // Ease into the ends: never faster than what can still stop in the distance left (v = sqrt(2 a d)).
            float left = velocity > 0f ? travel - height : height;
            float easeCap = Mathf.Sqrt(2f * endEasing * Mathf.Max(0f, left)) + 0.15f;
            velocity = Mathf.Clamp(velocity, -easeCap, easeCap);
        }

        // Something under a descending pan (pan A descends when velocity < 0, pan B when > 0): stop, don't crush.
        if (velocity < 0f && Blocked(panA, -velocity * dt)) velocity = 0f;
        else if (velocity > 0f && Blocked(panB, velocity * dt)) velocity = 0f;

        float next = height + velocity * dt;
        if (next <= 0f || next >= travel) { next = Mathf.Clamp(next, 0f, travel); velocity = 0f; }
        Drive(next, dt);
    }

    private readonly RaycastHit2D[] hits = new RaycastHit2D[8];

    // Casts only the pan's own box (not its scale trigger or lever) and counts only things whose top is below the pan's
    // bottom, so riders and crates resting on the pan never count as "in the way".
    private bool Blocked(Rigidbody2D pan, float distance)
    {
        if (!pan.TryGetComponent(out BoxCollider2D box)) return false;
        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(LayerMask.GetMask("Player", "PhysicsObject"));
        int n = box.Cast(Vector2.down, filter, hits, distance + 0.02f, true);
        float bottom = box.bounds.min.y;
        for (int i = 0; i < n; i++)
            if (hits[i].collider != null && hits[i].rigidbody != pan && hits[i].collider.bounds.max.y <= bottom + 0.05f) return true;
        return false;
    }

    // Kinematic pans are moved with a velocity (not teleported), so riders and crates on them move along.
    private void Drive(float next, float dt)
    {
        Vector2 targetA = bottomA + Vector2.up * next, targetB = bottomB + Vector2.up * (travel - next);
        panA.linearVelocity = (targetA - panA.position) / dt;
        panB.linearVelocity = (targetB - panB.position) / dt;
        height = next;
    }

    public void ResetToStart()
    {
        if (!ready) return;
        height = start * travel;
        velocity = 0f;
        balancedFor = 0f;
        panA.position = bottomA + Vector2.up * height;
        panB.position = bottomB + Vector2.up * (travel - height);
        panA.linearVelocity = panB.linearVelocity = Vector2.zero;
    }

#if UNITY_EDITOR
    public void EditorSetup(Rigidbody2D a, Rigidbody2D b, PressurePlate wA, PressurePlate wB, float travelMetres, float startFraction, SignalSource releaseSignal)
    {
        panA = a; panB = b; scaleA = wA; scaleB = wB; travel = travelMetres; start = startFraction; release = releaseSignal;
    }

    public void EditorSetKind(Kind k) => kind = k;

    private void OnDrawGizmos()
    {
        if (panA == null || panB == null) return;
        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.8f);
        foreach (Rigidbody2D p in new[] { panA, panB }) Gizmos.DrawLine(p.transform.position + Vector3.down * 3f, p.transform.position + Vector3.up * 3f);
    }
#endif
}
