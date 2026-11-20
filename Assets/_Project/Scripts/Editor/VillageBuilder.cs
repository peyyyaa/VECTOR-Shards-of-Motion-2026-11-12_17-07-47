using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > Shards of Motion > Game Shell > Build Village.
/// Builds Alderbrook, the village hub: two screens side by side, joined by a normal doorway (Docs/Status_Dec1.md):
///   WREN'S LAB (west, x 0; where a new game starts): Wren Alder (NpcTalk), her research bench with the machine parts
///     found so far (PartsDisplay), the spot for the wall map (map screen comes next), her tent.
///   MARKET SQUARE (east, x 20): the Field Requests board, the Village Fund stall (shop comes next), the well,
///     and the road east: the Shattered Grounds (RegionGate) and the locked roads to the other regions.
/// Rooms are prefabs in Prefabs/Rooms/Village/ (dressed by RoomDresser); the Village scene holds the managers
/// (camera, light, UI canvas with pause menu, co-op spawner, RoomManager without progress saving) and the backgrounds.
/// Needs: Characters > 1. Build Character Sprites (Wren), Effects > 1. Build Effect Sprites, Create UI Prefabs,
/// Rooms > Set Up Adjustable Doorway. Re-runnable.
/// </summary>
public static class VillageBuilder
{
    private const string RoomFolder = "Assets/_Project/Prefabs/Rooms/Village";
    private const string LabPath = RoomFolder + "/Village_Lab.prefab";
    private const string SquarePath = RoomFolder + "/Village_Square.prefab";
    private const string ScenePath = "Assets/_Project/Scenes/Village.unity";
    private const string StandardRoom = "Assets/_Project/Prefabs/Rooms/Room_Standard.prefab";
    private const string DoorwayPrefab = "Assets/_Project/Prefabs/Rooms/Doorway_Side.prefab";
    private const string VillageTexture = "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Village Props.png";
    private const string Background = "Assets/_Project/Art/Backgrounds/Processed/BG_MountainVista_A.png";
    private const float Floor = -4.5f;
    private static readonly Color NameInk = new Color32(62, 98, 48, 255);

    private static readonly string[] Built =
    {
        "Wren", "Research_Table", "Wall_Map_Spot", "Request_Board", "Fund_Stall", "Gate_Region1", "Gate_Sign",
        "Gate_Light", "Sign_Region1", "Sign_Region2", "Sign_Region3", "Locked_Road",
    };

    private static Dictionary<string, Sprite> village;

