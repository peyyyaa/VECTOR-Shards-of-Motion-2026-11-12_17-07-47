using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The Great Shard (Room 8, the end of the region). Interact with BOTH players beside it to free it: it flares, a banner
/// reads that the region is restored, the region is saved as complete (event "region1_complete", the room as solved), and
/// after a moment the game returns to the village (the transition card plays). With one player alone it asks for the other.
/// It bobs and glows meanwhile. Setup: a trigger Collider2D on the Interactable layer, a SpriteRenderer, a Light2D.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GreatShard : MonoBehaviour, IInteractable
{
    [SerializeField] private string regionEvent = "region1_complete";
    [SerializeField] private string roomObject = "R1_Room08";
    [Tooltip("Both players must be within this distance (m).")]
    [SerializeField, Min(0.5f)] private float togetherDistance = 2.5f;
    [SerializeField] private Light2D glow;
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField, Min(0f)] private float leaveAfter = 2.5f;

    private Vector3 home;
    private bool freed;
    private float freedTimer;
    private ScreenLabel label;

    public string InteractVerb => freed ? "" : "Free the Great Shard";

    private void Start() => home = transform.position;

    public bool CanInteract(PlayerInteractor player) => isActiveAndEnabled && !freed;

    public void Interact(PlayerInteractor player)
    {
        if (freed) return;
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (PlayerMovement p in players)
            if (((Vector2)(p.transform.position - transform.position)).sqrMagnitude > togetherDistance * togetherDistance)
            {
                ShowLabel("Together!", new Color(1f, 0.9f, 0.6f));
                return;
            }
        freed = true;
        if (label != null) { Destroy(label.gameObject); label = null; } // a fading "Together!" hint: start a fresh banner
        SaveSystem.MarkEvent(regionEvent);
        SaveSystem.MarkEvent(RoomManager.SolvedEventId(roomObject));
        Fx.Flash(transform.position, new Color(0.6f, 0.95f, 1f), 7f, 3f, 1.2f);
        ShowLabel("The Great Shard is free!\nThe Shattered Grounds are restored.", new Color(0.75f, 0.97f, 1f));
    }

    private void Update()
    {
        float t = Time.time;
        transform.position = home + Vector3.up * (Mathf.Sin(t * 1.6f) * bobHeight * (freed ? 2f : 1f));
        if (glow != null) glow.intensity = (freed ? 3f : 1.2f) + 0.3f * Mathf.Sin(t * 3f);
        if (label != null) label.WorldPosition = home + Vector3.up * 2.2f;
        if (!freed) return;
        freedTimer += Time.deltaTime;
        if (freedTimer >= leaveAfter && freedTimer - Time.deltaTime < leaveAfter) GameFlow.ToVillage();
    }

    private void ShowLabel(string text, Color color)
    {
        if (label == null) label = WorldText.Create("Great Shard label", null, 34f, color, 9f);
        if (label == null) return;
        label.Text.color = color;
        label.SetText(text);
        if (!freed) Destroy(label.gameObject, 2f);
    }

#if UNITY_EDITOR
    public void EditorSetup(Light2D light, string room) { glow = light; roomObject = room; }
#endif
}
