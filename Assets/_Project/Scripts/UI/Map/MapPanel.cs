using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The map screen (part of UI_Canvas): Wren's ink-and-parchment map of a region. Opens from the wall map in Wren's Lab
/// (WallMap) and from the pause menu's Map button.
///   - Region totals at the top (shards, machine parts, journal entries read).
///   - The rooms along a winding path from Alderbrook (MapMarker): reached rooms are inked with their names and progress,
///     the furthest one is pinned, unreached rooms are faint and unnamed. Locked regions are fogged ("coming soon").
///   - Travel: choose a reached room and both players go there. In the region, they move straight to that room
///     (RoomManager.GoToRoomNumber); anywhere else (the village), the region loads at that room (GameFlow.EnterRegion).
///   - Back (button, Esc, gamepad Start or B) closes it.
/// While open, time is stopped and both players' controls are off (as in the pause menu). Everything shown is read
/// from the save through the RegionMapData asset.
///
/// Note: like the other menus, it reads the keyboard/gamepads directly (menu keys, not gameplay input) and is navigated
/// through the EventSystem (arrows/stick + Enter/South, or the mouse).
/// </summary>
public class MapPanel : MonoBehaviour
{
    [SerializeField] private RegionMapData map;
    [Tooltip("One marker per room, in the same order as the map asset's rooms.")]
    [SerializeField] private MapMarker[] markers = new MapMarker[0];
    [SerializeField] private TMP_Text totals;
    [SerializeField] private TMP_Text info;
    [SerializeField] private TMP_Text hint;
    [SerializeField] private Button back;
    [Tooltip("Seconds after opening before presses count, so the press that opened the map doesn't also travel or close it.")]
    [SerializeField, Min(0f)] private float inputDelay = 0.25f;

    private Action onBack;
    private Action onLeave;
    private bool pausedHere;
    private float openedAt;
    private MapMarker lastSelected;

    public bool IsOpen => gameObject.activeSelf;
    public RegionMapData Map => map;

    /// <summary>Frame the map last closed on, so the same Esc/B press does not also act on the menu behind it.</summary>
    public static int ClosedFrame { get; private set; } = -1;

    private void Awake()
    {
        back.onClick.AddListener(Close);
        for (int i = 0; i < markers.Length; i++) markers[i].Init(this, i);
    }

    /// <summary>
    /// Opens the map. backTo: called when Back closes it (e.g. show the pause menu again).
    /// leaving: called when the players travel (e.g. the pause menu resumes the game).
    /// If the game isn't paused already, the map pauses it while open.
    /// </summary>
    public void Open(Action backTo = null, Action leaving = null)
    {
        if (map == null) { Debug.LogWarning("MapPanel: no RegionMapData assigned.", this); return; }
        onBack = backTo;
        onLeave = leaving;
        pausedHere = !PauseMenu.IsPaused;
        if (pausedHere)
        {
            Time.timeScale = 0f;
            SetPlayerControls(false);
        }
        gameObject.SetActive(true);
        openedAt = Time.unscaledTime;
        Refresh();
    }

    /// <summary>Back: closes the map and returns to whatever opened it.</summary>
    public void Close()
    {
        if (!IsOpen) return;
        Action backTo = onBack;
        Hide();
        backTo?.Invoke();
    }

    /// <summary>Called by a marker: both players go to room 'index' (0-based), if it has been reached.</summary>
    public void Travel(int index)
    {
        if (!IsOpen || TooSoon || !map.IsReached(index)) return;
        RoomManager rooms = FindFirstObjectByType<RoomManager>();
        bool inRegion = rooms != null && rooms.RegionId == map.regionId;
        Action leaving = onLeave;
        Hide();
        leaving?.Invoke();
        if (inRegion) rooms.GoToRoomNumber(index + 1);
        else GameFlow.EnterRegion(map.regionId, index + 1);
    }

