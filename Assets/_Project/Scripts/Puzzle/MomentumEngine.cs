using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The Momentum Engine (Room 8, the region's finale). Interact to install the machine parts found in the region (all of them
/// are needed; missing ones are named, with the room to find them in). With all parts found, Interact opens the assembly
/// screen (EngineAssemblyPanel, both players fit the parts together); without that screen the parts go straight in. Once installed:
///   - its FLYWHEEL stores momentum. A crate that hits its paddle adds the crate's momentum (p = m·v along the hit, as the
///     rubble walls measure it): impulse in, momentum stored.
///   - friction drains it slowly, the Bearing keeping that drain low (Drain per second).
///   - it is a SignalSource that is ON while the stored momentum is at least Gate Threshold (e.g. it holds the Heart Gate
///     open), so the time the gate stays open is (momentum - threshold) / drain: friction is the timer.
/// What players see as momentum builds:
///   - the frame's RUNES light up one by one as a meter. The runes before the threshold rune fill from 0 to the threshold
///     (the target rune glows gold until reached); the rest fill from the threshold to the maximum, so as friction drains
///     the engine they go out one by one: the gate's remaining time.
///   - each hit pops up "+p kg·m/s"; the wheel spins faster (motion-blur copies fade in near full); the hub light grows;
///     crossing the threshold sends out a pulse ring; a small gauge shows "momentum / max" and, while open, the seconds left.
/// Installed state is saved (event "r1_engine_installed"). Momentum is not saved and returns to 0 on a room reset.
/// Setup: a trigger Collider2D on the Interactable layer (to install); an EnginePaddle with a solid collider that crates
/// hit; part visuals that appear once installed (the flywheel, the caged counterweight crate); the body SpriteRenderer
/// (sprite swapped to the loaded one on install); the rune SpriteRenderers in meter order.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MomentumEngine : SignalSource, IInteractable, IRoomResettable
{
    [Header("Parts")]
    [SerializeField] private string[] partIds = new string[0];
    [Tooltip("Shown when a part is missing, same order as Part Ids, e.g. \"Bearing (Room 5)\".")]
    [SerializeField] private string[] partHints = new string[0];
    [Tooltip("Objects switched on once the parts are installed (wheel, counterweight...).")]
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

    [Header("Look")]
    [Tooltip("The engine's body. Its sprite becomes Loaded Sprite once installed (counterweight on the hook).")]
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Sprite loadedSprite;
    [Tooltip("Rune segments in meter order (left post bottom to top, beam, right post top to bottom).")]
    [SerializeField] private SpriteRenderer[] runes = new SpriteRenderer[0];
    [Tooltip("Faint copies on the wheel, a few degrees behind it: shown at high speed (motion blur).")]
    [SerializeField] private SpriteRenderer[] blur = new SpriteRenderer[0];
    [SerializeField] private Light2D hubLight;
    [SerializeField] private Color targetRuneColor = new Color(1f, 0.82f, 0.35f);
    [SerializeField] private Color openRuneColor = new Color(0.75f, 1f, 0.85f);

    [Header("Feedback")]
    [Tooltip("Lit while the signal is on.")]
    [SerializeField] private Light2D lamp;
    [SerializeField] private Vector2 gaugeOffset = new Vector2(0f, 2.75f);
    [Tooltip("Show the small momentum gauge above the engine. Off where another display shows it (EngineMilestones).")]
    [SerializeField] private bool showGauge = true;
    [SerializeField] private FxLibrary fx;

    private float momentum;
    private float shown;      // the meter's value: catches up with momentum quickly, so a hit visibly fills it
    private float hitPulse;   // the hub crystal flares on each hit (0..1, fades)
    private bool installed;
    private bool wasActive;
    private Sprite emptySprite;
    private ScreenLabel gauge;
    private ScreenLabel message;
    private float messageTimer;

    private struct Popup { public ScreenLabel label; public Vector3 at; public float age; }
    private readonly List<Popup> popups = new List<Popup>();
    private const float PopupLife = 1.2f;

    /// <summary>Raised by every hit that adds momentum, with the momentum (before, after) it. Lets a listener (EngineStage) catch
    /// a threshold the moment it is crossed, before friction drains anything (so a hit that fills it to exactly the maximum counts).</summary>
    public event System.Action<float, float> MomentumAdded;

    public bool Installed => installed;
    public float Momentum => momentum;
    public float MaxMomentum => maxMomentum;
    public float GateThreshold => gateThreshold;
    public override bool IsActive => installed && momentum >= gateThreshold;
    public string InteractVerb => "Install parts";
    public Vector2 HomePoint => transform.position;

    private void Awake()
    {
        if (body != null) emptySprite = body.sprite;
    }

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
        EngineAssemblyPanel panel = UIRoot.Instance != null ? UIRoot.Instance.EngineAssembly : null;
        if (panel != null) panel.Open(this);
        else
        {
            Debug.LogWarning("MomentumEngine: no assembly screen in this scene's UI_Canvas (run Create UI Prefabs after Room 8 > 0. " +
                             "Import Engine Art). Installing the parts directly.", this);
            CompleteInstall();
        }
    }

    /// <summary>The parts are in (called by the assembly screen when both players finish it).</summary>
    public void CompleteInstall()
    {
        if (installed) return;
        SaveSystem.MarkEvent(installedEvent);
        SetInstalled(true);
        Fx.Flash(transform.position, new Color(1f, 0.85f, 0.45f), 4f, 2f, 0.6f);
        ShowMessage("The engine is whole again. Give the flywheel a push!");
    }

    private void SetInstalled(bool on)
    {
        installed = on;
        // Installed: the install trigger is done. Switched off so no "Install parts" prompt lingers (and it can't take
        // Interact away from things nearby). Hits still arrive through the paddle, which has its own collider.
        if (TryGetComponent(out Collider2D installZone)) installZone.enabled = !on;
        foreach (GameObject g in installedVisuals) if (g != null) g.SetActive(on);
        if (body != null && loadedSprite != null) body.sprite = on ? loadedSprite : emptySprite;
    }

    /// <summary>Called by the paddle when a crate hits it: the crate's momentum goes into the flywheel.</summary>
    public void AddMomentum(float p)
    {
        if (!installed || p <= 0.5f) return;
        float before = momentum;
        momentum = Mathf.Min(maxMomentum, momentum + p);
        Vector3 hub = wheel != null ? wheel.position : transform.position;
        if (fx != null && fx.spark != null)
            for (int i = 0; i < 6; i++)
                Fx.Sprite(fx.spark, hub + (Vector3)(Random.insideUnitCircle * 0.6f), new Color(1f, 0.85f, 0.5f), 0.35f, 6)
                  .Move(Random.insideUnitCircle * 3f, 6f);
        SpawnPopup(momentum - before < p - 0.5f ? $"+{p:0} kg·m/s (full)" : $"+{p:0} kg·m/s", hub);
        hitPulse = Mathf.Max(hitPulse, Mathf.Clamp(p / 30f, 0.4f, 1f)); // a bigger hit, a brighter flare
        MomentumAdded?.Invoke(before, momentum);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (installed && momentum > 0f) momentum = Mathf.Max(0f, momentum - drain * dt);
        shown = Mathf.MoveTowards(shown, momentum, Mathf.Max(30f, Mathf.Abs(momentum - shown) * 6f) * dt);
        float fill = momentum / maxMomentum;

        if (wheel != null) wheel.Rotate(0f, 0f, -momentum * spinPerMomentum * dt);
        float blurAmount = Mathf.Clamp01((fill - 0.6f) / 0.4f);
        for (int i = 0; i < blur.Length; i++)
            if (blur[i] != null) blur[i].color = new Color(1f, 1f, 1f, blurAmount * 0.32f / (i + 1));
        if (hubLight != null)
        {
            hubLight.enabled = installed && momentum > 0.1f;
            hitPulse = Mathf.MoveTowards(hitPulse, 0f, 2.5f * dt);
            hubLight.intensity = 0.25f + 1.6f * fill + 2.2f * hitPulse;
            hubLight.pointLightOuterRadius = 1.4f + 1.8f * fill;
        }
        if (lamp != null) lamp.enabled = IsActive;
        UpdateRunes();

        bool active = IsActive;
        if (active && !wasActive) Pulse();
        wasActive = active;

        UpdateGauge();
        UpdatePopups(dt);
        if (message != null && (messageTimer -= dt) <= 0f) { Destroy(message.gameObject); message = null; }
    }

    // The meter: runes [0, target] cover 0..threshold, runes after it cover threshold..max.
    private void UpdateRunes()
    {
        int n = runes.Length;
        if (n == 0) return;
        int target = TargetRune;
        for (int i = 0; i < n; i++)
        {
            SpriteRenderer r = runes[i];
            if (r == null) continue;
            if (!installed) { r.color = Color.clear; continue; }
            float lit;
            if (i <= target) lit = Mathf.Clamp01(shown / gateThreshold * (target + 1) - i);
            else lit = Mathf.Clamp01((shown - gateThreshold) / Mathf.Max(0.01f, maxMomentum - gateThreshold) * (n - 1 - target) - (i - target - 1));
            Color on = i == target ? targetRuneColor : i > target ? openRuneColor : Color.white;
            if (i == target && lit < 1f)
            {
                float hint = 0.3f + 0.15f * Mathf.Sin(Time.time * 4f); // the goal: a soft gold glow until it's reached
                r.color = new Color(on.r, on.g, on.b, Mathf.Max(hint, lit));
            }
            else r.color = new Color(on.r, on.g, on.b, lit);
        }
    }

    /// <summary>The rune that marks the threshold: the last one of the "0 to threshold" part of the meter.</summary>
    private int TargetRune => Mathf.Clamp(Mathf.RoundToInt(gateThreshold / maxMomentum * runes.Length) - 1, 0, Mathf.Max(0, runes.Length - 2));

    private void Pulse()
    {
        Vector3 hub = wheel != null ? wheel.position : transform.position;
        Fx.Flash(hub, new Color(0.55f, 0.95f, 1f), 5f, 2.2f, 0.5f);
        if (fx != null && fx.ring != null && fx.ring.Length > 0)
            Fx.Frames(fx.ring, hub, new Color(0.6f, 1f, 1f), 0.5f, 7).Grow(3f, 10f);
    }

    private void UpdateGauge()
    {
        if (!installed || !showGauge) { if (gauge != null) { Destroy(gauge.gameObject); gauge = null; } return; }
        if (gauge == null) gauge = WorldText.Create("Engine gauge", null, 22f, Color.white, 7f);
        if (gauge == null) return;
        string text = IsActive
            ? $"<color=#7dff8a>Momentum {momentum:0} / {maxMomentum:0} kg·m/s · gate open {(momentum - gateThreshold) / Mathf.Max(0.01f, drain):0} s</color>"
            : $"Momentum {momentum:0} / {maxMomentum:0} kg·m/s · gate at {gateThreshold:0}";
        gauge.SetText(text);
        gauge.WorldPosition = transform.position + (Vector3)gaugeOffset;
    }

    private void SpawnPopup(string text, Vector3 at)
    {
        ScreenLabel label = WorldText.Create("Engine +p", null, 30f, new Color(1f, 0.85f, 0.45f), 6f);
        if (label == null) return;
        if (label.Background != null) label.Background.enabled = false; // just the number, floating up
        label.SetText(text);
        Vector3 start = at + new Vector3(0f, 1.7f, 0f);
        label.WorldPosition = start;
        popups.Add(new Popup { label = label, at = start, age = 0f });
    }

    private void UpdatePopups(float dt)
    {
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            Popup p = popups[i];
            p.age += dt;
            if (p.label == null || p.age >= PopupLife)
            {
                if (p.label != null) Destroy(p.label.gameObject);
                popups.RemoveAt(i);
                continue;
            }
            float t = p.age / PopupLife;
            p.label.WorldPosition = p.at + Vector3.up * (1.1f * (1f - (1f - t) * (1f - t))); // rises, slowing down
            p.label.Alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            popups[i] = p;
        }
    }

    private void ShowMessage(string text)
    {
        if (message == null) message = WorldText.CreatePaper("Engine message", 28f, 7f);
        if (message == null) return;
        message.SetText(text);
        message.WorldPosition = transform.position + (Vector3)gaugeOffset + Vector3.up * 1.2f;
        messageTimer = 4f;
    }

    public void ResetToStart()
    {
        momentum = shown = 0f;
        wasActive = false;
    }

    private void OnDisable()
    {
        if (gauge != null) Destroy(gauge.gameObject);
        if (message != null) Destroy(message.gameObject);
        foreach (Popup p in popups) if (p.label != null) Destroy(p.label.gameObject);
        popups.Clear();
        gauge = message = null;
    }

#if UNITY_EDITOR
    public void EditorSetup(string[] ids, string[] hints, GameObject[] visuals, Transform wheelTransform, Light2D lampLight, FxLibrary library)
    {
        partIds = ids; partHints = hints; installedVisuals = visuals; wheel = wheelTransform; lamp = lampLight; fx = library;
    }

    public void EditorSetupNumbers(float max, float drainPerSecond, float threshold, float spin, Vector2 gauge, string installEvent)
    {
        maxMomentum = max; drain = drainPerSecond; gateThreshold = threshold; spinPerMomentum = spin; gaugeOffset = gauge;
        installedEvent = installEvent;
    }

    public void EditorSetupLook(SpriteRenderer bodyRenderer, Sprite loaded, SpriteRenderer[] runeRenderers, SpriteRenderer[] blurCopies, Light2D hub)
    {
        body = bodyRenderer; loadedSprite = loaded; runes = runeRenderers; blur = blurCopies; hubLight = hub;
    }
#endif
}
