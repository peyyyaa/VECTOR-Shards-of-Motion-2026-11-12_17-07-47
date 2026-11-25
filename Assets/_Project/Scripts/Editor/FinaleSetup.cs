using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tools > Shards of Motion > Finale > 1. Add Finale Rooms To Scene (with Region1_ShatteredGrounds open). Re-runnable.
/// Makes Room 8 ("Heart of the Forest") a three-part finale, one entry on the map:
///   - room prefabs R1_Room09 (part 2, the Heart Engine) and R1_Room10 (part 3, the Great Shard) from Room_Standard,
///     placed east of Room 8 (20 m apart); Room 8 loses its east wall, Room 10 keeps one;
///   - doorways Doorway_R8_R9 (raised to 3.4 m: parts 1 and 2 meet on a landing) and Doorway_R9_R10 (floor level);
///   - parts 1 and 2 don't count as solved when left (Room.solvedOnExit off): the Great Shard solves the finale;
///   - the map's Room 8 entry also counts the shards of parts 2 and 3 (RegionMapData alsoRooms);
///   - light moods getting darker from Room 5 to the finale (RoomMood on each room + MoodDirector next to RoomManager).
/// Save the scene afterwards, then build: Room 8 > 1, Room 8 Part 2 > 1, Room 8 Part 3 > 1, Backgrounds > 2.
/// </summary>
public static class FinaleSetup
{
    private const string RoomsFolder = "Assets/_Project/Prefabs/Rooms/Region1";
    private const string RoomStandard = "Assets/_Project/Prefabs/Rooms/Room_Standard.prefab";
    private const string DoorwayPrefab = "Assets/_Project/Prefabs/Rooms/Doorway_Side.prefab";
    public const float Part1To2Doorway = 3.4f;

    // Global light per room: deeper into the forest, darker and bluer.
    private static readonly (int room, Color color, float intensity)[] Moods =
    {
        (5, new Color(0.97f, 0.97f, 0.94f), 0.95f),
        (6, new Color(0.92f, 0.95f, 0.93f), 0.9f),
        (7, new Color(0.86f, 0.9f, 0.95f), 0.84f),
        (8, new Color(0.76f, 0.82f, 0.95f), 0.72f),
        (9, new Color(0.68f, 0.78f, 0.95f), 0.62f),
        (10, new Color(0.66f, 0.76f, 0.95f), 0.58f),
    };

