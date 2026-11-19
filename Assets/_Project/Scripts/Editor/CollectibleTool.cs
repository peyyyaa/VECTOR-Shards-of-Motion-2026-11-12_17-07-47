using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Editor tool: Tools > Shards of Motion > Collectibles.
///   1. Create Motion Shard   - draws the 12x16 shard sprite (cyan crystal, Village-style shading) and builds
///                              Prefabs/Interactables/MotionShard (sprite, trigger, glow light, MotionShard)
///   2. Place Room 1 Shards   - puts Room 1's three shards inside R1_Room01 (re-runnable: replaces them)
/// </summary>
public static class CollectibleTool
{
    private const string SpritePath = "Assets/_Project/Art/Sprites/Props/SP_MotionShard.png";
    private const string PrefabPath = "Assets/_Project/Prefabs/Interactables/MotionShard.prefab";
    private const string Room1Prefab = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room01.prefab";

    [MenuItem("Tools/Shards of Motion/Collectibles/1. Create Motion Shard")]
    public static void CreateShard()
    {
        // Cyan magic palette (the art direction's Water/Magic accent), dark teal outline like the pack's soft outlines.
        Dictionary<char, Color32> palette = new Dictionary<char, Color32>
        {
            { 'O', Hex("16323a") }, { 'D', Hex("2a7f8f") }, { 'M', Hex("49c2d0") },
            { 'L', Hex("9ff1f2") }, { 'W', Hex("e8ffff") },
        };
        string[] rows =
        {
            ".....OO.....",
            "....OWLO....",
            "...OWLMMO...",
            "...OLMMDO...",
            "..OWLMMMDO..",
            "..OLMMMMDO..",
            ".OLLMMMMDDO.",
            ".OLMMMMMDDO.",
            ".OLMMMMDDDO.",
            "..OMMMMDDO..",
            "..OMMMDDDO..",
            "...OMMDDO...",
            "...OMDDDO...",
            "....ODDO....",
            ".....OO.....",
            "............",
        };
        Texture2D tex = new Texture2D(12, 16, TextureFormat.RGBA32, false);
        for (int row = 0; row < rows.Length; row++)
            for (int x = 0; x < 12; x++)
                tex.SetPixel(x, 15 - row, rows[row][x] == '.' ? new Color32(0, 0, 0, 0) : palette[rows[row][x]]);
        Directory.CreateDirectory(Path.GetDirectoryName(SpritePath));
        File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        GameObject root = new GameObject("MotionShard");
        root.layer = LayerMask.NameToLayer("Interactable");
        CircleCollider2D trigger = root.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.32f;
        root.AddComponent<MotionShard>();

        GameObject art = new GameObject("Sprite");
        art.transform.SetParent(root.transform, false);
        SpriteRenderer sr = art.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        sr.sortingLayerName = "Interactables";
        sr.sortingOrder = 5;

        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(root.transform, false);
        Light2D light = glow.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(0.45f, 0.95f, 1f);
        light.intensity = 0.7f;
        light.pointLightOuterRadius = 1.2f;
        light.pointLightInnerRadius = 0.2f;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log("Collectibles: Motion Shard sprite and prefab created (" + PrefabPath + ").");
    }

    [MenuItem("Tools/Shards of Motion/Collectibles/2. Place Room 1 Shards")]
    public static void PlaceRoom1()
    {
        GameObject shardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (shardPrefab == null) { Debug.LogError("Collectibles: run 1. Create Motion Shard first."); return; }

        // Room-local positions (floor top y = -4.5). Checked against jump (~2.4 m) and the room's ledges.
        (string name, Vector2 pos)[] shards =
        {
            ("Shard_Ledge", new Vector2(0.8f, -0.4f)),   // easy: jump from the top of the crate ledge (2 m) to reach it
            ("Shard_Float", new Vector2(3.9f, 1.1f)),    // float: on the float ledge, next to the Float journal
            ("Shard_Roof",  new Vector2(-8.4f, 3.25f)),  // explorer: on top of the niche roof, jump up from the perch
        };

        GameObject room = PrefabUtility.LoadPrefabContents(Room1Prefab);
        try
        {
            Transform objects = room.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Objects");
            if (objects == null) { Debug.LogError("Collectibles: R1_Room01 has no Objects group."); return; }
            foreach (Transform t in objects.Cast<Transform>().Where(t => t.name.StartsWith("Shard_")).ToArray())
                Object.DestroyImmediate(t.gameObject);

            foreach ((string name, Vector2 pos) in shards)
            {
                GameObject s = (GameObject)PrefabUtility.InstantiatePrefab(shardPrefab, objects);
                s.name = name;
                s.transform.position = pos;
            }
            PrefabUtility.SaveAsPrefabAsset(room, Room1Prefab);
            Debug.Log("Collectibles: 3 Motion Shards placed in Room 1.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(room);
        }
    }

    private static Color32 Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
