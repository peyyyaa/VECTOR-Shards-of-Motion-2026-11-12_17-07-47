using System;
using System.Text;
using UnityEngine;

/// <summary>
/// Wren's Field Requests: small goals in the regions, read from the save (no inventory, no rewards yet).
/// A request is done once its progress reaches its target; it's then recorded in the save (questsDone).
/// Add requests to the list below.
/// </summary>
public static class FieldRequests
{
    public struct Request
    {
        public string id;
        public string title;
        public Func<int> progress;
        public int target;
    }

    public static readonly Request[] All =
    {
        new Request { id = "reach_aqueduct", title = "Reach the old aqueduct (Shattered Grounds, room 4)", target = 4,
                      progress = () => SaveSystem.Data.region == "Region1" ? Mathf.Min(SaveSystem.Data.furthestRoom, 4) : 0 },
        new Request { id = "parts_2", title = "Recover 2 machine parts", target = 2, progress = () => SaveSystem.Data.questItems.Count },
        new Request { id = "shards_6", title = "Collect 6 Motion Shards", target = 6, progress = () => SaveSystem.Data.shards.Count },
        new Request { id = "rubble", title = "Smash through a rubble wall", target = 1, progress = () => SaveSystem.HasEvent("wall_broken") ? 1 : 0 },
    };

    /// <summary>The board's text, e.g. "[x] Recover 2 machine parts" / "[ ] Collect 6 Motion Shards  (3/6)".</summary>
    public static string Describe()
    {
        StringBuilder sb = new StringBuilder("<b>Field Requests</b>   <size=80%>- W. Alder</size>\n");
        foreach (Request r in All)
        {
            int have = Mathf.Min(r.progress(), r.target);
            bool done = have >= r.target;
            if (done) SaveSystem.CompleteQuest(r.id);
            sb.Append(done ? "<color=#3e6230>[x]</color> " : "[ ] ").Append(r.title);
            if (!done && r.target > 1) sb.Append($"  ({have}/{r.target})");
            sb.Append('\n');
        }
        return sb.ToString().TrimEnd();
    }
}

/// <summary>
/// The village notice board: Interact to read Wren's Field Requests (live progress); Interact again, or walk away, to close.
/// Setup: a trigger Collider2D on the Interactable layer, plus the board's sprite.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RequestBoard : MonoBehaviour, IInteractable
{
    [SerializeField] private Vector2 offset = new Vector2(0f, 3.6f); // above the interact prompt
    [SerializeField, Min(1f)] private float closeDistance = 3.5f;

    private ScreenLabel label;

    public string InteractVerb => label == null ? "Read" : "Close";

    public bool CanInteract(PlayerInteractor player) => isActiveAndEnabled;

    public void Interact(PlayerInteractor player)
    {
        if (label != null) { Close(); return; }
        label = WorldText.CreatePaper("Field Requests", 30f, 9f);
        Refresh();
    }

    private void Update()
    {
        if (label == null) return;
        Refresh();
        float maxSqr = closeDistance * closeDistance;
        foreach (PlayerMovement p in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
            if (((Vector2)(p.transform.position - transform.position)).sqrMagnitude <= maxSqr) return;
        Close();
    }

    private void Refresh()
    {
        if (label == null) return;
        label.SetText(FieldRequests.Describe());
        label.WorldPosition = transform.position + (Vector3)offset;
    }

    private void Close()
    {
        if (label != null) Destroy(label.gameObject);
        label = null;
    }

    private void OnDisable() => Close();
}
