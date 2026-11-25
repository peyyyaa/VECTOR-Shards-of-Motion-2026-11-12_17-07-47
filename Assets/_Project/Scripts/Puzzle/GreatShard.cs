using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The Great Shard (the region's last room). With Pedestals set (the finale): it frees itself while BOTH pedestals are
/// held by the two players at once (stand on one each and hold Interact) for Hold Seconds; letting go drains the progress.
/// Without pedestals: Interact with both players beside it (the older setup).
/// When freed: time slows for a moment, the shard flares and rises, the region is saved as complete (event
/// "region1_complete", the region's last room as solved), then the end-of-region sequence plays (RegionFinale: a card
/// for every room and what it taught, Wren's words) and the game returns to the village.
/// It bobs and glows meanwhile. Setup: a trigger Collider2D on the Interactable layer, a SpriteRenderer, a Light2D.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GreatShard : MonoBehaviour, IInteractable
{
    [SerializeField] private string regionEvent = "region1_complete";
    [SerializeField] private string roomObject = "R1_Room08";
    [Tooltip("Both players must be within this distance (m) (when there are no pedestals).")]
    [SerializeField, Min(0.5f)] private float togetherDistance = 2.5f;
    [SerializeField] private Light2D glow;
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField, Min(0f)] private float leaveAfter = 2.5f;

    [Header("Finale")]
    [SerializeField] private ShardPedestal[] pedestals = new ShardPedestal[0];
    [SerializeField, Min(0.1f)] private float holdSeconds = 3f;
    [Tooltip("Sprite shown once freed (the healed shard). Empty = keep the sprite.")]
    [SerializeField] private Sprite healedSprite;
    [SerializeField] private RegionMapData map;
    [Tooltip("One line per room (same order as the map's rooms): what it taught.")]
    [SerializeField, TextArea] private string[] lessons = new string[0];
    [SerializeField, TextArea] private string closing = "";

    private Vector3 home;
    private bool freed;
    private float freedTimer;
    private float progress;
    private ScreenLabel label;

    public string InteractVerb => freed ? "" : (pedestals.Length > 0 ? "" : "Free the Great Shard");

    private void Start() => home = transform.position;

    public bool CanInteract(PlayerInteractor player) => isActiveAndEnabled && !freed;

    public void Interact(PlayerInteractor player)
    {
        if (freed) return;
        if (pedestals.Length > 0)
        {
            ShowLabel("Stand on the two pedestals\nand hold Interact together.", new Color(1f, 0.9f, 0.6f), 3f);
            return;
        }
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (PlayerMovement p in players)
            if (((Vector2)(p.transform.position - transform.position)).sqrMagnitude > togetherDistance * togetherDistance)
            {
                ShowLabel("Together!", new Color(1f, 0.9f, 0.6f), 2f);
                return;
            }
        Free();
    }

    private void Free()
    {
        freed = true;
        progress = 1f;
        if (label != null) { Destroy(label.gameObject); label = null; }
        SaveSystem.MarkEvent(regionEvent);
        SaveSystem.MarkEvent(RoomManager.SolvedEventId(roomObject));
        if (healedSprite != null && TryGetComponent(out SpriteRenderer sr)) sr.sprite = healedSprite;
        Fx.Flash(transform.position, new Color(0.6f, 0.95f, 1f), 7f, 3f, 1.2f);
        Fx.Flash(transform.position, Color.white, 16f, 2.5f, 2.5f);
        FxCameraKick.Kick(new Vector2(0f, -0.2f), 0.8f);
        Time.timeScale = 0.35f; // a held breath; back to normal in Update
        ShowLabel("The Great Shard is free!\nThe Shattered Grounds are restored.", new Color(0.75f, 0.97f, 1f), 0f);
    }

    private void Update()
    {
        float t = Time.time;
        if (!freed && pedestals.Length > 0)
        {
            PlayerInputReader a = null, b = null;
            foreach (ShardPedestal p in pedestals)
            {
                PlayerInputReader h = p != null ? p.Holder : null;
                if (h == null) continue;
                if (a == null) a = h; else if (h != a) b = h;
            }
            bool together = a != null && b != null;
            progress = Mathf.Clamp01(progress + (together ? 1f / holdSeconds : -0.6f) * Time.deltaTime);
            if (progress >= 1f) Free();
        }
        foreach (ShardPedestal p in pedestals) if (p != null) p.Show(progress, freed);

        float rise = freed ? Mathf.Min(1.5f, freedTimer * 0.8f) : progress * 0.4f;
        transform.position = home + Vector3.up * (rise + Mathf.Sin(t * 1.6f) * bobHeight * (freed ? 2f : 1f + progress * 2f));
        if (glow != null) glow.intensity = (freed ? 3.5f : 1.2f + 2f * progress) + 0.3f * Mathf.Sin(t * 3f);
        if (label != null) label.WorldPosition = home + Vector3.up * 2.6f;
        if (!freed) return;

        freedTimer += Time.unscaledDeltaTime;
        if (Time.timeScale < 1f && freedTimer > 1.2f && !MenuFreeze.Active) Time.timeScale = 1f;
        if (freedTimer >= leaveAfter && freedTimer - Time.unscaledDeltaTime < leaveAfter)
        {
            Time.timeScale = 1f;
            if (map != null && lessons.Length > 0) RegionFinale.Play(map, lessons, closing);
            else GameFlow.ToVillage(GameFlow.VillageLabScreen); // home to Wren's lab (the homecoming)
        }
    }

    private void ShowLabel(string text, Color color, float life)
    {
        if (label == null) label = WorldText.Create("Great Shard label", null, 34f, color, 9f);
        if (label == null) return;
        label.Text.color = color;
        label.SetText(text);
        if (life > 0f) Destroy(label.gameObject, life);
    }

#if UNITY_EDITOR
    public void EditorSetup(Light2D light, string room) { glow = light; roomObject = room; }

    public void EditorSetupFinale(ShardPedestal[] pads, float hold, Sprite healed, RegionMapData regionMap, string[] roomLessons, string closingWords)
    {
        pedestals = pads; holdSeconds = hold; healedSprite = healed; map = regionMap; lessons = roomLessons; closing = closingWords;
    }
#endif
}
