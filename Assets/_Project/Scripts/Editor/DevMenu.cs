using UnityEditor;

/// <summary>
/// Tools > Shards of Motion > Dev > Play From Room > Room N: enters Play mode and jumps straight to room N
/// (DevTools does the jump once the players have spawned). Normal Play still starts in Room 1.
/// Open the region scene first (e.g. Region1_ShatteredGrounds).
/// </summary>
public static class DevMenu
{
    private const string Menu = "Tools/Shards of Motion/Dev/Play From Room/";

    [MenuItem(Menu + "Room 1")] private static void R1() => PlayFrom(1);
    [MenuItem(Menu + "Room 2")] private static void R2() => PlayFrom(2);
    [MenuItem(Menu + "Room 3")] private static void R3() => PlayFrom(3);
    [MenuItem(Menu + "Room 4")] private static void R4() => PlayFrom(4);
    [MenuItem(Menu + "Room 5")] private static void R5() => PlayFrom(5);
    [MenuItem(Menu + "Room 6")] private static void R6() => PlayFrom(6);
    [MenuItem(Menu + "Room 7")] private static void R7() => PlayFrom(7);
    [MenuItem(Menu + "Room 8")] private static void R8() => PlayFrom(8);

    private static void PlayFrom(int room)
    {
        if (EditorApplication.isPlaying) return;
        SessionState.SetInt(DevTools.StartRoomKey, room);
        EditorApplication.isPlaying = true;
    }
}
