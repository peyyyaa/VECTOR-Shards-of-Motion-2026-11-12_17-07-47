using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A menu entry drawn as plain words (title screen): dimmed when not selected; when selected it brightens, slides a
/// little to the right and the shared marker (a small shard) moves next to it. Hovering with the mouse selects it,
/// so mouse, keyboard and gamepad all share one highlight. Put it on the Button's GameObject.
/// </summary>
public class TextMenuItem : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private RectTransform marker;
    [SerializeField] private Color normal = new Color(1f, 0.96f, 0.86f, 0.55f);
    [SerializeField] private Color selected = new Color(1f, 1f, 0.95f, 1f);
    [SerializeField] private float slide = 18f;

    private float t; // 0 = idle, 1 = selected (eased)
    private readonly Vector3[] corners = new Vector3[4];

    public void OnPointerEnter(PointerEventData e) => EventSystem.current?.SetSelectedGameObject(gameObject);

    private void Update()
    {
        bool isSelected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        t = Mathf.MoveTowards(t, isSelected ? 1f : 0f, Time.unscaledDeltaTime * 8f);
        label.color = Color.Lerp(normal, selected, t);
        label.rectTransform.anchoredPosition = new Vector2(slide * t, 0f);
        if (isSelected && marker != null)
        {
            RectTransform me = (RectTransform)transform;
            me.GetWorldCorners(corners);
            marker.gameObject.SetActive(true);
            marker.position = (corners[0] + corners[1]) * 0.5f; // left edge, vertically centred
            marker.anchoredPosition += new Vector2(-30f, 2f + Mathf.Sin(Time.unscaledTime * 4f) * 3f); // just left of the word, gentle bob
        }
    }

#if UNITY_EDITOR
    public void EditorSetup(TMP_Text text, RectTransform sharedMarker) { label = text; marker = sharedMarker; }
#endif
}
