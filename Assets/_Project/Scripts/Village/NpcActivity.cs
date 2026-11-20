using UnityEngine;

/// <summary>
/// A villager's animation: loops the Idle frames, and every few seconds plays the Act frames once (Wren turns a page,
/// Bram hammers, Mira waters), then goes back to idling. Purely visual; NpcTalk handles talking.
/// Setup: on the NPC's SpriteRenderer object; frames come from the character's CharacterAnimationSet (Idle / Act).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class NpcActivity : MonoBehaviour
{
    [SerializeField] private Sprite[] idle = new Sprite[0];
    [SerializeField, Min(1f)] private float idleFps = 4f;
    [SerializeField] private Sprite[] act = new Sprite[0];
    [SerializeField, Min(1f)] private float actFps = 6f;
    [Tooltip("Seconds between actions (random in this range).")]
    [SerializeField] private Vector2 actEvery = new Vector2(4f, 9f);

    private SpriteRenderer sr;
    private float time, nextAct;
    private bool acting;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        nextAct = Random.Range(1f, actEvery.y);
    }

    private void Update()
    {
        time += Time.deltaTime;
        if (acting)
        {
            int f = (int)(time * actFps);
            if (f < act.Length) { sr.sprite = act[f]; return; }
            acting = false;
            time = 0f;
            nextAct = Random.Range(actEvery.x, actEvery.y);
        }
        if (idle.Length > 0) sr.sprite = idle[(int)(time * idleFps) % idle.Length];
        nextAct -= Time.deltaTime;
        if (nextAct <= 0f && act.Length > 0) { acting = true; time = 0f; }
    }

#if UNITY_EDITOR
    public void EditorSetup(Sprite[] idleFrames, float idleRate, Sprite[] actFrames, float actRate)
    {
        idle = idleFrames; idleFps = idleRate; act = actFrames; actFps = actRate;
        if (idleFrames.Length > 0) GetComponent<SpriteRenderer>().sprite = idleFrames[0];
    }
#endif
}
