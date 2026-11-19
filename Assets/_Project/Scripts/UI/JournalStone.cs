using UnityEngine;

/// <summary>
/// A Motion Journal stone: a small object in the world that players read with Interact.
/// Pressing Interact opens its entry in the Journal panel at the bottom of the screen;
/// pressing again (either player) closes it. It also closes when both players walk away.
///
/// Setup: a Collider2D with Is Trigger ON, on the Interactable layer (that's how players find it),
/// plus a sprite as the in-world marker. Needs a UI_Canvas (UIRoot) in the scene.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class JournalStone : MonoBehaviour, IInteractable
{
    [SerializeField] private string title = "Journal";
    [SerializeField, TextArea(3, 10)] private string body = "Explanation...";
    [Tooltip("The entry closes when no player is within this distance (units).")]
    [SerializeField, Min(0.5f)] private float closeDistance = 3f;

    private PlayerMovement[] players = new PlayerMovement[0];

    public string InteractVerb => IsShowingMe ? "Close" : "Read";

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

        if (journal.Source == this) journal.Hide();
        else journal.Show(this, title, body, "Interact again to close");
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
}
