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
/// Builds Alderbrook, the village hub: three screens side by side, joined by normal doorways (Docs/Village_Art_Plan.md):
///   WREN'S LAB (west, x 0; a new game starts here): the lab and observatory, Wren Alder (he) reading outside, a chest of
///     notes and his research table with the machine parts found so far (PartsDisplay).
///   MARKET SQUARE (x 20): Mira's cottage and Mira, the Field Requests board, the fruit stall, the shard shrine, the well.
///   FORGE &amp; EAST ROAD (x 40): Bram's forge (Bram opens the Village Fund ledger, FundPanel), the trailhead: the trail-map board
///     (Interact: map screen, WallMap), the closed roads signposted, and the gateway to the Shattered Grounds (RegionGate:
///     both players on the rune stones). Built in VillageBuilder.Trailhead.cs.
/// Restoration projects (RestorationProject: well, lamps, garden, shrine) show broken until funded.
/// Buildings: PixelLab art adapted to Art/Village/ (sources in Art/Village/Source), on the Midground layer behind the
/// play space; pack props on Props; villagers on Player. Rooms are prefabs in Prefabs/Rooms/Village/ (RoomDresser does the
/// ground; the outer edge walls keep their colliders but are hidden behind bushes and trees).
/// Needs: Characters > 1. Build Character Sprites (Wren, Bram, Mira), Effects > 1. Build Effect Sprites, Create UI Prefabs,
/// Rooms > Set Up Adjustable Doorway. Re-runnable.
/// </summary>
public static partial class VillageBuilder
{
    private const string RoomFolder = "Assets/_Project/Prefabs/Rooms/Village";
    private const string LabPath = RoomFolder + "/Village_Lab.prefab";
    private const string SquarePath = RoomFolder + "/Village_Square.prefab";
    private const string ForgePath = RoomFolder + "/Village_Forge.prefab";
    private const string ScenePath = "Assets/_Project/Scenes/Village.unity";
    private const string StandardRoom = "Assets/_Project/Prefabs/Rooms/Room_Standard.prefab";
    private const string DoorwayPrefab = "Assets/_Project/Prefabs/Rooms/Doorway_Side.prefab";
    private const string VillageTexture = "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Village Props.png";
    private const string ArtFolder = "Assets/_Project/Art/Village";
    private const string Background = "Assets/_Project/Art/Backgrounds/Processed/BG_MountainVista_A.png";
    private const float Floor = -4.5f;
    private const float Sink = 2f / 32f;   // buildings sit 2 px into the ground so the grass edge overlaps their base
    private const float PPU = 32f;
    private static readonly Color NameInk = new Color32(62, 98, 48, 255);
    private static readonly Color BramInk = new Color32(140, 62, 38, 255);
    private static readonly Color MiraInk = new Color32(92, 64, 120, 255);
    private static readonly Color LampColor = new Color(1f, 0.78f, 0.5f);

    // Everything this builder adds lives in these groups (removed and rebuilt each run); the old names are from earlier versions.
    private static readonly string[] Built =
    {
        "Village_Buildings", "Village_Props", "Village_People", "Village_Exit",
        "Wren", "Bram", "Mira", "Research_Table", "Wall_Map", "Wall_Map_Spot", "Request_Board", "Fund_Stall", "Gate_Region1", "Gate_Sign",
        "Gate_Light", "Sign_Region1", "Sign_Region2", "Sign_Region3", "Locked_Road",
    };

    private static Dictionary<string, Sprite> village;
    private static FxLibrary fx;

    private struct Npc { public CharacterAnimationSet.Clip idle, act; }

