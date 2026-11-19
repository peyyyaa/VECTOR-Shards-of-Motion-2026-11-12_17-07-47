using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor tool: Tools > Shards of Motion > Organize Hierarchy.
/// Sorts the open scene into a standard layout (safe to run again any time; supports Undo):
///
///   _Managers     camera, lights, UI canvas, RoomManager, CoopSpawner, spawn points
///   RoomXX        each Room object (left to right), with child groups:
///       Layout    floors, walls, ledges (everything static)
///       Puzzle    plates, levers, relays, doors/gates, movers, currents, shrines, seesaws
///       Objects   blocks / crates (Dynamic bodies)
///       Hazards   acid / lava pools
///       Info      Journal stones, signs
///   Connections   doorways, room exits, player barriers, arrival points
///   _Unsorted     anything not inside any room's area
///
/// Doorways and exits never go inside a Room: an exit uses its parent Room to decide which
/// room it belongs to, so nesting a two-way doorway under one room would break the other way.
/// World positions, rotations and sizes are kept exactly.
/// </summary>
public static class HierarchyOrganizer
{
    private const string Managers = "_Managers";
    private const string Connections = "Connections";
    private const string Unsorted = "_Unsorted";
    private static readonly string[] RoomGroups = { "Layout", "Puzzle", "Objects", "Hazards", "Info" };

    [MenuItem("Tools/Shards of Motion/Organize Hierarchy")]
    public static void Organize()
    {
        Scene scene = SceneManager.GetActiveScene();
        Undo.SetCurrentGroupName("Organize Hierarchy");
        int undoGroup = Undo.GetCurrentGroup();

        Room[] rooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None)
            .Where(r => r.gameObject.scene == scene)
            .OrderBy(r => r.transform.position.x).ThenByDescending(r => r.transform.position.y).ToArray();

