using UnityEngine;

/// <summary>
/// Shows a water current (AreaEffector2D zone): while the effector is on, pale streaks flow through the zone in the
/// force's direction and the water tint is visible; while it's off, the channel looks still. Purely visual.
/// Added by RoomDresser to AreaEffector2D zones.
/// </summary>
[RequireComponent(typeof(AreaEffector2D))]
public class CurrentVisual : MonoBehaviour
{
    [SerializeField] private FxLibrary fx;
    [SerializeField] private SpriteRenderer tint;
    [SerializeField] private Color streakColor = new Color(0.8f, 0.95f, 1f, 0.7f);
    [SerializeField] private float streaksPerSecondPerMetre = 1.2f;

    private AreaEffector2D effector;
    private BoxCollider2D zone;
    private float timer;

    private void Awake()
    {
        effector = GetComponent<AreaEffector2D>();
        zone = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        bool on = effector.enabled;
        if (tint != null) tint.enabled = on;
        if (!on || fx == null || fx.wisp == null || zone == null) return;

        Bounds b = zone.bounds;
        float angle = effector.forceAngle * Mathf.Deg2Rad;
        Vector2 dir = effector.useGlobalAngle ? new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))
                                              : (Vector2)transform.TransformDirection(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = 1f / Mathf.Max(0.1f, streaksPerSecondPerMetre * b.size.x);
        Vector3 p = new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y + 0.1f, b.min.y + b.size.y * 0.6f), 0f);
        Fx.Sprite(fx.wisp, p, streakColor, Random.Range(0.4f, 0.7f), 1).Move(dir.normalized * Random.Range(2.5f, 4f)).Order("Water", 5);
    }

#if UNITY_EDITOR
    public void EditorSetup(FxLibrary library, SpriteRenderer water) { fx = library; tint = water; }
#endif
}
