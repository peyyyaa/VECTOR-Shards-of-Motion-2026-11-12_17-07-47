using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Combines several puzzle inputs (pressure plates, levers, other relays) into one on/off signal,
/// then fires events to whatever should react (doors, platforms, water gates).
///
/// Rule of thumb: if two or more inputs control one thing, wire them through a relay
/// instead of connecting each input to the door directly (otherwise the last input to
/// change wins, e.g., releasing one plate closes a door the other plate should hold open).
///
/// - Mode All: on only while EVERY input is on ("both plates pressed").
///   Mode Any: on while AT LEAST ONE input is on.
///   Mode Switch: on while an ODD number of inputs are on, so flipping ANY input flips the result
///   (two-way switches: "each lever sends the ferry the other way").
/// - Invert: flips the result ("door closes while the plate is pressed").
/// - Latch: once it turns on, it stays on (until a room reset).
///
/// Inputs are checked every physics step, so the result never depends on which input
/// changed first. A relay is itself a SignalSource, so relays can feed other relays.
/// </summary>
public class PuzzleRelay : SignalSource, IRoomResettable
{
    public enum RelayMode { All, Any, Switch }

    [Header("Logic")]
    [Tooltip("Plates, levers or other relays. Empty slots are ignored.")]
    [SerializeField] private SignalSource[] inputs = new SignalSource[0];
    [SerializeField] private RelayMode mode = RelayMode.All;
    [SerializeField] private bool invert = false;
    [Tooltip("Once on, stay on.")]
    [SerializeField] private bool latch = false;

    [Header("Events")]
    [SerializeField] private UnityEvent onActivated;
    [SerializeField] private UnityEvent onDeactivated;

    private bool isActive;
    private bool latched;

    public override bool IsActive => isActive;

    // Evaluated in FixedUpdate (after every Start), so receivers like doors have already
    // set their own starting state before the relay's first event.
    private void FixedUpdate()
    {
        bool result = latched || Evaluate();
        if (result && latch) latched = true;

        if (result == isActive) return;
        isActive = result;

        if (isActive) onActivated.Invoke();
        else onDeactivated.Invoke();
    }

    private bool Evaluate()
    {
        int counted = 0;
        int on = 0;
        foreach (SignalSource source in inputs)
        {
            if (source == null) continue;
            counted++;
            if (source.IsActive) on++;
        }

        bool result;
        if (counted == 0) result = false; // no inputs: never on (before Invert)
        else if (mode == RelayMode.All) result = on == counted;
        else if (mode == RelayMode.Any) result = on > 0;
        else result = on % 2 == 1; // Switch

        return invert ? !result : result;
    }

    public Vector2 HomePoint => transform.position;

    public void ResetToStart() => ResetRelay();

    /// <summary>Clears the latch and turns the relay off (used by a room reset).</summary>
    public void ResetRelay()
    {
        latched = false;
        if (isActive)
        {
            isActive = false;
            onDeactivated.Invoke();
        }
    }

    // Scene view: a small box always; select the relay to see a line to each input (green = that input is on).
    private void OnDrawGizmos()
    {
        Gizmos.color = Application.isPlaying && isActive ? Color.green : Color.white;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.4f);
    }

    private void OnDrawGizmosSelected()
    {
        if (inputs == null) return;
        foreach (SignalSource source in inputs)
        {
            if (source == null) continue;
            Gizmos.color = Application.isPlaying && source.IsActive ? Color.green : new Color(1f, 0.8f, 0.2f, 0.8f);
            Gizmos.DrawLine(source.transform.position, transform.position);
        }
    }
}
