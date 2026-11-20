using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared helpers for the room graybox builders (Room3Builder onward): clone Room 1's proven graybox parts
/// (stone ledge, plate, lift, gate, relay) so layers, colliders and settings match, and place them by rectangle.
/// Coordinates are room-local: floor top y = -4.5, doorways at x = -10 / 10.
/// </summary>
public static class RoomBuildKit
{
    public const string Room1Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room01.prefab";
    public const string Interactables = "Assets/_Project/Prefabs/Interactables";

    public static Transform Find(Transform root, string name) =>
        root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name);

    public static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

    /// <summary>A Room 1 graybox part (by name) to clone from, or null with an error.</summary>
    public static Transform Template(string name)
    {
        GameObject r1 = Load(Room1Path);
        Transform t = r1 != null ? Find(r1.transform, name) : null;
        if (t == null) Debug.LogError($"Room builder: template '{name}' not found in R1_Room01.");
        return t;
    }

    // A copy of a Room 1 graybox object without its art children (art comes from the dressing pass).
    public static GameObject Clone(Transform template, Transform parent, string name)
    {
        GameObject go = Object.Instantiate(template.gameObject, parent);
        go.name = name;
        for (int i = go.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
        if (go.TryGetComponent(out SpriteRenderer sr)) sr.enabled = true;
        return go;
    }

    public static void SetRect(Transform tr, float left, float bottom, float right, float top)
    {
        tr.localPosition = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0f);
        tr.localScale = new Vector3(right - left, top - bottom, 1f);
    }

    public static GameObject Solid(Transform template, Transform parent, string name, float left, float bottom, float right, float top)
    {
        GameObject go = Clone(template, parent, name);
        SetRect(go.transform, left, bottom, right, top);
        return go;
    }

    public static PressurePlate Plate(Transform template, Transform parent, string name, float x, float floorY, float newtons,
        PlateReadout.Display display, bool countPlayers, float width = 1.5f)
    {
        GameObject go = Clone(template, parent, name);
        go.transform.localPosition = new Vector3(x, floorY + 0.05f, 0f);
        go.transform.localScale = new Vector3(width, 0.1f, 1f);
        PressurePlate plate = go.GetComponent<PressurePlate>();
        SerializedObject p = new SerializedObject(plate);
        p.FindProperty("requiredWeight").floatValue = newtons;
        p.FindProperty("plateRenderer").objectReferenceValue = go.GetComponent<SpriteRenderer>();
        p.FindProperty("releasedColor").colorValue = new Color(0.6f, 0.6f, 0.2f);
        p.FindProperty("pressedColor").colorValue = new Color(0.2f, 0.9f, 0.3f);
        p.FindProperty("slab").objectReferenceValue = null;
        p.FindProperty("countPlayers").boolValue = countPlayers;
        p.ApplyModifiedPropertiesWithoutUndo();
        if (go.TryGetComponent(out PlateReadout readout))
        {
            SerializedObject ro = new SerializedObject(readout);
            ro.FindProperty("display").enumValueIndex = (int)display;
            ro.ApplyModifiedPropertiesWithoutUndo();
        }
        return plate;
    }

    public static PuzzleRelay Relay(Transform template, Transform parent, string name, Vector2 pos, int mode, params SignalSource[] inputs)
    {
        GameObject go = Clone(template, parent, name);
        go.transform.localPosition = pos;
        PuzzleRelay relay = go.GetComponent<PuzzleRelay>();
        SerializedObject r = new SerializedObject(relay);
        SerializedProperty list = r.FindProperty("inputs");
        list.arraySize = inputs.Length;
        for (int i = 0; i < inputs.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = inputs[i];
        r.FindProperty("mode").enumValueIndex = mode; // 0 = All, 1 = Any
        r.FindProperty("latch").boolValue = false;
        r.ApplyModifiedPropertiesWithoutUndo();
        return relay;
    }

    /// <summary>A kinematic platform (cloned from Room 1's lift) that travels while its signal is on.</summary>
    public static Mover Platform(Transform liftTemplate, Transform parent, string name, float left, float bottom, float right, float top,
        Vector2 travel, float speed, SignalSource signal, bool stopIfBlocked)
    {
        GameObject go = Solid(liftTemplate, parent, name, left, bottom, right, top);
        Mover m = go.GetComponent<Mover>();
        SerializedObject s = new SerializedObject(m);
        s.FindProperty("travel").vector2Value = travel;
        s.FindProperty("speed").floatValue = speed;
        s.FindProperty("signal").objectReferenceValue = signal;
        s.FindProperty("stopIfBlocked").boolValue = stopIfBlocked;
        s.ApplyModifiedPropertiesWithoutUndo();
        return m;
    }

    public static GameObject Crate(GameObject prefab, Transform parent, string name, float x, float y, bool showMass = true)
    {
        GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        c.name = name;
        c.transform.localPosition = new Vector3(x, y, 0f);
        if (!showMass && c.TryGetComponent(out MassLabel label)) label.enabled = false;
        return c;
    }

    public static GameObject Instance(GameObject prefab, Transform parent, string name, Vector2 pos)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = name;
        go.transform.localPosition = pos;
        return go;
    }

    /// <summary>Removes objects built by a previous run (by name), so a builder can be re-run.</summary>
    public static void RemoveBuilt(Transform room, params string[] names)
    {
        foreach (string n in names)
        {
            Transform old = Find(room, n);
            while (old != null) { Object.DestroyImmediate(old.gameObject); old = Find(room, n); }
        }
    }
}
