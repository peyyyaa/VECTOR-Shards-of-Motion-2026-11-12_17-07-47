using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static RoomBuildKit;

/// <summary>
/// Building blocks shared by the three finale builders (Room 8 parts 1-3): the Heart Cart, wet ramps (a polygon collider
/// with the Wet_Stone material), wet track edges, the ore chute, a rubble wall, lamps and plain sprites.
/// Coordinates are room-local (floor top y = -4.5), as in RoomBuildKit.
/// </summary>
public static class FinaleKit
{
    public const float Floor = -4.5f, Ceiling = 5.625f;
    public const string WetPath = "Assets/_Project/PhysicsMaterials/Wet_Stone.physicsMaterial2D";
    public const string FxLibraryPath = "Assets/_Project/Art/FX/FX_Library.asset";

    public static PhysicsMaterial2D Wet => AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(WetPath);
    public static FxLibrary FxLib => AssetDatabase.LoadAssetAtPath<FxLibrary>(FxLibraryPath);

    /// <summary>The Heart Cart: a copy of the 2 kg block with a cart body, two wheels and two (hidden) ore chunks.</summary>
    public static HeartCart Cart(GameObject block2kg, Transform parent, string name, float x, float bottomY)
    {
        GameObject go = Crate(block2kg, parent, name, x, bottomY + 0.375f);
        if (go.TryGetComponent(out SpriteRenderer own)) own.enabled = false; // drawn by its body
        if (go.TryGetComponent(out BoxCollider2D box)) box.size = new Vector2(1.2f, 0.75f);
        if (go.TryGetComponent(out Rigidbody2D rb)) rb.freezeRotation = true; // never flips or wedges; the art tilts instead
        Transform art = new GameObject("Visual").transform;
        art.SetParent(go.transform, false);
        SpriteRenderer body = Child(art, "Body", FinaleArt.Get("Cart_Body"), new Vector3(0f, 0.03f, 0f), "Interactables", 2);
        Transform[] wheels = new Transform[2];
        for (int i = 0; i < 2; i++)
            wheels[i] = Child(art, "Wheel_" + (i + 1), FinaleArt.Get("Cart_Wheel"), new Vector3(i == 0 ? -0.38f : 0.38f, -0.3f, 0f), "Interactables", 3).transform;
        GameObject[] ore = new GameObject[2];
        for (int i = 0; i < 2; i++)
        {
            ore[i] = Child(art, "Ore_" + (i + 1), FinaleArt.Get("Shard_Ore"), new Vector3(i == 0 ? -0.18f : 0.2f, 0.42f, 0f), "Interactables", 1).gameObject;
            ore[i].SetActive(false);
        }
        HeartCart cart = go.AddComponent<HeartCart>();
        cart.EditorSetup(2f, 1f, 2, ore, wheels, art);
        if (body == null) Debug.LogWarning("Finale: cart art missing (FinaleArt.Ensure).");
        return cart;
    }

    /// <summary>A solid wet ramp/wedge from a polygon (room-local points, counter-clockwise), with a slab drawn along its top.</summary>
    public static GameObject Ramp(Transform template, Transform parent, string name, Vector2 top1, Vector2 top2, float bottomY)
    {
        GameObject go = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        go.transform.SetParent(parent, false);
        PolygonCollider2D poly = go.AddComponent<PolygonCollider2D>();
        poly.points = new[] { top1, top2, new Vector2(top2.x, bottomY), new Vector2(top1.x, bottomY) };
        poly.sharedMaterial = Wet;
        // Graybox look: the template's sprite as a thin slab along the slope (no collider of its own).
        GameObject slab = Clone(template, go.transform, name + "_Top");
        foreach (Collider2D c in slab.GetComponents<Collider2D>()) Object.DestroyImmediate(c);
        Vector2 d = top2 - top1;
        slab.transform.localPosition = (Vector3)((top1 + top2) * 0.5f) + new Vector3(0f, -0.15f, 0f);
        slab.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        slab.transform.localScale = new Vector3(d.magnitude, 0.3f, 1f);
        if (slab.TryGetComponent(out SpriteRenderer sr)) sr.color = new Color(0.55f, 0.7f, 0.8f);
        return go;
    }

    /// <summary>A thin sloped slab (wet polygon of even thickness) from top1 to top2: rails that leave room underneath.</summary>
    public static GameObject Slab(Transform template, Transform parent, string name, Vector2 top1, Vector2 top2, float thickness)
    {
        GameObject go = Ramp(template, parent, name, top1, top2, Mathf.Min(top1.y, top2.y) - thickness);
        PolygonCollider2D poly = go.GetComponent<PolygonCollider2D>();
        poly.points = new[] { top1, top2, top2 - new Vector2(0f, thickness), top1 - new Vector2(0f, thickness) };
        return go;
    }