    [MenuItem("Tools/Shards of Motion/Game Shell/Build Village")]
    public static void Build()
    {
        Npc wren = NpcClips("Wren"), bram = NpcClips("Bram"), mira = NpcClips("Mira");
        if (wren.idle == null || bram.idle == null || mira.idle == null)
        {
            Debug.LogError("Village: villager sprites (Wren, Bram, Mira) not found. Run Tools > Shards of Motion > Characters > 1. Build Character Sprites first.");
            return;
        }
        if (!ImportArt()) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        village = AssetDatabase.LoadAllAssetsAtPath(VillageTexture).OfType<Sprite>().GroupBy(s => s.name).ToDictionary(g => g.Key, g => g.First());
        fx = AssetDatabase.LoadAssetAtPath<FxLibrary>("Assets/_Project/Art/FX/FX_Library.asset");
        if (fx == null) Debug.LogWarning("Village: FX library not found (Effects > 1. Build Effect Sprites); no pollen, smoke or sparkles.");

        EnsureRoom(LabPath, "Village_Lab", keepLeftWall: true, keepRightWall: false);
        EnsureRoom(SquarePath, "Village_Square", keepLeftWall: false, keepRightWall: false);
        EnsureRoom(ForgePath, "Village_Forge", keepLeftWall: false, keepRightWall: true);
        Edit(LabPath, root => BuildLab(root, wren));
        Edit(SquarePath, root => BuildSquare(root, mira));
        Edit(ForgePath, root => BuildForge(root, bram));
        DressLab();
        DressSquare();
        DressForge();
        BuildScene();
        AddToBuild();
        Debug.Log("Village: Alderbrook built (Wren's Lab + Market Square + Forge & East Road) in " + ScenePath + ".");
    }

    private static Npc NpcClips(string who)
    {
        CharacterAnimationSet set = AssetDatabase.LoadAssetAtPath<CharacterAnimationSet>($"Assets/_Project/Art/Characters/{who}/{who}_Animations.asset");
        CharacterAnimationSet.Clip idle = set != null ? set.Get("Idle") : null, act = set != null ? set.Get("Act") : null;
        return new Npc { idle = idle != null && idle.frames.Length > 0 ? idle : null, act = act };
    }

    // Building art: 32 PPU, Point, uncompressed, pivot at the bottom centre (sprites stand on the ground).
    private static bool ImportArt()
    {
        string[] files = Directory.Exists(ArtFolder) ? Directory.GetFiles(ArtFolder, "*.png", SearchOption.TopDirectoryOnly) : new string[0];
        if (files.Length == 0) { Debug.LogError("Village: no building art in " + ArtFolder + "."); return false; }
        foreach (string f in files)
        {
            string path = f.Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PPU;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            TextureImporterSettings s = new TextureImporterSettings();
            importer.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            s.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(s);
            importer.SaveAndReimport();
        }
        return true;
    }

    private static Sprite Art(string name)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{name}.png");
        if (s == null) Debug.LogWarning("Village: missing art " + name);
        return s;
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
        // The sides that join another screen have a doorway instead of a wall.
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