    [MenuItem("Tools/Shards of Motion/Game Shell/Build Village")]
    public static void Build()
    {
        CharacterAnimationSet wren = AssetDatabase.LoadAssetAtPath<CharacterAnimationSet>("Assets/_Project/Art/Characters/Wren/Wren_Animations.asset");
        CharacterAnimationSet.Clip wrenIdle = wren != null ? wren.Get("Idle") : null;
        if (wrenIdle == null || wrenIdle.frames.Length == 0)
        {
            Debug.LogError("Village: Wren's sprites not found. Run Tools > Shards of Motion > Characters > 1. Build Character Sprites first.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        village = AssetDatabase.LoadAllAssetsAtPath(VillageTexture).OfType<Sprite>().GroupBy(s => s.name).ToDictionary(g => g.Key, g => g.First());

        EnsureRoom(LabPath, "Village_Lab", keepLeftWall: true, keepRightWall: false);
        EnsureRoom(SquarePath, "Village_Square", keepLeftWall: false, keepRightWall: true);
        Edit(LabPath, root => BuildLab(root, wrenIdle));
        Edit(SquarePath, BuildSquare);
        DressLab();
        DressSquare();
        BuildScene();
        AddToBuild();
        Debug.Log("Village: Alderbrook built (Wren's Lab + Market Square) in " + ScenePath + ".");
    }

    // ------------------------------------------------------------------ rooms

    private static void EnsureRoom(string path, string name, bool keepLeftWall, bool keepRightWall)
    {
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(RoomFolder);
            GameObject room = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(StandardRoom));
            PrefabUtility.UnpackPrefabInstance(room, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            room.name = name;
            Transform layout = Group(room.transform, "Layout");
            foreach (string n in new[] { "Floor", "Wall_Left", "Wall_Right" })
            {
                Transform t = room.transform.Find(n);
                if (t != null) t.SetParent(layout, true);
            }
            foreach (string g in new[] { "Puzzle", "Objects", "Info", "Hazards" }) Group(room.transform, g);
            PrefabUtility.SaveAsPrefabAsset(room, path);
            Object.DestroyImmediate(room);
        }
        // The side that joins the other screen has a doorway instead of a wall.
        Edit(path, root =>
        {
            if (!keepLeftWall) RoomBuildKit.RemoveBuilt(root.transform, "Wall_Left");
            if (!keepRightWall) RoomBuildKit.RemoveBuilt(root.transform, "Wall_Right");
        });
    }

    private static void Edit(string path, System.Action<GameObject> change)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            change(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Transform Group(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) return t;
        t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    // Wren's Lab: Wren greets players arriving from the west; her bench shows the machine parts; the map hangs on the right.
    private static void BuildLab(GameObject root, CharacterAnimationSet.Clip wrenIdle)
    {
        Transform objects = Group(root.transform, "Objects"), info = Group(root.transform, "Info");
        RoomBuildKit.RemoveBuilt(root.transform, Built);
        int interactable = LayerMask.NameToLayer("Interactable");

        GameObject w = new GameObject("Wren") { layer = interactable };
        w.transform.SetParent(info, false);
        w.transform.position = new Vector3(-2.2f, Floor + 0.75f, 0f); // feet on the floor (character pivot: 24 px above the feet)
        SpriteRenderer wsr = w.AddComponent<SpriteRenderer>();
        wsr.sprite = wrenIdle.frames[0];
        wsr.flipX = true; // faces the players coming in from the left
        wsr.sortingLayerName = "Player";
        w.AddComponent<SpriteFlipbook>().EditorSetup(wrenIdle.frames, wrenIdle.framesPerSecond);
        BoxCollider2D wc = w.AddComponent<BoxCollider2D>();
        wc.isTrigger = true;
        wc.size = new Vector2(1.6f, 1.6f);
        w.AddComponent<NpcTalk>().EditorSetup("Wren Alder", NameInk, FirstVisit, PartsFound, Idle);

        GameObject table = Prop(objects, "Research_Table", "TX Village Props - Table", 0.4f, Floor, "Props");
        PartsDisplay display = table.AddComponent<PartsDisplay>();
        float top = table.GetComponent<SpriteRenderer>().bounds.max.y;
        AddPart(display, table.transform, "TX Village Props - Wheelbarrow Wheel", "R1_Part_Flywheel", 0.0f, top);
        AddPart(display, table.transform, "TX Village Props - Anvil", "R1_Part_Counterweight", 0.8f, top);

        // Where the wall map will hang (the map screen is the next step): a notice board for now.
        Prop(objects, "Wall_Map_Spot", "TX Village Props - Billboard", 4.6f, Floor, "Props");
    }

    // Market Square: the board, the Village Fund stall, the well, and the roads out (Shattered Grounds open, others locked).
    private static void BuildSquare(GameObject root)
    {
        Transform objects = Group(root.transform, "Objects"), info = Group(root.transform, "Info"), puzzle = Group(root.transform, "Puzzle");
        RoomBuildKit.RemoveBuilt(root.transform, Built);
        int interactable = LayerMask.NameToLayer("Interactable");

        GameObject board = Prop(objects, "Request_Board", "TX Village Props - Billboard", -5.4f, Floor, "Props");
        board.layer = interactable;
        board.AddComponent<BoxCollider2D>().isTrigger = true;
        board.AddComponent<RequestBoard>();

        // The Village Fund stall (shop comes next): stall table under a striped tent.
        GameObject stall = Prop(objects, "Fund_Stall", "TX Village Props - Stall Table", -0.6f, Floor, "Props");
        Prop(stall.transform, "Tent", "TX Village Props - Stall Tent", -0.6f, stall.GetComponent<SpriteRenderer>().bounds.max.y - 0.2f, "Props").GetComponent<SpriteRenderer>().sortingOrder = -1;

        // The road east: Shattered Grounds (open), with the locked roads to the other regions signposted.
        GameObject gate = new GameObject("Gate_Region1");
        gate.transform.SetParent(puzzle, false);
        gate.transform.position = new Vector3(8.3f, Floor + 1.5f, 0f);
        BoxCollider2D gc = gate.AddComponent<BoxCollider2D>();
        gc.isTrigger = true;
        gc.size = new Vector2(2.2f, 3f);
        gate.AddComponent<RegionGate>().EditorSetup("Region1", "the Shattered Grounds");
        Prop(info, "Gate_Sign", "TX Village Props - Road Sign 01", 5.9f, Floor, "Props");
        Prop(info, "Locked_Road", "TX Village Props - Barricade", 3.9f, Floor, "Props");
        GameObject lightGo = new GameObject("Gate_Light");
        lightGo.transform.SetParent(puzzle, false);
        lightGo.transform.position = new Vector3(8.6f, Floor + 1.8f, 0f);
        Light2D l = lightGo.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = new Color(0.45f, 0.9f, 1f);
        l.intensity = 1f;
        l.pointLightOuterRadius = 3f;
        l.pointLightInnerRadius = 0.3f;

        Sign(info, "Sign_Region1", "The Shattered Grounds  >", new Vector2(5.9f, -1.4f), 4f);
        Sign(info, "Sign_Region2", "The Gravity Divide\n(road closed: coming soon)", new Vector2(3.9f, -1.9f), 3f);
        Sign(info, "Sign_Region3", "The Mechanical Depths\n(road closed: coming soon)", new Vector2(3.9f, -0.6f), 3f);
    }

    private static void Sign(Transform parent, string name, string text, Vector2 pos, float showDistance)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        SerializedObject so = new SerializedObject(go.AddComponent<WorldSign>());
        so.FindProperty("text").stringValue = text;
        so.FindProperty("width").floatValue = 4.5f;
        so.FindProperty("fontSizePx").floatValue = 24f;
        so.FindProperty("showDistance").floatValue = showDistance;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject Prop(Transform parent, string name, string sprite, float x, float groundY, string layer)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (village.TryGetValue(sprite, out Sprite s)) sr.sprite = s;
        sr.sortingLayerName = layer;
        // Room-local = world here (room prefab roots are at the origin). Centre on x, bottom on groundY.
        go.transform.position = new Vector3(x, groundY, 0f);
        if (sr.sprite != null) go.transform.position += new Vector3(x - sr.bounds.center.x, groundY - sr.bounds.min.y, 0f);
        return go;
    }

    private static void AddPart(PartsDisplay display, Transform table, string sprite, string id, float x, float top)
    {
        SpriteRenderer sr = Prop(table, "Part " + id, sprite, x, top, "Props").GetComponent<SpriteRenderer>();
        sr.sortingOrder = 2;
        sr.color = new Color(1f, 0.9f, 0.6f);
        display.EditorAdd(id, sr);
    }

    // ------------------------------------------------------------------ dressing

    private static void DressLab()
    {
        RoomDresser.Dress(LabPath, ctx =>
        {
            ctx.BackProp("TX Village Props - Tree 01", -8f, Floor, 0.35f);
            ctx.BackProp("TX Village Props - Stall Tent", 2.4f, Floor, 0.3f);   // Wren's research tent
            ctx.BackProp("TX Village Props - Wood Logs", 7.6f, Floor, 0.45f);
            ctx.Lamp(-5.4f, Floor);
            ctx.Prop(ctx.Village, "TX Village Props - Barrel", -0.9f, Floor, "Props", -1);
            ctx.Prop(ctx.Village, "TX Village Props - Pot 02", 1.5f, Floor, "Props", -1);
            ctx.Prop(ctx.Forest, "TX Village Props - Bush 01", -7.2f, Floor, "Props", -1);
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 02", 6.4f, Floor);
            ctx.Prop(ctx.Village, "TX Village Props - Flower 01", -3.6f, Floor);
        });
    }

    private static void DressSquare()
    {
        RoomDresser.Dress(SquarePath, ctx =>
        {
            ctx.BackProp("TX Village Props - Well", -3f, Floor, 0.35f);
            ctx.BackProp("TX Village Props - Tree 02", 9.2f, Floor, 0.4f, flip: true);
            ctx.BackProp("TX Village Props - Stall Fruit 01", 1.8f, Floor, 0.35f);
            ctx.Lamp(-7.6f, Floor);
            ctx.Lamp(2.9f, Floor);
            ctx.Prop(ctx.Village, "TX Village Props - Crate Large", -8.8f, Floor, "Props", -1);
            ctx.Prop(ctx.Village, "TX Village Props - Flower 02", -1.9f, Floor);
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 05", 7.1f, Floor);
            ctx.Prop(ctx.Village, "TX Village Props - Rock 02", 9.4f, Floor, "Props", -1);
        });
    }

    // ------------------------------------------------------------------ the scene

    private static void BuildScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform managers = new GameObject("_Managers").transform;

        GameObject camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        camGo.transform.SetParent(managers, false);
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.625f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.12f, 0.12f);
        PixelPerfectCamera ppc = camGo.AddComponent<PixelPerfectCamera>();
        ppc.assetsPPU = 32; ppc.refResolutionX = 640; ppc.refResolutionY = 360;
        ppc.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;
        Light2D global = new GameObject("Global Light 2D").AddComponent<Light2D>();
        global.transform.SetParent(managers, false);
        global.lightType = Light2D.LightType.Global;
        GameObject ui = (GameObject)PrefabUtility.InstantiatePrefab(UIPrefabBuilder.EnsurePrefabs());
        ui.transform.SetParent(managers, false);

