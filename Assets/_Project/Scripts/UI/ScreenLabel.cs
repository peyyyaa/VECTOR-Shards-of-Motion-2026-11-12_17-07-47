using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A label (panel + text) on the UI canvas that follows a point in the world. Lives on the
/// Label prefab (restyle that prefab to change every label). Created by WorldText.Create.
/// Set WorldPosition (every frame for moving things), change the text with SetText (the panel
/// resizes to fit), and fade it with Alpha.
///
/// Runs late (after the room camera moves) so labels don't lag during camera slides.
/// Labels belonging to other rooms end up off-screen, so they're simply not visible.
/// </summary>
[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(CanvasGroup))]
public class ScreenLabel : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private Image background;
    [Tooltip("Space between the text and the panel edge, in 1080p pixels.")]
    [SerializeField] private Vector2 padding = new Vector2(14f, 6f);

    public TMP_Text Text => text;

    /// <summary>Where the label should appear, in world coordinates.</summary>
    public Vector3 WorldPosition { get; set; }

    /// <summary>0 = invisible, 1 = fully visible.</summary>
    public float Alpha
    {
        get => Group.alpha;
        set => Group.alpha = value;
    }

    /// <summary>Longest line before wrapping, in world units.</summary>
    public float WidthUnits { get; set; } = 6f;

    private CanvasGroup group;
    private CanvasGroup Group => group != null ? group : (group = GetComponent<CanvasGroup>());
    private float lastMaxWidth = -1f;
    private bool sizeDirty = true;

    /// <summary>Used when a label is built in code (no Label prefab in the scene).</summary>
    public void Init(TMP_Text label, Image panel)
    {
        text = label;
        background = panel;
    }

    public void SetText(string value)
    {
        if (text.text == value) return;
        text.text = value;
        sizeDirty = true;
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Screen-space overlay canvas: a RectTransform's position is in screen pixels (rounded: crisper text).
        Vector3 screen = cam.WorldToScreenPoint(WorldPosition);
        transform.position = new Vector3(Mathf.Round(screen.x), Mathf.Round(screen.y), 0f);

        float canvasUnitsPerWorldUnit = WorldText.ReferenceHeight / (2f * cam.orthographicSize);
        float maxWidth = WidthUnits * canvasUnitsPerWorldUnit;
        if (sizeDirty || !Mathf.Approximately(maxWidth, lastMaxWidth)) Resize(maxWidth);
    }

    // Fit the panel snugly around the text.
    private void Resize(float maxWidth)
    {
        lastMaxWidth = maxWidth;
        sizeDirty = false;

        Vector2 preferred = text.GetPreferredValues(text.text, maxWidth, 0f);
        Vector2 textSize = new Vector2(Mathf.Min(preferred.x, maxWidth), preferred.y);
        text.rectTransform.sizeDelta = textSize;
        ((RectTransform)transform).sizeDelta = textSize + padding * 2f;
    }

#if UNITY_EDITOR
    public void EditorSetup(TMP_Text label, Image panel) => Init(label, panel);
#endif
}
