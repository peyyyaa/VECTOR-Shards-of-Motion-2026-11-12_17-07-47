using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The credits (the game's ending), played once after Wren's homecoming talk in the village (NpcTalk, Credits After
/// Homecoming): night falls over the screen, then a few cards: the title, the team, the physics inside, the art and
/// fonts, thank you (Regions 2 and 3 coming soon). Then the screen clears and the players are back in Alderbrook, free to
/// fund the last projects or hunt the last shards. Saved as "credits_seen".
/// Built in code on its own overlay canvas, like RegionFinale (nothing to set up). Cards advance by themselves;
/// Space / Enter / gamepad South moves on sooner. Gameplay is frozen meanwhile (MenuFreeze). Menu keys are read
/// directly, like the other menus.
/// EDIT THE TEAM below (Team): the names are placeholders until the team fills them in.
/// </summary>
public class CreditsRoll : MonoBehaviour
{
    public const string SeenEvent = "credits_seen";

    // ---- EDIT HERE: one line per person, "Name: role". Placeholders until the team fills them in.
    private static readonly string[] Team =
    {
        "[Team member 1]: [role]",
        "[Team member 2]: [role]",
        "[Team member 3]: [role]",
        "[Team member 4]: [role]",
    };

    private static readonly string Physics =
        "Newton's laws · force and acceleration (a = F ÷ m)\n" +
        "impulse and momentum (J = Δp) · friction\n" +
        "weight (W = m·g) · gravity (g = 29.43 m/s² in this world)\n" +
        "collisions and the coefficient of restitution";

    private static readonly string Art =
        "Buildings, title painting and the Momentum Engine: made by the team with PixelLab\n" +
        "\"Legacy Fantasy – High Forest\" by Anokolisa\n" +
        "\"Pixel Art Platformer – Village Props\" and \"Interactive Pixel Water\" by Cainos\n" +
        "Jersey 10 font by The Soft Type Project Authors (SIL Open Font License 1.1)";

    private const float CardSeconds = 5f, Fade = 0.6f;

    private CanvasGroup group;
    private TMP_Text heading, title, body;
    private bool froze;

    public static bool Playing { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => Playing = false;

    public static void Play()
    {
        if (Playing) return;
        GameObject go = new GameObject("Credits");
        CreditsRoll c = go.AddComponent<CreditsRoll>();
        c.Build();
        c.StartCoroutine(c.Run());
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
        heading = Text("Heading", font, 30f, new Color(0.55f, 0.9f, 1f, 0.8f), new Vector2(0f, 250f), new Vector2(1400f, 50f));
        title = Text("Title", font, 64f, new Color(1f, 0.85f, 0.5f), new Vector2(0f, 140f), new Vector2(1400f, 90f));
        body = Text("Body", font, 36f, new Color(0.92f, 0.95f, 1f), new Vector2(0f, -60f), new Vector2(1500f, 330f));
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
        Playing = true;
        froze = MenuFreeze.Freeze();
        SaveSystem.MarkEvent(SeenEvent);
        yield return FadeGroup(0f, 1f, 1.5f); // night falls
        yield return Card("", "Shards of Motion", "A two-player physics adventure in the Shattered Grounds");
        yield return Card("Made by", "", string.Join("\n", Team));
        yield return Card("The physics inside", "", Physics, 1.4f);
        yield return Card("Art and fonts", "", Art, 1.4f);
        yield return Card("", "Thank you for playing!",
                          "The roads to the Gravity Divide and the Mechanical Depths are still closed... for now.\n" +
                          "Regions 2 and 3: coming soon.", 1.4f);
        yield return FadeGroup(1f, 0f, 1.2f); // morning: back in Alderbrook
        if (froze) MenuFreeze.Unfreeze();
        Playing = false;
        Destroy(gameObject);
    }

    private IEnumerator Card(string h, string t, string b, float hold = 1f)
    {
        heading.text = h;
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
            heading.alpha = title.alpha = body.alpha = a;
            yield return null;
        }
        heading.alpha = title.alpha = body.alpha = to;
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

    private void OnDestroy()
    {
        if (!Playing) return; // left the scene mid-roll
        if (froze) MenuFreeze.Unfreeze();
        Playing = false;
    }
}
