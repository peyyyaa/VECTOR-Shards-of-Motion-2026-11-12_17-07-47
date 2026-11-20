using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Motion Journal: a field notebook (Wren Alder's journal) that opens over the game when a player
/// reads a Journal stone. Shows one page at a time: title ribbon, force diagram and margin notes (left page),
/// Big Idea, Wren's notes, Look Closer formulas and a fun fact (right page). Empty sections are hidden.
///
/// Controls while open (either player): TAP Interact = next page (closes on the last page),
/// HOLD Interact = close from any page. Walking away also closes it (see JournalStone).
/// The bottom bar shows both players' real Interact keys, page dots and a "hold to close" hint.
///
/// Feel: the book pops open from the spine; turning a page folds a blank page over the spine while
/// the content fades; the diagram's arrows then grow out of the object one by one.
///
/// Part of the UI_Canvas prefab (built by Tools > Shards of Motion > Create UI Prefabs). Starts inactive.
/// Animations use unscaled time.
/// </summary>
public class JournalPanel : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bigIdeaText;
    [SerializeField] private TMP_Text notesText;
    [SerializeField] private TMP_Text lookCloserText;
    [SerializeField] private TMP_Text funFactText;
    [SerializeField] private TMP_Text marginP1Text;
    [SerializeField] private TMP_Text marginP2Text;
    [SerializeField] private TMP_Text captionText;

    [Header("Sections (hidden when empty)")]
    [SerializeField] private GameObject bigIdeaCard;
    [SerializeField] private GameObject lookCloserCard;
    [SerializeField] private GameObject funFactRow;
    [SerializeField] private GameObject marginP1Row;
    [SerializeField] private GameObject marginP2Row;
    [SerializeField] private GameObject diagramCard;
    [SerializeField] private ForceDiagramView diagram;
    [Tooltip("Shows a page's sketch (e.g. Wren's room sketch) in the diagram card instead of a force diagram.")]
    [SerializeField] private Image sketchImage;

    [Header("Pages")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private CanvasGroup leftContent;
    [SerializeField] private CanvasGroup rightContent;
    [Tooltip("Blank page that folds over the spine when turning.")]
    [SerializeField] private RectTransform flap;
    [SerializeField] private RectTransform pageCurl;

    [Header("Bottom bar")]
    [SerializeField] private Image[] dots = new Image[0];
    [SerializeField] private RectTransform[] keyBadges = new RectTransform[2];
    [SerializeField] private TMP_Text[] keyTexts = new TMP_Text[2];
    [SerializeField] private TMP_Text promptLabel;
    [SerializeField] private GameObject promptArrow;
    [SerializeField] private TMP_Text holdHint;
    [SerializeField] private Image holdFill;
    [SerializeField] private float promptRight = 335f;

    [Header("Timing")]
    [SerializeField] private float openTime = 0.3f;
    [SerializeField] private float turnTime = 0.26f;
    [SerializeField] private float contentFadeTime = 0.12f;
    [Tooltip("Hold Interact this long to close the journal.")]
    [SerializeField] private float holdToClose = 0.5f;

    private static readonly Color DotOn = new Color32(74, 47, 28, 255), DotOff = new Color32(74, 47, 28, 70);
    private const float Spine = 20f; // half the gap between the pages

    private JournalEntry entry;
    private int page;
    private float openTimer = float.MaxValue;
    private float turnTimer = float.MaxValue;
    private bool swapped = true;
    private float fadeTimer = float.MaxValue;
    private int openFrame = -1;
    private PlayerInputReader[] readers = new PlayerInputReader[0];
    private readonly List<float> holdTimes = new List<float>();
    private readonly List<bool> tracking = new List<bool>();

    /// <summary>The stone whose entry is showing, or null when closed.</summary>
    public JournalStone Source { get; private set; }
    public bool IsOpen => Source != null;
    public bool HasNextPage => entry != null && page < entry.pages.Count - 1;

    public void Open(JournalStone source, JournalEntry journalEntry)
    {
        Source = source;
        entry = journalEntry;
        page = 0;
        openFrame = Time.frameCount; // the press that opened the journal must not also turn the page
        FindReaders();
        gameObject.SetActive(true);

        openTimer = 0f;
        turnTimer = float.MaxValue;
        swapped = true;
        fadeTimer = float.MaxValue;
        SetContentAlpha(1f);
        if (flap != null) flap.gameObject.SetActive(false);
        ShowPage(openTime * 0.6f);
        ApplyOpen(0f);
    }

    /// <summary>Turns the page (with the fold animation). Returns false (and closes) if this was the last page.</summary>
    public bool NextPage()
    {
        if (!HasNextPage) { Hide(); return false; }
        if (turnTimer < turnTime) FinishTurn(); // tapping fast: finish the current turn first
        page++;
        turnTimer = 0f;
        swapped = false;
        fadeTimer = float.MaxValue;
        SetContentAlpha(0f);
        if (flap != null) flap.gameObject.SetActive(true);
        return true;
    }

    public void Hide()
    {
        Source = null;
        entry = null;
        transform.localScale = Vector3.one;
        gameObject.SetActive(false);
    }

    // Kept for older callers: shows a simple one-page entry.
    public void Show(JournalStone source, string title, string body, string hint)
    {
        JournalEntry e = ScriptableObject.CreateInstance<JournalEntry>();
        e.title = title;
        e.pages.Add(new JournalPage { notes = body });
        Open(source, e);
    }

    // ------------------------------------------------------------------ per frame

    private void Update()
    {
        if (!IsOpen) return;
        float dt = Time.unscaledDeltaTime;

        if (openTimer < openTime) { openTimer += dt; ApplyOpen(Mathf.Clamp01(openTimer / openTime)); }
        if (turnTimer < turnTime) { turnTimer += dt; ApplyTurn(); }
        if (fadeTimer < contentFadeTime) { fadeTimer += dt; SetContentAlpha(Mathf.Clamp01(fadeTimer / contentFadeTime)); }

        if (pageCurl != null) // the folded corner wiggles while there's another page
        {
            float s = HasNextPage ? 1f + 0.08f * Mathf.Max(0f, Mathf.Sin(Time.unscaledTime * 5f)) : 1f;
            pageCurl.localScale = new Vector3(s, s, 1f);
        }

        ReadInput(dt);
    }

    // Tap (press and release before Hold To Close) = next page; hold = close. Either player.
    private void ReadInput(float dt)
    {
        float longest = 0f;
        for (int i = 0; i < readers.Length; i++)
        {
            PlayerInputReader r = readers[i];
            if (r == null) continue;
            if (r.InteractPressedThisFrame && Time.frameCount != openFrame) { tracking[i] = true; holdTimes[i] = 0f; }
            if (!tracking[i]) continue;

            if (r.InteractHeld)
            {
                holdTimes[i] += dt;
                if (holdTimes[i] >= holdToClose) { Hide(); return; }
                longest = Mathf.Max(longest, holdTimes[i]);
            }
            else
            {
                tracking[i] = false;
                NextPage();
                if (!IsOpen) return;
                LayoutBottomBar();
            }
        }

        if (holdFill != null) // only shows once it's clearly a hold, so taps don't flash it
        {
            float fill = longest > 0.12f ? Mathf.Clamp01(longest / holdToClose) : 0f;
            holdFill.fillAmount = fill;
            holdFill.enabled = fill > 0f;
        }
    }

    private void ApplyOpen(float t)
    {
        // Pops open from the spine: width overshoots slightly, then settles.
        float x = Mathf.LerpUnclamped(0.25f, 1f, BackOut(t));
        float y = Mathf.Lerp(0.92f, 1f, Mathf.SmoothStep(0f, 1f, t));
        transform.localScale = new Vector3(x, y, 1f);
        if (group != null) group.alpha = Mathf.Clamp01(t * 2.5f);
    }

    private void ApplyTurn()
    {
        float half = turnTime * 0.5f;
        if (flap == null)
        {
            if (!swapped) { ShowPage(0f); swapped = true; }
        }
        else if (turnTimer < half)
        {
            // Phase 1: a page lifts off the right side and folds toward the spine.
            float s = 1f - Mathf.SmoothStep(0f, 1f, turnTimer / half);
            SetFlap(new Vector2(0f, 0.5f), Spine, s);
        }
        else
        {
            if (!swapped) { ShowPage(half + contentFadeTime * 0.5f); swapped = true; }
            // Phase 2: it lands on the left side.
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((turnTimer - half) / half));
            SetFlap(new Vector2(1f, 0.5f), -Spine, s);
        }

        if (turnTimer >= turnTime) FinishTurn();
    }

    private void FinishTurn()
    {
        if (!swapped) { ShowPage(0f); swapped = true; }
        turnTimer = float.MaxValue;
        if (flap != null) flap.gameObject.SetActive(false);
        fadeTimer = 0f; // content fades back in
    }

    private void SetFlap(Vector2 pivot, float x, float scaleX)
    {
        flap.pivot = pivot;
        flap.anchoredPosition = new Vector2(x, 0f);
        flap.localScale = new Vector3(Mathf.Max(0.001f, scaleX), 1f, 1f);
        Image img = flap.GetComponent<Image>();
        if (img != null) img.color = Color.Lerp(new Color(0.78f, 0.74f, 0.68f), Color.white, scaleX); // darker when edge-on
    }

    private void SetContentAlpha(float a)
    {
        if (leftContent != null) leftContent.alpha = a;
        if (rightContent != null) rightContent.alpha = a;
    }

    private static float BackOut(float t)
    {
        const float c1 = 1.4f, c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }

    // ------------------------------------------------------------------ content

    private void ShowPage(float diagramDelay)
    {
        JournalPage p = entry.pages.Count > 0 ? entry.pages[Mathf.Clamp(page, 0, entry.pages.Count - 1)] : new JournalPage();
        Set(titleText, entry.title);
        Set(bigIdeaText, p.bigIdea, bigIdeaCard);
        Set(notesText, p.notes);
        Set(lookCloserText, p.lookCloser, lookCloserCard);
        Set(funFactText, p.funFact, funFactRow);
        Set(marginP1Text, p.marginP1, marginP1Row);
        Set(marginP2Text, p.marginP2, marginP2Row);
        Set(captionText, p.diagram != null ? p.diagram.caption : "");
        bool hasSketch = p.sketch != null && sketchImage != null;
        if (diagramCard != null) diagramCard.SetActive(hasSketch || (p.diagram != null && p.diagram.show));
        if (diagram != null) diagram.Draw(hasSketch ? null : p.diagram, diagramDelay);
        if (sketchImage != null)
        {
            sketchImage.gameObject.SetActive(hasSketch);
            if (hasSketch) sketchImage.sprite = p.sketch;
        }

        int count = Mathf.Max(1, entry.pages.Count);
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] == null) continue;
            dots[i].gameObject.SetActive(count > 1 && i < count);
            dots[i].color = i == page ? DotOn : DotOff;
        }
        LayoutBottomBar();
    }

    // Right-aligned: [P1 key] [P2 key]  Next page ▸   (or "Close" on the last page)
    private void LayoutBottomBar()
    {
        bool more = HasNextPage;
        if (pageCurl != null) pageCurl.gameObject.SetActive(more);
        if (holdHint != null) holdHint.gameObject.SetActive(more);
        if (promptArrow != null) promptArrow.SetActive(more);
        if (promptLabel == null) return;

        promptLabel.text = more ? "Next page" : "Close";
        float x = promptRight - (more ? 32f : 0f);
        float w = promptLabel.GetPreferredValues(promptLabel.text).x;
        RectTransform lr = promptLabel.rectTransform;
        lr.sizeDelta = new Vector2(w + 4f, lr.sizeDelta.y);
        lr.anchoredPosition = new Vector2(x, lr.anchoredPosition.y);
        x -= w + 16f;

        for (int i = keyBadges.Length - 1; i >= 0; i--)
        {
            RectTransform badge = keyBadges[i];
            if (badge == null) continue;
            PlayerInputReader r = ReaderFor(i);
            badge.gameObject.SetActive(r != null);
            if (r == null || keyTexts[i] == null) continue;
            keyTexts[i].text = r.InteractDisplayName;
            float bw = Mathf.Max(44f, keyTexts[i].GetPreferredValues(keyTexts[i].text).x + 30f);
            badge.sizeDelta = new Vector2(bw, badge.sizeDelta.y);
            badge.anchoredPosition = new Vector2(x, badge.anchoredPosition.y);
            x -= bw + 8f;
        }
    }

    private PlayerInputReader ReaderFor(int playerIndex)
    {
        foreach (PlayerInputReader r in readers) if (r != null && r.PlayerIndex == playerIndex) return r;
        return null;
    }

    private void FindReaders()
    {
        readers = FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None);
        System.Array.Sort(readers, (a, b) => a.PlayerIndex.CompareTo(b.PlayerIndex));
        holdTimes.Clear();
        tracking.Clear();
        foreach (PlayerInputReader _ in readers) { holdTimes.Add(0f); tracking.Add(false); }
        if (holdFill != null) holdFill.enabled = false;
    }

    private static void Set(TMP_Text text, string value, GameObject section = null)
    {
        bool has = !string.IsNullOrWhiteSpace(value);
        if (section != null) section.SetActive(has);
        if (text != null) text.text = has ? value : "";
    }

