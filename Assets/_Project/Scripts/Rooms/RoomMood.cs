using UnityEngine;

/// <summary>
/// A room's light mood (on the room's root, next to Room): the colour and brightness of the scene's global light while
/// players are in this room. Rooms without one use the scene's normal light. MoodDirector blends between rooms as the
/// camera moves, so going deeper into the forest gets visibly darker. Purely visual.
/// </summary>
[RequireComponent(typeof(Room))]
public class RoomMood : MonoBehaviour
{
    [SerializeField] private Color lightColor = Color.white;
    [SerializeField, Range(0f, 2f)] private float intensity = 1f;

    public Color LightColor => lightColor;
    public float Intensity => intensity;

#if UNITY_EDITOR
    public void EditorSetup(Color color, float brightness) { lightColor = color; intensity = brightness; }
#endif
}
