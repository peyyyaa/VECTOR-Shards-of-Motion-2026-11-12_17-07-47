using UnityEngine;

/// <summary>
/// The Winch Hammer's ratchet (finale, part 2), made visible: a toothed wheel on the hammer's winding drum and a pawl
/// that drops into its teeth. As P2 winds, the wheel clicks round one tooth for every Height Per Tooth the hammer rises,
/// and the pawl snaps into each new tooth; that is what holds the hammer when P2 lets the cord go. When P1 releases the
/// hammer, the pawl lifts and the drum spins back freely as the chain pays out.
/// Purely visual: reads WinchHammer.Height and Swinging (the ratchet itself is in WinchHammer).
/// Setup: the hammer; the wheel and pawl transforms (pivots at their centres of rotation).
/// </summary>
public class WinchRatchet : MonoBehaviour
{
    [SerializeField] private WinchHammer hammer;
    [SerializeField] private Transform wheel;
    [SerializeField] private Transform pawl;
    [SerializeField, Min(3)] private int teeth = 12;
    [Tooltip("Hammer height per tooth (m): one click.")]
    [SerializeField, Min(0.01f)] private float heightPerTooth = 0.1f;
    [Tooltip("The pawl's angle while it rests in a tooth (degrees).")]
    [SerializeField] private float pawlRest = 0f;
    [Tooltip("How far the pawl lifts when the hammer is released (degrees).")]
    [SerializeField] private float pawlLift = 28f;

    private int shownTooth;
    private float wheelAngle, kick;

    private void LateUpdate()
    {
        if (hammer == null) return;
        float dt = Time.deltaTime;
        float step = 360f / teeth;
        if (hammer.Swinging)
        {
            shownTooth = 0;
            wheelAngle = Mathf.MoveTowards(wheelAngle, 0f, 900f * dt); // unwinds freely
        }
        else
        {
            int tooth = Mathf.FloorToInt(hammer.Height / heightPerTooth + 0.001f);
            if (tooth > shownTooth) kick = 1f;                          // a click: the pawl drops into the next tooth
            shownTooth = tooth;
            wheelAngle = -tooth * step;                                  // winding turns it clockwise, a tooth at a time
        }
        kick = Mathf.MoveTowards(kick, 0f, 7f * dt);
        if (wheel != null) wheel.localRotation = Quaternion.Euler(0f, 0f, wheelAngle);
        if (pawl != null)
        {
            float a = hammer.Swinging ? pawlLift : pawlRest + Mathf.Sin(kick * Mathf.PI) * 14f; // snaps up and back down
            pawl.localRotation = Quaternion.Euler(0f, 0f, a);
        }
    }

#if UNITY_EDITOR
    public void EditorSetup(WinchHammer target, Transform wheelTransform, Transform pawlTransform)
    {
        hammer = target; wheel = wheelTransform; pawl = pawlTransform;
    }
#endif
}
