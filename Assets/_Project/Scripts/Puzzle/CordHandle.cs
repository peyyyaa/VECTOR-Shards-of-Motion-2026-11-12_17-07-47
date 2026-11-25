using UnityEngine;

/// <summary>
/// A cord handle in a floor slot (the finale's winch and points cords): a small block P2 pulls with the Wind Tether.
/// It slides only along its slot (Rigidbody2D: y and rotation frozen), from Home up to Travel metres in Pull Direction.
/// The cord pulls it back toward home with Cord Tension (N) the whole time, so:
///   - the tether (24 N) moves it only while it beats the tension: net force = 24 - tension (Newton's 2nd law);
///   - let go, and the tension pulls it home again.
/// Pulled: how far it is out (0..Travel, m). As a SignalSource it is ON while pulled out at least Hold Fraction of the way
/// (the points cord: held = points open). A WinchHammer reads Pulled to wind its hammer up.
/// Setup: on a copy of the 1 kg block (PhysicsObject layer, so the tether can grab it).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CordHandle : SignalSource
{
    [SerializeField] private Vector2 pullDirection = Vector2.right;
    [SerializeField, Min(0.1f)] private float travel = 2.4f;
    [Tooltip("The cord's pull back toward home, in newtons.")]
    [SerializeField, Min(0f)] private float cordTension = 15f;
    [SerializeField, Range(0.1f, 1f)] private float holdFraction = 0.8f;
    [SerializeField] private LineRenderer cord;
    [Tooltip("Where the cord goes from the handle (e.g. up to a pulley), room-local; drawn as a line.")]
    [SerializeField] private Vector2[] cordPath = new Vector2[0];

    private Rigidbody2D body;
    private Vector2 home;

    public float Pulled { get; private set; }
    public float Travel => travel;
    public float Tension { get => cordTension; set => cordTension = value; }
    public override bool IsActive => Pulled >= travel * holdFraction;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        home = body.position;
        pullDirection = pullDirection.normalized;
    }

    private void FixedUpdate()
    {
        float along = Vector2.Dot(body.position - home, pullDirection);
        if (along < 0f || along > travel)
        {
            // End stops of the slot.
            body.position = home + pullDirection * Mathf.Clamp(along, 0f, travel);
            float v = Vector2.Dot(body.linearVelocity, pullDirection);
            if ((along < 0f && v < 0f) || (along > travel && v > 0f)) body.linearVelocity -= pullDirection * v;
            along = Mathf.Clamp(along, 0f, travel);
        }
        Pulled = along;
        if (along > 0.01f) body.AddForce(-pullDirection * cordTension, ForceMode2D.Force);
        DrawCord();
    }

    private void DrawCord()
    {
        if (cord == null || cordPath.Length == 0) return;
        cord.useWorldSpace = true;
        cord.positionCount = cordPath.Length + 1;
        cord.SetPosition(0, body.position);
        Transform room = transform.parent;
        for (int i = 0; i < cordPath.Length; i++)
            cord.SetPosition(i + 1, room != null ? room.TransformPoint(cordPath[i]) : (Vector3)cordPath[i]);
    }

#if UNITY_EDITOR
    public void EditorSetup(Vector2 direction, float slotLength, float tension, float hold, LineRenderer line, Vector2[] path)
    {
        pullDirection = direction; travel = slotLength; cordTension = tension; holdFraction = hold; cord = line; cordPath = path;
    }
#endif
}
