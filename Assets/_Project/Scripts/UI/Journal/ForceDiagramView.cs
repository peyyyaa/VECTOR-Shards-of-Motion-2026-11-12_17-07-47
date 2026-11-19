using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws a ForceDiagram (free-body diagram) inside its RectTransform: the object in the middle, a ground
/// line, and one arrow per force. Arrow length is proportional to the force (the largest force gets the
/// longest arrow), so balanced forces are visibly equal. Colors follow the game: P1 push = orange,
/// P2 pull = cyan, friction = red, weight = brown, normal (support) = green, impulse = gold.
/// Arrows start at the object's edge and point the way the force acts.
/// When drawn, the arrows grow out of the object one after another and their labels pop in
/// (unscaled time, so it also works while the game is paused).
/// </summary>
public class ForceDiagramView : MonoBehaviour
{
    [SerializeField] private Sprite blockSprite;
    [SerializeField] private Sprite shaftSprite;
    [SerializeField] private Sprite headSprite;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private float blockSize = 144f; // 16-px block sprite x 9: whole pixels stay crisp
    [SerializeField] private float maxArrow = 200f;
    [SerializeField] private float minArrow = 60f;

    [SerializeField] private float growTime = 0.28f;
    [SerializeField] private float stagger = 0.16f;

    private class Grow { public RectTransform shaft, head, label; public Vector2 start, dir; public float length, delay; }

    private readonly List<GameObject> drawn = new List<GameObject>();
    private readonly List<Grow> grows = new List<Grow>();
    private float clock;

    public static Color KindColor(DiagramForce.Kind kind)
    {
        switch (kind)
        {
            case DiagramForce.Kind.Push: return new Color32(232, 137, 58, 255);
            case DiagramForce.Kind.Pull: return new Color32(54, 168, 186, 255);
            case DiagramForce.Kind.Friction: return new Color32(178, 68, 58, 255);
            case DiagramForce.Kind.Weight: return new Color32(107, 74, 46, 255);
            case DiagramForce.Kind.Normal: return new Color32(94, 138, 62, 255);
            default: return new Color32(196, 150, 50, 255);
        }
    }

    /// <summary>Draws the diagram; the arrows start growing after Delay seconds.</summary>
    public void Draw(ForceDiagram diagram, float delay = 0f)
    {
        foreach (GameObject g in drawn) if (g != null) Destroy(g);
        drawn.Clear();
        grows.Clear();
        clock = -delay;
        gameObject.SetActive(diagram != null && diagram.show);
        if (diagram == null || !diagram.show) return;

        bool up = false, down = false, left = false, right = false, supported = false;
        float biggest = 0.0001f;
        foreach (DiagramForce f in diagram.forces)
        {
            biggest = Mathf.Max(biggest, Mathf.Abs(f.newtons));
            up |= f.direction == DiagramForce.Direction.Up;
            down |= f.direction == DiagramForce.Direction.Down;
            left |= f.direction == DiagramForce.Direction.Left;
            right |= f.direction == DiagramForce.Direction.Right;
            supported |= f.kind == DiagramForce.Kind.Normal || f.kind == DiagramForce.Kind.Friction;
        }

        // If forces act on one side only, slide the object to the other side so its arrows get the room.
        Vector2 area = ((RectTransform)transform).rect.size;
        const float labelRoom = 130f; // space past the tip of a sideways arrow for its label
        float half = blockSize * 0.5f, edge = 8f;
        Vector2 center = Vector2.zero;
        if (down && !up) center.y = area.y * 0.5f - half - edge;
        else if (up && !down) center.y = -(area.y * 0.5f - half - edge);
        if (right && !left) center.x = -(area.x * 0.5f - half - edge);
        else if (left && !right) center.x = area.x * 0.5f - half - edge;

        // Room for the longest arrow on each axis; one scale for all arrows so equal forces get equal arrows.
        float fitY = (up && down) || (!up && !down) ? area.y * 0.5f - half : area.y - blockSize - edge * 2f;
        float fitX = ((left && right) || (!left && !right) ? area.x * 0.5f - half : area.x - blockSize - edge * 2f) - labelRoom;
        float longest = maxArrow;
        if (up || down) longest = Mathf.Min(longest, fitY);
        if (left || right) longest = Mathf.Min(longest, fitX);
        longest = Mathf.Max(minArrow, longest);

        // Ground line only when something is actually supporting the object (a normal or friction force).
        // A floating or falling object gets no ground: no surface, no support force.
        if (supported)
            Rect(center + new Vector2(0f, -half - 3f), new Vector2(blockSize * 3f, 6f), new Color32(107, 74, 46, 255), shaftSprite);

        RectTransform block = Rect(center, new Vector2(blockSize, blockSize), Color.white, blockSprite);
        Text(block, diagram.objectLabel, 30f, Vector2.zero, new Vector2(blockSize - 12f, blockSize - 16f), new Color32(255, 251, 240, 255), TextAlignmentOptions.Center);

        Dictionary<DiagramForce.Direction, int> perSide = new Dictionary<DiagramForce.Direction, int>();
        foreach (DiagramForce f in diagram.forces)
        {
            perSide.TryGetValue(f.direction, out int stack);
            perSide[f.direction] = stack + 1;

            Vector2 dir = f.direction == DiagramForce.Direction.Right ? Vector2.right : f.direction == DiagramForce.Direction.Left ? Vector2.left
                        : f.direction == DiagramForce.Direction.Up ? Vector2.up : Vector2.down;
            Vector2 side = new Vector2(-dir.y, dir.x);                       // perpendicular, to stack several arrows on one side
            Vector2 start = center + dir * half + side * (stack * 30f);
            float length = Mathf.Lerp(minArrow, longest, Mathf.Abs(f.newtons) / biggest);
            Color color = KindColor(f.kind);

            Grow grow = new Grow { start = start, dir = dir, length = length, delay = grows.Count * stagger };
            Arrow(grow, color);
            string value = Mathf.Approximately(f.newtons, Mathf.Round(f.newtons)) ? f.newtons.ToString("0") : f.newtons.ToString("0.#");
            Vector2 labelPos = dir.y != 0f
                ? start + dir * (length * 0.5f) + new Vector2(half + 70f, 0f)   // beside a vertical arrow
                : start + dir * (length + labelRoom * 0.5f);                      // past the tip of a sideways arrow
            Color labelColor = Color.Lerp(color, Color.black, 0.3f);              // darker than the arrow, readable on paper
            RectTransform label = Text((RectTransform)transform, $"{f.label}\n{value} {f.unit}", 28f, labelPos, new Vector2(labelRoom + 30f, 64f), labelColor, TextAlignmentOptions.Center);
            drawn.Add(label.gameObject);
            grow.label = label;
            grows.Add(grow);
        }
        Animate();
    }

