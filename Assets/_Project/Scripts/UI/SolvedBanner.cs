using TMPro;
using UnityEngine;

/// <summary>
/// The "room solved" moment (part of UI_Canvas). When the players first leave a room through its exit into the next
/// one (RoomManager.RoomSolved), a journal ribbon slides down from the top of the screen for a few seconds:
///   Solved!  3 · Together We Move
///   the room's physics takeaway (its main journal entry's first Big Idea)
///   All shards found!  /  Shards 2/3: 1 still hidden in that room
/// and a gold flash goes off at both players. It never pauses the game or takes input, and sits at the very top so it
/// covers as little of the room as possible. Room names and journal entries come from the RegionMapData.
/// </summary>
public class SolvedBanner : MonoBehaviour
{
    [SerializeField] private RegionMapData map;
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text idea;
    [SerializeField] private TMP_Text shards;
    [SerializeField, Min(0.5f)] private float holdTime = 3.5f;
    [SerializeField, Min(0.05f)] private float slideTime = 0.3f;

    private const float ShownY = -16f, HiddenY = 230f;
    private static readonly Color Gold = new Color(1f, 0.85f, 0.45f);
    private float timer = -1f;
    private bool flashPending;

    private void OnEnable()
    {
        RoomManager.RoomSolved += Show;
        Hide();
    }

    private void OnDisable() => RoomManager.RoomSolved -= Show;

    private void Show(Room room)
    {
        int i = map != null && room != null ? map.IndexOf(room.name) : -1;
        if (i < 0)
        {
            title.text = "Solved!";
            idea.text = "";
            shards.text = "";
        }
        else
        {
            RegionMapData.RoomInfo info = map.rooms[i];
            title.text = $"Solved!   {i + 1} · {info.displayName}";
            idea.text = BigIdea(info.journal);
            int found = Mathf.Min(map.ShardsFound(i), info.shards), missing = info.shards - found;
            shards.text = info.shards == 0 ? "" : missing == 0 ? "All shards found!"
                : $"Shards {found}/{info.shards}:  {missing} still hidden in that room";
        }
        timer = 0f;
        flashPending = true; // next frame, once the players stand in the new room
    }

    private static string BigIdea(JournalEntry entry)
    {
        if (entry == null) return "";
        foreach (JournalPage page in entry.pages)
            if (page != null && !string.IsNullOrWhiteSpace(page.bigIdea)) return page.bigIdea;
        return "";
    }

    private void Update()
    {
        if (timer < 0f) return;
        if (flashPending)
        {
            flashPending = false;
            foreach (PlayerMovement p in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
                Fx.Flash(p.transform.position, Gold, 2.5f, 1.6f, 0.5f);
        }
        timer += Time.unscaledDeltaTime;
        float into = Mathf.Clamp01(timer / slideTime);
        float outOf = Mathf.Clamp01((timer - slideTime - holdTime) / slideTime);
        float k = Mathf.SmoothStep(0f, 1f, into) * (1f - Mathf.SmoothStep(0f, 1f, outOf));
        panel.anchoredPosition = new Vector2(0f, Mathf.Lerp(HiddenY, ShownY, k));
        group.alpha = k;
        if (outOf >= 1f) Hide();
    }

    private void Hide()
    {
        timer = -1f;
        group.alpha = 0f;
        panel.anchoredPosition = new Vector2(0f, HiddenY);
    }

#if UNITY_EDITOR
    public void EditorSetup(RegionMapData data, RectTransform panelRect, CanvasGroup canvasGroup, TMP_Text titleText, TMP_Text ideaText, TMP_Text shardText)
    {
        map = data; panel = panelRect; group = canvasGroup; title = titleText; idea = ideaText; shards = shardText;
    }
#endif
}
