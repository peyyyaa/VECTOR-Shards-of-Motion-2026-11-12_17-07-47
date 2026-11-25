using UnityEngine;

/// <summary>
/// The Heart Cart (the finale's pushable object, instead of a crate): an old ore cart that rolls on the wet rails.
/// Loading glowing shard ore (OreChute) makes it heavier: Rigidbody2D mass = empty mass + ore x ore mass.
/// Why it matters (the finale's lesson):
///   - a Kinetic Burst gives ANY mass the same momentum (J = Δp: 15 kg·m/s), so a heavier cart just goes slower;
///   - rolling down a slope gives ANY mass the same speed (v = √(2·g·h)), so a heavier cart arrives with MORE momentum.
///   So the heavy, loaded cart rolled down the ramp is the one that breaks root walls and spins the engine.
/// Its wheels turn with its speed. Its body never rotates (no flipping over, no wedging on edges); only its Visual tilts
/// to follow the ground under it. Ore is not saved: a room reset empties it (with the body back home).
/// Setup: on a Block prefab copy (Dynamic Rigidbody2D on the PhysicsObject layer, reset handled like a crate);
/// Ore Visuals = sprites switched on one per ore; Wheels = transforms turned by the cart's speed.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class HeartCart : MonoBehaviour, IRoomResettable
{
    [SerializeField, Min(0.1f)] private float emptyMass = 2f;
    [SerializeField, Min(0.1f)] private float oreMass = 1f;
    [SerializeField, Min(0)] private int maxOre = 2;
    [SerializeField] private GameObject[] oreVisuals = new GameObject[0];
    [SerializeField] private Transform[] wheels = new Transform[0];
    [SerializeField, Min(0.01f)] private float wheelRadius = 0.2f;
    [Tooltip("The art (body, wheels, ore), tilted to match the slope under the cart.")]
    [SerializeField] private Transform visual;

    private Rigidbody2D body;
    private Vector2 home;
    private int ore;

    public int Ore => ore;
    public int MaxOre => maxOre;
    public bool Full => ore >= maxOre;
    public float Mass => emptyMass + ore * oreMass;
    public Vector2 HomePoint => home;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.freezeRotation = true;
        transform.rotation = Quaternion.identity;
        home = transform.position;
        Apply();
    }

    /// <summary>Adds one ore (if there's room). Returns false when full.</summary>
    public bool AddOre()
    {
        if (Full) return false;
        ore++;
        Apply();
        return true;
    }

    private void Apply()
    {
        if (body != null) body.mass = Mass;
        for (int i = 0; i < oreVisuals.Length; i++) if (oreVisuals[i] != null) oreVisuals[i].SetActive(i < ore);
    }

    private void Update()
    {
        if (body == null) return;
        float degrees = -body.linearVelocity.x / wheelRadius * Mathf.Rad2Deg * Time.deltaTime; // rolling without slipping
        foreach (Transform w in wheels) if (w != null) w.Rotate(0f, 0f, degrees);
        if (visual == null) return;
        float target = 0f;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.8f, LayerMask.GetMask("Ground"));
        if (hit.collider != null) target = Mathf.Atan2(-hit.normal.x, hit.normal.y) * Mathf.Rad2Deg;
        float current = visual.localEulerAngles.z;
        visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(current, target, 1f - Mathf.Exp(-15f * Time.deltaTime)));
    }

    public void ResetToStart()
    {
        ore = 0;
        Apply(); // position/velocity are reset with the room's other bodies
    }

#if UNITY_EDITOR
    public void EditorSetup(float empty, float perOre, int max, GameObject[] ores, Transform[] wheelTransforms, Transform art)
    {
        emptyMass = empty; oreMass = perOre; maxOre = max; oreVisuals = ores; wheels = wheelTransforms; visual = art;
    }
#endif
}