    private void Update()
    {
        if (grows.Count == 0) return;
        clock += Time.unscaledDeltaTime;
        Animate();
    }

    private void Animate()
    {
        foreach (Grow g in grows)
        {
            float t = Mathf.Clamp01((clock - g.delay) / growTime);
            float eased = 1f - (1f - t) * (1f - t) * (1f - t);            // fast start, soft landing
            Place(g, g.length * eased);
            float pop = Mathf.Clamp01((clock - g.delay - growTime * 0.7f) / 0.15f);
            float scale = pop <= 0f ? 0f : pop >= 1f ? 1f : 1f + 0.25f * Mathf.Sin(pop * Mathf.PI); // label pops in
            g.label.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private const float HeadSize = 30f, Thickness = 14f;

    private void Arrow(Grow g, Color color)
    {
        g.shaft = Rect(g.start, new Vector2(1f, Thickness), color, shaftSprite);
        g.head = Rect(g.start, new Vector2(HeadSize, HeadSize), color, headSprite);
        float angle = Mathf.Atan2(g.dir.y, g.dir.x) * Mathf.Rad2Deg; // 90-degree steps only: pixel art stays clean
        g.shaft.localRotation = Quaternion.Euler(0f, 0f, angle);
        g.head.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // Lays out one arrow at its current (growing) length.
    private static void Place(Grow g, float length)
    {
        float head = Mathf.Min(HeadSize, length);
        float shaftLength = Mathf.Max(0f, length - head);
        g.shaft.gameObject.SetActive(shaftLength > 0.5f);
        g.shaft.sizeDelta = new Vector2(Mathf.Max(1f, shaftLength), Thickness);
        g.shaft.anchoredPosition = g.start + g.dir * (shaftLength * 0.5f);
        g.head.gameObject.SetActive(length > 0.5f);
        g.head.sizeDelta = new Vector2(head, HeadSize * Mathf.Clamp01(head / HeadSize));
        g.head.anchoredPosition = g.start + g.dir * (shaftLength + head * 0.5f);
    }

    private RectTransform Rect(Vector2 pos, Vector2 size, Color color, Sprite sprite)
    {
        GameObject go = new GameObject("Part", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        img.color = color;
        img.raycastTarget = false;
        drawn.Add(go);
        return rt;
    }

    private RectTransform Text(RectTransform parent, string text, float size, Vector2 pos, Vector2 box, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        return rt;
    }

#if UNITY_EDITOR
    public void EditorSetup(Sprite block, Sprite shaft, Sprite head, TMP_FontAsset f)
    {
        blockSprite = block; shaftSprite = shaft; headSprite = head; font = f;
    }
#endif
}
