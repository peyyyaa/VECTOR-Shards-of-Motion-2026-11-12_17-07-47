using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The Momentum Engine assembly screen (part of UI_Canvas; opened by MomentumEngine in Room 8 once all its parts are found).
/// A blueprint of the empty engine with the parts in a tray beside it. Each player moves their own ring cursor (P1 orange,
/// P2 teal) with their own movement keys / stick and grabs or drops a part with their own Interact, read through their
/// PlayerInputReader (MenuMove, MenuGrabPressedThisFrame). Dropping a part on its socket fits it. Three small physics rules:
///   - FORCE: heavy parts (flywheel, counterweight) barely move with one cursor (a = F / m: one pull, little acceleration);
///     grab them with both and they move freely (two pulls add up).
///   - FRICTION: the flywheel won't go on the bare axle; the bearing goes in first.
///   - WEIGHT: the counterweight hangs on the hook (W = m·g).
/// When all are fitted it says so, closes and tells the engine (MomentumEngine.CompleteInstall). Esc / Start / B steps
/// away without installing (the parts go back to the tray next time).
/// While open, time is stopped and gameplay ignores input (MenuFreeze.Freeze(keepControlsForMenu: true)).
/// Positions are in canvas units (1080p reference) relative to the Board.
/// </summary>
public class EngineAssemblyPanel : MonoBehaviour
{
    [Serializable]
    public class Part
    {
        public string name = "Part";
        [Tooltip("This part's line in the checklist, e.g. \"Bearing  ->  the axle (centre)\".")]
        public string step = "";
        public RectTransform rect;
        public Image image;
        [Tooltip("Needs both cursors to move at full speed.")]
        public bool heavy;
        public Vector2 trayPosition;
        public Vector2 socketPosition;
        [Tooltip("Sprite and size once fitted. No sprite: the part's image hides and the blueprint becomes Blueprint After.")]
        public Sprite fittedSprite;
        public Vector2 fittedSize;
        public Sprite blueprintAfter;
        [Tooltip("Index of a part that must be fitted first (-1: none).")]
        public int requires = -1;
        [TextArea] public string requiresHint;
        [TextArea] public string fittedHint;
        public Image socketMark;

        [NonSerialized] public bool fitted;
        [NonSerialized] public int holders;                       // bit 0 = P1, bit 1 = P2
        [NonSerialized] public Vector2[] offsets = new Vector2[2]; // grab point to part centre, per player
        [NonSerialized] public Sprite traySprite;
        [NonSerialized] public Vector2 traySize;
        [NonSerialized] public Vector2 position;
    }

    [SerializeField] private RectTransform board;
    [SerializeField] private Image blueprint;
    [SerializeField] private Part[] parts = new Part[0];
    [SerializeField] private RectTransform[] cursors = new RectTransform[2];
    [SerializeField] private TMP_Text hint;
    [Tooltip("The numbered checklist: done steps ticked off, the next one highlighted.")]
    [SerializeField] private TMP_Text steps;
    [SerializeField] private TMP_Text controls;
    [Tooltip("Half the area cursors can move in (canvas units).")]
    [SerializeField] private Vector2 boardHalfSize = new Vector2(700f, 400f);
    [SerializeField, Min(1f)] private float cursorSpeed = 750f;
    [Tooltip("Speed factor for a heavy part held by one cursor (one force instead of two, a = F / m).")]
    [SerializeField, Range(0.02f, 1f)] private float heavyAloneFactor = 0.15f;
    [SerializeField, Min(1f)] private float grabRadius = 90f;
    [SerializeField, Min(1f)] private float snapRadius = 80f;
    [SerializeField, Min(0f)] private float inputDelay = 0.25f;
    [SerializeField, Min(0f)] private float finishDelay = 1.6f;

