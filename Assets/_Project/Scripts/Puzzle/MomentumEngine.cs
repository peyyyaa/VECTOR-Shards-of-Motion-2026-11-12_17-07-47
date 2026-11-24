using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The Momentum Engine (Room 8, the region's finale). Interact to install the machine parts found in the region (all of them
/// are needed; missing ones are named, with the room to find them in). Once installed:
///   - its FLYWHEEL stores momentum. A crate that hits its paddle adds the crate's momentum (p = m·v along the hit, as the
///     rubble walls measure it): impulse in, momentum stored.
///   - friction drains it slowly, the Bearing keeping that drain low (Drain per second).
///   - it is a SignalSource that is ON while the stored momentum is at least Gate Threshold (e.g. it holds the Heart Gate
///     open), so the time the gate stays open is (momentum - threshold) / drain: friction is the timer.
/// The wheel visibly spins faster with more momentum; a gauge above it shows "momentum / max"; a lamp lights while it's on.
/// Installed state is saved (event "r1_engine_installed"). Momentum is not saved and returns to 0 on a room reset.
/// Setup: a trigger Collider2D on the Interactable layer (to install); an EnginePaddle child with a solid collider that crates
/// hit; part visuals that appear once installed (the flywheel sprite, the bearing at the hub, the caged counterweight crate).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MomentumEngine : SignalSource, IInteractable, IRoomResettable
{
    [Header("Parts")]
    [SerializeField] private string[] partIds = new string[0];
    [Tooltip("Shown when a part is missing, same order as Part Ids, e.g. \"Bearing (Room 5)\".")]
    [SerializeField] private string[] partHints = new string[0];
    [Tooltip("Objects switched on once the parts are installed (wheel, bearing, counterweight...).")]
    [SerializeField] private GameObject[] installedVisuals = new GameObject[0];
    [SerializeField] private string installedEvent = "r1_engine_installed";

    [Header("Flywheel")]
    [SerializeField] private Transform wheel;
    [Tooltip("Momentum the flywheel can hold (kg·m/s).")]
    [SerializeField, Min(1f)] private float maxMomentum = 50f;
    [Tooltip("Momentum lost to friction each second (kg·m/s per s = N). Low thanks to the Bearing.")]
    [SerializeField, Min(0f)] private float drain = 1.5f;
    [Tooltip("The signal is on at or above this momentum.")]
    [SerializeField, Min(0f)] private float gateThreshold = 35f;
    [Tooltip("Wheel spin, degrees per second for each kg·m/s stored.")]
    [SerializeField, Min(0f)] private float spinPerMomentum = 12f;

    [Header("Feedback")]
    [SerializeField] private Light2D lamp;
    [SerializeField] private Vector2 gaugeOffset = new Vector2(0f, 3.2f);
    [SerializeField] private FxLibrary fx;

    private float momentum;
    private bool installed;
    private ScreenLabel gauge;
    private ScreenLabel message;
    private float messageTimer;

    public bool Installed => installed;
    public float Momentum => momentum;
    public override bool IsActive => installed && momentum >= gateThreshold;
    public string InteractVerb => "Install parts";
    public Vector2 HomePoint => transform.position;

    private void Start()
    {
        SetInstalled(SaveSystem.HasEvent(installedEvent));
    }

    public bool CanInteract(PlayerInteractor player) => isActiveAndEnabled && !installed;

    public void Interact(PlayerInteractor player)
    {
        if (installed) return;
        string missing = "";
        for (int i = 0; i < partIds.Length; i++)
            if (!SaveSystem.Data.questItems.Contains(partIds[i]))
                missing += (missing.Length > 0 ? "\n" : "") + (i < partHints.Length ? partHints[i] : partIds[i]);
        if (missing.Length > 0)
        {
            ShowMessage("The engine is missing:\n" + missing);
            return;
        }
        SaveSystem.MarkEvent(installedEvent);
        SetInstalled(true);
        Fx.Flash(transform.position, new Color(1f, 0.85f, 0.45f), 4f, 2f, 0.6f);
        ShowMessage("All three parts fit. Give the flywheel a push!");
    }

    private void SetInstalled(bool on)
    {
        installed = on;
        foreach (GameObject g in installedVisuals) if (g != null) g.SetActive(on);
    }

    /// <summary>Called by the paddle when a crate hits it: the crate's momentum goes into the flywheel.</summary>
    public void AddMomentum(float p)
    {
        if (!installed || p <= 0.5f) return;
        momentum = Mathf.Min(maxMomentum, momentum + p);
        if (fx != null && fx.spark != null && wheel != null)
            for (int i = 0; i < 6; i++)
                Fx.Sprite(fx.spark, wheel.position + (Vector3)(Random.insideUnitCircle * 0.6f), new Color(1f, 0.85f, 0.5f), 0.35f, 6)
                  .Move(Random.insideUnitCircle * 3f, 6f);
    }

    private void Update()
    {
        if (installed && momentum > 0f) momentum = Mathf.Max(0f, momentum - drain * Time.deltaTime);
        if (wheel != null) wheel.Rotate(0f, 0f, -momentum * spinPerMomentum * Time.deltaTime);
        if (lamp != null) lamp.enabled = IsActive;
        UpdateGauge();
        if (message != null && (messageTimer -= Time.deltaTime) <= 0f) { Destroy(message.gameObject); message = null; }
    }

    private void UpdateGauge()
    {
        if (!installed) { if (gauge != null) { Destroy(gauge.gameObject); gauge = null; } return; }
        if (gauge == null) gauge = WorldText.Create("Engine gauge", null, 26f, Color.white, 6f);
        if (gauge == null) return;
        string col = IsActive ? "#7dff8a" : "#ffffff";
        gauge.SetText($"<color={col}>Momentum {momentum:0} / {maxMomentum:0} kg·m/s</color>");
        gauge.WorldPosition = transform.position + (Vector3)gaugeOffset;
    }

    private void ShowMessage(string text)
    {
        if (message == null) message = WorldText.CreatePaper("Engine message", 28f, 7f);
        if (message == null) return;
        message.SetText(text);
        message.WorldPosition = transform.position + (Vector3)gaugeOffset + Vector3.up * 1.2f;
        messageTimer = 4f;
    }

    public void ResetToStart() => momentum = 0f;

    private void OnDisable()
    {
        if (gauge != null) Destroy(gauge.gameObject);
        if (message != null) Destroy(message.gameObject);
        gauge = message = null;
    }

#if UNITY_EDITOR
    public void EditorSetup(string[] ids, string[] hints, GameObject[] visuals, Transform wheelTransform, Light2D lampLight, FxLibrary library)
    {
        partIds = ids; partHints = hints; installedVisuals = visuals; wheel = wheelTransform; lamp = lampLight; fx = library;
    }
#endif
}
