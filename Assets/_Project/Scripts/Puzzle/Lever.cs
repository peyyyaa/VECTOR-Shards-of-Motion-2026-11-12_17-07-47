using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A lever: a player in range presses Interact to flip it. It's a SignalSource, so it can feed
/// a PuzzleRelay, or drive things directly through its events (e.g., ExitDoor.Open / Close).
///
/// Modes:
/// - Toggle: each press flips it on/off.
/// - Timed: a press turns it on, and it switches itself off after Auto Off Seconds
///   (for "hold the gate open while your partner runs through" puzzles).
///
/// Setup: a Collider2D with Is Trigger ON, on the Interactable layer (that's how players find it).
/// Optional: a SpriteRenderer to show on/off by color (graybox) or by flipping the sprite.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Lever : SignalSource, IInteractable
{
    public enum LeverMode { Toggle, Timed }

    [Header("Behaviour")]
    [SerializeField] private LeverMode mode = LeverMode.Toggle;
    [Tooltip("Timed mode: seconds before the lever switches itself off.")]
    [SerializeField, Min(0.1f)] private float autoOffSeconds = 3f;
    [SerializeField] private bool startOn = false;
    [Tooltip("Ignore presses for this long after a flip, so both players pressing at once doesn't cancel out.")]
    [SerializeField, Min(0f)] private float pressCooldown = 0.25f;

    [Header("Feedback (optional)")]
    [SerializeField] private SpriteRenderer leverRenderer;
    [SerializeField] private Color offColor = new Color(0.7f, 0.3f, 0.2f);
    [SerializeField] private Color onColor = new Color(0.3f, 0.9f, 0.4f);
    [Tooltip("Also mirror the sprite horizontally when on (looks like the handle moved).")]
    [SerializeField] private bool flipSpriteWhenOn = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onTurnedOn;
    [SerializeField] private UnityEvent onTurnedOff;

    private bool isOn;
    private float cooldownRemaining;
    private float autoOffRemaining;

    public override bool IsActive => isOn;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
        leverRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        isOn = startOn;
        UpdateVisual();
    }

    private void Update()
    {
        if (cooldownRemaining > 0f) cooldownRemaining -= Time.deltaTime;

        if (isOn && mode == LeverMode.Timed)
        {
            autoOffRemaining -= Time.deltaTime;
            if (autoOffRemaining <= 0f) SetOn(false);
        }
    }

    public bool CanInteract(PlayerInteractor player) => isActiveAndEnabled && cooldownRemaining <= 0f;

    public void Interact(PlayerInteractor player)
    {
        cooldownRemaining = pressCooldown;

        if (mode == LeverMode.Timed)
        {
            autoOffRemaining = autoOffSeconds; // pressing again while on restarts the timer
            SetOn(true);
        }
        else
        {
            SetOn(!isOn);
        }
    }

    /// <summary>Sets the lever directly (e.g., from a room reset). Fires events only if it changes.</summary>
    public void SetOn(bool on)
    {
        if (on == isOn) return;
        isOn = on;
        UpdateVisual();

        if (isOn) onTurnedOn.Invoke();
        else onTurnedOff.Invoke();
    }

    private void UpdateVisual()
    {
        if (leverRenderer == null) return;
        leverRenderer.color = isOn ? onColor : offColor;
        if (flipSpriteWhenOn) leverRenderer.flipX = isOn;
    }
}
