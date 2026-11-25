using UnityEngine;

/// <summary>
/// A signal that is ON once a saved event has happened (e.g. "r1_engine_awake": the Heart Engine has woken), so
/// something in a later room can react to what players did in an earlier one. Optional Pulse: while the event is
/// set, it switches on and off every half period (for platforms that should keep moving back and forth).
/// </summary>
public class EventSignal : SignalSource
{
    [SerializeField] private string eventId = "r1_engine_awake";
    [Tooltip("0 = simply on. Otherwise on for half of each period, off for the other half (seconds).")]
    [SerializeField, Min(0f)] private float pulsePeriod = 0f;
    [Tooltip("Shifts this signal's pulse, in seconds (so neighbouring platforms don't move in step).")]
    [SerializeField] private float pulseOffset = 0f;

    private bool happened;

    public override bool IsActive
    {
        get
        {
            if (!happened) happened = SaveSystem.HasEvent(eventId); // cached once true: events are never unset
            if (!happened) return false;
            return pulsePeriod <= 0f || Mathf.Repeat(Time.time + pulseOffset, pulsePeriod) < pulsePeriod * 0.5f;
        }
    }

#if UNITY_EDITOR
    public void EditorSetup(string id, float period, float offset) { eventId = id; pulsePeriod = period; pulseOffset = offset; }
#endif
}
