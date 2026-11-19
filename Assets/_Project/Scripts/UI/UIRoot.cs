using UnityEngine;

/// <summary>
/// The scene's UI canvas (prefab "UI_Canvas", placed under _Managers). Holds:
/// - Labels: the container that world-pinned labels (prompts, mass labels, plate readouts) are created in,
///   using the Label prefab (restyle that prefab to change how every label looks);
/// - Journal: the Motion Journal panel at the bottom of the screen.
///
/// One per scene. Created by Tools > Shards of Motion > Create UI Prefabs (Add To Scene).
/// </summary>
[RequireComponent(typeof(Canvas))]
public class UIRoot : MonoBehaviour
{
    [SerializeField] private RectTransform labelContainer;
    [SerializeField] private ScreenLabel labelPrefab;
    [SerializeField] private JournalPanel journal;

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
