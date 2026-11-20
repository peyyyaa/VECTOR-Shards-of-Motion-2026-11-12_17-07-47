using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tools > Shards of Motion > Room 1 > Add Trail Home.
/// Adds the trail home to the west edge of R1_Room01 (inside the prefab, group Objects/Trail_Home): a small version of the
/// village gateway in the same wood (two posts, a short crossbeam, a hanging "&lt;ALDERBROOK" sign) with two rune stones
/// on the floor under it. The stones are a RegionGate set to To Village: both players on them -> transition -> the village
/// trailhead. The players spawn on these stones, so the gate only arms once both have stepped off.
/// Art and one trigger only: the wall, floor, plate and spawn points are not touched. Re-runnable (replaces Trail_Home).
/// </summary>
public static class TrailHomeTool
{
    private const string RoomPrefab = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room01.prefab";
    private const float Floor = -4.5f, PPU = 32f;
    private const float LeftPost = -8.85f, RightPost = -6.85f; // the wall's face is at x -9; the lift plate at -5.6
    private static readonly Color RuneGlow = new Color(0.45f, 0.9f, 1f);

    [MenuItem("Tools/Shards of Motion/Room 1/Add Trail Home")]
    public static void Add()
    {
        Dictionary<string, Sprite> art = TrailheadArt.Build();
        GameObject root = PrefabUtility.LoadPrefabContents(RoomPrefab);
        try
        {
            Transform objects = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Objects" && t.parent == root.transform);
            if (objects == null)
            {
                objects = new GameObject("Objects").transform;
                objects.SetParent(root.transform, false);
            }
            Transform old = objects.Find("Trail_Home");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            Transform group = new GameObject("Trail_Home").transform;
            group.SetParent(objects, false);

            Vector3 P(float x, float y) => root.transform.TransformPoint(new Vector3(x, y, 0f)); // room-local -> prefab space
            float px = 1f / PPU, mid = (LeftPost + RightPost) * 0.5f;
            float beamBottom = Floor + 82f * px;
            Piece(group, "Post_Left", art, "Post", P(LeftPost, Floor), 0);
            Piece(group, "Post_Right", art, "Post", P(RightPost, Floor), 0);
            Piece(group, "Beam", art, "Beam_Small", P(mid, beamBottom), 1);
            float signH = art.TryGetValue("Sign_Home", out Sprite sign) ? sign.rect.height * px : 0f;
            Piece(group, "Sign_Home", art, "Sign_Home", P(mid, beamBottom - signH), 2);

            GameObject gate = new GameObject("Gate_Home");
            gate.transform.SetParent(group, false);
            gate.transform.position = P(mid, Floor + 1.5f);
            BoxCollider2D trigger = gate.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(RightPost - LeftPost - 0.4f, 3f);
            float[] stoneX = { mid - 0.45f, mid + 0.45f }; // under the two spawn points (-8.3, -7.4)
            SpriteRenderer[] stones = new SpriteRenderer[stoneX.Length];
            Light2D[] glows = new Light2D[stoneX.Length];
            for (int i = 0; i < stoneX.Length; i++)
            {
                stones[i] = Piece(gate.transform, "Rune_Stone_" + (i + 1), art, "Stone_Dark", P(stoneX[i], Floor - 2f * px), 1);
                GameObject g = new GameObject("Glow");
                g.transform.SetParent(stones[i].transform, false);
                g.transform.position = P(stoneX[i], Floor + 0.3f);
                Light2D l = g.AddComponent<Light2D>();
                l.lightType = Light2D.LightType.Point;
                l.color = RuneGlow;
                l.intensity = 0f;
                l.pointLightOuterRadius = 1.6f;
                l.pointLightInnerRadius = 0.16f;
                glows[i] = l;
            }
            RegionGate home = gate.AddComponent<RegionGate>();
            home.EditorSetupHome();
            home.EditorSetupStones(stones, glows, art.TryGetValue("Stone_Dark", out Sprite dark) ? dark : null,
                art.TryGetValue("Stone_Lit", out Sprite lit) ? lit : null);

            PrefabUtility.SaveAsPrefabAsset(root, RoomPrefab);
            Debug.Log("Trail home added to R1_Room01 (Objects/Trail_Home). Open Region1_ShatteredGrounds to see it.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static SpriteRenderer Piece(Transform parent, string name, Dictionary<string, Sprite> art, string sprite, Vector3 at, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = at;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (art.TryGetValue(sprite, out Sprite s)) sr.sprite = s;
        else Debug.LogWarning("Trail home: missing art " + sprite);
        sr.sortingLayerName = "Props";
        sr.sortingOrder = order;
        return sr;
    }
}
