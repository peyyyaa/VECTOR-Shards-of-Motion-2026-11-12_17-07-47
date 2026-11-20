using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Gently bobs this object up and down (pixel-snapped), and optionally pulses a light in step: the shard shrine's crystal.
/// Purely visual.
/// </summary>
public class Hover : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.12f;
    [SerializeField] private float speed = 1.6f;
    [SerializeField] private Light2D pulseLight;

    private Vector3 home;
    private float lightBase;

    private void OnEnable()
    {
        home = transform.localPosition;
        if (pulseLight != null) lightBase = pulseLight.intensity;
    }

    private void OnDisable()
    {
        transform.localPosition = home;
        if (pulseLight != null) pulseLight.intensity = lightBase;
    }

    private void Update()
    {
        float s = Mathf.Sin(Time.time * speed);
        Vector3 p = transform.parent != null ? transform.parent.TransformPoint(home) : home;
        transform.position = Fx.Snap(p + Vector3.up * (s * amplitude));
        if (pulseLight != null) pulseLight.intensity = lightBase * (0.85f + 0.15f * s);
    }

#if UNITY_EDITOR
    public void EditorSetup(Light2D light) => pulseLight = light;
#endif
}
