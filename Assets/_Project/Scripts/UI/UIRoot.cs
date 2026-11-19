using UnityEngine;

/// <summary>
/// The scene's UI canvas (prefab "UI_Canvas", placed under _Managers). Holds:
/// - Labels: the container that world-pinned labels (prompts, mass labels, plate readouts) are created in,
///   using the Label prefab (restyle that prefab to change how every label looks);
/// - Journal: the Motion Journal notebook panel.
/// While the journal is open, the world labels (prompts, mass labels) fade out so they don't poke out around it.
///
/// One per scene. Created by Tools > Shards of Motion > Create UI Prefabs (Add To Scene).
/// </summary>
[RequireComponent(typeof(Canvas))]
public class UIRoot : MonoBehaviour
{
    [SerializeField] private RectTransform labelContainer;
    [SerializeField] private ScreenLabel labelPrefab;
    [SerializeField] private JournalPanel journal;

    [Tooltip("Seconds for the world labels to fade out/in when the journal opens/closes.")]
    [SerializeField, Min(0.01f)] private float labelFadeTime = 0.12f;

    private CanvasGroup labelGroup;

    public static UIRoot Instance { get; private set; }

    public RectTransform LabelContainer => labelContainer;
    public ScreenLabel LabelPrefab => labelPrefab;
    public JournalPanel Journal => journal;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("More than one UIRoot in the scene; using the first.", this);
            return;
        }
        Instance = this;
        if (labelContainer != null)
        {
            labelGroup = labelContainer.GetComponent<CanvasGroup>();
            if (labelGroup == null) labelGroup = labelContainer.gameObject.AddComponent<CanvasGroup>();
            labelGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (labelGroup == null) return;
        float target = journal != null && journal.IsOpen ? 0f : 1f;
        labelGroup.alpha = Mathf.MoveTowards(labelGroup.alpha, target, Time.unscaledDeltaTime / labelFadeTime);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

#if UNITY_EDITOR
    /// <summary>Editor tool hook: wires the references after building the prefab.</summary>
    public void EditorSetup(RectTransform container, ScreenLabel prefab, JournalPanel journalPanel)
    {
        labelContainer = container;
        labelPrefab = prefab;
        journal = journalPanel;
    }
#endif
}
