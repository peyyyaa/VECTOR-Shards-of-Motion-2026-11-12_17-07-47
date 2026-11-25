using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Moving between the game's scenes, in one place: Title -> Village -> a region -> back. Every change goes through the
/// SceneTransition (fade, title card while loading) when its prefab is in Resources. Also applies the player's
/// settings (volume, fullscreen, resolution) when the game starts and whenever they change.
/// Scenes must be in File > Build Profiles (Tools > Shards of Motion > Game Shell > Build Title Scene adds them).
/// </summary>
public static class GameFlow
{
    public const string TitleScene = "Title";
    public const string VillageScene = "Village";

    /// <summary>The village screen with the trailhead (Forge &amp; East Road), numbered left to right: where players arrive home.</summary>
    public const int VillageTrailheadScreen = 3;

    /// <summary>Wren's lab, the first village screen: where the players come home to after freeing a region's Great Shard.</summary>
    public const int VillageLabScreen = 1;

    /// <summary>The scene for a region id stored in the save.</summary>
    public static string RegionScene(string region) => region switch
    {
        "Region1" => "Region1_ShatteredGrounds",
        _ => "Region1_ShatteredGrounds",
    };

    public static bool VillageExists => Application.CanStreamedLevelBeLoaded(VillageScene);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplySettingsOnStart()
    {
        SaveSystem.SettingsChanged += ApplySettings;
        ApplySettings();
    }

    public static void ApplySettings()
    {
        SettingsData s = SaveSystem.Data.settings;
        AudioListener.volume = s.masterVolume;
#if !UNITY_EDITOR
        int w = s.resolutionWidth > 0 ? s.resolutionWidth : Screen.currentResolution.width;
        int h = s.resolutionHeight > 0 ? s.resolutionHeight : Screen.currentResolution.height;
        Screen.SetResolution(w, h, s.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
#endif
    }

    /// <summary>New Game: clear progress, then start in the village (or straight in Region 1 if there's no village yet).</summary>
    public static void NewGame()
    {
        SaveSystem.NewGame();
        ShardTracker.ReloadFromSave();
        QuestItem.ReloadFromSave();
        if (VillageExists) Load(VillageScene); // starts outside Wren's lab
        else Load(RegionScene("Region1"), "Region1");
    }

    /// <summary>Continue: back into the saved region, at the furthest room reached.</summary>
    public static void Continue()
    {
        RoomManager.RequestedStartRoom = SaveSystem.Data.furthestRoom;
        Load(RegionScene(SaveSystem.Data.region), SaveSystem.Data.region);
    }

    public static void EnterRegion(string region, int room = 0)
    {
        RoomManager.RequestedStartRoom = room;
        Load(RegionScene(region), region);
    }

    /// <summary>Back home (trail home gate, pause menu): arrives at the village trailhead, as if walking in along the road.
    /// The end of a region arrives at Wren's lab instead (screen = VillageLabScreen), for the homecoming.</summary>
    public static void ToVillage() => ToVillage(VillageTrailheadScreen);

    public static void ToVillage(int screen)
    {
        if (!VillageExists) { ToTitle(); return; }
        RoomManager.RequestedStartRoom = screen;
        Load(VillageScene);
    }

    public static void ToTitle() => Load(TitleScene);

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void Load(string scene, string regionId = null)
    {
        if (SceneTransition.Busy) return;
        Time.timeScale = 1f; // leaving a paused game
        SaveSystem.Save();
        if (!SceneTransition.Go(scene, regionId)) SceneManager.LoadScene(scene); // no transition prefab: load at once
    }
}
