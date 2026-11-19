using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Editor tool: Tools > Shards of Motion > Region 1 Setup.
/// One-time project setup for the Region 1 visual pass (see Docs/Region1_Art_Direction.md):
///   A. Sorting layers:   Background, BackDecor, Midground, Default, Props, Interactables, Player, Water, FX, Foreground
///   B. Village Props textures: Compression None, Point filter (import settings only; package files untouched)
///   C. Project-owned lit material: Art/Materials/MT_VillageProps_Lit (Sprite-Lit-Default)
///   D. Test scene Scenes/Prototype/Art_Lit_Test: original (top) vs lit material (bottom) under neutral light
///   F. Region 1 scene + 8 room prefabs (Prefabs/Rooms/Region1/R1_Room01..08), empty shells
/// Safe to re-run: existing layers/materials/room prefabs are kept, and the Region 1 scene is never overwritten.
/// </summary>
public static class Region1Setup
{
    private const string VillageTextures = "Assets/Cainos/Pixel Art Platformer - Village Props/Texture";
    private const string VillageDefaultMaterial = "Assets/Cainos/Pixel Art Platformer - Village Props/Material/MT Village Props - Default.mat";
    private const string VillagePrefabs = "Assets/Cainos/Pixel Art Platformer - Village Props/Prefab";
    private const string LitMaterialPath = "Assets/_Project/Art/Materials/MT_VillageProps_Lit.mat";
    private const string TestScenePath = "Assets/_Project/Scenes/Prototype/Art_Lit_Test.unity";
    private const string RegionScenePath = "Assets/_Project/Scenes/Regions/Region1_ShatteredGrounds.unity";
    private const string RoomsFolder = "Assets/_Project/Prefabs/Rooms/Region1";
    private const string JournalStonePath = "Assets/_Project/Prefabs/Interactables/JournalStone.prefab";

    private static readonly string[] SortingOrder =
        { "Background", "BackDecor", "Midground", "Default", "Props", "Interactables", "Player", "Water", "FX", "Foreground" };

    private static readonly string[] RoomTitles =
    {
        "First Exchange", "Weight of Things", "Together We Move", "Broken Crossing",
        "Long Push", "Weight of Cooperation", "Unstoppable Object", "Heart of the Forest",
    };

    private static readonly string[] RoomConcepts =
    {
        "Inertia, balanced and unbalanced forces.", "Mass and acceleration (a = F / m).",
        "Net force: combining forces.", "Impulse and momentum (J = Δp).",
        "Friction.", "Weight, normal force and equilibrium.",
        "Momentum and braking.", "Everything combined.",
    };

    // ---------------------------------------------------------------- menu

