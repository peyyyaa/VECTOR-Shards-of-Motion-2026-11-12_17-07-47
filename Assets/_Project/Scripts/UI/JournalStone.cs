using UnityEngine;

/// <summary>
/// A Motion Journal stone: a small object in the world that players read with Interact.
/// Interact opens its entry in the journal. While open, the journal itself reads Interact
/// (tap = next page / close on the last page, hold = close). It also closes when both players walk away.
/// Either player can read or turn pages.
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

    public string InteractVerb => !IsShowingMe ? "Read" : (Journal.HasNextPage ? "Next page" : "Close");

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

    public bool CanInteract(PlayerInteractor player) => isActiveAndEnabled && Journal != null;

    public void Interact(PlayerInteractor player)
    {
        JournalPanel journal = Journal;
        if (journal == null) return;

        if (journal.IsOpen) return; // while open, the journal reads Interact itself (tap = next page, hold = close)
        if (entry != null) journal.Open(this, entry);
        else journal.Show(this, title, body, "");
    }

    private void Update()
    {
        if (!IsShowingMe) return;

        if (players.Length < 2) players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        float maxSqr = closeDistance * closeDistance;
        foreach (PlayerMovement p in players)
        {
            if (p != null && ((Vector2)(p.transform.position - transform.position)).sqrMagnitude <= maxSqr) return;
        }
        Journal.Hide(); // everyone walked away
    }

    private void OnDisable()
    {
        if (IsShowingMe) Journal.Hide();
    }

#if UNITY_EDITOR
    public void EditorSetEntry(JournalEntry e) => entry = e;
#endif
}
