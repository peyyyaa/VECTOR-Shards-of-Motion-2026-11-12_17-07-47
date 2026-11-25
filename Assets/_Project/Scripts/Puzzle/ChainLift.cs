using UnityEngine;

/// <summary>
/// The finale's cart lift: platforms hung on one endless chain loop that you can see. The chain runs UP the shaft, over the
/// top sprocket, DOWN the back (behind the striker; platforms there aren't solid), and back under the floor. Nothing ever
/// disappears and nothing goes back and forth: the chain only ever moves forward.
/// It moves only when it has a reason:
///   1. The cart comes to rest on the platform at the floor: that platform carries it straight up to the chosen stop
///      (no stop in between): the LOW stop (the rail) or, with the Road lever on, the HIGH stop (the sky rail).
///   2. At the stop it waits while the cart sits there (players load ore, release the hammer, open the points...).
///      At the high stop it gives the cart a small push west every so often (the tipping arm), in case the way is open.
///   3. Once the cart has left, the chain moves on a little so the next platform is waiting at the floor.
/// Platforms are evenly spaced along the loop, so one is always at the floor when the chain is parked.
/// Setup: kinematic platform bodies; the loop's corners (room-local deck-top heights); the cart; a Road lever (optional).
/// </summary>
public class ChainLift : MonoBehaviour
{
    [SerializeField] private Rigidbody2D[] platforms = new Rigidbody2D[0];
    [SerializeField] private HeartCart cart;
    [Tooltip("On = carry the cart to the high stop (the sky rail); off = the low stop (the rail).")]
    [SerializeField] private SignalSource highRoad;
    [Header("Loop (room-local; heights are platform deck tops)")]
    [SerializeField] private float upX = 1f;
    [SerializeField] private float downX = 2.9f;
    [SerializeField] private float bottomY = -6f;
    [SerializeField] private float topY = 2f;
    [SerializeField] private float floorY = -4.5f;
    [SerializeField] private float lowStopY = -1.5f;
    [SerializeField] private float highStopY = 1f;
    [SerializeField, Min(0.05f)] private float deckThickness = 0.3f;
    [Header("Motion")]
    [SerializeField, Min(0.1f)] private float speed = 4.5f;
    [Tooltip("Push given to a cart waiting at the high stop (N·s, west), every Kick Interval seconds.")]
    [SerializeField, Min(0f)] private float kick = 3f;
    [SerializeField, Min(0.1f)] private float kickInterval = 1.2f;
    [SerializeField, Min(0.01f)] private float restSpeed = 0.6f;
    [SerializeField] private LineRenderer chainLine;

    private enum State { Parked, Carrying, AtStop, Reparking }
    private State state;
    private float offset, target, stopY, kickTimer;
    private Rigidbody2D cartBody;
    private Collider2D[][] colliders;
    private Transform origin; // positions are room-local: relative to the room

    /// <summary>How far the chain has run in total (m, only ever grows). Read by visuals (ChainGears turns the sprockets).</summary>
    public float Travelled => offset;

    private float Up => topY - bottomY;
    private float Across => downX - upX;
    private float Length => 2f * (Up + Across);
    private float Spacing => Length / Mathf.Max(1, platforms.Length);
    private float FloorS => floorY - bottomY;

