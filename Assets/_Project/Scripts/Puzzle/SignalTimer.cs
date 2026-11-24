using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// A timed signal: ON while its Input is on, and for Hold Seconds after the input turns off, then off. Put it between a
/// trigger and what it drives ("the lift stays up 2.5 s after you step off the plate"). During the countdown its lamps
/// and sprites blink, faster and faster, so players see the time running out.
///
/// Setup: Input = any SignalSource (plate, lever, relay). Drive a Mover / SignalActivator / relay with this timer instead
/// of the input. Optional: Light2D lamps and SpriteRenderers to blink during the countdown (lit while the input is on).
/// </summary>
public class SignalTimer : SignalSource, IRoomResettable
{
    [SerializeField] private SignalSource input;
    [Tooltip("Seconds the signal stays on after the input turns off.")]
    [SerializeField, Min(0f)] private float holdSeconds = 2.5f;
    [Tooltip("Lamps lit while on, blinking during the countdown.")]
    [SerializeField] private Light2D[] lamps = new Light2D[0];
    [Tooltip("Sprites shown while on, blinking during the countdown.")]
    [SerializeField] private SpriteRenderer[] blinkers = new SpriteRenderer[0];

    private float remaining;

    public override bool IsActive => (input != null && input.IsActive) || remaining > 0f;

    /// <summary>Seconds left in the countdown (0 while the input is on or when it's off).</summary>
    public float Remaining => input != null && input.IsActive ? 0f : remaining;

    public Vector2 HomePoint => transform.position;

    private void Update()
    {
        bool held = input != null && input.IsActive;
        if (held) remaining = holdSeconds;
        else if (remaining > 0f) remaining = Mathf.Max(0f, remaining - Time.deltaTime);

        // Lit while held; blinking during the countdown, from 3 to 12 blinks per second as it runs out.
        bool on = held;
        if (!held && remaining > 0f)
        {
            float urgency = 1f - remaining / Mathf.Max(0.01f, holdSeconds);
            float rate = Mathf.Lerp(3f, 12f, urgency);
            on = Mathf.Repeat(Time.time * rate, 1f) < 0.5f;
        }
        foreach (Light2D l in lamps) if (l != null) l.enabled = on;
        foreach (SpriteRenderer sr in blinkers) if (sr != null) sr.enabled = on;
    }

    public void ResetToStart() => remaining = 0f;

#if UNITY_EDITOR
    public void EditorSetup(SignalSource source, float seconds, Light2D[] lights)
    {
        input = source; holdSeconds = seconds; lamps = lights;
    }
#endif
}
