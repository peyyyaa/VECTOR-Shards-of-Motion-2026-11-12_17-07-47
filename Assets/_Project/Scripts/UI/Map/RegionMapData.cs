using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// What the map screen knows about a region: its rooms in path order (name, shards, machine part, main journal entry,
/// marker position on the map art) and the regions still locked. The map can be opened from the village, where the
/// region's rooms aren't loaded, so these numbers live here instead of being counted from the scene.
/// Progress itself (rooms reached, shards, parts, journals read) always comes from the save.
///
/// Asset: Assets/_Project/Data/Map/Region1_Map.asset, created by Tools > Shards of Motion > Map > Draw Region Map.
/// Edit names / counts here freely; after moving markers (Map Position), run Draw Region Map again so the path follows.
/// </summary>
[CreateAssetMenu(menuName = "Shards of Motion/Region Map", fileName = "Region_Map")]
public class RegionMapData : ScriptableObject
{
    public enum LabelSide { Below, Above, Left, Right }

    [Serializable]
    public class RoomInfo
    {
        [Tooltip("The room's object name in the region scene, e.g. R1_Room03. Shard ids start with it.")]
        public string roomObject;
        public string displayName;
        [Tooltip("Motion Shards hidden in this room.")]
        [Min(0)] public int shards = 3;
        [Tooltip("Machine part hidden in this room (its QuestItem id), or empty.")]
        public string partId;
        [Tooltip("The part's name as shown on the map.")]
        public string partName;
        [Tooltip("The room's main journal entry; the map shows it as read once it has been opened. Empty = not written yet.")]
        public JournalEntry journal;
        [Tooltip("Marker position on the map art, in map pixels (0,0 = bottom left).")]
        public Vector2 mapPosition;
        [Tooltip("Where the room's name sits next to its marker.")]
        public LabelSide label = LabelSide.Below;
    }

    [Serializable]
    public class LockedRegion
    {
        public string name;
        [Tooltip("Fogged area on the map art, in map pixels.")]
        public Rect area;
    }

    [Tooltip("Region id used by the save and GameFlow (e.g. Region1).")]
    public string regionId = "Region1";
    public string regionName = "The Shattered Grounds";
    [Tooltip("The ink drawing (path, village, trees, fog), drawn by Map > Draw Region Map.")]
    public Sprite mapArt;
    [Tooltip("Size of the map art in pixels; shown at 3x on screen.")]
    public Vector2Int mapSize = new Vector2Int(480, 220);
    public string villageName = "Alderbrook";
    public Vector2 villagePosition = new Vector2(34f, 52f);
    public List<RoomInfo> rooms = new List<RoomInfo>();
    public List<LockedRegion> lockedRegions = new List<LockedRegion>();

    // ------------------------------------------------------------------ progress, read from the save

    /// <summary>Rooms reached in this region (1..n are reached; room 1, the region's entrance, is always open).</summary>
    public int ReachedRooms => SaveSystem.Data.region == regionId ? Mathf.Clamp(SaveSystem.Data.furthestRoom, 1, rooms.Count) : 1;

    public bool IsReached(int index) => index < ReachedRooms;

    public int ShardsFound(int index)
    {
        string prefix = rooms[index].roomObject + "/";
        int n = 0;
        foreach (string id in SaveSystem.Data.shards) if (id.StartsWith(prefix, StringComparison.Ordinal)) n++;
        return n;
    }

    public bool HasPart(int index) => !string.IsNullOrEmpty(rooms[index].partId);
    public bool PartFound(int index) => HasPart(index) && SaveSystem.Data.questItems.Contains(rooms[index].partId);
    public bool HasJournal(int index) => rooms[index].journal != null;
    public bool JournalRead(int index) => JournalStone.IsRead(rooms[index].journal);

    /// <summary>Region totals: (found, total) for shards, machine parts and journal entries.</summary>
    public (int found, int total) ShardTotals()
    {
        int found = 0, total = 0;
        for (int i = 0; i < rooms.Count; i++) { found += Mathf.Min(ShardsFound(i), rooms[i].shards); total += rooms[i].shards; }
        return (found, total);
    }

    public (int found, int total) PartTotals()
    {
        int found = 0, total = 0;
        for (int i = 0; i < rooms.Count; i++) if (HasPart(i)) { total++; if (PartFound(i)) found++; }
        return (found, total);
    }

    public (int found, int total) JournalTotals()
    {
        int found = 0, total = 0;
        for (int i = 0; i < rooms.Count; i++) if (HasJournal(i)) { total++; if (JournalRead(i)) found++; }
        return (found, total);
    }
}
