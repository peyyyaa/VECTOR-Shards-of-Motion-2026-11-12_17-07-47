using TMPro;
using UnityEngine;

/// <summary>
/// A readable sign placed in the world: Lab station instructions now, Motion Journal stones
/// in real rooms. It fades in when a player comes within Show Distance, and fades out again
/// when both walk away, so rooms aren't covered in text. (Show Distance 0 = always visible.)
///
/// The text is drawn crisp on the screen-space labels canvas, pinned to this object's position.
/// Scene view: a small marker; select the sign to preview its text.
/// </summary>
public class WorldSign : MonoBehaviour
{
    [SerializeField, TextArea(2, 6)] private string text = "Sign text";
    [Tooltip("Text box width in world units; longer lines wrap.")]
    [SerializeField, Min(1f)] private float width = 8f;
    [Tooltip("Text size in pixels at a 1080p screen.")]
    [SerializeField, Min(4f)] private float fontSizePx = 24f;
    [SerializeField] private Color textColor = new Color(1f, 0.95f, 0.8f);
    [Tooltip("Leave empty to use the project's default TMP font.")]
    [SerializeField] private TMP_FontAsset font;

    [Header("Visibility")]
    [Tooltip("Shows while a player is within this distance (units). 0 = always visible.")]
    [SerializeField, Min(0f)] private float showDistance = 5f;
    [Tooltip("Seconds to fade in or out.")]
    [SerializeField, Min(0.01f)] private float fadeTime = 0.25f;

    private ScreenLabel label;
    private PlayerMovement[] players = new PlayerMovement[0];
    private float findPlayersTimer;

    private void Start()
    {
        label = WorldText.Create($"{name} (text)", font, fontSizePx, textColor, width);
        label.SetText(text);
        label.WorldPosition = transform.position;
        label.Alpha = showDistance <= 0f ? 1f : 0f;
    }

    private void Update()
    {
        if (label == null) return;
        label.WorldPosition = transform.position;

        bool show = showDistance <= 0f || PlayerNearby();
        float target = show ? 1f : 0f;
        label.Alpha = Mathf.MoveTowards(label.Alpha, target, Time.deltaTime / fadeTime);
    }

    private bool PlayerNearby()
    {
        // Players are spawned at runtime; look them up until both are found (cheap, twice a second).
        if (players.Length < 2)
        {
            findPlayersTimer -= Time.deltaTime;
            if (findPlayersTimer <= 0f)
            {
                players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
                findPlayersTimer = 0.5f;
            }
        }

        float maxSqr = showDistance * showDistance;
        foreach (PlayerMovement p in players)
        {
            if (p != null && ((Vector2)(p.transform.position - transform.position)).sqrMagnitude <= maxSqr) return true;
        }
        return false;
    }

    private void OnDestroy()
    {
        if (label != null) Destroy(label.gameObject);
    }

#if UNITY_EDITOR
    // Unselected: a small marker only (keeps the Scene view clean).
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.95f, 0.8f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(0.6f, 0.4f, 0f));
    }

    // Selected: the text box, its text, and the distance at which it appears.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.95f, 0.8f, 0.5f);
        Gizmos.DrawWireCube(transform.position, new Vector3(width, 1.2f, 0f));
        if (showDistance > 0f)
        {
            Gizmos.color = new Color(1f, 0.95f, 0.8f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, showDistance);
        }
        UnityEditor.Handles.Label(transform.position, text);
    }
#endif
}
