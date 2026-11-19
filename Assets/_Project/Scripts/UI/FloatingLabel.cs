using TMPro;
using UnityEngine;

/// <summary>
/// Base for a text label that floats above an object and follows it (mass labels, plate
/// readouts). The label is created when the game starts, drawn sharp on the screen-space
/// labels canvas, and stays upright even if the object rotates. Subclasses fill in Refresh().
/// </summary>
public abstract class FloatingLabel : MonoBehaviour
{
    [Header("Label")]
    [Tooltip("Position of the label relative to this object's center, in world units.")]
    [SerializeField] protected Vector2 offset = new Vector2(0f, 0.85f);
    [Tooltip("Text size in pixels at a 1080p screen.")]
    [SerializeField, Min(4f)] protected float fontSizePx = 26f;
    [SerializeField] protected Color textColor = Color.white;
    [Tooltip("Leave empty to use the project's default TMP font.")]
    [SerializeField] protected TMP_FontAsset font;

    protected ScreenLabel Label { get; private set; }
    private string shownText;

    protected virtual void Start()
    {
        Label = WorldText.Create($"{name} (label)", font, fontSizePx, textColor);
        Follow();
    }

    protected virtual void LateUpdate()
    {
        if (Label == null) return;
        Follow();
        Refresh();
    }

    private void Follow()
    {
        Vector3 p = transform.position;
        Label.WorldPosition = new Vector3(p.x + offset.x, p.y + offset.y, p.z);
    }

    /// <summary>Sets the text only when it changes (avoids rebuilding the text mesh every frame).</summary>
    protected void SetText(string text)
    {
        if (text == shownText) return;
        shownText = text;
        Label.SetText(text);
    }

    protected void SetColor(Color color)
    {
        if (Label.Text.color != color) Label.Text.color = color;
    }

    protected abstract void Refresh();

    private void OnEnable()
    {
        if (Label != null) Label.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (Label != null) Label.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Label != null) Destroy(Label.gameObject);
    }
}
