using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Blends the scene's global light toward the current room's RoomMood (or the light's starting colour and brightness in
/// rooms without one). Lives next to RoomManager. Purely visual.
/// </summary>
public class MoodDirector : MonoBehaviour
{
    [SerializeField] private RoomManager rooms;
    [SerializeField] private Light2D globalLight;
    [Tooltip("How fast the light changes (per second, 0..1 of the way).")]
    [SerializeField, Min(0.1f)] private float blendSpeed = 2f;

    private Color baseColor;
    private float baseIntensity;

    private void Start()
    {
        if (globalLight == null) return;
        baseColor = globalLight.color;
        baseIntensity = globalLight.intensity;
    }

    private void Update()
    {
        if (globalLight == null || rooms == null) return;
        Room current = rooms.CurrentRoom;
        RoomMood mood = current != null ? current.GetComponent<RoomMood>() : null;
        Color c = mood != null ? mood.LightColor : baseColor;
        float i = mood != null ? mood.Intensity : baseIntensity;
        float k = 1f - Mathf.Exp(-blendSpeed * Time.deltaTime);
        globalLight.color = Color.Lerp(globalLight.color, c, k);
        globalLight.intensity = Mathf.Lerp(globalLight.intensity, i, k);
    }

#if UNITY_EDITOR
    public void EditorSetup(RoomManager manager, Light2D light) { rooms = manager; globalLight = light; }
#endif
}