    private void Awake()
    {
        origin = transform.parent != null ? transform.parent : transform;
        if (cart != null) cartBody = cart.GetComponent<Rigidbody2D>();
        colliders = new Collider2D[platforms.Length][];
        for (int i = 0; i < platforms.Length; i++) colliders[i] = platforms[i].GetComponents<Collider2D>();
        offset = 0f;
        Place(true);
        DrawChain();
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case State.Parked:
                if (CartRestingAt(floorY))
                {
                    stopY = highRoad != null && highRoad.IsActive ? highStopY : lowStopY;
                    target = offset + (stopY - floorY);
                    state = State.Carrying;
                }
                break;
            case State.Carrying:
                if (Advance()) { state = State.AtStop; kickTimer = 0f; }
                break;
            case State.AtStop:
                if (CartRestingAt(stopY) || CartNear(stopY))
                {
                    if (stopY > lowStopY + 0.1f && kick > 0f && cartBody != null && (kickTimer += Time.fixedDeltaTime) >= kickInterval)
                    {
                        kickTimer = 0f;
                        cartBody.AddForce(new Vector2(-kick, 0f), ForceMode2D.Impulse);
                    }
                    break;
                }
                target = NextParked(offset);
                state = State.Reparking;
                break;
            case State.Reparking:
                if (Advance()) state = State.Parked;
                break;
        }
        Place(false);
    }

    private bool Advance()
    {
        offset = Mathf.MoveTowards(offset, target, speed * Time.fixedDeltaTime);
        return Mathf.Approximately(offset, target);
    }

    // The next offset at which a platform sits exactly at the floor stop.
    private float NextParked(float current)
    {
        float k = Mathf.Ceil((current - 0.001f) / Spacing);
        return k * Spacing;
    }

    private bool CartRestingAt(float deckY) =>
        CartNear(deckY) && cartBody.linearVelocity.magnitude < restSpeed;

    private bool CartNear(float deckY)
    {
        if (cartBody == null || !cart.gameObject.activeInHierarchy) return false;
        Vector2 c = origin.InverseTransformPoint(cartBody.position);
        return Mathf.Abs(c.x - upX) < 0.9f && c.y > deckY && c.y < deckY + 1f;
    }

    private void Place(bool teleport)
    {
        for (int i = 0; i < platforms.Length; i++)
        {
            float s = Mathf.Repeat(FloorS + offset + i * Spacing, Length);
            Vector2 deck = PointAt(s, out bool solid);
            Vector2 world = origin.TransformPoint(deck - new Vector2(0f, deckThickness * 0.5f));
            if (teleport) platforms[i].position = world; else platforms[i].MovePosition(world);
            foreach (Collider2D c in colliders[i]) if (c != null && c.enabled != solid) c.enabled = solid;
        }
    }

    // Room-local deck-top point at distance s along the loop (0 = the bottom of the up side).
    private Vector2 PointAt(float s, out bool solid)
    {
        solid = false;
        if (s < Up) { solid = true; return new Vector2(upX, bottomY + s); } // the up side carries: always solid
        s -= Up;
        if (s < Across) return new Vector2(upX + s, topY);
        s -= Across;
        if (s < Up) return new Vector2(downX, topY - s);
        s -= Up;
        return new Vector2(downX - s, bottomY);
    }

    private void DrawChain()
    {
        if (chainLine == null) return;
        chainLine.useWorldSpace = false;
        chainLine.positionCount = 5;
        chainLine.SetPositions(new Vector3[]
        {
            new Vector3(upX, bottomY), new Vector3(upX, topY), new Vector3(downX, topY), new Vector3(downX, bottomY), new Vector3(upX, bottomY),
        });
    }

#if UNITY_EDITOR
    public void EditorSetup(Rigidbody2D[] chain, HeartCart heartCart, SignalSource roadLever, float up, float down, float bottom, float top,
                            float floor, float low, float high, LineRenderer line)
    {
        platforms = chain; cart = heartCart; highRoad = roadLever; upX = up; downX = down; bottomY = bottom; topY = top;
        floorY = floor; lowStopY = low; highStopY = high; chainLine = line;
    }

    /// <summary>Room-local position of platform i when the chain is at rest (for the builder).</summary>
    public static Vector2 ParkedDeck(int i, int count, float up, float down, float bottom, float top, float floor)
    {
        float length = 2f * ((top - bottom) + (down - up)), s = Mathf.Repeat((floor - bottom) + i * length / count, length);
        float u = top - bottom, a = down - up;
        if (s < u) return new Vector2(up, bottom + s);
        s -= u;
        if (s < a) return new Vector2(up + s, top);
        s -= a;
        if (s < u) return new Vector2(down, top - s);
        s -= u;
        return new Vector2(down - s, bottom);
    }
#endif
}