        GameObject lab = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LabPath));
        lab.transform.position = Vector3.zero;
        GameObject square = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SquarePath));
        square.transform.position = new Vector3(20f, 0f, 0f);
        Transform connections = new GameObject("Connections").transform;
        GameObject door = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DoorwayPrefab));
        door.name = "Doorway_Lab_Square";
        door.transform.SetParent(connections, false);
        door.transform.position = new Vector3(10f, Floor, 0f);

        Sprite bg = AssetDatabase.LoadAssetAtPath<Sprite>(Background);
        if (bg != null)
            foreach (float x in new[] { 0f, 20f })
            {
                SpriteRenderer sr = new GameObject("Background").AddComponent<SpriteRenderer>();
                sr.sprite = bg;
                sr.transform.position = new Vector3(x, 0f, 0f);
                sr.sortingLayerName = "Background";
                sr.sortingOrder = -100;
            }

        Room labRoom = lab.GetComponent<Room>();
        LocalCoopSpawner spawner = new GameObject("CoopSpawner").AddComponent<LocalCoopSpawner>();
        spawner.transform.SetParent(managers, false);
        SerializedObject s = new SerializedObject(spawner);
        s.FindProperty("player1Prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player1.prefab");
        s.FindProperty("player2Prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player2.prefab");
        s.FindProperty("player1Spawn").objectReferenceValue = labRoom != null ? labRoom.Player1Entry : null;
        s.FindProperty("player2Spawn").objectReferenceValue = labRoom != null ? labRoom.Player2Entry : null;
        s.ApplyModifiedPropertiesWithoutUndo();

        RoomManager manager = new GameObject("RoomManager").AddComponent<RoomManager>();
        manager.transform.SetParent(managers, false);
        SerializedObject m = new SerializedObject(manager);
        m.FindProperty("startingRoom").objectReferenceValue = labRoom;
        m.FindProperty("spawner").objectReferenceValue = spawner;
        m.FindProperty("cameraTransform").objectReferenceValue = camGo.transform;
        m.FindProperty("regionId").stringValue = "Village";
        m.FindProperty("saveProgress").boolValue = false; // the village isn't a region room
        m.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static void AddToBuild()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.Where(x => x.path != ScenePath).ToList();
        int title = scenes.FindIndex(x => x.path.EndsWith("/Title.unity"));
        scenes.Insert(title >= 0 ? title + 1 : 0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ------------------------------------------------------------------ Wren's lines

    private static readonly string[] FirstVisit =
    {
        "Oh! You must be my two new field assistants. I'm Wren Alder. I study motion: how things start, stop, and push each other around.",
        "East of Alderbrook lie the Shattered Grounds: old ruins full of glowing Motion Shards, and machines nobody has worked out.",
        "I've left journal stones in there with my notes. Read them, help each other, and bring back anything that looks like a machine part.",
        "The market is through there to the east, then the road out. Check the board in the square if you'd like a goal. Off you go!",
    };

    private static readonly string[] PartsFound =
    {
        "You've found {parts} machine part(s)! I've laid them out on my bench.",
        "See how they fit? They pass motion from one piece to the next. Something big was built in those ruins.",
        "Find the rest and we'll find out what it does.",
    };

    private static readonly string[] Idle =
    {
        "The road to the ruins is past the market, on the far right. Remember: what one of you can't move, two of you might.",
        "Stuck? My journal stones will offer a tip if you wait a little. No shame in reading it!",
    };
}
