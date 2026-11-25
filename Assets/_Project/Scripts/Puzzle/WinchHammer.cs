using UnityEngine;

/// <summary>
/// The Winch Hammer (finale, part 2): a heavy hammer on a chain beside the top of the cart lift, wound up by P2 and let go
/// by P1 to knock the cart off the rail.
/// Winding: its rope runs through a PULLEY BLOCK (Advantage, e.g. 6:1) to a CordHandle. Every metre of cord P2 pulls out
/// raises the hammer 1/Advantage m, and a RATCHET keeps it there (letting the cord go doesn't lower it). The pulley is why
/// it works: the hammer weighs m·g (4 kg: 118 N), far more than the tether's 24 N, but through 6:1 the cord only needs
/// 118 ÷ 6 = 19.6 N (that is the handle's cord tension). Work in = work out: 19.6 N × 6 m of cord = 118 N × 1 m of height.
/// Release (HammerCatch): it swings down as a pendulum (θ'' = -(g/L)·sin θ, g = 29.43), with no losses on the way down,
/// and reaches the bottom at u = √(2·g·h).
/// The strike (a 1D collision along the rail, coefficient of restitution e), hammer m₁ at u₁, cart m₂ at u₂:
///   v₁ = (m₁u₁ + m₂u₂ - m₂·e·(u₁ - u₂)) / (m₁ + m₂)      (hammer after)
///   v₂ = (m₁u₁ + m₂u₂ + m₁·e·(u₁ - u₂)) / (m₁ + m₂)      (cart after)
///   Momentum is conserved exactly: m₁v₁ + m₂v₂ = m₁u₁ + m₂u₂.
///   Kinetic energy lost = ½·μ·(1 - e²)·(u₁ - u₂)², μ = m₁m₂ / (m₁ + m₂) (none if e = 1).
///   The cart's mass includes its ore (HeartCart sets the body's mass). With a 4 kg hammer and e = 0.9:
///   empty 2 kg cart: it takes 63% of the hammer's momentum and the hammer swings on; 3 kg: 81%; 4 kg (two ore, the
///   same mass as the hammer): 95%, and the hammer nearly stops (like Newton's cradle).
/// The hit happens where the head's face actually meets the cart, anywhere along the swing (usually just past the bottom,
/// wherever the cart sits on the lift platform), with the head's horizontal speed at that moment (u₁ = L·ω·cos θ; the cart
/// moves along the rail, so only the horizontal part counts). Near the bottom cos θ ≈ 1, so u₁ ≈ √(2·g·h).
/// After the hit (or, with no cart there, once the free swing turns back) the pivot's brake settles the hammer, then it
/// hangs still, ready to be wound again. Purely kinematic (the swing is computed), so the hit is reliable.
/// Setup: Head = the hammer's transform (moved along the arc); Pivot (room-local); the cord handle; the cart.
/// </summary>
public class WinchHammer : MonoBehaviour
{
    [SerializeField] private Transform head;
    [SerializeField] private Vector2 pivot;
    [SerializeField, Min(0.5f)] private float length = 2.5f;
    [Tooltip("+1: the hammer is wound back to the east (it swings west); -1: the other way.")]
    [SerializeField] private float side = 1f;
    [SerializeField, Min(0.1f)] private float mass = 4f;
    [SerializeField, Min(1f)] private float advantage = 6f;
    [SerializeField, Min(0.1f)] private float maxHeight = 1.5f;
    [Tooltip("Coefficient of restitution of the hammer-cart hit: 1 = perfectly elastic, 0 = they move on together.")]
    [SerializeField, Range(0f, 1f)] private float restitution = 0.9f;
    [Tooltip("The pivot's brake after the hit (1/s): how fast the leftover swing dies away. Not active on the way down.")]
    [SerializeField, Min(0f)] private float brake = 2.5f;
    [SerializeField] private CordHandle cord;
    [SerializeField] private HeartCart cart;
    [Tooltip("The chain from the pivot to the head (drawn each frame).")]
    [SerializeField] private LineRenderer chain;
    [Tooltip("Half the head's width (m), from its face to its centre. 0 = measured from the head's sprite.")]
    [SerializeField, Min(0f)] private float headHalfWidth = 0f;

    private const float G = 29.43f;
    private Transform origin;
    private Rigidbody2D cartBody;
    private Collider2D cartCollider;
    private float height;         // wound height above the bottom of the swing (m)
    private float lastPulled;
    private bool swinging;
    private bool passedBottom;    // this swing has gone through the bottom once
    private bool hit;             // this swing has hit the cart
    private bool braking;         // the pivot's brake is on (after the hit, or once a free swing turns back)
    private float angle, angularVelocity;

    public float Height => height;
    public bool Swinging => swinging;
    public float Mass => mass;
    public float MaxHeight => maxHeight;
    public float Advantage => advantage;
    public float Length => length;
    public float Side => side;
    /// <summary>The pivot, room-local.</summary>
    public Vector2 Pivot => pivot;
    /// <summary>The chain's angle from straight down (radians, + toward the wound side).</summary>
    public float Angle => angle;

