using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Which interactable each player is aiming at (reported by PlayerInteractor every frame). There is one prompt per
/// interactable, so when both players aim at the same thing they share it: [E] [Enter] Talk.
/// </summary>
public static class InteractPrompts
{
    public struct Slot
    {
        public Collider2D target;
        public string key;
        public string verb;
    }

    private static readonly Slot[] slots = new Slot[2];
    private static readonly Dictionary<Collider2D, InteractPrompt> prompts = new Dictionary<Collider2D, InteractPrompt>();

    // Statics can survive between Play sessions in the editor: start clean each time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        slots[0] = slots[1] = default;
        prompts.Clear();
    }

    /// <summary>Player 'player' (0 or 1) aims at 'target' (null = nothing) with this key and verb.</summary>
    public static void Report(int player, Collider2D target, string key, string verb)
    {
        if (player < 0 || player >= slots.Length) return;
        slots[player] = new Slot { target = target, key = key, verb = verb };
        if (target == null || (prompts.TryGetValue(target, out InteractPrompt existing) && existing != null)) return;
        InteractPrompt prompt = InteractPrompt.Create(target);
        if (prompt != null) prompts[target] = prompt;
    }

    public static Slot Get(int player) => slots[player];

    public static int Players => slots.Length;

    internal static void Forget(Collider2D target, InteractPrompt prompt)
    {
        if (target != null && prompts.TryGetValue(target, out InteractPrompt p) && p == prompt) prompts.Remove(target);
    }
}

/// <summary>
/// A small interact prompt above an interactable: a wooden key-cap per player aiming at it (the key in paper colour,
/// with a dot in the player's colour: P1 orange, P2 teal) and the verb in light text with a thin ink outline.
/// No background panel, so it covers as little of the room as possible. It pops in, bobs by a pixel, fades out when
/// nobody aims at it any more, and then removes itself. Lives in the UI_Canvas label container, so it fades
/// while the journal is open.
/// Created by InteractPrompts; its look comes from UIRoot (Prompt Key sprite, Prompt Dot sprite, Paper Font).
/// </summary>
[DefaultExecutionOrder(1000)] // after PlayerInteractor and the room camera, like ScreenLabel
public class InteractPrompt : MonoBehaviour
{
    private static readonly Color[] PlayerColors = { new Color32(232, 132, 48, 255), new Color32(72, 186, 206, 255) };
    private static readonly Color Paper = new Color32(255, 251, 240, 255), Outline = new Color32(43, 28, 18, 230);

    private const float KeyFont = 22f, VerbFont = 24f, CapHeight = 30f, CapPadding = 9f, MinCap = 34f, Gap = 4f, VerbGap = 7f;
    private const float FadeTime = 0.12f, AboveTarget = 0.2f;

    private Collider2D target;
    private CanvasGroup group;
    private RectTransform body;
    private readonly RectTransform[] caps = new RectTransform[2];
    private readonly TMP_Text[] keys = new TMP_Text[2];
    private TMP_Text verb;
    private float shown;          // 0..1 fade/pop
    private bool anyone;
    private readonly bool[] aiming = new bool[2];
    private string layoutKey;

    public static InteractPrompt Create(Collider2D target)
    {
        UIRoot ui = UIRoot.Instance;
        if (ui == null || ui.LabelContainer == null) return null;

        GameObject go = new GameObject($"Prompt ({target.name})", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(ui.LabelContainer, false);
        InteractPrompt p = go.AddComponent<InteractPrompt>();
        p.target = target;
        p.Build(ui);
        return p;
    }

    private void Build(UIRoot ui)
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        RectTransform root = (RectTransform)transform;
        root.sizeDelta = Vector2.zero;

        body = NewRect(root, "Body");
        body.pivot = new Vector2(0.5f, 0f);

        for (int i = 0; i < caps.Length; i++)
        {
            caps[i] = NewRect(body, "Key_P" + (i + 1));
            caps[i].pivot = new Vector2(0f, 0.5f);
            Image cap = caps[i].gameObject.AddComponent<Image>();
            cap.raycastTarget = false;
            if (ui.PromptKeySprite != null) { cap.sprite = ui.PromptKeySprite; cap.type = Image.Type.Sliced; }
            else cap.color = new Color32(92, 60, 36, 255); // no sprite assigned: plain wood colour

            RectTransform dot = NewRect(caps[i], "Dot");
            dot.anchorMin = dot.anchorMax = new Vector2(0f, 1f);
            dot.anchoredPosition = new Vector2(3f, -3f);
            dot.sizeDelta = new Vector2(9f, 9f);
            Image d = dot.gameObject.AddComponent<Image>();
            d.sprite = ui.PromptDotSprite;
            d.color = PlayerColors[i];
            d.raycastTarget = false;

            keys[i] = NewText(caps[i], "Key", ui.PaperFont, KeyFont, Paper, TextAlignmentOptions.Center);
            RectTransform kr = keys[i].rectTransform;
            kr.anchorMin = Vector2.zero; kr.anchorMax = Vector2.one;
            kr.offsetMin = new Vector2(0f, 3f); kr.offsetMax = Vector2.zero; // sit on the cap, above its darker bottom edge
        }

        verb = NewText(body, "Verb", ui.PaperFont, VerbFont, Paper, TextAlignmentOptions.Left);
        verb.rectTransform.pivot = new Vector2(0f, 0.5f);
        verb.outlineWidth = 0.22f;   // a thin ink outline keeps it readable on any background without a panel
        verb.outlineColor = Outline;
    }

