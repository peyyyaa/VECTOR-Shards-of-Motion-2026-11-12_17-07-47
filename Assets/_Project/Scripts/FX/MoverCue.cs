using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Blinks a light on a Mover while it's about to move (its Signal Delay is counting down), e.g. a ferry
/// that's been loaded and will sail in a moment: "hop on now". Off otherwise. Purely visual.
/// Added by RoomDresser to movers that have a Signal Delay.
/// </summary>
public class MoverCue : MonoBehaviour
{
    [SerializeField] private Mover mover;
    [SerializeField] private Light2D cueLight;
    [SerializeField] private float blinkRate = 6f;
    [SerializeField] private float intensity = 1.4f;

    private void Update()
    {
        if (mover == null || cueLight == null) return;
        bool blinking = mover.IsAboutToMove;
        cueLight.enabled = blinking;
        if (blinking) cueLight.intensity = Mathf.Sin(Time.time * blinkRate) > 0f ? intensity : intensity * 0.25f;
    }

#if UNITY_EDITOR
    public void EditorSetup(Mover m, Light2D l) { mover = m; cueLight = l; }
#endif
}