    /// <summary>Shows a room's details in the line under the map (called when its marker is selected).</summary>
    public void Describe(int index)
    {
        lastSelected = markers[index];
        RegionMapData.RoomInfo room = map.rooms[index];
        string text = $"{index + 1} · {room.displayName}   -   Shards {Mathf.Min(map.ShardsFound(index), room.shards)}/{room.shards}";
        if (map.HasPart(index)) text += $"   ·   {room.partName}: {(map.PartFound(index) ? "found" : "not found yet")}";
        if (map.HasJournal(index)) text += $"   ·   Journal: {(map.JournalRead(index) ? "read" : "not read yet")}";
        info.text = text;
        hint.text = CurrentRoomIndex() == index ? "You are here" : "Enter / A: travel here";
    }

    private void Update()
    {
        if (TooSoon) return;
        bool pressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                       || Gamepad.all.Any(g => g.startButton.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame);
        if (pressed) Close();
    }

    private bool TooSoon => Time.unscaledTime - openedAt < inputDelay;

    private void Refresh()
    {
        (int s, int sTotal) = map.ShardTotals();
        (int p, int pTotal) = map.PartTotals();
        (int j, int jTotal) = map.JournalTotals();
        totals.text = $"Shards {s}/{sTotal}      Machine parts {p}/{pTotal}      Journal {j}/{jTotal}";

        int furthest = map.ReachedRooms - 1;
        int count = Mathf.Min(markers.Length, map.rooms.Count);
        for (int i = 0; i < count; i++) markers[i].Show(map, i == furthest);
        SetNavigation(count);

        // Start on the room the players are in (in the region), else on the furthest room reached.
        int start = CurrentRoomIndex();
        if (start < 0 || !map.IsReached(start)) start = furthest;
        if (start >= 0 && start < count) EventSystem.current?.SetSelectedGameObject(markers[start].Button.gameObject);
        if (start >= 0 && start < count) Describe(start); // also when the EventSystem already had it selected
    }

    // Left/right (and up/down along the path) step through the reached rooms in path order; down from a room goes
    // to Back, up from Back returns to the last room selected.
    private void SetNavigation(int count)
    {
        int reached = Mathf.Min(map.ReachedRooms, count);
        for (int i = 0; i < count; i++)
        {
            Navigation nav = new Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit };
            if (i < reached)
            {
                nav.selectOnLeft = i > 0 ? markers[i - 1].Button : null;
                nav.selectOnRight = i + 1 < reached ? markers[i + 1].Button : null;
                nav.selectOnDown = back;
                nav.selectOnUp = nav.selectOnRight;
            }
            markers[i].Button.navigation = nav;
        }
        MapMarker up = lastSelected != null ? lastSelected : (reached > 0 ? markers[reached - 1] : null);
        back.navigation = new Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnUp = up != null ? up.Button : null };
    }

    // The room the players are in, if the map is open inside its own region; else -1.
    private int CurrentRoomIndex()
    {
        RoomManager rooms = FindFirstObjectByType<RoomManager>();
        if (rooms == null || rooms.RegionId != map.regionId || rooms.CurrentRoom == null) return -1;
        return map.rooms.FindIndex(r => r.roomObject == rooms.CurrentRoom.name);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
        ClosedFrame = Time.frameCount;
        onBack = onLeave = null;
        if (pausedHere)
        {
            pausedHere = false;
            Time.timeScale = 1f;
            SetPlayerControls(true);
        }
    }

    private static void SetPlayerControls(bool on)
    {
        LocalCoopSpawner spawner = FindFirstObjectByType<LocalCoopSpawner>();
        if (spawner == null) return;
        foreach (PlayerInput p in spawner.Players)
        {
            if (p == null) continue;
            if (on) p.ActivateInput(); else p.DeactivateInput();
        }
    }

    private void OnDestroy()
    {
        if (pausedHere) Time.timeScale = 1f; // never leave the game frozen when the scene changes
    }

#if UNITY_EDITOR
    public void EditorSetup(RegionMapData data, MapMarker[] roomMarkers, TMP_Text totalsText, TMP_Text infoText, TMP_Text hintText, Button backButton)
    {
        map = data; markers = roomMarkers; totals = totalsText; info = infoText; hint = hintText; back = backButton;
    }
#endif
}
