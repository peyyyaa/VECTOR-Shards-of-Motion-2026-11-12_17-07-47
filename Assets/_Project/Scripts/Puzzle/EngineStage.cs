using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One stage of the Heart Engine (finale, part 2): a signal that is ON while the engine's stored momentum is at least
/// Threshold. With a Latch Event it stays on for good once reached, and that event is saved: the last stage
/// ("r1_engine_awake") is the AWAKENING: the camera shakes, light floods out of the wheel and a banner reads that the
/// engine has woken; later rooms react through EventSignal. Lights listed here switch on with the stage (hall lamps).
/// Crossing is caught two ways, so the stage never depends on which script's Update runs first:
///   - the engine's MomentumAdded event: a hit that takes the momentum from below Threshold to at least Threshold counts at
///     once (and a latch is saved right then), even if friction drains it below the line before the next Update
///     (e.g. a hit that fills the engine to exactly its maximum, 100);
///   - every frame, the momentum itself (as before).
/// </summary>
public class EngineStage : SignalSource
{
    [SerializeField] private MomentumEngine engine;
    [SerializeField, Min(0f)] private float threshold = 30f;
    [Tooltip("Empty = on only while the momentum is high enough. Set = stays on once reached (saved).")]
    [SerializeField] private string latchEvent = "";
    [SerializeField] private Light2D[] lights = new Light2D[0];
    [Tooltip("Objects switched on with the lights and left on (e.g. the lit lantern sprites).")]
    [SerializeField] private GameObject[] showOnReach = new GameObject[0];
    [Tooltip("Banner shown when the stage is first reached (empty = none).")]
    [SerializeField, TextArea] private string banner = "";
    [SerializeField] private bool bigMoment;

    private bool latched;
    private bool wasOn;
    private bool crossed; // a hit reached the threshold since the last Update
    private ScreenLabel label;
    private float labelTimer;

    public override bool IsActive => latched || (engine != null && engine.Installed && engine.Momentum >= threshold);

    private void OnEnable()
    {
        if (engine != null) engine.MomentumAdded += OnMomentumAdded;
    }

    // Called by the engine the moment a hit adds momentum, before any friction drain.
    private void OnMomentumAdded(float before, float after)
    {
        if (after < threshold - 0.001f) return;
        crossed = true;
        Latch();
    }

    private void Latch()
    {
        if (latched || string.IsNullOrEmpty(latchEvent)) return;
        latched = true;
        SaveSystem.MarkEvent(latchEvent);
    }

    private void Start()
    {
        latched = !string.IsNullOrEmpty(latchEvent) && SaveSystem.HasEvent(latchEvent);
        wasOn = IsActive;
        SetLights(wasOn);
    }

    private void Update()
    {
        bool reached = crossed || (engine != null && engine.Installed && engine.Momentum >= threshold);
        crossed = false;
        if (reached) Latch();
        bool on = reached || IsActive;
        if (on && !wasOn) Reached();
        wasOn = on;
        if (label != null)
        {
            labelTimer -= Time.deltaTime;
            label.Alpha = Mathf.Clamp01(labelTimer);
            if (labelTimer <= 0f) { Destroy(label.gameObject); label = null; }
        }
    }

    private void Reached()
    {
        SetLights(true);
        Vector3 at = engine != null ? engine.transform.position : transform.position;
        if (bigMoment)
        {
            Fx.Flash(at, new Color(0.6f, 0.97f, 1f), 14f, 3.5f, 2.5f);
            Fx.Flash(at, new Color(1f, 0.9f, 0.6f), 6f, 2f, 1.2f);
            FxCameraKick.Kick(new Vector2(0f, -0.25f), 0.6f);
        }
        if (string.IsNullOrEmpty(banner)) return;
        if (label == null) label = WorldText.Create("Engine stage banner", null, bigMoment ? 40f : 28f, new Color(0.75f, 0.97f, 1f), 10f);
        if (label == null) return;
        if (label.Background != null) label.Background.enabled = !bigMoment;
        label.SetText(banner);
        label.WorldPosition = at + Vector3.up * (bigMoment ? 5.6f : 4.8f);
        labelTimer = bigMoment ? 5f : 3f;
    }

    // Stage lights (e.g. the hall lamps) stay on once lit, so the hall visibly wakes up stage by stage.
    private void SetLights(bool on)
    {
        if (!on) return;
        foreach (Light2D l in lights) if (l != null) l.enabled = true;
        foreach (GameObject g in showOnReach) if (g != null) g.SetActive(true);
    }

    private void OnDisable()
    {
        if (engine != null) engine.MomentumAdded -= OnMomentumAdded;
        if (label != null) Destroy(label.gameObject);
        label = null;
    }

#if UNITY_EDITOR
    public void EditorSetup(MomentumEngine target, float atMomentum, string latch, Light2D[] stageLights, string bannerText, bool big)
    {
        engine = target; threshold = atMomentum; latchEvent = latch; lights = stageLights; banner = bannerText; bigMoment = big;
    }
#endif
}
