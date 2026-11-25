using UnityEngine;

/// <summary>
/// Makes the Winch Hammer readable (finale, part 2). Purely visual: reads WinchHammer and its CordHandle.
///   - A HEIGHT RULER beside the swing: marks at 0.5 / 1.0 / 1.5 m (labels made here) and a cyan marker that slides up
///     with the hammer's wound height, so both players see how much is stored (h, for v = √(2·g·h)).
///   - The SWING PATH: a dotted arc from where the hammer hangs now, down through the bottom and on toward the cart,
///     shown while it is wound and waiting.
///   - The PULLEY, near the winch ring (shown while someone is near it, or the hammer is wound): how much cord has been
///     wound in and what it lifted, e.g. "Cord 3.6 m -> hammer +0.6 m", under the rule "6 m of cord = 1 m of height".
/// Setup: the hammer and cord; Ruler Base (room-local) = the height of the head's centre at the bottom of the swing,
/// at the ruler's x; the marker transform; a LineRenderer for the arc (dotted material, texture mode Tile);
/// Cord Label At (room-local), above the ring's slot.
/// </summary>
public class HammerGauge : MonoBehaviour
{
    [SerializeField] private WinchHammer hammer;
    [SerializeField] private CordHandle cord;
    [Header("Ruler")]
    [SerializeField] private Vector2 rulerBase;
    [SerializeField] private Transform marker;
    [SerializeField] private float[] marks = { 0.5f, 1f, 1.5f };
    [SerializeField] private float markLabelOffset = 1.15f;
    [Header("Swing path")]
    [SerializeField] private LineRenderer arc;
    [Tooltip("How far past the bottom the path is drawn (radians): toward the cart.")]
    [SerializeField] private float pastBottom = 0.45f;
    [SerializeField] private Color arcColor = new Color(0.55f, 0.95f, 1f, 0.75f);
    [Header("Pulley")]
    [SerializeField] private Vector2 cordLabelAt;
    [SerializeField, Min(0.5f)] private float showWithin = 4.5f;

    private Transform origin;
    private ScreenLabel[] markLabels = new ScreenLabel[0];
    private ScreenLabel cordLabel;
    private PlayerMovement[] players = new PlayerMovement[0];
    private float findTimer, shownHeight;
    private const int ArcPoints = 28;

    private void Awake()
    {
        origin = transform.parent != null ? transform.parent : transform;
    }

    private void Update()
    {
        if (hammer == null) return;
        float dt = Time.deltaTime;
        shownHeight = Mathf.MoveTowards(shownHeight, hammer.Swinging ? 0f : hammer.Height, 3f * dt);
        if (marker != null) marker.position = origin.TransformPoint(rulerBase + Vector2.up * shownHeight);
        Marks();
        Arc();
        Pulley(dt);
    }

    private void Marks()
    {
        if (markLabels.Length != marks.Length)
        {
            markLabels = new ScreenLabel[marks.Length];
            for (int i = 0; i < marks.Length; i++)
            {
                markLabels[i] = WorldText.Create("Hammer height mark", null, 17f, new Color(1f, 0.88f, 0.6f), 1.2f);
                if (markLabels[i] == null) continue;
                if (markLabels[i].Background != null) markLabels[i].Background.enabled = false;
                markLabels[i].SetText($"{marks[i]:0.0} m");
            }
        }
        for (int i = 0; i < marks.Length; i++)
        {
            if (markLabels[i] == null) continue;
            markLabels[i].WorldPosition = origin.TransformPoint(rulerBase + new Vector2(markLabelOffset, marks[i]));
            bool reached = !hammer.Swinging && hammer.Height >= marks[i] - 0.001f;
            markLabels[i].Text.color = reached ? new Color(0.6f, 1f, 1f) : new Color(1f, 0.88f, 0.6f, 0.75f);
        }
    }

    // The path the head will take: from its wound angle through the bottom, a little way on toward the cart.
    private void Arc()
    {
        if (arc == null) return;
        bool show = !hammer.Swinging && hammer.Height >= 0.05f;
        arc.enabled = show;
        if (!show) return;
        float a0 = hammer.Angle, a1 = -pastBottom;
        arc.useWorldSpace = true;
        arc.positionCount = ArcPoints;
        for (int i = 0; i < ArcPoints; i++)
        {
            float a = Mathf.Lerp(a0, a1, i / (ArcPoints - 1f));
            Vector2 p = hammer.Pivot + new Vector2(hammer.Side * Mathf.Sin(a), -Mathf.Cos(a)) * hammer.Length;
            arc.SetPosition(i, origin.TransformPoint(p));
        }
        Color c = arcColor;
        c.a *= 0.75f + 0.25f * Mathf.Sin(Time.time * 4f); // a gentle shimmer: "it will go this way"
        arc.startColor = c;
        c.a *= 0.35f;
        arc.endColor = c;
    }

    private void Pulley(float dt)
    {
        if (cord == null) return;
        if ((findTimer -= dt) <= 0f) { findTimer = 1f; players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None); }
        Vector3 ring = cord.transform.position;
        bool near = false;
        foreach (PlayerMovement p in players) if (p != null && Vector2.Distance(p.transform.position, ring) <= showWithin) near = true;
        bool show = near || hammer.Height >= 0.05f || cord.Pulled > 0.05f;
        if (!show)
        {
            if (cordLabel != null) { Destroy(cordLabel.gameObject); cordLabel = null; }
            return;
        }
        if (cordLabel == null) cordLabel = WorldText.Create("Winch pulley", null, 19f, new Color(1f, 0.92f, 0.75f), 5.5f);
        if (cordLabel == null) return;
        float ratio = hammer.Advantage;
        // The cord wound in so far (the ratchet keeps the hammer's height: cord = height × advantage), plus this pull.
        float wound = hammer.Swinging ? 0f : hammer.Height * ratio;
        string now = hammer.Swinging ? "Hammer released!"
                   : hammer.Height >= hammer.MaxHeight - 0.001f ? $"Cord {wound:0.0} m -> hammer +{hammer.Height:0.0} m (full)"
                   : $"Cord {wound:0.0} m -> hammer +{hammer.Height:0.0} m";
        cordLabel.SetText($"{now}\n<size=78%>Pulley {ratio:0}:1 · {ratio:0} m of cord = 1 m of height</size>");
        cordLabel.WorldPosition = origin.TransformPoint(cordLabelAt);
    }

    private void OnDisable()
    {
        foreach (ScreenLabel l in markLabels) if (l != null) Destroy(l.gameObject);
        markLabels = new ScreenLabel[0];
        if (cordLabel != null) Destroy(cordLabel.gameObject);
        cordLabel = null;
    }

#if UNITY_EDITOR
    public void EditorSetup(WinchHammer target, CordHandle winchCord, Vector2 baseOfRuler, Transform heightMarker, LineRenderer swingArc, Vector2 labelAt)
    {
        hammer = target; cord = winchCord; rulerBase = baseOfRuler; marker = heightMarker; arc = swingArc; cordLabelAt = labelAt;
    }
#endif
}