    private void Awake()
    {
        origin = transform.parent != null ? transform.parent : transform;
        if (cart != null) { cartBody = cart.GetComponent<Rigidbody2D>(); cartCollider = cart.GetComponent<Collider2D>(); }
        if (cord != null) cord.Tension = mass * G / advantage; // the cord carries the hammer's weight ÷ the pulley's advantage
        if (headHalfWidth <= 0f && head != null && head.TryGetComponent(out SpriteRenderer sr) && sr.sprite != null)
            headHalfWidth = sr.sprite.bounds.extents.x * Mathf.Abs(head.lossyScale.x);
        if (headHalfWidth <= 0f) headHalfWidth = 0.4f;
        Pose(0f);
    }

    /// <summary>P1 lets it go (HammerCatch). Returns false if it isn't wound.</summary>
    public bool Release()
    {
        if (swinging || height < 0.05f) return false;
        swinging = true;
        passedBottom = hit = braking = false;
        angularVelocity = 0f;
        return true;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (!swinging)
        {
            // Ratchet: only the cord going OUT winds the hammer up.
            if (cord != null)
            {
                float pulled = cord.Pulled;
                if (pulled > lastPulled) height = Mathf.Min(maxHeight, height + (pulled - lastPulled) / advantage);
                lastPulled = pulled;
            }
            angle = AngleFor(height);
            Pose(angle);
            return;
        }

        // The swing: a pendulum. No losses on the way down, so it reaches the bottom at √(2·g·h).
        float before = angle;
        angularVelocity += -(G / length) * Mathf.Sin(angle) * dt;
        if (braking) angularVelocity *= Mathf.Exp(-brake * dt);
        angle += angularVelocity * dt;
        if (before > 0f && angle <= 0f) passedBottom = true;

        if (!hit) TryStrike();
        // The brake: on after the hit, or once a free swing (nothing hit) has gone through the bottom and turned back.
        if (!braking && (hit || (passedBottom && angularVelocity > 0f))) braking = true;
        if (braking && Mathf.Abs(angle) < 0.02f && Mathf.Abs(angularVelocity) < 0.2f) Settle();
        Pose(angle);
    }

    // Where the head's face meets the cart: a 1D collision along the rail (see the summary).
    private void TryStrike()
    {
        if (cartBody == null || !cart.gameObject.activeInHierarchy) return;
        Vector2 headAt = pivot + new Vector2(side * Mathf.Sin(angle), -Mathf.Cos(angle)) * length; // room-local
        Vector2 c = origin.InverseTransformPoint(cartBody.position);
        if (Mathf.Abs(c.y - headAt.y) > 0.7f) return;                       // not at the head's height (e.g. still on its way up)
        if ((headAt.x - c.x) * side <= 0f) return;                           // the cart is behind the head, not in its path
        float cartHalf = cartCollider != null ? cartCollider.bounds.extents.x : 0.6f;
        float gap = (headAt.x - side * headHalfWidth - (c.x + side * cartHalf)) * side; // face to face; > 0: not touching yet
        if (gap > 0f) return;

        Vector2 along = new Vector2(-side, 0f);                             // the direction the hammer swings toward the cart
        float cos = Mathf.Max(0.2f, Mathf.Cos(angle));
        float u1 = -angularVelocity * length * cos;                         // the head's horizontal speed toward the cart
        float u2 = Vector2.Dot(cartBody.linearVelocity, along);             // the cart's speed along it (usually 0: it's waiting)
        if (u1 - u2 < 0.1f) return;                                         // not catching up: no hit
        float m1 = mass, m2 = cartBody.mass, total = m1 + m2;
        float v1 = (m1 * u1 + m2 * u2 - m2 * restitution * (u1 - u2)) / total;
        float v2 = (m1 * u1 + m2 * u2 + m1 * restitution * (u1 - u2)) / total;
        cartBody.AddForce(along * (m2 * (v2 - u2)), ForceMode2D.Impulse); // the impulse on the cart: J = m₂·Δv₂
        angularVelocity = -v1 / (length * cos);                          // the hammer carries on (or nearly stops) at v₁
        hit = true;
        Fx.Flash(head.position, new Color(1f, 0.85f, 0.55f), 2.5f, 1.6f, 0.2f);
        FxCameraKick.Kick(new Vector2(-side * 0.08f, 0f), 0.12f);
    }

    // The swing has died away: hang still, ready to be wound again (the ratchet counts from here).
    private void Settle()
    {
        swinging = false;
        angle = 0f;
        angularVelocity = 0f;
        height = 0f;
        lastPulled = cord != null ? cord.Pulled : 0f;
    }

    private float AngleFor(float h) => Mathf.Acos(Mathf.Clamp(1f - h / length, -1f, 1f));

    private void Pose(float a)
    {
        if (head == null) return;
        Vector2 p = pivot + new Vector2(side * Mathf.Sin(a), -Mathf.Cos(a)) * length;
        head.position = origin.TransformPoint(p);
        head.rotation = Quaternion.Euler(0f, 0f, side * a * Mathf.Rad2Deg);
        if (chain != null)
        {
            chain.useWorldSpace = true;
            chain.positionCount = 2;
            chain.SetPosition(0, origin.TransformPoint(pivot));
            chain.SetPosition(1, head.position);
        }
    }

#if UNITY_EDITOR
    public void EditorSetup(Transform hammerHead, Vector2 pivotPoint, float chainLength, float swingSide, float hammerMass, float pulley,
                            CordHandle winchCord, HeartCart heartCart, LineRenderer chainLine)
    {
        head = hammerHead; pivot = pivotPoint; length = chainLength; side = swingSide; mass = hammerMass; advantage = pulley;
        cord = winchCord; cart = heartCart; chain = chainLine;
    }
#endif
}
