#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Developer shortcuts for testing rooms without solving the ones before them. Exists ONLY in the Editor and in
/// Development builds (the whole file is compiled out of release builds). Created automatically when Play starts
/// in a scene with a RoomManager; nothing to set up.
///
///   Page Down / Page Up  both players jump to the next / previous room (same as walking through a doorway)
///   F9                   reset the current room (crates home, levers off, players to the entrance)
///   F10                  hold the current room's gates open (press again to release)
/// Tools > Shards of Motion > Dev > Play From Room > Room N starts Play and jumps straight to room N.
///
/// Note: these hotkeys read the keyboard directly. That's fine for a developer tool, but gameplay scripts must
/// still read input only through PlayerInputReader (so the two players stay separate).
/// Rooms are numbered left to right by their position in the scene.
/// </summary>
public class DevTools : MonoBehaviour
{
    /// <summary>Editor key holding the room number to start in (set by the Play From Room menu; survives the
    /// script reload when Play starts). 0 = start normally.</summary>
    public const string StartRoomKey = "ShardsOfMotion.DevStartRoom";

    private RoomManager manager;
    private Room[] rooms = new Room[0];
    private bool startHandled;
    private bool gatesOpen;
    private GUIStyle style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindFirstObjectByType<RoomManager>() == null || FindFirstObjectByType<DevTools>() != null) return;
        new GameObject("DevTools (editor/dev builds only)").AddComponent<DevTools>();
    }

    private void Start()
    {
        manager = FindFirstObjectByType<RoomManager>();
        rooms = FindObjectsByType<Room>(FindObjectsSortMode.None).OrderBy(r => r.transform.position.x).ToArray();
    }

    private void Update()
    {
        if (manager == null || rooms.Length == 0) return;

        // Play From Room: once the players exist and the starting room is set, jump to the requested room.
        if (!startHandled && manager.CurrentRoom != null)
        {
            startHandled = true;
#if UNITY_EDITOR
            int n = UnityEditor.SessionState.GetInt(StartRoomKey, 0);
            UnityEditor.SessionState.EraseInt(StartRoomKey); // one time only: the next normal Play starts in Room 1
            if (n >= 1 && n <= rooms.Length) GoTo(rooms[n - 1]);
#endif
        }

        Keyboard k = Keyboard.current;
        if (k == null) return;
        int current = System.Array.IndexOf(rooms, manager.CurrentRoom);
        if (k.pageDownKey.wasPressedThisFrame && current >= 0 && current < rooms.Length - 1) GoTo(rooms[current + 1]);
        if (k.pageUpKey.wasPressedThisFrame && current > 0) GoTo(rooms[current - 1]);
        if (k.f9Key.wasPressedThisFrame) manager.ResetCurrentRoom();
        if (k.f10Key.wasPressedThisFrame) { gatesOpen = !gatesOpen; ApplyGates(); }
    }

    private void GoTo(Room room)
    {
        if (gatesOpen) { gatesOpen = false; ApplyGates(); } // release the old room's gates

        // Arrive exactly where players arrive when walking in: the arrival points of the doorway from the previous room
        // (Doorway_R{n-1}_R{n}, e.g. on a raised landing). Fallback: the room's own entry points.
        int n = System.Array.IndexOf(rooms, room) + 1;
        GameObject doorway = n > 1 ? GameObject.Find($"Doorway_R{n - 1}_R{n}") : null;
        Transform a1 = doorway != null ? doorway.transform.Find("Arrive_Right_P1") : null;
        Transform a2 = doorway != null ? doorway.transform.Find("Arrive_Right_P2") : null;
        manager.TransitionTo(room, a1 != null ? a1 : room.Player1Entry, a2 != null ? a2 : room.Player2Entry);
    }

    // Every mover named "Gate..." in the current room goes to (or stops being held at) its open end.
    private void ApplyGates()
    {
        if (manager.CurrentRoom == null) return;
        foreach (Mover m in manager.CurrentRoom.GetComponentsInChildren<Mover>(true))
            if (m.name.StartsWith("Gate")) m.DevHoldOpen = gatesOpen;
        foreach (Mover m in FindObjectsByType<Mover>(FindObjectsSortMode.None))
            if (!gatesOpen) m.DevHoldOpen = false;
    }

    private void OnGUI()
    {
        if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(1f, 1f, 1f, 0.75f) } };
        int current = System.Array.IndexOf(rooms, manager != null ? manager.CurrentRoom : null) + 1;
        GUI.Label(new Rect(10, Screen.height - 26, 900, 24),
            $"DEV  Room {current}/{rooms.Length}   PgDn/PgUp: next/prev room   F9: reset room   F10: gates {(gatesOpen ? "HELD OPEN" : "normal")}", style);
    }
}
#endif