    private static Transform NewGroup(Transform parent, string name, float x = 0f)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.position = new Vector3(x, 0f, 0f);
        return t;
    }

    // ------------------------------------------------------------------ Wren's Lab

    private static void BuildLab(GameObject root, Npc wren)
    {
        RoomBuildKit.RemoveBuilt(root.transform, Built);
        Transform buildings = NewGroup(Group(root.transform, "Objects"), "Village_Buildings");
        Transform props = NewGroup(Group(root.transform, "Objects"), "Village_Props");
        Transform people = NewGroup(Group(root.transform, "Info"), "Village_People");

        SpriteRenderer lab = Building(buildings, "Lab", -5.3f);
        Marker(lab.transform, "Smoke_Point", Px(lab, 40, 26));

        // Wren reads outside, facing the players coming out of the lab.
        Person(people, "Wren", wren, -0.6f, true).EditorSetup("Wren Alder", NameInk, FirstVisit, PartsFound, Idle);

        // Wren's chest of field notes (the map moved to the trailhead, where the road leaves the village).
        PackProp(props, "Notes_Chest", "Chest Wooden", 1.7f, Floor);

        GameObject table = PackProp(props, "Research_Table", "Table", 4.2f, Floor).gameObject;
        PartsDisplay display = table.AddComponent<PartsDisplay>();
        float top = table.GetComponent<SpriteRenderer>().bounds.max.y;
        AddPart(display, table.transform, "Wheelbarrow Wheel", "R1_Part_Flywheel", 3.8f, top);
        AddPart(display, table.transform, "Anvil", "R1_Part_Counterweight", 4.6f, top);
        // Machine part 3 (Room 5): the Bearing, drawn by Room5Builder (no pack sprite fits).
        Sprite bearing = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Sprites/Props/SP_Part_Bearing.png") ?? Room5Builder.BearingSprite();
        GameObject bearingGo = new GameObject("Part R1_Part_Bearing");
        bearingGo.transform.SetParent(table.transform, false);
        bearingGo.transform.position = new Vector3(4.2f, top + 0.25f, 0f);
        SpriteRenderer bearingSr = bearingGo.AddComponent<SpriteRenderer>();
        bearingSr.sprite = bearing;
        bearingSr.sortingLayerName = "Props";
        bearingSr.sortingOrder = 3;
        display.EditorAdd("R1_Part_Bearing", bearingSr);
        PackProp(table.transform, "Bottle", "White Bottle", 5.2f, top, "Props", 1);
        PackProp(table.transform, "Cup", "Cup", 3.2f, top, "Props", 1);
        PackProp(props, "Chair", "Chair", 5.6f, Floor, "Props", -1);
        PackProp(props, "Crate", "Crate Small", 6.7f, Floor);
        PackProp(props, "Barrel", "Barrel", 7.3f, Floor, "Props", -1);
        Lamp(props, 8.2f);
    }

    // ------------------------------------------------------------------ Market Square

    private static void BuildSquare(GameObject root, Npc mira)
    {
        RoomBuildKit.RemoveBuilt(root.transform, Built);
        Transform buildings = NewGroup(Group(root.transform, "Objects"), "Village_Buildings");
        Transform props = NewGroup(Group(root.transform, "Objects"), "Village_Props");
        Transform people = NewGroup(Group(root.transform, "Info"), "Village_People");
        int interactable = LayerMask.NameToLayer("Interactable");

        // 1. Mira's cottage, with her garden (restoration: wilted boxes -> sunflowers in bloom).
        SpriteRenderer cottage = Building(buildings, "Cottage", -7.0f);
        Marker(cottage.transform, "Smoke_Point", Px(cottage, 55, 0));
        Transform garden = NewGroup(props, "Garden");
        Transform gBroken = NewGroup(garden, "Broken"), gRestored = NewGroup(garden, "Restored");
        ArtSprite(gBroken, "Wilted_Boxes", "Cottage_Wilted", cottage.transform.position.x, cottage.transform.position.y, "Midground", -9);
        PackProp(gRestored, "Sunflower A", "Sunflower 01", -4.55f, Floor, "Props", -1);
        PackProp(gRestored, "Sunflower B", "Sunflower 03", -4.2f, Floor, "Props", -1);
        PackProp(gRestored, "Pot", "Pot 03", -9.3f, Floor, "Props", 1);
        Project(garden, "garden", gBroken, gRestored);
        Person(people, "Mira", mira, -3.6f, true).EditorSetup("Mira", MiraInk, MiraFirst, MiraParts, MiraIdle);
        Lamp(props, -2.6f);

        // 2. The Field Requests board.
        SpriteRenderer board = PackProp(props, "Request_Board", "Billboard", -1.2f, Floor);
        board.gameObject.layer = interactable;
        board.gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
        board.gameObject.AddComponent<RequestBoard>();
        PackProp(board.transform, "Note A", "Billboard Noti 01", -1.55f, board.bounds.center.y - 0.1f, "Props", 1);
        PackProp(board.transform, "Note B", "Billboard Noti 03", -0.85f, board.bounds.center.y, "Props", 1);

        // 3. The fruit stall.
        SpriteRenderer stall = PackProp(props, "Fruit_Stall", "Stall Table", 1.4f, Floor);
        float stallTop = stall.bounds.max.y;
        PackProp(stall.transform, "Tent", "Stall Tent", 1.4f, stallTop - 0.2f, "Props", -1);
        for (int i = 0; i < 4; i++) PackProp(stall.transform, "Fruit " + (i + 1), $"Stall Fruit 0{i + 1}", 0.65f + i * 0.5f, stallTop - 0.05f, "Props", 1);
        PackProp(props, "Basket", "Basket", 2.75f, Floor, "Props", 1);
        PackProp(props, "Sack", "Gunny Bag 01", 0.15f, Floor, "Props", 1);

        // 4. The shard shrine (restoration: dim crystal + overgrowth -> glowing crystal with motes).
        SpriteRenderer shrine = Building(buildings, "Shrine", 5.0f);
        Transform shrineState = NewGroup(props, "Shrine_State");
        Transform sBroken = NewGroup(shrineState, "Broken"), sRestored = NewGroup(shrineState, "Restored");
        float crystalBottom = shrine.transform.position.y + 66f / PPU; // from the source art: crystal floats 66 px above the base
        ArtSprite(sBroken, "Overgrowth", "Shrine_Overgrown", 5.0f, shrine.transform.position.y, "Midground", -9);
        ArtSprite(sBroken, "Crystal_Dim", "Shrine_Crystal_Dim", 5.0f, crystalBottom, "Props", 0);
        SpriteRenderer crystal = ArtSprite(sRestored, "Crystal", "Shrine_Crystal", 5.0f, crystalBottom, "Props", 0);
        Light2D glow = PointLight(crystal.transform, "Shrine Light", crystal.bounds.center, new Color(0.45f, 0.9f, 1f), 1.3f, 4f);
        crystal.gameObject.AddComponent<Hover>().EditorSetup(glow);
        Marker(crystal.transform, "Mote_Point", crystal.bounds.center);
        Project(shrineState, "shrine", sBroken, sRestored);

        // 5. The well (restoration: broken roof -> repaired, bucket on the rope).
        Transform well = NewGroup(props, "Well");
        Transform wBroken = NewGroup(well, "Broken"), wRestored = NewGroup(well, "Restored");
        ArtSprite(wBroken, "Well_Broken", "Well_Broken", 8.4f, Floor, "Props", 0);
        PackProp(wBroken, "Bucket_Tipped", "Bucket", 9.45f, Floor, "Props", 1).transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        PackProp(wRestored, "Well", "Well", 8.4f, Floor);
        PackProp(wRestored, "Bucket", "Bucket", 9.5f, Floor, "Props", 1);
        Project(well, "well", wBroken, wRestored);
    }

    // ------------------------------------------------------------------ Forge & East Road

    private static void BuildForge(GameObject root, Npc bram)
    {
        RoomBuildKit.RemoveBuilt(root.transform, Built);
        Transform buildings = NewGroup(Group(root.transform, "Objects"), "Village_Buildings");
        Transform props = NewGroup(Group(root.transform, "Objects"), "Village_Props");
        Transform people = NewGroup(Group(root.transform, "Info"), "Village_People");
        Transform exit = NewGroup(Group(root.transform, "Puzzle"), "Village_Exit");

        SpriteRenderer forge = Building(buildings, "Forge", -3.6f);
        Marker(forge.transform, "Smoke_Point", Px(forge, 73, 0));
        Vector3 hearth = Px(forge, 68, 166);
        PointLight(forge.transform, "Lamp Hearth Glow", hearth, new Color(1f, 0.55f, 0.25f), 1.4f, 3.5f); // always lit; flickers with the lamps
        Marker(forge.transform, "Ember_Point", hearth + new Vector3(0f, 0.2f, 0f));

        // Bram hammers at the anvil beside his hearth; after his first talk, Interact opens the Village Fund ledger.
        NpcTalk bramTalk = Person(people, "Bram", bram, -3.5f, true);
        bramTalk.EditorSetup("Bram", BramInk, BramFirst, BramParts, BramIdle);
        bramTalk.EditorOpensVillageFund(true);
        PackProp(props, "Anvil", "Anvil", -4.3f, Floor, "Props", 1); // where his hammer lands
        Lamp(props, 0.5f); // just past the forge, leaving the rest of the screen to the trailhead

        // The road east: the trailhead (map board, closed-roads signpost, gateway with rune stones). See VillageBuilder.Trailhead.cs.
        BuildTrailhead(exit);

        // Coming home (trail home gate, Return to Village) starts on this screen: arrive at the trailhead,
        // between the signpost and the gateway, as if walking in along the road.
        foreach ((string entry, float x) in new[] { ("Entry_P1", 4.3f), ("Entry_P2", 4.9f) })
        {
            Transform t = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(c => c.name == entry);
            if (t != null) t.position = new Vector3(x, Floor + 0.75f, 0f);
            else Debug.LogWarning("Village: forge screen has no " + entry + "; players coming home will start at its default spot.");
        }
    }

    // ------------------------------------------------------------------ pieces

    /// <summary>A building from Art/Village (base with lights off) plus its "_Lights" overlay, which the lamps project switches on.</summary>
    private static SpriteRenderer Building(Transform parent, string art, float x)
    {
        SpriteRenderer b = ArtSprite(parent, art, art, x, Floor - Sink, "Midground", -10);
        Sprite lit = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{art}_Lights.png");
        if (lit != null)
        {
            SpriteRenderer l = ArtSprite(b.transform, "Lights", art + "_Lights", x, Floor - Sink, "Midground", -9);
            l.gameObject.SetActive(false);
            Project(b.transform, "lamps", null, l.transform);
        }
        return b;
    }

    /// <summary>A lamp post: unlit until the "lamps" project is funded.</summary>
    private static void Lamp(Transform parent, float x)
    {
        Transform lamp = NewGroup(parent, "Lamp_Post", x);
        SpriteRenderer pillar = PackProp(lamp, "Pillar", "Road Lamp Pillar", x, Floor);
        Vector2 top = new Vector2(pillar.bounds.center.x, pillar.bounds.max.y - 0.3f);
        Transform off = NewGroup(lamp, "Broken", x), on = NewGroup(lamp, "Restored", x);
        PackProp(off, "Light_Off", "Road Lamp Light Off", top.x, top.y, "Props", 1);
        SpriteRenderer bulb = PackProp(on, "Light_On", "Road Lamp Light On", top.x, top.y, "Props", 1);
        PointLight(on, "Lamp Light", bulb.bounds.center, LampColor, 1.1f, 5f);
        Project(lamp, "lamps", off, on);
    }

    private static void Project(Transform owner, string id, Transform broken, Transform restored)
    {
        if (broken != null) broken.gameObject.SetActive(true);   // the prefab shows the broken state; the save decides at runtime
        if (restored != null) restored.gameObject.SetActive(false);
        owner.gameObject.AddComponent<RestorationProject>().EditorSetup(id, broken != null ? broken.gameObject : null, restored != null ? restored.gameObject : null, fx);
    }

    /// <summary>A villager: idle/act animation (NpcActivity), a trigger on the Interactable layer, and NpcTalk (lines set by the caller).</summary>
    private static NpcTalk Person(Transform parent, string name, Npc clips, float x, bool faceLeft)
    {
        GameObject go = new GameObject(name) { layer = LayerMask.NameToLayer("Interactable") };
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, Floor + 0.75f, 0f); // feet on the floor (character pivot: 24 px above the feet)
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.flipX = faceLeft;
        sr.sortingLayerName = "Player";
        Sprite[] act = clips.act != null ? clips.act.frames : new Sprite[0];
        go.AddComponent<NpcActivity>().EditorSetup(clips.idle.frames, clips.idle.framesPerSecond, act, clips.act != null ? clips.act.framesPerSecond : 6f);
        BoxCollider2D c = go.AddComponent<BoxCollider2D>();
        c.isTrigger = true;
        c.size = new Vector2(1.6f, 1.6f);
        return go.AddComponent<NpcTalk>();
    }

    /// <summary>A sprite from Art/Village (bottom-centre pivot), standing at (x, groundY).</summary>
    private static SpriteRenderer ArtSprite(Transform parent, string name, string art, float x, float groundY, string layer, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, groundY, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Art(art);
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return sr;
    }

    /// <summary>A Village Props sprite, centred on x with its bottom on groundY. Room-local = world (prefab roots are at the origin).</summary>
    private static SpriteRenderer PackProp(Transform parent, string name, string sprite, float x, float groundY, string layer = "Props", int order = 0)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (village.TryGetValue("TX Village Props - " + sprite, out Sprite s)) sr.sprite = s;
        else Debug.LogWarning("Village: no pack sprite " + sprite);
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        go.transform.position = new Vector3(x, groundY, 0f);
        if (sr.sprite != null) go.transform.position += new Vector3(x - sr.bounds.center.x, groundY - sr.bounds.min.y, 0f);
        return sr;
    }

    /// <summary>World position of a pixel (x right, y down from the top-left) of a bottom-centre-pivoted building sprite.</summary>
    private static Vector3 Px(SpriteRenderer sr, float px, float py)
    {
        Rect r = sr.sprite.rect;
        Vector3 p = sr.transform.position;
        return new Vector3(p.x - r.width / (2f * PPU) + (px + 0.5f) / PPU, p.y + (r.height - py - 0.5f) / PPU, 0f);
    }

    private static Transform Marker(Transform parent, string name, Vector3 world)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.position = world;
        return t;
    }

    private static Light2D PointLight(Transform parent, string name, Vector3 world, Color color, float intensity, float radius)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = world;
        Light2D l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.pointLightOuterRadius = radius;
        l.pointLightInnerRadius = radius * 0.1f;
        return l;
    }


    private static void AddPart(PartsDisplay display, Transform table, string sprite, string id, float x, float top)
    {
        SpriteRenderer sr = PackProp(table, "Part " + id, sprite, x, top, "Props", 2);
        sr.color = new Color(1f, 0.9f, 0.6f);
        display.EditorAdd(id, sr);
    }

    // ------------------------------------------------------------------ dressing (ground, background props, edge foliage, ambience)

    private static void DressLab()
    {
        RoomDresser.Dress(LabPath, ctx =>
        {
            ctx.BackProp("TX Village Props - Tree 01", -9.2f, Floor, 0.35f);
            ctx.BackProp("TX Village Props - Tree 02", 9.3f, Floor, 0.4f, flip: true);
            ctx.Prop(ctx.Forest, "TX Village Props - Bush 03", -9.7f, Floor, "Foreground", 0);   // hides the edge wall
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 04", -8.9f, Floor, "Foreground", 1);
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 02", 2.9f, Floor);
            ctx.Prop(ctx.Village, "TX Village Props - Flower 01", 0.5f, Floor);
            ctx.Prop(ctx.Forest, "TX Village Props - Bush 01", 9.0f, Floor, "Props", -2);
            Ambience(ctx);
        }, "Wall_Left");
    }

    private static void DressSquare()
    {
        RoomDresser.Dress(SquarePath, ctx =>
        {
            ctx.BackProp("TX Village Props - Tree 02", 1.4f, Floor, 0.6f);
            ctx.BackProp("TX Village Props - Tree 01", 9.4f, Floor, 0.4f, flip: true);
            ctx.Prop(ctx.Village, "TX Village Props - Flower 02", -2.0f, Floor);
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 05", 3.0f, Floor);
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 02", 7.2f, Floor);
            ctx.Prop(ctx.Village, "TX Village Props - Rock 01", -9.6f, Floor, "Props", -1);
            Ambience(ctx);
        });
    }

    private static void DressForge()
    {
        RoomDresser.Dress(ForgePath, ctx =>
        {
            ctx.BackProp("TX Village Props - Tree 02", 7.0f, Floor, 0.4f);
            ctx.BackProp("TX Village Props - Tree 01", 9.6f, Floor, 0.3f, flip: true);
            ctx.Prop(ctx.Forest, "TX Village Props - Bush 02", 9.6f, Floor, "Foreground", 0);  // hides the edge wall
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 07", 8.9f, Floor, "Foreground", 1);
            ctx.Prop(ctx.Village, "TX Village Props - Stump", 3.6f, Floor, "Props", -1);
            ctx.Prop(ctx.Village, "TX Village Props - Rock 02", 7.6f, Floor, "Props", -1);
            ctx.Prop(ctx.Village, "TX Village Props - Barrel", 4.4f, Floor, "Props", -1);
            ctx.Prop(ctx.Village, "TX Village Props - Crate Small", 4.95f, Floor, "Props", -1);
            ctx.Prop(ctx.Village, "TX Village Props - Wheelbarrow", 6.6f, Floor, "Props", -1);
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 05", 1.5f, Floor);
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 02", 7.8f, Floor);
            Ambience(ctx);
        }, "Wall_Right");
    }

    /// <summary>Village life on this screen (VillageAmbience): lamps flicker, foliage sways, birds, pollen, chimney smoke, embers, motes.</summary>
    private static void Ambience(RoomDresser.Context ctx)
    {
        GameObject go = new GameObject("Ambience" + RoomDresser.VisualTag);
        go.transform.SetParent(ctx.Decor, false);
        go.transform.position = ctx.Room.position;
        Light2D[] lamps = ctx.Room.GetComponentsInChildren<Light2D>(true).Where(l => l.name.StartsWith("Lamp")).ToArray();
        SpriteRenderer[] sway = ctx.Decor.GetComponentsInChildren<SpriteRenderer>()
            .Where(r => (r.name.Contains("Grass") || r.name.Contains("Flower") || r.name.Contains("Bush")) && r.sortingLayerName != "BackDecor").ToArray();
        Transform[] Markers(string n) => ctx.Room.GetComponentsInChildren<Transform>(true).Where(t => t.name == n).ToArray();
        go.AddComponent<VillageAmbience>().EditorSetup(fx, lamps, sway, Markers("Smoke_Point"), Markers("Ember_Point"), Markers("Mote_Point"));
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

        string[] rooms = { LabPath, SquarePath, ForgePath };
        GameObject lab = null;
        for (int i = 0; i < rooms.Length; i++)
        {
            GameObject room = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(rooms[i]));
            room.transform.position = new Vector3(20f * i, 0f, 0f);
            if (i == 0) lab = room;
        }
        Transform connections = new GameObject("Connections").transform;
        foreach ((string name, float x) in new[] { ("Doorway_Lab_Square", 10f), ("Doorway_Square_Forge", 30f) })
        {
            GameObject door = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DoorwayPrefab));
            door.name = name;
            door.transform.SetParent(connections, false);
            door.transform.position = new Vector3(x, Floor, 0f);
            // The village flows from screen to screen: full-height opening (the wall above goes off screen), no exit glow.
            Doorway d = door.GetComponent<Doorway>();
            if (d != null)
            {
                SerializedObject so = new SerializedObject(d);
                so.FindProperty("openingHeight").floatValue = 12f;
                Object glow = so.FindProperty("exitGlow").objectReferenceValue;
                so.ApplyModifiedPropertiesWithoutUndo();
                d.Apply();
                if (glow is Transform g) g.gameObject.SetActive(false);
            }
            // The stone above the opening is separate art (not always linked to the Doorway): hide it in the village.
            foreach (Transform t in door.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Wall_Above_Visual") t.gameObject.SetActive(false);
                else if (t.name == "Wall_Above" && t.TryGetComponent(out SpriteRenderer wallSr)) wallSr.enabled = false;
            }
        }

        Sprite bg = AssetDatabase.LoadAssetAtPath<Sprite>(Background);
        if (bg != null)
        {
            Transform backgrounds = new GameObject("Backgrounds").transform;
            for (int i = 0; i < rooms.Length; i++)
            {
                SpriteRenderer sr = new GameObject("Background").AddComponent<SpriteRenderer>();
                sr.transform.SetParent(backgrounds, false);
                sr.sprite = bg;
                sr.transform.position = new Vector3(20f * i, 0f, 0f);
                sr.sortingLayerName = "Background";
                sr.sortingOrder = -100;
            }
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

    // ------------------------------------------------------------------ dev: restoration states (play mode)

    [MenuItem("Tools/Shards of Motion/Dev/Village: Restore All Projects")]
    private static void DevRestoreAll() { foreach (string id in RestorationProject.All) RestorationProject.Restore(id); }

    [MenuItem("Tools/Shards of Motion/Dev/Village: Unrestore All Projects")]
    private static void DevUnrestoreAll() { foreach (string id in RestorationProject.All) RestorationProject.DevUnrestore(id); }

    [MenuItem("Tools/Shards of Motion/Dev/Village: Restore All Projects", true)]
    [MenuItem("Tools/Shards of Motion/Dev/Village: Unrestore All Projects", true)]
    private static bool DevPlaying() => Application.isPlaying;

    // ------------------------------------------------------------------ Wren's lines

    private static readonly string[] FirstVisit =
    {
        "Oh! You must be my two new field assistants. I'm Wren Alder. I study motion: how things start, stop, and push each other around.",
        "East of Alderbrook lie the Shattered Grounds: old ruins full of glowing Motion Shards, and machines nobody has worked out.",
        "I've left journal stones in there with my notes. Read them, help each other, and bring back anything that looks like a machine part.",
        "The market square is just east of here, and Bram's forge is past it, at the start of the road out. Check the board in the square if you'd like a goal. Off you go!",
    };

    private static readonly string[] PartsFound =
    {
        "You've found {parts} machine part(s)! I've laid them out on my table.",
        "See how they fit? They pass motion from one piece to the next. Something big was built in those ruins.",
        "Find the rest and we'll find out what it does.",
    };

    private static readonly string[] Idle =
    {
        "The road to the ruins starts past Bram's forge, on the far right. Remember: what one of you can't move, two of you might.",
        "Stuck? My journal stones will offer a tip if you wait a little. No shame in reading it!",
    };

    // ------------------------------------------------------------------ Bram's lines (blacksmith, keeper of the Village Fund)

    private static readonly string[] BramFirst =
    {
        "Name's Bram. I keep the forge, and the Village Fund along with it.",
        "Bring back Motion Shards and we'll put them toward fixing up Alderbrook: the well, the lamps, Mira's garden, the old shrine.",
        "Talk to me again and I'll open the ledger. Every shard you bring home goes on the page.",
    };

    private static readonly string[] BramParts =
    {
        "Wren's got {parts} of those old parts on his table now. Heavy as anything.",
        "Whoever built them knew a thing or two about momentum.",
    };

    private static readonly string[] BramIdle =
    {
        "A heavy crate is slow to start and slow to stop. Same with stubborn blacksmiths.",
        "Push the same crate together and your forces add. Two of you shove twice as hard as one.",
    };

    // ------------------------------------------------------------------ Mira's lines (gardener)

    private static readonly string[] MiraFirst =
    {
        "Mind the flowers, dears. I'm Mira. I've tended this square since before the ruins went quiet.",
        "Those shards you're after used to drift through here like pollen. The whole village hummed with them.",
        "Bring them home and maybe it will again.",
    };

    private static readonly string[] MiraParts =
    {
        "Wren's {parts}-part puzzle is all he talks about. I haven't seen him this happy in years.",
    };

    private static readonly string[] MiraIdle =
    {
        "A seed doesn't move until something pushes it: the wind, a bird, a careless boot. Nothing else does either.",
        "Drop a pebble and a pumpkin together and they land together. Gravity pulls everything down at the same rate, if the air stays out of it.",
    };
}
