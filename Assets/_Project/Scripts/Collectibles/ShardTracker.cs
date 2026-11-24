using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps count of Motion Shards: how many exist in the scene, how many the players have collected,
/// overall and per room. Shared by both players (one team count). Collected shards stay collected
/// for the rest of the play session (room resets don't bring them back).
/// </summary>
public static class ShardTracker
{
    private static readonly HashSet<MotionShard> all = new HashSet<MotionShard>();
    private static readonly HashSet<string> collected = new HashSet<string>();

    /// <summary>Raised whenever the count changes (a shard registered or was collected).</summary>
    public static event Action Changed;

    public static int Total => all.Count;
    public static int Collected { get; private set; }

    // Statics can survive between Play sessions in the editor: start clean each time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        all.Clear();
        collected.Clear();
        Collected = 0;
        Changed = null;
    }

    // After the save file is loaded: shards collected in earlier sessions stay collected.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadFromSave() => ReloadFromSave();

    // Each time a scene has loaded (its shards have registered by then), drop saved ids of shards that no longer exist.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void HookCleanup()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        RemoveStaleIds();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RemoveStaleIds();

    /// <summary>
    /// A rebuilt room can rename or remove its shards; their old ids would stay in the save and inflate every count
    /// ("21 / 18", the Village Fund balance). For each room in the loaded scene, saved ids of that room that match no
    /// shard in it are removed. Rooms not loaded (other regions) are left alone.
    /// </summary>
    private static void RemoveStaleIds()
    {
        HashSet<string> rooms = new HashSet<string>();
        foreach (Room room in UnityEngine.Object.FindObjectsByType<Room>(FindObjectsSortMode.None)) rooms.Add(room.name);
        if (rooms.Count == 0) return;
        HashSet<string> existing = new HashSet<string>();
        foreach (MotionShard s in all) if (s != null) existing.Add(s.Id);
        int removed = SaveSystem.Data.shards.RemoveAll(id =>
        {
            int slash = id.IndexOf('/');
            return slash > 0 && rooms.Contains(id.Substring(0, slash)) && !existing.Contains(id);
        });
        if (removed == 0) return;
        SaveSystem.Save();
        ReloadFromSave();
    }

    /// <summary>Takes the collected shards from the save (also called after New Game).</summary>
    public static void ReloadFromSave()
    {
        collected.Clear();
        foreach (string id in SaveSystem.Data.shards) collected.Add(id);
        Collected = collected.Count;
        Changed?.Invoke();
    }

    public static void Register(MotionShard shard)
    {
        if (all.Add(shard)) Changed?.Invoke();
    }

    public static void Unregister(MotionShard shard)
    {
        if (all.Remove(shard)) Changed?.Invoke();
    }

    public static bool IsCollected(string id) => collected.Contains(id);

    public static void Collect(MotionShard shard)
    {
        if (!collected.Add(shard.Id)) return;
        Collected++;
        SaveSystem.AddShard(shard.Id);
        Changed?.Invoke();
    }

    /// <summary>Shards in a room (total, collected).</summary>
    public static (int total, int collected) InRoom(Room room)
    {
        int total = 0, got = 0;
        foreach (MotionShard s in all)
        {
            if (s == null || s.Room != room) continue;
            total++;
            if (collected.Contains(s.Id)) got++;
        }
        return (total, got);
    }
}
