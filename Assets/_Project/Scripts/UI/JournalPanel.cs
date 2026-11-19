using TMPro;
using UnityEngine;

/// <summary>
/// The Motion Journal panel: a fixed panel at the bottom of the screen that shows a Journal
/// stone's title and explanation. Opened and closed by JournalStone (players press Interact).
/// Part of the UI_Canvas prefab; restyle it there.
/// </summary>
public class JournalPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text hintText;

    /// <summary>The stone whose entry is showing, or null when closed.</summary>
    public JournalStone Source { get; private set; }
    public bool IsOpen => Source != null;

    // The panel object starts inactive (saved that way in the UI_Canvas prefab) and is shown on demand.

    public void Show(JournalStone source, string title, string body, string hint)
    {
        Source = source;
        titleText.text = title;
        bodyText.text = body;
        if (hintText != null) hintText.text = hint;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        Source = null;
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    public void EditorSetup(TMP_Text title, TMP_Text body, TMP_Text hint)
    {
        titleText = title;
        bodyText = body;
        hintText = hint;
    }
#endif
}