        // 1. Collect the objects to sort: everything at the top level, plus the contents of
        //    plain grouping objects (empty, non-prefab), which are dissolved afterwards.
        List<Transform> units = new List<Transform>();
        List<GameObject> oldGroups = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects()) Collect(root.transform, units, oldGroups);

        int moved = 0;
        foreach (Transform unit in units)
        {
            Transform destination = DestinationFor(unit, rooms);
            if (destination != null && unit.parent != destination)
            {
                Undo.SetTransformParent(unit, destination, true, "Organize Hierarchy");
                moved++;
            }
        }

        // 2. Rooms go to the top level, in left-to-right order, after _Managers.
        Transform managers = Group(null, Managers);
        managers.SetAsFirstSibling();
        foreach (Room room in rooms)
        {
            if (room.transform.parent != null) Undo.SetTransformParent(room.transform, null, true, "Organize Hierarchy");
            room.transform.SetAsLastSibling();
        }
        Transform connections = FindGroup(null, Connections);
        if (connections != null) connections.SetAsLastSibling();
        Transform unsorted = FindGroup(null, Unsorted);
        if (unsorted != null) unsorted.SetAsLastSibling();

        // 3. Remove grouping objects that are now empty, and sort each room's groups.
        oldGroups.Reverse(); // innermost groups first, so their (now empty) parents can be removed too
        foreach (GameObject g in oldGroups)
        {
            if (g != null && g.transform.childCount == 0 && !IsOurGroup(g.transform)) Undo.DestroyObjectImmediate(g);
        }
        foreach (Room room in rooms)
        {
            foreach (string name in RoomGroups) // Layout, Puzzle, Objects, Hazards, Info — in that order, last
            {
                Transform g = FindGroup(room.transform, name);
                if (g != null) g.SetAsLastSibling();
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"Organize Hierarchy: {moved} objects moved into {rooms.Length} rooms + groups. Save the scene to keep it (Ctrl+S).");
    }

    // A plain grouping object: only a Transform, not a prefab instance, not a Room, not one of our groups.
    private static bool IsPlainGroup(GameObject go)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(go)) return false;
        if (go.GetComponent<Room>() != null) return false;
        return go.GetComponents<Component>().Length == 1 && go.transform.childCount > 0;
    }

    private static bool IsOurGroup(Transform t)
    {
        string n = t.name;
        if (n == Managers || n == Connections || n == Unsorted) return true;
        return t.parent != null && t.parent.GetComponent<Room>() != null && RoomGroups.Contains(n);
    }

    private static void Collect(Transform t, List<Transform> units, List<GameObject> oldGroups)
    {
        if (t.GetComponent<Room>() != null)
        {
            // Loose objects already sitting directly inside a Room get sorted into its groups.
            // (Children of a Room prefab instance, like its floor and walls, can't be moved and are left alone.)
            foreach (Transform child in t.Cast<Transform>().ToArray())
            {
                if (IsOurGroup(child)) { foreach (Transform c in child.Cast<Transform>().ToArray()) units.Add(c); continue; }
                if (PrefabUtility.IsPartOfPrefabInstance(child.gameObject) && !PrefabUtility.IsOutermostPrefabInstanceRoot(child.gameObject)) continue;
                units.Add(child);
            }
            return;
        }
        if (IsOurGroup(t) || IsPlainGroup(t.gameObject))
        {
            if (!IsOurGroup(t)) oldGroups.Add(t.gameObject);
            foreach (Transform child in t.Cast<Transform>().ToArray()) Collect(child, units, oldGroups);
            return;
        }
        units.Add(t);
    }

    private static Transform DestinationFor(Transform unit, Room[] rooms)
    {
        GameObject go = unit.gameObject;

        if (go.GetComponentInChildren<Camera>(true) || go.GetComponentInChildren<Light2D>(true) || go.GetComponent<Canvas>() ||
            go.GetComponent<RoomManager>() || go.GetComponent<LocalCoopSpawner>() || go.name.EndsWith("_Spawn"))
            return Group(null, Managers);

        if (go.GetComponentInChildren<RoomExit>(true) || go.layer == LayerMask.NameToLayer("PlayerBarrier") || go.name.Contains("Entry"))
            return Group(null, Connections);

        Room room = RoomAt(PositionOf(unit), rooms);
        if (room == null) return Group(null, Unsorted);

        return Group(room.transform, CategoryOf(go));
    }

    private static string CategoryOf(GameObject go)
    {
        if (go.GetComponentInChildren<WorldSign>(true) || go.GetComponentInChildren<JournalStone>(true)) return "Info";
        if (go.GetComponentInChildren<HazardZone>(true)) return "Hazards";

        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (go.GetComponent<BlockHome>() || (rb != null && rb.bodyType == RigidbodyType2D.Dynamic && go.GetComponent<Joint2D>() == null))
            return "Objects";

        if (go.GetComponentInChildren<SignalSource>(true) || go.GetComponentInChildren<ExitDoor>(true) ||
            go.GetComponentInChildren<Mover>(true) || go.GetComponentInChildren<SignalActivator>(true) ||
            go.GetComponentInChildren<Effector2D>(true) || go.GetComponentInChildren<ResetShrine>(true) ||
            go.GetComponentInChildren<Joint2D>(true))
            return "Puzzle";

        return "Layout";
    }

    // Centre of the object's colliders/renderers (so a wide floor counts by its middle), else its position.
    private static Vector2 PositionOf(Transform t)
    {
        Renderer r = t.GetComponentInChildren<Renderer>();
        if (r != null) return r.bounds.center;
        Collider2D c = t.GetComponentInChildren<Collider2D>();
        if (c != null) return c.bounds.center;
        return t.position;
    }

    private static Room RoomAt(Vector2 p, Room[] rooms)
    {
        foreach (Room room in rooms) if (room.Contains(p)) return room;
        return null;
    }

    private static Transform FindGroup(Transform parent, string name)
    {
        if (parent == null)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.name == name && root.GetComponents<Component>().Length == 1) return root.transform;
            return null;
        }
        foreach (Transform child in parent)
            if (child.name == name && child.GetComponents<Component>().Length == 1) return child;
        return null;
    }

    private static Transform Group(Transform parent, string name)
    {
        Transform existing = FindGroup(parent, name);
        if (existing != null) return existing;

        GameObject g = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(g, "Organize Hierarchy");
        if (parent != null) Undo.SetTransformParent(g.transform, parent, false, "Organize Hierarchy");
        g.transform.localPosition = Vector3.zero;
        g.transform.localRotation = Quaternion.identity;
        g.transform.localScale = Vector3.one;
        return g.transform;
    }
}
