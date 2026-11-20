using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Player-facing settings, saved with the game.</summary>
[Serializable]
public class SettingsData
{
    [Range(0f, 1f)] public float masterVolume = 0.8f;
    [Range(0f, 1f)] public float musicVolume = 0.7f;   // used once music is added
    [Range(0f, 1f)] public float soundVolume = 0.8f;   // used once sound effects are added
    public bool fullscreen = true;
    public int resolutionWidth;                          // 0 = keep the current resolution
    public int resolutionHeight;
    [Tooltip("Seconds in a room before Wren's tip appears; 0 = tips off.")]
    public float tipDelay = 90f;
}

/// <summary>Everything the game remembers between sessions (one slot).</summary>
[Serializable]
public class SaveData
{
    public const int CurrentVersion = 2;

    public int version = CurrentVersion;
    public bool started;                                  // a game has been started (enables Continue)
    public string region = "Region1";
    public int furthestRoom = 1;                          // in the current region, numbered left to right
    public List<string> shards = new List<string>();      // MotionShard ids collected: "<room object>/<shard name>", e.g. "R1_Room03/Shard_Arch"
    public List<string> questItems = new List<string>();  // QuestItem ids found (machine parts...)
    public List<string> questsDone = new List<string>();  // village requests completed
    public List<string> events = new List<string>();      // one-off things that happened: "wall_broken", "met_Bram", "fund_well",
                                                          // "journal_<entry asset name>" (a journal entry was read)...
    public SettingsData settings = new SettingsData();
}

/// <summary>
/// One automatic save slot, stored as JSON in Application.persistentDataPath/save.json (on Windows:
/// %USERPROFILE%/AppData/LocalLow/&lt;company&gt;/&lt;product&gt;/save.json). There's no save button: the game saves when a
/// shard or machine part is picked up, when players go through a doorway, and when settings change.
/// Loaded once when the game starts. Delete the file (or use New Game) to start over.
/// </summary>
public static class SaveSystem
{
    private const string FileName = "save.json";
    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    private static SaveData data;

    /// <summary>The save, loaded from disk the first time it is needed.</summary>
    public static SaveData Data
    {
        get { if (data == null) Load(); return data; }
        private set => data = value;
    }

    /// <summary>Raised after settings change (GameFlow applies resolution / volume).</summary>
    public static event Action SettingsChanged;

    public static bool HasSave => Data.started;

    // Each play session starts by reading the file again (statics can survive between Play sessions in the editor).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        SettingsChanged = null;
        data = null;
    }

    public static void Load()
    {
        try
        {
            data = File.Exists(FilePath) ? JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)) ?? new SaveData() : new SaveData();
        }
        catch (Exception e)
        {
            Debug.LogWarning("SaveSystem: couldn't read the save file, starting fresh. " + e.Message);
            data = new SaveData();
        }
        if (data.settings == null) data.settings = new SettingsData();
        Migrate(data);
    }

    // Older saves: version 1 shard ids ended with the shard's position ("R1_Room03/Shard_Arch@(30.00, -2.10, 0.00)"),
    // which changed whenever a shard was moved. Keep only "<room>/<name>" (duplicates removed).
    private static void Migrate(SaveData d)
    {
        if (d.version >= SaveData.CurrentVersion) return;
        if (d.shards == null) d.shards = new List<string>();
        List<string> shards = new List<string>();
        foreach (string id in d.shards)
        {
            if (string.IsNullOrEmpty(id)) continue;
            int at = id.IndexOf('@');
            string clean = at >= 0 ? id.Substring(0, at) : id;
            if (!shards.Contains(clean)) shards.Add(clean);
        }
        d.shards = shards;
        d.version = SaveData.CurrentVersion;
    }

    public static void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning("SaveSystem: couldn't write the save file. " + e.Message);
        }
    }

    /// <summary>Starts over: progress is cleared, settings are kept.</summary>
    public static void NewGame()
    {
        SettingsData keep = Data.settings;
        Data = new SaveData { started = true, settings = keep };
        Save();
    }

    public static void AddShard(string id) { if (Add(Data.shards, id)) Save(); }
    public static void AddQuestItem(string id) { if (Add(Data.questItems, id)) Save(); }
    public static void CompleteQuest(string id) { if (Add(Data.questsDone, id)) Save(); }
    public static void MarkEvent(string id) { if (Data.events == null) Data.events = new List<string>(); if (Add(Data.events, id)) Save(); }
    public static bool HasEvent(string id) => Data.events != null && Data.events.Contains(id);

    public static void ReachedRoom(string region, int room)
    {
        if (Data.region == region && room <= Data.furthestRoom) return;
        if (Data.region != region) { Data.region = region; Data.furthestRoom = room; }
        else Data.furthestRoom = room;
        Save();
    }

    public static void SettingsUpdated()
    {
        Save();
        SettingsChanged?.Invoke();
    }

    private static bool Add(List<string> list, string id)
    {
        if (string.IsNullOrEmpty(id) || list.Contains(id)) return false;
        list.Add(id);
        return true;
    }
}