    [MenuItem("Tools/Shards of Motion/Region 1 Setup/Run All (A, B, C+D, F)")]
    public static void RunAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SortingLayers();
        VillageTextureImport();
        LitMaterialAndTestScene();
        Region1SceneAndRooms();
    }

    [MenuItem("Tools/Shards of Motion/Region 1 Setup/A. Sorting Layers")]
    public static void SortingLayers()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("m_SortingLayers");

        // Insert each missing layer right after the layer that precedes it in SortingOrder.
        List<string> added = new List<string>();
        for (int k = 0; k < SortingOrder.Length; k++)
        {
            if (IndexOfLayer(layers, SortingOrder[k]) >= 0) continue;
            int insertAt = 0;
            for (int p = k - 1; p >= 0; p--)
            {
                int prev = IndexOfLayer(layers, SortingOrder[p]);
                if (prev >= 0) { insertAt = prev + 1; break; }
            }
            layers.InsertArrayElementAtIndex(insertAt);
            SerializedProperty layer = layers.GetArrayElementAtIndex(insertAt);
            layer.FindPropertyRelative("name").stringValue = SortingOrder[k];
            layer.FindPropertyRelative("uniqueID").uintValue = (uint)Random.Range(1, int.MaxValue);
            layer.FindPropertyRelative("locked").boolValue = false;
            added.Add(SortingOrder[k]);
        }
        tagManager.ApplyModifiedProperties();
        Debug.Log(added.Count > 0 ? "A. Sorting layers added: " + string.Join(", ", added) : "A. Sorting layers already set.");
    }

    [MenuItem("Tools/Shards of Motion/Region 1 Setup/B. Village Textures: No Compression")]
    public static void VillageTextureImport()
    {
        int changed = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { VillageTextures }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;

            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Point;
            TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings("Standalone");
            if (standalone.overridden)
            {
                standalone.overridden = false;
                importer.SetPlatformTextureSettings(standalone);
            }
            importer.SaveAndReimport();
            changed++;
        }
        Debug.Log($"B. {changed} Village Props textures set to Compression None + Point filter.");
    }

    [MenuItem("Tools/Shards of Motion/Region 1 Setup/C + D. Lit Material + Test Scene")]
    public static void LitMaterialAndTestScene()
    {
        Material lit = EnsureLitMaterial();
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(null);
        CreateGlobalLight(null);

        Material original = AssetDatabase.LoadAssetAtPath<Material>(VillageDefaultMaterial);
        string[] props = { "Crate Small", "Stone of Recall", "Torch", "Bush 01", "Rock 02", "Wood Log 01", "Tree 01" };
        float x = -8f;
        foreach (string p in props)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{VillagePrefabs}/PF Village Props - {p}.prefab");
            if (prefab == null) continue;

            GameObject top = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            top.transform.position = new Vector3(x, 1f, 0f);
            GameObject bottom = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            bottom.transform.position = new Vector3(x, -3.5f, 0f);
            foreach (SpriteRenderer r in bottom.GetComponentsInChildren<SpriteRenderer>(true))
                if (r.sharedMaterial == original) r.sharedMaterial = lit;
            x += 2.6f;
        }

        GameObject sign = new GameObject("Sign_Compare");
        sign.transform.position = new Vector3(0f, 4.3f, 0f);
        WorldSign worldSign = sign.AddComponent<WorldSign>();
        SerializedObject so = new SerializedObject(worldSign);
        so.FindProperty("text").stringValue = "TOP: original Village material (unlit)   BOTTOM: MT_VillageProps_Lit (Sprite-Lit)\nUnder this neutral light (white, intensity 1) both rows should look identical.";
        so.FindProperty("width").floatValue = 18f;
        so.FindProperty("showDistance").floatValue = 0f;
        so.ApplyModifiedPropertiesWithoutUndo();

        EnsureFolder(Path.GetDirectoryName(TestScenePath));
        EditorSceneManager.SaveScene(scene, TestScenePath);
        Debug.Log("C + D. Lit material ready; test scene saved: " + TestScenePath + " (press Play or compare in the Game view).");
    }

    [MenuItem("Tools/Shards of Motion/Region 1 Setup/F. Region 1 Scene + Room Prefabs")]
    public static void Region1SceneAndRooms()
    {
        if (File.Exists(RegionScenePath))
        {
            EditorUtility.DisplayDialog("Region 1 Setup", "Region1_ShatteredGrounds already exists, so it won't be rebuilt (that would overwrite your work).", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SortingLayers(); // the tilemaps below use the new sorting layers

        GameObject roomStandard = Load("Assets/_Project/Prefabs/Rooms/Room_Standard.prefab");
        GameObject doorway = Load("Assets/_Project/Prefabs/Rooms/Doorway_Side.prefab");
        GameObject shrine = Load("Assets/_Project/Prefabs/Interactables/ResetShrine.prefab");
        GameObject player1 = Load("Assets/_Project/Prefabs/Player/Player1.prefab");
        GameObject player2 = Load("Assets/_Project/Prefabs/Player/Player2.prefab");
        GameObject uiCanvas = UIPrefabBuilder.EnsurePrefabs();
        GameObject journal = EnsureJournalStonePrefab();
        if (roomStandard == null || doorway == null || shrine == null || player1 == null || player2 == null) return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Scene-level groups and shared systems.
        Transform managers = new GameObject("_Managers").transform;
        Transform camera = CreateCamera(managers);
        CreateGlobalLight(managers);
        GameObject ui = (GameObject)PrefabUtility.InstantiatePrefab(uiCanvas);
        ui.transform.SetParent(managers, false);
        GameObject spawnerObject = new GameObject("CoopSpawner");
        spawnerObject.transform.SetParent(managers, false);
        LocalCoopSpawner spawner = spawnerObject.AddComponent<LocalCoopSpawner>();
        GameObject managerObject = new GameObject("RoomManager");
        managerObject.transform.SetParent(managers, false);
        RoomManager roomManager = managerObject.AddComponent<RoomManager>();
        new GameObject("_Background");
        Transform connections = new GameObject("Connections").transform;

        // Rooms (each its own prefab), left to right, 20 units apart.
        EnsureFolder(RoomsFolder);
        List<GameObject> rooms = new List<GameObject>();
        for (int i = 1; i <= 8; i++)
        {
            GameObject room = BuildOrLoadRoom(i, roomStandard, shrine, journal);
            room.transform.position = new Vector3(20f * (i - 1), 0f, 0f);
            rooms.Add(room);
        }

        // Doorways between neighbouring rooms (scene level, never inside a room prefab).
        for (int i = 1; i < 8; i++)
        {
            GameObject d = (GameObject)PrefabUtility.InstantiatePrefab(doorway);
            d.name = $"Doorway_R{i}_R{i + 1}";
            d.transform.SetParent(connections, false);
            d.transform.position = new Vector3(20f * (i - 1) + 10f, -4.5f, 0f);
        }

        // Wiring: players spawn at Room 1's entry points; the game starts in Room 1.
        Transform entry1 = rooms[0].transform.Find("Entry_P1");
        Transform entry2 = rooms[0].transform.Find("Entry_P2");
        SerializedObject s = new SerializedObject(spawner);
        s.FindProperty("player1Prefab").objectReferenceValue = player1;
        s.FindProperty("player2Prefab").objectReferenceValue = player2;
        s.FindProperty("player1Spawn").objectReferenceValue = entry1;
        s.FindProperty("player2Spawn").objectReferenceValue = entry2;
        s.ApplyModifiedPropertiesWithoutUndo();
        SerializedObject m = new SerializedObject(roomManager);
        m.FindProperty("startingRoom").objectReferenceValue = rooms[0].GetComponent<Room>();
        m.FindProperty("spawner").objectReferenceValue = spawner;
        m.FindProperty("cameraTransform").objectReferenceValue = camera;
        m.ApplyModifiedPropertiesWithoutUndo();

        EnsureFolder(Path.GetDirectoryName(RegionScenePath));
        EditorSceneManager.SaveScene(scene, RegionScenePath);
        Debug.Log("F. Region 1 scene saved: " + RegionScenePath + " with 8 room prefabs in " + RoomsFolder);
    }

    [MenuItem("Tools/Shards of Motion/Region 1 Setup/F (Rebuild). Delete + Rebuild Region 1 Scene and Rooms")]
    public static void RebuildRegion1()
    {
        bool ok = EditorUtility.DisplayDialog("Rebuild Region 1",
            "This DELETES Region1_ShatteredGrounds and every room prefab in Prefabs/Rooms/Region1, then builds them again " +
            "as empty shells.\n\nOnly do this while the rooms have no work in them.", "Delete and rebuild", "Cancel");
        if (!ok || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // close the scene before deleting it
        AssetDatabase.DeleteAsset(RegionScenePath);
        AssetDatabase.DeleteAsset(RoomsFolder);
        AssetDatabase.Refresh();
        Region1SceneAndRooms();
    }

    // ---------------------------------------------------------------- rooms

    private static GameObject BuildOrLoadRoom(int i, GameObject roomStandard, GameObject shrine, GameObject journal)
    {
        string path = $"{RoomsFolder}/R1_Room{i:00}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return (GameObject)PrefabUtility.InstantiatePrefab(existing); // never overwrite room work

        // Start from Room_Standard (Room component, graybox floor/walls, entry points), as an independent prefab.
        GameObject room = (GameObject)PrefabUtility.InstantiatePrefab(roomStandard);
        PrefabUtility.UnpackPrefabInstance(room, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        room.name = $"R1_Room{i:00}";
        room.transform.position = Vector3.zero;

        Transform layout = Child(room.transform, "Layout");
        Transform puzzle = Child(room.transform, "Puzzle");
        Child(room.transform, "Objects");
        Child(room.transform, "Hazards");
        Transform info = Child(room.transform, "Info");

        // Graybox floor + walls go into Layout; walls on a doorway side are removed (the doorway has its own).
        MoveOrRemove(room.transform, "Floor", layout, false);
        MoveOrRemove(room.transform, "Wall_Left", layout, i > 1);
        MoveOrRemove(room.transform, "Wall_Right", layout, i < 8);

        // Tilemaps (empty, ready for the art pass).
        Transform grid = Child(layout, "Grid");
        grid.gameObject.AddComponent<Grid>().cellSize = Vector3.one;
        AddTilemap(grid, "TM_BackWall", "BackDecor", false, null);
        AddTilemap(grid, "TM_Terrain", "Midground", true, null);
        if (i == 5)
        {
            AddTilemap(grid, "TM_Terrain_Wet", "Midground", true, AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/_Project/PhysicsMaterials/Wet_Stone.physicsMaterial2D"));
            AddTilemap(grid, "TM_Terrain_Moss", "Midground", true, AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/_Project/PhysicsMaterials/Moss.physicsMaterial2D"));
        }
        AddTilemap(grid, "TM_Decor", "Props", false, null);
        AddTilemap(grid, "TM_Foreground", "Foreground", false, null);

        // Each room has a reset shrine and a Journal stone near its entrance.
        GameObject s = (GameObject)PrefabUtility.InstantiatePrefab(shrine);
        s.name = $"Shrine_Room{i:00}";
        s.transform.SetParent(puzzle, false);
        s.transform.localPosition = new Vector3(-4.5f, -3.9f, 0f);
        GameObject j = (GameObject)PrefabUtility.InstantiatePrefab(journal);
        j.name = $"Journal_Room{i:00}";
        j.transform.SetParent(info, false);
        j.transform.localPosition = new Vector3(-3.2f, -4.2f, 0f);
        SerializedObject js = new SerializedObject(j.GetComponent<JournalStone>());
        js.FindProperty("title").stringValue = $"{i} · {RoomTitles[i - 1]}";
        js.FindProperty("body").stringValue = RoomConcepts[i - 1] + "\n(Journal entry to be written with the room.)";
        js.ApplyModifiedPropertiesWithoutUndo();

        // Save at the origin (rooms are positioned in the scene, not in the prefab), and keep using the
        // SCENE object: SaveAsPrefabAssetAndConnect returns the prefab asset, which must not be moved or referenced.
        PrefabUtility.SaveAsPrefabAssetAndConnect(room, path, InteractionMode.AutomatedAction);
        return room;
    }

    private static void AddTilemap(Transform grid, string name, string sortingLayer, bool solid, PhysicsMaterial2D material)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(grid, false);
        go.AddComponent<Tilemap>();
        TilemapRenderer renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingLayerName = sortingLayer;
        if (!solid) return;

        go.layer = LayerMask.NameToLayer("Ground");
        TilemapCollider2D tiles = go.AddComponent<TilemapCollider2D>();
        tiles.compositeOperation = Collider2D.CompositeOperation.Merge;
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D composite = go.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        if (material != null) composite.sharedMaterial = material;
    }

    private static void MoveOrRemove(Transform room, string childName, Transform layout, bool remove)
    {
        Transform t = room.Find(childName);
        if (t == null) return;
        if (remove) Object.DestroyImmediate(t.gameObject);
        else t.SetParent(layout, true);
    }

    private static GameObject EnsureJournalStonePrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(JournalStonePath);
        if (existing != null) return existing;

        GameObject go = new GameObject("JournalStone");
        go.layer = LayerMask.NameToLayer("Interactable");
        go.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
        SpriteRenderer r = go.AddComponent<SpriteRenderer>();
        r.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Graybox/Graybox_Tile_32.png");
        r.color = new Color(0.95f, 0.8f, 0.4f);
        r.sortingLayerName = "Interactables";
        go.AddComponent<BoxCollider2D>().isTrigger = true;
        go.AddComponent<JournalStone>();
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, JournalStonePath);
        Object.DestroyImmediate(go);
        return saved;
    }

    // ---------------------------------------------------------------- shared helpers

    private static Material EnsureLitMaterial()
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        Material mat = new Material(shader) { name = "MT_VillageProps_Lit" };
        EnsureFolder(Path.GetDirectoryName(LitMaterialPath));
        AssetDatabase.CreateAsset(mat, LitMaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    // Main camera matching the game's setup: orthographic, Pixel Perfect 640 x 360 at 32 PPU.
    private static Transform CreateCamera(Transform parent)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.625f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.192f, 0.302f, 0.475f);
        go.AddComponent<UniversalAdditionalCameraData>();
        PixelPerfectCamera ppc = go.AddComponent<PixelPerfectCamera>();
        ppc.assetsPPU = 32;
        ppc.refResolutionX = 640;
        ppc.refResolutionY = 360;
        ppc.cropFrame = PixelPerfectCamera.CropFrame.Windowbox; // show exactly the room, black bars if the window shape differs
        go.AddComponent<AudioListener>();
        return go.transform;
    }

    private static void CreateGlobalLight(Transform parent)
    {
        GameObject go = new GameObject("Global Light 2D");
        if (parent != null) go.transform.SetParent(parent, false);
        Light2D light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
        light.color = Color.white;
    }

    private static int IndexOfLayer(SerializedProperty layers, string name)
    {
        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name) return i;
        return -1;
    }

    private static Transform Child(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static GameObject Load(string path)
    {
        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go == null) Debug.LogError("Region 1 Setup: missing " + path);
        return go;
    }

    private static void EnsureFolder(string path)
    {
        path = path.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
