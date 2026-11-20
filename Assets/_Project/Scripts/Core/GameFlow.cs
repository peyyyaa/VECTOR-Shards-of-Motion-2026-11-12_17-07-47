using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Moving between the game's scenes, in one place: Title -> Village -> a region -> back. Also applies the player's
/// settings (volume, fullscreen, resolution) when the game starts and whenever they change.
/// Scenes must be in File > Build Profiles (Tools > Shards of Motion > Game Shell > Build Title Scene adds them).
/// </summary>
public static class GameFlow
{
    public const string TitleScene = "Title";
    public const string VillageScene = "Village";

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
        Load(VillageExists ? VillageScene : RegionScene("Region1"));
    }

    /// <summary>Continue: back into the saved region, at the furthest room reached.</summary>
    public static void Continue()
    {
        RoomManager.RequestedStartRoom = SaveSystem.Data.furthestRoom;
        Load(RegionScene(SaveSystem.Data.region));
    }

    public static void EnterRegion(string region, int room = 0)
    {
        RoomManager.RequestedStartRoom = room;
        Load(RegionScene(region));
    }

    public static void ToVillage() => Load(VillageExists ? VillageScene : TitleScene);
    public static void ToTitle() => Load(TitleScene);

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void Load(string scene)
    {
        Time.timeScale = 1f; // leaving a paused game
        SaveSystem.Save();
        SceneManager.LoadScene(scene);
    }
}
