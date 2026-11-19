using UnityEngine;

/// <summary>
/// Plays a list of sprites in a loop on this object's SpriteRenderer at a fixed frame rate.
/// Used by the character preview scene to show an animation; the real player animator
/// (which picks the animation from the player's movement state) comes later.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFlipbook : MonoBehaviour
{
    [SerializeField] private Sprite[] frames = new Sprite[0];
    [SerializeField, Min(1f)] private float framesPerSecond = 10f;

    private SpriteRenderer sr;
    private float time;

    private void Awake() => sr = GetComponent<SpriteRenderer>();

    private void Update()
    {
        if (frames.Length == 0) return;
        time += Time.deltaTime;
        sr.sprite = frames[(int)(time * framesPerSecond) % frames.Length];
    }

#if UNITY_EDITOR
    public void EditorSetup(Sprite[] sprites, float fps)
    {
        frames = sprites;
        framesPerSecond = fps;
        if (sprites.Length > 0) GetComponent<SpriteRenderer>().sprite = sprites[0];
    }
#endif
}
