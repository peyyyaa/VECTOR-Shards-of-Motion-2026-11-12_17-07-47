using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One room on the map screen: a ring marker (a Button), the room's name, and a small row with its progress
/// (shards x/y, journal read, machine part found). Reached rooms are inked and can be selected; unreached rooms are
/// faint (the Button's disabled colour), unnamed and skipped by keyboard/gamepad navigation. The furthest room
/// reached gets a red pin. The selected marker gently pulses. Built by Tools > Shards of Motion > Create UI Prefabs.
/// </summary>
public class MapMarker : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    [SerializeField] private Button button;
    [SerializeField] private GameObject pin;
    [SerializeField] private GameObject details;     // name + progress row
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text shardText;
    [SerializeField] private Image journalIcon;
    [SerializeField] private Image partIcon;
    [Tooltip("Small check shown once the room is solved (left through its exit into the next room).")]
    [SerializeField] private GameObject solvedMark;

    private const float Dim = 0.3f; // icons for things not found / not read yet
    private MapPanel panel;
    private int index;

    public Button Button => button;

    public void Init(MapPanel owner, int roomIndex)
    {
        panel = owner;
        index = roomIndex;
        button.onClick.AddListener(() => panel.Travel(index));
    }

    /// <summary>Shows this room's state from the save.</summary>
    public void Show(RegionMapData map, bool furthest)
    {
        RegionMapData.RoomInfo room = map.rooms[index];
        bool reached = map.IsReached(index);
        button.interactable = reached;
        pin.SetActive(furthest);
        details.SetActive(reached);
        if (solvedMark != null) solvedMark.SetActive(reached && map.IsSolved(index));
        if (!reached) return;

        nameText.text = room.displayName;
        shardText.text = $"{Mathf.Min(map.ShardsFound(index), room.shards)}/{room.shards}";
        journalIcon.gameObject.SetActive(map.HasJournal(index));
        journalIcon.color = new Color(1f, 1f, 1f, map.JournalRead(index) ? 1f : Dim);
        partIcon.gameObject.SetActive(map.HasPart(index));
        partIcon.color = new Color(1f, 1f, 1f, map.PartFound(index) ? 1f : Dim);
    }

    public void OnSelect(BaseEventData eventData) => panel.Describe(index);

    // Mouse: hovering a reached room selects it (so its details show), like moving there with the keys.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable) EventSystem.current?.SetSelectedGameObject(gameObject);
    }

    private void Update()
    {
        bool selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        float s = selected ? 1.12f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f) : 1f; // the game may be paused: unscaled time
        button.transform.localScale = new Vector3(s, s, 1f);
    }

#if UNITY_EDITOR
    public void EditorSetup(Button b, GameObject pinObject, GameObject detailsObject, TMP_Text roomName, TMP_Text shards, Image journal, Image part)
    {
        button = b; pin = pinObject; details = detailsObject; nameText = roomName; shardText = shards; journalIcon = journal; partIcon = part;
    }

    public void EditorSetupSolved(GameObject mark) => solvedMark = mark;
#endif
}
