using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The Village Fund ledger (part of UI_Canvas), opened by talking to Bram (NpcTalk with Opens Village Fund).
/// Shows one of his lines, the shards there are to spend (VillageFund.Balance) and the restoration projects (FundRow).
/// Fund a project with its button: it's restored at once (RestorationProject.Restore) and the ledger says where to see it.
/// While open, time stops and both players' controls are off (MenuFreeze). Back (button, Esc, gamepad Start or B) closes it.
/// Like the other menus, it reads the keyboard/gamepads directly (menu keys) and is navigated through the EventSystem.
/// </summary>
public class FundPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text speech;
    [SerializeField] private TMP_Text balance;
    [SerializeField] private TMP_Text status;
    [Tooltip("One row per project, in VillageFund.All order.")]
    [SerializeField] private FundRow[] rows = new FundRow[0];
    [SerializeField] private Button back;
    [Tooltip("Seconds after opening before presses count, so the press that opened it doesn't also fund or close.")]
    [SerializeField, Min(0f)] private float inputDelay = 0.25f;

    private static readonly Color Ink = new Color32(74, 47, 28, 255), Green = new Color32(62, 98, 48, 255), Red = new Color32(150, 60, 40, 255);

    private bool frozeHere;
    private float openedAt;

    public bool IsOpen => gameObject.activeSelf;

    /// <summary>Frame the ledger last closed on, so the press that closed it doesn't also open it again.</summary>
    public static int ClosedFrame { get; private set; } = -1;

    private void Awake()
    {
        back.onClick.AddListener(Close);
        for (int i = 0; i < rows.Length; i++) rows[i].Init(this, i);
    }

    /// <summary>Opens the ledger with a line from the villager who keeps it (e.g. Bram).</summary>
    public void Open(string who, Color whoColor, string line)
    {
        frozeHere = MenuFreeze.Freeze();
        gameObject.SetActive(true);
        openedAt = Time.unscaledTime;
        speech.text = string.IsNullOrEmpty(line) ? "" : $"<color=#{ColorUtility.ToHtmlStringRGB(whoColor)}>{who}</color>  {line}";
        status.text = "";
        Refresh();
        // Start on the first project that can be funded, else the first one still open, else Back.
        int start = -1;
        for (int i = 0; i < Count && start < 0; i++) if (!VillageFund.IsFunded(VillageFund.All[i]) && VillageFund.Balance >= VillageFund.All[i].price) start = i;
        for (int i = 0; i < Count && start < 0; i++) if (!VillageFund.IsFunded(VillageFund.All[i])) start = i;
        EventSystem.current?.SetSelectedGameObject(start >= 0 ? rows[start].Button.gameObject : back.gameObject);
    }

    public void Close()
    {
        if (!IsOpen) return;
        gameObject.SetActive(false);
        ClosedFrame = Time.frameCount;
        if (frozeHere) { frozeHere = false; MenuFreeze.Unfreeze(); }
    }

    /// <summary>Called by a row's button.</summary>
    public void Fund(int index)
    {
        if (!IsOpen || TooSoon || index >= Count) return;
        VillageFund.Project p = VillageFund.All[index];
        if (VillageFund.IsFunded(p)) return;
        if (VillageFund.Fund(p))
        {
            status.color = Green;
            status.text = $"Funded: {p.name}! Go and see {p.where}.";
        }
        else
        {
            status.color = Red;
            int need = p.price - VillageFund.Balance;
            status.text = $"{need} more shard{(need == 1 ? "" : "s")} needed. There are more out in the Shattered Grounds.";
        }
        Refresh();
        // The funded row's button greys out: move on to the next open project (or Back).
        if (EventSystem.current != null && (EventSystem.current.currentSelectedGameObject == null || !EventSystem.current.currentSelectedGameObject.activeInHierarchy
            || !rows[index].Button.interactable))
        {
            FundRow next = rows.Take(Count).FirstOrDefault(r => r.Button.interactable);
            EventSystem.current.SetSelectedGameObject(next != null ? next.Button.gameObject : back.gameObject);
        }
    }

    private int Count => Mathf.Min(rows.Length, VillageFund.All.Length);

    private bool TooSoon => Time.unscaledTime - openedAt < inputDelay;

    private void Update()
    {
        if (TooSoon) return;
        bool pressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                       || Gamepad.all.Any(g => g.startButton.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame);
        if (pressed) Close();
    }

    private void Refresh()
    {
        int have = VillageFund.Balance;
        balance.color = Ink;
        balance.text = $"Shards to spend: <b>{have}</b>      (collected {VillageFund.Collected})";
        for (int i = 0; i < Count; i++) rows[i].Show(VillageFund.All[i], have);

        // Up/down through the open projects, then Back.
        Selectable prev = null;
        for (int i = 0; i < Count; i++)
        {
            Button b = rows[i].Button;
            if (!b.interactable) continue;
            Navigation nav = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = prev, selectOnDown = back };
            b.navigation = nav;
            if (prev != null) { Navigation pn = prev.navigation; pn.selectOnDown = b; prev.navigation = pn; }
            prev = b;
        }
        back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = prev };
    }

    private void OnDestroy()
    {
        if (frozeHere) MenuFreeze.Unfreeze(); // never leave the game frozen when the scene changes
    }

#if UNITY_EDITOR
    public void EditorSetup(TMP_Text speechText, TMP_Text balanceText, TMP_Text statusText, FundRow[] projectRows, Button backButton)
    {
        speech = speechText; balance = balanceText; status = statusText; rows = projectRows; back = backButton;
    }
#endif
}