    private readonly PlayerInputReader[] readers = new PlayerInputReader[2];
    private readonly Vector2[] cursorPos = new Vector2[2];
    private readonly int[] holding = { -1, -1 };
    private MomentumEngine engine;
    private Sprite blueprintStart;
    private bool frozeHere;
    private bool finishing;
    private float openedAt, finishTimer;
    private static readonly string Intro = "Move your ring onto a part and press Interact to pick it up. Carry it to its glowing outline " +
                                           "on the engine and press Interact again to drop it in.";

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        if (blueprint != null) blueprintStart = blueprint.sprite;
        foreach (Part p in parts)
        {
            if (p.image != null) p.traySprite = p.image.sprite;
            if (p.rect != null) p.traySize = p.rect.sizeDelta;
        }
    }

    public void Open(MomentumEngine target)
    {
        if (IsOpen || target == null) return;
        frozeHere = MenuFreeze.Freeze(keepControlsForMenu: true);
        if (!frozeHere) return; // the pause menu or another screen is up
        engine = target;
        gameObject.SetActive(true); // the first time, this runs Awake (remembers the tray look)
        openedAt = Time.unscaledTime;
        finishing = false;

        foreach (PlayerInputReader r in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))
            if (r.PlayerIndex == 0 || r.PlayerIndex == 1) readers[r.PlayerIndex] = r;
        for (int i = 0; i < 2; i++)
        {
            cursorPos[i] = new Vector2(i == 0 ? -120f : 120f, -260f);
            holding[i] = -1;
            if (cursors[i] != null) cursors[i].gameObject.SetActive(readers[i] != null);
        }
        if (blueprint != null) blueprint.sprite = blueprintStart;
        foreach (Part p in parts) ResetPart(p);
        SetHint(Intro);
        if (controls != null)
            controls.text = $"P1: move + {Key(0)} to grab / drop      P2: move + {Key(1)} to grab / drop      Esc / B: step away";
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen) return;
        gameObject.SetActive(false);
        for (int i = 0; i < 2; i++) readers[i] = null;
        if (frozeHere) MenuFreeze.Unfreeze();
        frozeHere = false;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (finishing)
        {
            if ((finishTimer -= dt) > 0f) return;
            MomentumEngine target = engine;
            Close();
            if (target != null) target.CompleteInstall();
            return;
        }
        if (Time.unscaledTime - openedAt < inputDelay) return;

        bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                    || Gamepad.all.Any(g => g.startButton.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame);
        if (back) { Close(); return; }

        for (int i = 0; i < 2; i++)
        {
            PlayerInputReader r = readers[i];
            if (r == null) continue;
            float speed = cursorSpeed;
            int h = holding[i];
            if (h >= 0 && parts[h].heavy && CountHolders(parts[h]) < 2) speed *= heavyAloneFactor;
            cursorPos[i] += r.MenuMove * speed * dt;
            cursorPos[i] = new Vector2(Mathf.Clamp(cursorPos[i].x, -boardHalfSize.x, boardHalfSize.x),
                                       Mathf.Clamp(cursorPos[i].y, -boardHalfSize.y, boardHalfSize.y));
            if (r.MenuGrabPressedThisFrame) { if (holding[i] >= 0) Release(i); else Grab(i); }
        }

        foreach (Part p in parts)
        {
            if (p.holders == 0) continue;
            Vector2 sum = Vector2.zero;
            int n = 0;
            for (int i = 0; i < 2; i++)
                if ((p.holders & (1 << i)) != 0) { sum += cursorPos[i] + p.offsets[i]; n++; }
            p.position = sum / n;
        }
        Refresh();
    }

    private void Grab(int player)
    {
        int best = -1;
        float bestDist = float.MaxValue;
        for (int k = 0; k < parts.Length; k++)
        {
            Part p = parts[k];
            if (p.fitted) continue;
            float reach = Mathf.Max(grabRadius, Mathf.Min(p.rect.sizeDelta.x, p.rect.sizeDelta.y) * 0.5f);
            float d = Vector2.Distance(cursorPos[player], p.position);
            if (d <= reach && d < bestDist) { best = k; bestDist = d; }
        }
        if (best < 0) return;
        Part part = parts[best];
        holding[player] = best;
        part.holders |= 1 << player;
        part.offsets[player] = part.position - cursorPos[player];
        part.rect.SetAsLastSibling();
        KeepCursorsOnTop();
        if (part.heavy && CountHolders(part) < 2)
            SetHint($"The {part.name} is heavy: one pull barely moves it (a = F / m). Grab it together: two forces add up.");
        else if (part.heavy)
            SetHint($"Together! Two pulls on the {part.name}: double the force, double the acceleration.");
    }

    private void Release(int player)
    {
        int k = holding[player];
        holding[player] = -1;
        if (k < 0) return;
        Part part = parts[k];
        part.holders &= ~(1 << player);
        if (part.holders == 0) TryFit(k);
    }

    private void TryFit(int k)
    {
        Part part = parts[k];
        if (Vector2.Distance(part.position, part.socketPosition) <= snapRadius)
        {
            if (part.requires >= 0 && part.requires < parts.Length && !parts[part.requires].fitted)
            {
                SetHint(part.requiresHint);
                part.position = part.trayPosition;
                return;
            }
            part.fitted = true;
            part.position = part.socketPosition;
            if (part.fittedSprite != null)
            {
                part.image.sprite = part.fittedSprite;
                part.rect.sizeDelta = part.fittedSize;
            }
            else
            {
                part.image.enabled = false;
                if (blueprint != null && part.blueprintAfter != null) blueprint.sprite = part.blueprintAfter;
            }
            if (part.socketMark != null) part.socketMark.enabled = false;
            SetHint(part.fittedHint);
            if (parts.All(p => p.fitted))
            {
                SetHint(part.fittedHint + "\n<color=#3e6230>The Momentum Engine is whole again!</color>");
                finishing = true;
                finishTimer = finishDelay;
            }
            return;
        }
        for (int j = 0; j < parts.Length; j++)
            if (j != k && !parts[j].fitted && Vector2.Distance(part.position, parts[j].socketPosition) <= snapRadius)
            {
                SetHint($"The {part.name} doesn't fit there.");
                part.position = part.trayPosition;
                return;
            }
        // Dropped elsewhere: it stays where it was put.
    }

    private void Refresh()
    {
        for (int k = 0; k < parts.Length; k++)
        {
            Part p = parts[k];
            if (p.rect == null) continue;
            Vector2 at = p.position;
            if (p.holders != 0 && p.heavy && CountHolders(p) < 2)
                at += new Vector2(Mathf.Sin(Time.unscaledTime * 40f) * 2f, 0f); // straining
            p.rect.anchoredPosition = at;
            if (p.socketMark != null && !p.fitted)
            {
                // The next step's outline glows; outlines for later steps stay faint.
                bool next = k == NextStep;
                bool near = p.holders != 0 && Vector2.Distance(p.position, p.socketPosition) <= snapRadius;
                float pulse = next ? 0.45f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f) : 0.12f;
                p.socketMark.color = near ? new Color(0.25f, 0.6f, 0.3f, 0.85f) : new Color(0.29f, 0.18f, 0.11f, pulse);
            }
        }
        if (steps != null)
        {
            string text = "";
            int next = NextStep;
            for (int k = 0; k < parts.Length; k++)
            {
                string line = $"{k + 1}. {parts[k].step}";
                if (parts[k].fitted) line = $"<color=#3e6230><s>{line}</s>  done</color>";
                else if (k == next) line = $"<b><color=#a26029>> {line}</color></b>";
                else line = $"<color=#8a7a66>{line}</color>";
                text += (k > 0 ? "\n" : "") + line;
            }
            steps.text = text;
        }
        for (int i = 0; i < 2; i++)
        {
            if (cursors[i] == null) continue;
            cursors[i].anchoredPosition = cursorPos[i];
            cursors[i].localScale = Vector3.one * (holding[i] >= 0 ? 0.8f : 1f);
        }
    }

    /// <summary>The first part (in checklist order) not fitted yet, or -1.</summary>
    private int NextStep
    {
        get
        {
            for (int k = 0; k < parts.Length; k++) if (!parts[k].fitted) return k;
            return -1;
        }
    }

    private void ResetPart(Part p)
    {
        p.fitted = false;
        p.holders = 0;
        p.position = p.trayPosition;
        if (p.image != null) { p.image.enabled = true; p.image.sprite = p.traySprite; }
        if (p.rect != null) p.rect.sizeDelta = p.traySize;
        if (p.socketMark != null) p.socketMark.enabled = true;
    }

    private void KeepCursorsOnTop()
    {
        foreach (RectTransform c in cursors) if (c != null) c.SetAsLastSibling();
    }

    private static int CountHolders(Part p) => (p.holders & 1) + ((p.holders >> 1) & 1);

    private string Key(int player) => readers[player] != null ? readers[player].InteractDisplayName : "Interact";

    private void SetHint(string text)
    {
        if (hint != null) hint.text = text;
    }

#if UNITY_EDITOR
    public void EditorSetup(RectTransform boardRect, Image blueprintImage, Part[] partList, RectTransform[] cursorRects, TMP_Text hintText,
                            TMP_Text controlsText, TMP_Text stepsText, Vector2 halfSize)
    {
        board = boardRect; blueprint = blueprintImage; parts = partList; cursors = cursorRects; hint = hintText; controls = controlsText;
        steps = stepsText; boardHalfSize = halfSize;
    }
#endif
}
