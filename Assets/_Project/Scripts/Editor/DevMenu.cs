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
    [MenuItem(Menu + "Room 8 part 2 (Heart Engine)")] private static void R9() => PlayFrom(9);
    [MenuItem(Menu + "Room 8 part 3 (Great Shard)")] private static void R10() => PlayFrom(10);

    /// <summary>Forgets that the Heart Engine was assembled and woken (save events), so the assembly screen and the
    /// awakening can be tested again. Everything else in the save (shards, parts, rooms) is kept. Outside Play mode only.</summary>
    [MenuItem("Tools/Shards of Motion/Dev/Reset Heart Engine (save)")]
    private static void ResetHeartEngine()
    {
        if (EditorApplication.isPlaying) { UnityEngine.Debug.LogWarning("Dev: stop Play mode first, then reset the Heart Engine."); return; }
        SaveSystem.Load();
        int removed = SaveSystem.Data.events == null ? 0
            : SaveSystem.Data.events.RemoveAll(e => e == "r1_heart_engine_installed" || e == "r1_engine_awake");
        SaveSystem.Save();
        UnityEngine.Debug.Log($"Dev: Heart Engine reset ({removed} saved event(s) removed): not assembled, not awake.");
    }

    /// <summary>Marks Region 1 as complete in the save (as freeing the Great Shard does), to test the village ending
    /// without replaying the finale. Outside Play mode only.</summary>
    [MenuItem("Tools/Shards of Motion/Dev/Village Ending: Mark Region 1 Complete (save)")]
    private static void MarkRegionComplete()
    {
        if (EditorApplication.isPlaying) { UnityEngine.Debug.LogWarning("Dev: stop Play mode first."); return; }
        SaveSystem.Load();
        SaveSystem.MarkEvent("region1_complete");
        UnityEngine.Debug.Log("Dev: Region 1 marked complete. Play the Village scene and talk to Wren for the homecoming and credits.");
    }

    /// <summary>Forgets the homecoming talks and the credits (they play again); Region 1 stays complete. Outside Play mode only.</summary>
    [MenuItem("Tools/Shards of Motion/Dev/Village Ending: Reset Homecoming + Credits (save)")]
    private static void ResetVillageEnding()
    {
        if (EditorApplication.isPlaying) { UnityEngine.Debug.LogWarning("Dev: stop Play mode first."); return; }
        SaveSystem.Load();
        int removed = SaveSystem.Data.events == null ? 0
            : SaveSystem.Data.events.RemoveAll(e => e.StartsWith("home_") || e == CreditsRoll.SeenEvent);
        SaveSystem.Save();
        UnityEngine.Debug.Log($"Dev: village ending reset ({removed} saved event(s) removed).");
    }

    private static void PlayFrom(int room)
    {
        if (EditorApplication.isPlaying) return;
        SessionState.SetInt(DevTools.StartRoomKey, room);
        EditorApplication.isPlaying = true;
    }
}
