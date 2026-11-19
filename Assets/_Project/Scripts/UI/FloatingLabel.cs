using TMPro;
using UnityEngine;

/// <summary>
/// Base for a text label that floats above an object and follows it (mass labels, plate
/// readouts). The label is created when the game starts, drawn sharp on the screen-space
/// labels canvas, and stays upright even if the object rotates. Subclasses fill in Refresh().
///
/// To keep the screen calm, labels only fade in when they're relevant: a player is within
/// Show Distance, or the object is busy (subclasses decide: a block moving, a plate's weight changing).
/// Untick Only When Relevant to always show the label.
/// </summary>
public abstract class FloatingLabel : MonoBehaviour
{
    [Header("Label")]
    [Tooltip("Position of the label relative to this object's center, in world units.")]
    [SerializeField] protected Vector2 offset = new Vector2(0f, 0.85f);
    [Tooltip("Text size in pixels at a 1080p screen.")]
    [SerializeField, Min(4f)] protected float fontSizePx = 26f;
    [Tooltip("Multiplies Font Size Px (smaller labels are less distracting).")]
    [SerializeField, Range(0.5f, 1.5f)] protected float sizeScale = 0.85f;
    [SerializeField] protected Color textColor = Color.white;
    [Tooltip("Leave empty to use the project's default TMP font.")]
    [SerializeField] protected TMP_FontAsset font;

    [Header("Show when relevant")]
    [SerializeField] protected bool onlyWhenRelevant = true;
    [Tooltip("The label fades in when a player is this close (units).")]
    [SerializeField, Min(0f)] protected float showDistance = 2.5f;
    [SerializeField, Min(0.01f)] protected float fadeTime = 0.2f;
    [Tooltip("Opacity when shown (a little see-through is calmer).")]
    [SerializeField, Range(0f, 1f)] protected float shownAlpha = 0.9f;

    protected ScreenLabel Label { get; private set; }
    private string shownText;

    private static PlayerMovement[] players = new PlayerMovement[0];
    private static float playersFoundAt = -10f;

    /// <summary>True while the object is doing something worth labelling, even with no player near.</summary>
    protected virtual bool IsBusy => false;

    protected virtual void Start()
    {
        Label = WorldText.Create($"{name} (label)", font, fontSizePx * sizeScale, textColor);
        if (onlyWhenRelevant) Label.Alpha = 0f;
        Follow();
    }

    protected virtual void LateUpdate()
    {
        if (Label == null) return;
        Follow();
        Refresh();

        float target = !onlyWhenRelevant || IsBusy || PlayerNear() ? shownAlpha : 0f;
        Label.Alpha = Mathf.MoveTowards(Label.Alpha, target, Time.deltaTime / fadeTime);
    }

    private bool PlayerNear()
    {
        // Players are found once a second (they can spawn after this label starts).
        if (Time.time - playersFoundAt > 1f)
        {
            players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            playersFoundAt = Time.time;
        }
        float maxSqr = showDistance * showDistance;
        foreach (PlayerMovement p in players)
            if (p != null && ((Vector2)(p.transform.position - transform.position)).sqrMagnitude <= maxSqr) return true;
        return false;
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