    private void LateUpdate()
    {
        // Who is aiming at this right now?
        anyone = false;
        string first = null;
        for (int i = 0; i < aiming.Length; i++)
        {
            InteractPrompts.Slot s = InteractPrompts.Get(i);
            aiming[i] = target != null && s.target == target;
            if (!aiming[i]) continue;
            anyone = true;
            if (first == null) first = s.verb;
        }
        if (anyone) Layout(aiming, first);

        // Pop in / fade out (unscaled: menus may have stopped time).
        shown = Mathf.MoveTowards(shown, anyone ? 1f : 0f, Time.unscaledDeltaTime / FadeTime);
        group.alpha = shown;
        float pop = anyone ? Mathf.Lerp(0.7f, 1f, 1f - (1f - shown) * (1f - shown)) : 1f;
        body.localScale = new Vector3(pop, pop, 1f);
        if (!anyone && shown <= 0f) { Destroy(gameObject); return; }

        Camera cam = Camera.main;
        if (cam == null || target == null) return;
        Bounds b = target.bounds;
        Vector3 screen = cam.WorldToScreenPoint(new Vector3(b.center.x, b.max.y + AboveTarget, 0f));
        float bob = Mathf.Round(Mathf.Sin(Time.unscaledTime * 3f) * 1.5f);
        transform.position = new Vector3(Mathf.Round(screen.x), Mathf.Round(screen.y) + bob, 0f);
    }

    // Key-caps side by side, then the verb; the whole row is centred over the target.
    private void Layout(bool[] aiming, string verbText)
    {
        string k = "";
        for (int i = 0; i < aiming.Length; i++) k += aiming[i] ? InteractPrompts.Get(i).key + "|" : "-|";
        k += verbText;
        if (k == layoutKey) return;
        layoutKey = k;

        float x = 0f;
        for (int i = 0; i < caps.Length; i++)
        {
            bool on = i < aiming.Length && aiming[i];
            caps[i].gameObject.SetActive(on);
            if (!on) continue;
            keys[i].text = InteractPrompts.Get(i).key;
            float w = Mathf.Max(MinCap, keys[i].GetPreferredValues(keys[i].text).x + CapPadding * 2f);
            caps[i].sizeDelta = new Vector2(w, CapHeight);
            caps[i].anchoredPosition = new Vector2(x, CapHeight * 0.5f);
            x += w + Gap;
        }

        bool hasVerb = !string.IsNullOrEmpty(verbText);
        verb.gameObject.SetActive(hasVerb);
        if (hasVerb)
        {
            verb.text = verbText;
            Vector2 size = verb.GetPreferredValues(verbText);
            x += VerbGap - Gap;
            verb.rectTransform.sizeDelta = new Vector2(size.x + 4f, CapHeight);
            verb.rectTransform.anchoredPosition = new Vector2(x, CapHeight * 0.5f + 1f);
            x += size.x + 4f;
        }
        else x -= Gap;

        // Centre the row: shift every piece left by half its width.
        float half = x * 0.5f;
        foreach (RectTransform child in body)
        {
            Vector2 p = child.anchoredPosition;
            child.anchoredPosition = new Vector2(p.x - half, p.y);
        }
    }

    private void OnDestroy() => InteractPrompts.Forget(target, this);

    private static RectTransform NewRect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static TMP_Text NewText(Transform parent, string name, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
    {
        RectTransform rt = NewRect(parent, name);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }
}
