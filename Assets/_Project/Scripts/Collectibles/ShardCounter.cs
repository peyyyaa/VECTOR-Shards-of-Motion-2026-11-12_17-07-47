using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Motion Shard counter in the top-right corner: a shard icon, "collected / total" for the scene,
/// and "This room: x / y" for the current room. Created automatically when a scene has shards
/// (inside the UI_Canvas if present). Text sizes are in 1080p pixels.
/// </summary>
public class ShardCounter : MonoBehaviour
{
    public static ShardCounter Instance { get; private set; }

    private TMP_Text totalText;
    private TMP_Text roomText;
    private RoomManager rooms;
    private float punch;

    /// <summary>Builds the counter under the scene's UI canvas (or its own canvas if there is none).</summary>
    public static void Create()
    {
        if (Instance != null) return;

        Transform parent;
        if (UIRoot.Instance != null) parent = UIRoot.Instance.transform;
        else
        {
            GameObject canvasObject = new GameObject("UI (shards)");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            parent = canvasObject.transform;
        }

        GameObject root = new GameObject("ShardCounter", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)root.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-28f, -24f);
        rt.sizeDelta = new Vector2(380f, 90f);

        Image panel = root.AddComponent<Image>();
        panel.color = WorldText.PanelColor;
        panel.raycastTarget = false;

        ShardCounter counter = root.AddComponent<ShardCounter>();
        counter.Build(rt);
    }

    private void Build(RectTransform rt)
    {
        Instance = this;

        // Icon: the first shard's sprite.
        MotionShard anyShard = FindFirstObjectByType<MotionShard>(FindObjectsInactive.Include);
        SpriteRenderer shardSprite = anyShard != null ? anyShard.GetComponentInChildren<SpriteRenderer>(true) : null;
        GameObject iconObject = new GameObject("Icon", typeof(RectTransform));
        iconObject.transform.SetParent(rt, false);
        Image icon = iconObject.AddComponent<Image>();
        icon.sprite = shardSprite != null ? shardSprite.sprite : null;
        icon.color = shardSprite != null ? Color.white : new Color(0.4f, 0.9f, 1f);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        RectTransform iconRt = (RectTransform)iconObject.transform;
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0f, 0.5f);
        iconRt.anchoredPosition = new Vector2(16f, 0f);
        iconRt.sizeDelta = new Vector2(42f, 56f);

        totalText = Text(rt, "Total", 34f, new Vector2(70f, 14f), FontStyles.Bold);
        roomText = Text(rt, "Room", 22f, new Vector2(70f, -22f), FontStyles.Normal);
        roomText.color = new Color(1f, 1f, 1f, 0.75f);

        rooms = FindFirstObjectByType<RoomManager>();
        ShardTracker.Changed += OnChanged;
        Refresh();
    }

    private static TMP_Text Text(RectTransform parent, string name, float size, Vector2 pos, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Left;
        t.raycastTarget = false;
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
        r.pivot = new Vector2(0f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(300f, 40f);
        return t;
    }

    private void OnChanged()
    {
        punch = 1f; // brief pulse when the count changes
        Refresh();
    }

    private void Update()
    {
        Refresh(); // the current room can change without the count changing
        if (punch > 0f)
        {
            punch = Mathf.Max(0f, punch - Time.deltaTime * 4f);
            transform.localScale = Vector3.one * (1f + 0.12f * punch);
        }
    }

    private void Refresh()
    {
        if (totalText == null) return;
        totalText.text = $"{ShardTracker.Collected} / {ShardTracker.Total}";
        Room current = rooms != null ? rooms.CurrentRoom : null;
        (int total, int got) = current != null ? ShardTracker.InRoom(current) : (0, 0);
        roomText.text = total > 0 ? (got == total ? $"This room: {got} / {total} - all found!" : $"This room: {got} / {total}") : "";
    }

    private void OnDestroy()
    {
        ShardTracker.Changed -= OnChanged;
        if (Instance == this) Instance = null;
    }
}
