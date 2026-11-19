using UnityEngine;

/// <summary>
/// A Motion Shard: an optional collectible. Either player touches it to collect it (shared team count,
/// see ShardTracker). It bobs gently; when collected it pops (grows and fades) and disappears.
/// Collected shards stay collected for the play session — room resets don't bring them back.
///
/// Setup: a trigger Collider2D (the MotionShard prefab is ready to use). Place it as a child of its room
/// (inside the room prefab) so the counter can show "This room: x / y".
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MotionShard : MonoBehaviour
{
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 2.2f;
    [SerializeField] private float popTime = 0.25f;

    private Vector3 basePosition;
    private float bobOffset;
    private bool collecting;
    private float popTimer;
    private SpriteRenderer sr;

    public Room Room { get; private set; }

    /// <summary>Unique per shard in the scene (room + its name), used to remember it was collected.</summary>
    public string Id { get; private set; }

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        Room = GetComponentInParent<Room>();
        Id = (Room != null ? Room.name + "/" : "") + name + "@" + transform.position.ToString("F2");
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnEnable() => ShardTracker.Register(this);
    private void OnDisable() => ShardTracker.Unregister(this);

    private void Start()
    {
        basePosition = transform.localPosition;
        bobOffset = transform.position.x * 1.3f; // shards don't all bob in sync
        if (ShardCounter.Instance == null) ShardCounter.Create(); // the first shard shows the counter
        if (ShardTracker.IsCollected(Id)) HideCollected();
    }

    private void Update()
    {
        if (collecting)
        {
            popTimer += Time.deltaTime;
            float t = Mathf.Clamp01(popTimer / popTime);
            transform.localScale = Vector3.one * (1f + t);
            if (sr != null) { Color c = sr.color; c.a = 1f - t; sr.color = c; }
            if (t >= 1f) HideCollected();
            return;
        }
        transform.localPosition = basePosition + new Vector3(0f, Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight, 0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collecting || other.attachedRigidbody == null || other.attachedRigidbody.GetComponent<PlayerMovement>() == null) return;
        collecting = true;
        ShardTracker.Collect(this);
    }

    // Collected: switch off visuals and collider but stay registered, so totals still count this shard.
    private void HideCollected()
    {
        collecting = false;
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (Behaviour b in GetComponentsInChildren<Behaviour>()) if (b != this) b.enabled = false;
        GetComponent<Collider2D>().enabled = false;
    }
}
