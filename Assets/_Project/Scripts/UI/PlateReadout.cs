using UnityEngine;

/// <summary>
/// Shows a pressure plate's measured weight against its threshold, e.g. "58.9 / 45 N",
/// turning green while pressed. Teaches W = m * g (and that stacked weights add up).
/// </summary>
[RequireComponent(typeof(PressurePlate))]
public class PlateReadout : FloatingLabel
{
    [SerializeField] private Color pressedColor = new Color(0.4f, 1f, 0.45f);

    private PressurePlate plate;

    private void Awake()
    {
        plate = GetComponent<PressurePlate>();
    }

    private void Reset()
    {
        offset = new Vector2(0f, 0.7f); // default when added in the Inspector
    }

    protected override void Refresh()
    {
        SetText($"{plate.CurrentWeight:0.0} / {plate.RequiredWeight:0.#} N");
        SetColor(plate.IsPressed ? pressedColor : textColor);
    }
}