#if UNITY_EDITOR
    public void EditorSetup(TMP_Text title, TMP_Text bigIdea, TMP_Text notes, TMP_Text lookCloser, TMP_Text funFact,
        TMP_Text marginP1, TMP_Text marginP2, TMP_Text caption,
        GameObject bigIdeaSection, GameObject lookCloserSection, GameObject funFactSection,
        GameObject marginP1Section, GameObject marginP2Section, GameObject diagramSection, ForceDiagramView diagramView,
        CanvasGroup canvasGroup, CanvasGroup left, CanvasGroup right, RectTransform flapPage, RectTransform curl)
    {
        titleText = title; bigIdeaText = bigIdea; notesText = notes; lookCloserText = lookCloser; funFactText = funFact;
        marginP1Text = marginP1; marginP2Text = marginP2; captionText = caption;
        bigIdeaCard = bigIdeaSection; lookCloserCard = lookCloserSection; funFactRow = funFactSection;
        marginP1Row = marginP1Section; marginP2Row = marginP2Section; diagramCard = diagramSection; diagram = diagramView;
        group = canvasGroup; leftContent = left; rightContent = right; flap = flapPage; pageCurl = curl;
    }

    public void EditorSetupSketch(Image sketch) => sketchImage = sketch;

    public void EditorSetupBar(Image[] pageDots, RectTransform[] badges, TMP_Text[] keys, TMP_Text label, GameObject arrow, TMP_Text hint, Image fill)
    {
        dots = pageDots; keyBadges = badges; keyTexts = keys; promptLabel = label; promptArrow = arrow; holdHint = hint; holdFill = fill;
    }
#endif
}
