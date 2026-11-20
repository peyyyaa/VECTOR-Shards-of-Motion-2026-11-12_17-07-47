using UnityEngine;

/// <summary>
/// A villager players can talk to (Interact). Lines appear one at a time in a speech label above the NPC; Interact
/// shows the next line, and the talk ends after the last one or when both players walk away.
/// What they say depends on progress (from the save):
///   First Visit  - the very first conversation (then never again);
///   Parts Found  - once at least one machine part is found ({parts} is replaced by the number found);
///   Idle         - otherwise.
/// Opens Village Fund (Bram): after the first conversation, Interact opens the Village Fund ledger (FundPanel) instead,
/// with one of the Parts Found / Idle lines written at its top.
/// Setup: a trigger Collider2D on the Interactable layer, plus the NPC's sprite.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NpcTalk : MonoBehaviour, IInteractable
{
    [SerializeField] private string npcName = "Wren";
    [SerializeField] private Color nameColor = new Color(0.55f, 0.85f, 0.5f);
    [SerializeField, TextArea(2, 4)] private string[] firstVisit = new string[0];
    [SerializeField, TextArea(2, 4)] private string[] partsFound = new string[0];
    [SerializeField, TextArea(2, 4)] private string[] idle = new string[0];
    [SerializeField] private Vector2 bubbleOffset = new Vector2(0f, 2.7f); // above the interact prompt
    [SerializeField, Min(1f)] private float closeDistance = 3.5f;
    [Tooltip("Bram: after the first conversation, Interact opens the Village Fund ledger.")]
    [SerializeField] private bool opensVillageFund;

    private ScreenLabel bubble;
    private string[] lines = new string[0];
    private int line = -1;

    public string InteractVerb => OpensFundNow ? "Village Fund" : line < 0 ? "Talk" : (line < lines.Length - 1 ? "Next" : "Bye");

    // Not on the frame the ledger closed: the press that closed it must not open it again.
    public bool CanInteract(PlayerInteractor player) => isActiveAndEnabled && Time.frameCount > FundPanel.ClosedFrame + 1;

    private static FundPanel Fund => UIRoot.Instance != null ? UIRoot.Instance.Fund : null;

    // Bram, not mid-conversation, already met: the ledger instead of talk.
    private bool OpensFundNow => opensVillageFund && line < 0 && Fund != null && SaveSystem.HasEvent("met_" + npcName);

    public void Interact(PlayerInteractor player)
    {
        if (OpensFundNow)
        {
            string[] said = ChooseLines();
            Fund.Open(npcName, nameColor, said.Length > 0 ? said[Random.Range(0, said.Length)] : "");
            return;
        }
        if (line < 0)
        {
            lines = ChooseLines();
            if (lines.Length == 0) return;
            line = 0;
        }
        else line++;

        if (line >= lines.Length) Close();
        else Show(lines[line]);
    }

    private string[] ChooseLines()
    {
        string met = "met_" + npcName;
        if (!SaveSystem.HasEvent(met) && firstVisit.Length > 0)
        {
            SaveSystem.MarkEvent(met);
            return firstVisit;
        }
        int parts = QuestItem.Found.Count;
        if (parts > 0 && partsFound.Length > 0)
            return System.Array.ConvertAll(partsFound, l => l.Replace("{parts}", parts.ToString()));
        return idle;
    }

    private void Show(string text)
    {
        if (bubble == null) bubble = WorldText.CreatePaper($"{name} (speech)", 32f, 7f);
        if (bubble == null) return;
        bubble.SetText($"<color=#{ColorUtility.ToHtmlStringRGB(nameColor)}>{npcName}</color>\n{text}");
        bubble.WorldPosition = transform.position + (Vector3)bubbleOffset;
    }

    private void Update()
    {
        if (line < 0 || bubble == null) return;
        bubble.WorldPosition = transform.position + (Vector3)bubbleOffset;
        float maxSqr = closeDistance * closeDistance;
        foreach (PlayerMovement p in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
            if (((Vector2)(p.transform.position - transform.position)).sqrMagnitude <= maxSqr) return;
        Close(); // everyone walked away
    }

    private void Close()
    {
        line = -1;
        if (bubble != null) Destroy(bubble.gameObject);
        bubble = null;
    }

    private void OnDisable() => Close();

#if UNITY_EDITOR
    public void EditorSetup(string who, Color color, string[] first, string[] parts, string[] idleLines)
    {
        npcName = who; nameColor = color; firstVisit = first; partsFound = parts; idle = idleLines;
    }

    public void EditorOpensVillageFund(bool on) => opensVillageFund = on;
#endif
}