    /// <summary>A line (chains, cords) with the default sprite material.</summary>
    public static LineRenderer Line(Transform parent, string name, Color color, float width, string layer, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        line.startColor = line.endColor = color;
        line.startWidth = line.endWidth = width;
        line.sortingLayerName = layer;
        line.sortingOrder = order;
        line.positionCount = 0;
        return line;
    }

    /// <summary>A wet edge collider (μ 0.05) over a span, for a box lowered 2 cm beneath it.</summary>
    public static void WetEdge(Transform parent, string name, float left, float right, float y)
    {
        GameObject go = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        go.transform.SetParent(parent, false);
        EdgeCollider2D edge = go.AddComponent<EdgeCollider2D>();
        edge.points = new[] { new Vector2(left, y), new Vector2(right, y) };
        edge.sharedMaterial = Wet;
        edge.useAdjacentStartPoint = true; edge.adjacentStartPoint = new Vector2(left - 1f, y);
        edge.useAdjacentEndPoint = true; edge.adjacentEndPoint = new Vector2(right + 1f, y);
    }

    /// <summary>The ore chute: a pull handle (Interactable) at 'handle', the chute's mouth above the cart's loading spot.</summary>
    public static OreChute Chute(Transform template, Transform parent, string name, HeartCart cart, Vector2 handle, Vector2 mouth)
    {
        GameObject go = new GameObject(name) { layer = LayerMask.NameToLayer("Interactable") };
        go.transform.SetParent(parent, false);
        go.transform.localPosition = handle;
        BoxCollider2D trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.2f, 1.6f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FinaleArt.Get("Shard_Ore");
        sr.sortingLayerName = "Interactables";
        GameObject mouthGo = new GameObject("Mouth");
        mouthGo.transform.SetParent(go.transform, false);
        mouthGo.transform.position = parent.TransformPoint(mouth);
        // The pipe (visual only): from the mouth up to the ceiling.
        GameObject pipe = Clone(template, go.transform, name + "_Pipe");
        foreach (Collider2D c in pipe.GetComponents<Collider2D>()) Object.DestroyImmediate(c);
        pipe.transform.SetParent(parent, true);
        SetRect(pipe.transform, mouth.x - 0.35f, mouth.y, mouth.x + 0.35f, Ceiling);
        pipe.transform.SetParent(go.transform, true);
        if (pipe.TryGetComponent(out SpriteRenderer psr)) { psr.color = new Color(0.45f, 0.4f, 0.32f); psr.sortingLayerName = "BackDecor"; psr.sortingOrder = 25; }
        OreChute chute = go.AddComponent<OreChute>();
        chute.EditorSetup(cart, mouthGo.transform, FinaleArt.Get("Shard_Ore"));
        return chute;
    }

    /// <summary>A rubble/root wall from floor to ceiling that breaks at 'momentum' kg·m/s (BreakableWall).</summary>
    public static BreakableWall Wall(Transform ledge, Transform parent, string name, float left, float right, float momentum)
    {
        GameObject w = Solid(ledge, parent, name, left, Floor, right, Ceiling);
        if (w.TryGetComponent(out SpriteRenderer sr)) sr.color = new Color(0.45f, 0.36f, 0.28f); // roots: darker, browner
        BreakableWall wall = w.AddComponent<BreakableWall>();
        wall.EditorSetup(momentum, FxLib);
        SerializedObject s = new SerializedObject(wall);
        s.FindProperty("offset").vector2Value = new Vector2(0f, Floor + 2.2f - (Floor + Ceiling) * 0.5f); // label at head height
        s.FindProperty("showDistance").floatValue = 5f;
        s.ApplyModifiedPropertiesWithoutUndo();
        return wall;
    }

    public static Light2D Lamp(Transform parent, string name, Vector2 at, Color color, float radius, float intensity, bool on)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        Light2D light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * 0.1f;
        light.enabled = on;
        return light;
    }

    /// <summary>A sprite at a room-local position (the prefab root sits at the origin while editing), true size under scaled parents.</summary>
    public static SpriteRenderer SpriteAt(Transform parent, string name, Sprite sprite, Vector3 roomPos, float scale, string layer, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = roomPos;
        go.transform.localScale = Vector3.one * scale / Mathf.Max(0.0001f, parent.lossyScale.x);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return sr;
    }

    private static SpriteRenderer Child(Transform parent, string name, Sprite sprite, Vector3 local, string layer, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return sr;
    }

    public static void StandOn(GameObject go, float groundY)
    {
        SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        go.transform.position += new Vector3(0f, groundY - sr.bounds.min.y, 0f);
    }

    /// <summary>The finale's room-local entries / shrine / journal heights, measured from Room 1's templates.</summary>
    public static float ShrineLift => Template("Shrine_Room01") is Transform t ? t.localPosition.y - Floor : 0.6f;
    public static float JournalLift => Template("Journal_Room01") is Transform t ? t.localPosition.y - Floor : 0.3f;
}
