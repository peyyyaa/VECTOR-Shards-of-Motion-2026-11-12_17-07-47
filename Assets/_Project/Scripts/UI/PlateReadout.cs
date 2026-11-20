using UnityEngine;

/// <summary>
/// Shows a pressure plate's measured weight against its threshold, e.g. "58.9 / 45 N",
/// turning green while pressed. Teaches W = m * g (and that stacked weights add up).
/// Shown below the plate (like a gauge set into the floor, so it never covers what stands on it),
/// when a player is near or for Linger seconds after the weight on the plate changes.
/// </summary>
[RequireComponent(typeof(PressurePlate))]
public class PlateReadout : FloatingLabel
{
    [SerializeField] private Color pressedColor = new Color(0.4f, 1f, 0.45f);
    [Tooltip("Show the readout under the plate instead of at Offset.")]
    [SerializeField] private bool belowPlate = true;
    [Tooltip("How far below the plate's centre the readout sits (units), when Below Plate is on.")]
    [SerializeField] private float belowDistance = 0.4f;
    [Tooltip("Seconds the readout stays after the weight on the plate changes.")]
    [SerializeField] private float linger = 3f;
    public enum Display { ReadingAndTarget, TargetOnly, ReadingOnly }
    [Tooltip("ReadingAndTarget: \"58.9 / 45 N\". TargetOnly: a locked pedestal that shows only the weight it needs and turns green " +
             "when reached, never what is on it. ReadingOnly: a test scale that just weighs (\"58.9 N\").")]
    [SerializeField] private Display display = Display.ReadingAndTarget;

    private PressurePlate plate;
    private float lastWeight;
    private float busyUntil = float.NegativeInfinity;

    protected override bool IsBusy => Time.time < busyUntil;

    private void Awake()
    {
        plate = GetComponent<PressurePlate>();
        if (belowPlate) offset = new Vector2(0f, -belowDistance);
    }

    private void Reset()
    {
        offset = new Vector2(0f, 0.7f); // used only when Below Plate is off
    }

    protected override void Refresh()
    {
        if (Mathf.Abs(plate.CurrentWeight - lastWeight) > 0.05f)
        {
            lastWeight = plate.CurrentWeight;
            busyUntil = Time.time + linger;
        }
        string text = display == Display.TargetOnly ? $"{plate.RequiredWeight:0.#} N"
                   : display == Display.ReadingOnly ? $"{plate.CurrentWeight:0.0} N"
                   : $"{plate.CurrentWeight:0.0} / {plate.RequiredWeight:0.#} N";
        SetText(text);
        SetColor(plate.IsPressed && display != Display.ReadingOnly ? pressedColor : textColor);
    }
}
