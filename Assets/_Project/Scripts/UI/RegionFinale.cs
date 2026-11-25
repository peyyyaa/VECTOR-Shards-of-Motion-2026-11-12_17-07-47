using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The end-of-region sequence, played when the Great Shard is freed: the screen fades to night, then one card per room
/// ("1 · First Exchange" and the one thing it taught, in the order the players walked them), then Wren's closing
/// words, and the game returns to the village at Wren's lab (GameFlow.ToVillage, with its transition card).
/// Built in code on its own overlay canvas (nothing to set up). Cards advance by themselves; Space / Enter / gamepad
/// South moves on sooner. Like the other menus, it reads the keyboard and gamepads directly (menu keys, not gameplay).
/// </summary>
public class RegionFinale : MonoBehaviour
{
    private const float CardSeconds = 3.2f, Fade = 0.6f;

    private RegionMapData map;
    private string[] lessons;
    private string closing;
    private CanvasGroup group;
    private TMP_Text heading, title, body;

    public static void Play(RegionMapData regionMap, string[] roomLessons, string closingWords)
    {
        GameObject go = new GameObject("Region Finale");
        RegionFinale f = go.AddComponent<RegionFinale>();
        f.map = regionMap;
        f.lessons = roomLessons;
        f.closing = closingWords;
        f.Build();
        f.StartCoroutine(f.Run());
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        GameObject bg = new GameObject("Night", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(transform, false);
        RectTransform r = (RectTransform)bg.transform;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.08f, 1f);

        TMP_FontAsset font = UIRoot.Instance != null ? UIRoot.Instance.PaperFont : null;
        heading = Text("Heading", font, 30f, new Color(0.55f, 0.9f, 1f, 0.8f), new Vector2(0f, 230f), new Vector2(1400f, 50f));
        title = Text("Title", font, 64f, new Color(1f, 0.85f, 0.5f), new Vector2(0f, 90f), new Vector2(1400f, 90f));
        body = Text("Body", font, 40f, new Color(0.92f, 0.95f, 1f), new Vector2(0f, -60f), new Vector2(1300f, 240f));
    }

    private TMP_Text Text(string name, TMP_FontAsset font, float size, Color color, Vector2 pos, Vector2 box)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    private IEnumerator Run()
    {
        yield return FadeGroup(0f, 1f, 1.5f); // night falls
        heading.text = "What the Shattered Grounds remember";
        int count = Mathf.Min(lessons.Length, map.rooms.Count);
        for (int i = 0; i < count; i++)
        {
            yield return Card($"{i + 1} · {map.rooms[i].displayName}", lessons[i]);
        }
        heading.text = "";
        yield return Card("", closing, 2f);
        GameFlow.ToVillage(GameFlow.VillageLabScreen); // home to Wren's lab: the homecoming (NpcTalk) and the credits
    }

    private IEnumerator Card(string t, string b, float hold = 1f)
    {
        title.text = t;
        body.text = b;
        yield return FadeTexts(0f, 1f);
        float timer = CardSeconds * hold;
        while (timer > 0f && !Skip()) { timer -= Time.unscaledDeltaTime; yield return null; }
        yield return null; // one press, one card
        yield return FadeTexts(1f, 0f);
    }

    private static bool Skip() =>
        (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
        || Gamepad.all.Any(g => g.buttonSouth.wasPressedThisFrame);

    private IEnumerator FadeTexts(float from, float to)
    {
        for (float t = 0f; t < Fade; t += Time.unscaledDeltaTime)
        {
            float a = Mathf.Lerp(from, to, t / Fade);
            title.alpha = body.alpha = a;
            yield return null;
        }
        title.alpha = body.alpha = to;
    }

    private IEnumerator FadeGroup(float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        group.alpha = to;
    }
}
