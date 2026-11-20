using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// A Motion Journal stone: a small object in the world that players read with Interact.
/// Interact opens its entry in the journal. While open, the journal itself reads Interact
/// (tap = next page / close on the last page, hold = close). It also closes when both players walk away.
/// Either player can read or turn pages.
///
/// Wren's tip: if the entry has a Tip, then after the time set in Settings (Wren's tips) in this room the stone glows,
/// its prompt says "Tip!", and the tip is added as the journal's last page.
///
/// Setup: a Collider2D with Is Trigger ON, on the Interactable layer, plus a sprite as the in-world marker.
/// Assign a JournalEntry asset (preferred). Without one, Title and Body are shown as a single page.
/// Needs a UI_Canvas (UIRoot) in the scene.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class JournalStone : MonoBehaviour, IInteractable
{
    [Tooltip("The journal entry this stone opens (Assets > Create > Shards of Motion > Journal Entry).")]
    [SerializeField] private JournalEntry entry;
    [Header("Fallback (only used without an Entry)")]
    [SerializeField] private string title = "Journal";
    [SerializeField, TextArea(3, 10)] private string body = "Explanation...";
    [Tooltip("The journal closes when no player is within this distance (units).")]
    [SerializeField, Min(0.5f)] private float closeDistance = 3f;

    private PlayerMovement[] players = new PlayerMovement[0];
    private Room room;
    private RoomManager manager;
    private float timeInRoom;
    private bool tipReady;
    private Light2D tipGlow;
    private JournalEntry withTip;

    private bool HasTip => entry != null && !string.IsNullOrWhiteSpace(entry.tip);

    public string InteractVerb => !IsShowingMe ? (tipReady ? "Tip!" : "Read") : (Journal.HasNextPage ? "Next page" : "Close");

    private bool IsShowingMe
    {
        get
        {
            JournalPanel journal = Journal;
            return journal != null && journal.Source == this;
        }
    }

    private static JournalPanel Journal => UIRoot.Instance != null ? UIRoot.Instance.Journal : null;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        room = GetComponentInParent<Room>();
        manager = FindFirstObjectByType<RoomManager>();
    }

    public bool CanInteract(PlayerInteractor player) => isActiveAndEnabled && Journal != null;

    public void Interact(PlayerInteractor player)
    {
        JournalPanel journal = Journal;
        if (journal == null) return;

        if (journal.IsOpen) return; // while open, the journal reads Interact itself (tap = next page, hold = close)
        if (entry != null) journal.Open(this, RuntimeEntry());
        else journal.Show(this, title, body, "");
    }

    public bool HasEntry => entry != null;

    /// <summary>Opens this stone's entry from the pause menu ("re-read this room's journal"), wherever the players are.</summary>
    public void OpenFromMenu()
    {
        JournalPanel journal = Journal;
        if (journal == null || entry == null || journal.IsOpen) return;
        journal.Open(this, RuntimeEntry());
        openedFromMenu = true; // players may be anywhere: do not close because they are far from the stone
    }

    private bool openedFromMenu;

    // The entry as shown: its pages, then Wren's sketch of the room (a clue: how things connect), then his tip
    // once it's unlocked. Built at run time; the asset isn't changed.
    private JournalEntry RuntimeEntry()
    {
        bool withSketch = entry.sketch != null;
        if (!withSketch && !tipReady) return entry;
        if (withTip != null && builtWithTip == tipReady) return withTip;

        withTip = ScriptableObject.CreateInstance<JournalEntry>();
        withTip.title = entry.title;
        withTip.pages.AddRange(entry.pages);
        if (withSketch)
            withTip.pages.Add(new JournalPage
            {
                bigIdea = "Wren's sketch of this room",
                notes = "How things connect here, as far as I can tell. Arrows show what powers what; the numbers are the " +
                        "weight a plate needs. How to make it all happen is up to you two.",
                marginP1 = "A map! Sort of.",
                marginP2 = "It's a clue, not the answer.",
                sketch = entry.sketch,
                diagram = new ForceDiagram { show = false, caption = entry.sketchCaption },
            });
        if (tipReady)
            withTip.pages.Add(new JournalPage
            {
                bigIdea = "Wren's tip",
                notes = entry.tip,
                marginP1 = "Oh. OH.",
                marginP2 = "Told you we'd get there.",
                diagram = new ForceDiagram { show = false },
            });
        builtWithTip = tipReady;
        return withTip;
    }

    private bool builtWithTip;

    private void Update()
    {
        UpdateTip();
        if (!IsShowingMe) { openedFromMenu = false; return; }
        if (openedFromMenu) return;

        if (players.Length < 2) players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        float maxSqr = closeDistance * closeDistance;
        foreach (PlayerMovement p in players)
        {
            if (p != null && ((Vector2)(p.transform.position - transform.position)).sqrMagnitude <= maxSqr) return;
        }
        Journal.Hide(); // everyone walked away
    }

    private void UpdateTip()
    {
        if (!HasTip || manager == null || room == null) return;
        if (manager.CurrentRoom != room) { timeInRoom = 0f; return; }
        if (!tipReady)
        {
            float delay = SaveSystem.Data.settings.tipDelay; // Settings > Wren's tips (0 = off)
            if (delay <= 0f) return;
            timeInRoom += Time.deltaTime;
            if (timeInRoom < delay) return;
            tipReady = true;
            GameObject g = new GameObject("Tip Glow");
            g.transform.SetParent(transform, false);
            tipGlow = g.AddComponent<Light2D>();
            tipGlow.lightType = Light2D.LightType.Point;
            tipGlow.color = new Color(1f, 0.85f, 0.5f);
            tipGlow.pointLightOuterRadius = 1.8f;
            tipGlow.pointLightInnerRadius = 0.2f;
        }
        if (tipGlow != null) tipGlow.intensity = 0.6f + 0.4f * Mathf.Sin(Time.time * 4f); // gentle pulse
    }

    private void OnDisable()
    {
        if (IsShowingMe) Journal.Hide();
    }

#if UNITY_EDITOR
    public void EditorSetEntry(JournalEntry e) => entry = e;
#endif
}