    [MenuItem("Tools/Shards of Motion/Finale/1. Add Finale Rooms To Scene")]
    public static void AddRooms()
    {
        RoomManager manager = Object.FindFirstObjectByType<RoomManager>();
        Room room8 = Object.FindObjectsByType<Room>(FindObjectsSortMode.None).FirstOrDefault(r => r.name == "R1_Room08");
        if (manager == null || room8 == null)
        {
            Debug.LogError("Finale: open Region1_ShatteredGrounds (it needs RoomManager and R1_Room08) and run this again.");
            return;
        }

        // Room prefabs and scene instances.
        Transform roomsParent = room8.transform.parent;
        for (int n = 9; n <= 10; n++)
        {
            string path = $"{RoomsFolder}/R1_Room{n:00}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null && !CreateRoomPrefab(n, path)) return;
            if (Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Any(r => r.name == $"R1_Room{n:00}")) continue;
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), room8.gameObject.scene);
            inst.transform.SetParent(roomsParent, false);
            inst.transform.position = room8.transform.position + new Vector3(20f * (n - 8), 0f, 0f);
        }
        EditPrefab($"{RoomsFolder}/R1_Room08.prefab", root =>
        {
            Transform wall = RoomBuildKit.Find(root.transform, "Wall_Right");
            if (wall != null) Object.DestroyImmediate(wall.gameObject); // the way on to part 2 is a doorway now
            SetSolvedOnExit(root, false);
        });
        EditPrefab($"{RoomsFolder}/R1_Room09.prefab", root => SetSolvedOnExit(root, false));

        // Doorways.
        Doorway d78 = Object.FindObjectsByType<Doorway>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(d => d.name == "Doorway_R7_R8");
        Transform connections = d78 != null ? d78.transform.parent : null;
        float x8 = room8.transform.position.x;
        EnsureDoorway("Doorway_R8_R9", connections, x8 + 10f, Part1To2Doorway);
        EnsureDoorway("Doorway_R9_R10", connections, x8 + 30f, 0f);

        // Moods.
        foreach ((int n, Color color, float intensity) in Moods)
            EditPrefab($"{RoomsFolder}/R1_Room{n:00}.prefab", root =>
            {
                RoomMood mood = root.GetComponent<RoomMood>();
                if (mood == null) mood = root.AddComponent<RoomMood>();
                mood.EditorSetup(color, intensity);
            });
        Light2D global = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None).FirstOrDefault(l => l.lightType == Light2D.LightType.Global);
        MoodDirector director = manager.GetComponent<MoodDirector>();
        if (director == null) director = manager.gameObject.AddComponent<MoodDirector>();
        director.EditorSetup(manager, global);
        if (global == null) Debug.LogWarning("Finale: no Global Light 2D found in the scene; room moods won't show.");

        // Map: one finale entry, counting the shards of all three parts.
        RegionMapData map = MapTool.EnsureData();
        int index = map != null ? map.IndexOf("R1_Room08") : -1;
        if (index >= 0)
        {
            map.rooms[index].alsoRooms = new[] { "R1_Room09", "R1_Room10" };
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
        }

        EditorSceneManager.MarkSceneDirty(room8.gameObject.scene);
        Debug.Log("Finale: rooms, doorways, moods and map ready. Save the scene (Ctrl+S), then run Room 8 > 1. Build Graybox, " +
                  "Room 8 Part 2 > 1. Build Graybox, Room 8 Part 3 > 1. Build Graybox, and Backgrounds > 2. Apply To Rooms.");
    }

    private static bool CreateRoomPrefab(int n, string path)
    {
        GameObject standard = AssetDatabase.LoadAssetAtPath<GameObject>(RoomStandard);
        if (standard == null) { Debug.LogError("Finale: Room_Standard.prefab not found. Nothing was changed."); return false; }
        GameObject room = (GameObject)PrefabUtility.InstantiatePrefab(standard);
        try
        {
            PrefabUtility.UnpackPrefabInstance(room, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            room.name = $"R1_Room{n:00}";
            room.transform.position = Vector3.zero;
            Transform layout = Region1Setup.Child(room.transform, "Layout");
            Region1Setup.Child(room.transform, "Puzzle");
            Region1Setup.Child(room.transform, "Objects");
            Region1Setup.Child(room.transform, "Hazards");
            Region1Setup.Child(room.transform, "Info");
            Region1Setup.MoveOrRemove(room.transform, "Floor", layout, false);
            Region1Setup.MoveOrRemove(room.transform, "Wall_Left", layout, true);       // a doorway on the west
            Region1Setup.MoveOrRemove(room.transform, "Wall_Right", layout, n < 10);    // the last room keeps its east wall
            Transform grid = Region1Setup.Child(layout, "Grid");
            grid.gameObject.AddComponent<Grid>().cellSize = Vector3.one;
            Region1Setup.AddTilemap(grid, "TM_BackWall", "BackDecor", false, null);
            Region1Setup.AddTilemap(grid, "TM_Terrain", "Midground", true, null);
            Region1Setup.AddTilemap(grid, "TM_Decor", "Props", false, null);
            Region1Setup.AddTilemap(grid, "TM_Foreground", "Foreground", false, null);
            PrefabUtility.SaveAsPrefabAsset(room, path);
            return true;
        }
        finally
        {
            Object.DestroyImmediate(room);
        }
    }

    private static void EnsureDoorway(string name, Transform parent, float x, float openingBottom)
    {
        Doorway door = Object.FindObjectsByType<Doorway>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(d => d.name == name);
        if (door == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorwayPrefab);
            if (prefab == null) { Debug.LogError("Finale: Doorway_Side.prefab not found."); return; }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, -4.5f, 0f);
            door = go.GetComponent<Doorway>();
            if (door == null) { Debug.LogError("Finale: Doorway_Side has no Doorway component (Rooms > Set Up Adjustable Doorway)."); return; }
        }
        SerializedObject so = new SerializedObject(door);
        so.FindProperty("openingBottom").floatValue = openingBottom;
        so.ApplyModifiedProperties();
        DoorwayEditor.ApplyAndSave(door);
    }

    private static void SetSolvedOnExit(GameObject root, bool on)
    {
        Room room = root.GetComponent<Room>();
        if (room == null) return;
        SerializedObject so = new SerializedObject(room);
        so.FindProperty("solvedOnExit").boolValue = on;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void EditPrefab(string path, System.Action<GameObject> change)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) { Debug.LogWarning("Finale: missing " + path); return; }
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try { change(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
