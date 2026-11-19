using System;
using System.Collections.Generic;
using UnityEngine;

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
