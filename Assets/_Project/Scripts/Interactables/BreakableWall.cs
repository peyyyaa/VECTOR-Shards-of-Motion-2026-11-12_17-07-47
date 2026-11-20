using UnityEngine;

/// <summary>
/// A cracked rubble wall that breaks when a crate hits it with enough MOMENTUM (p = m * v, in kg·m/s).
/// Players can't break it by running into it: only crates (Dynamic bodies that aren't players).
///
/// Physics: the crate's momentum at impact = its mass x its speed toward the wall just before contact
/// (Collision2D.relativeVelocity, taken along the contact normal, so a glancing hit counts less).
///   A Kinetic Burst gives ANY crate the same momentum: impulse J = change in momentum, so 15 N·s -> 15 kg·m/s.
///   Sliding friction then removes mu*m*g of momentum every second, so heavy crates lose it faster.
///   A water current keeps adding momentum (impulse = F x time), so a crate riding it arrives with more.
///
/// Its label (fades in when a player is near, or after a hit) shows what it needs, and the last hit, e.g.
/// "hit 9.8 / 12 kg·m/s", so players can see how close they came. It's rebuilt when the room resets.
/// Setup: BoxCollider2D (solid) + SpriteRenderer; children with SpriteRenderers (rubble art) hide with it.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class BreakableWall : FloatingLabel, IRoomResettable
{
    [Header("Breaking")]
    [Tooltip("Momentum a crate must hit with to break the wall, in kg·m/s.")]
    [SerializeField, Min(0.1f)] private float requiredMomentum = 12f;
    [SerializeField] private FxLibrary fx;

    private BoxCollider2D wallCollider;
    private SpriteRenderer[] renderers;
    private bool[] startedVisible; // the grey graybox shape stays hidden after a reset
    private bool broken;
    private float lastHit = -1f;
    private float busyUntil = float.NegativeInfinity;

    public Vector2 HomePoint => transform.position;
    public bool IsBroken => broken;
    protected override bool IsBusy => Time.time < busyUntil;

    private void Awake()
    {
        wallCollider = GetComponent<BoxCollider2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        startedVisible = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) startedVisible[i] = renderers[i].enabled;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D rb = collision.rigidbody;
        if (broken || rb == null || rb.bodyType != RigidbodyType2D.Dynamic || rb.GetComponent<PlayerMovement>() != null) return;

        Vector2 normal = collision.contactCount > 0 ? collision.GetContact(0).normal : Vector2.right;
        float speed = Mathf.Abs(Vector2.Dot(collision.relativeVelocity, normal)); // speed straight into the wall
        float momentum = rb.mass * speed;
        if (momentum < 0.5f) return; // just resting against it
        lastHit = momentum;
        busyUntil = Time.time + 3f;
        if (momentum >= requiredMomentum) Break(collision.GetContact(0).point);
    }

    private void Break(Vector2 at)
    {
        broken = true;
        wallCollider.enabled = false;
        foreach (SpriteRenderer sr in renderers) if (sr != null) sr.enabled = false;
        if (Label != null) Label.gameObject.SetActive(false);

        Bounds b = wallCollider.bounds;
        Fx.Flash(at, new Color(1f, 0.85f, 0.6f), 2.5f, 1.5f, 0.25f);
        if (fx == null) return;
        for (int i = 0; i < 18; i++) // tumbling rubble and dust
        {
            Vector3 p = new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), 0f);
            Fx.Sprite(fx.spark, p, new Color(0.55f, 0.5f, 0.45f), Random.Range(0.5f, 0.9f), 4)
              .Move(new Vector2(Random.Range(-1f, 3f), Random.Range(0f, 4f)), 14f, 0.5f);
        }
        if (fx.dust.Length > 0)
            for (int i = 0; i < 6; i++)
                Fx.Frames(fx.dust, new Vector3(b.center.x, b.min.y + 0.2f + i * 0.4f, 0f), Color.white, Random.Range(0.4f, 0.6f), 3)
                  .Move(new Vector2(Random.Range(-1f, 1f), Random.Range(0.2f, 0.8f)), 0f, 2f).Grow(1f, 1.6f);
    }

    public void ResetToStart()
    {
        broken = false;
        lastHit = -1f;
        if (wallCollider != null) wallCollider.enabled = true;
        if (renderers != null) for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].enabled = startedVisible[i];
        if (Label != null) Label.gameObject.SetActive(true);
    }

    protected override void Refresh()
    {
        if (broken) return;
        SetText(lastHit < 0f ? $"needs {requiredMomentum:0} kg·m/s" : $"hit {lastHit:0.0} / {requiredMomentum:0} kg·m/s");
        SetColor(lastHit >= 0f ? new Color(1f, 0.8f, 0.6f) : textColor);
    }

#if UNITY_EDITOR
    public void EditorSetup(float momentum, FxLibrary library) { requiredMomentum = momentum; fx = library; }
#endif
}
