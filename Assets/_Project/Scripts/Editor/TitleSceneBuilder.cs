using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tools > Shards of Motion > Game Shell > Build Title Scene.
/// Creates Assets/_Project/Scenes/Title.unity, an atmospheric title screen (inspired by the team's references):
///   - the "Heart of the Forest" painting (Art/Title/Title_Background.png, PixelLab, 344 x 192 at 16 PPU like the room
///     backgrounds: a root-covered ruined arch over a glowing altar), edges darkened by a vignette, slow light shafts;
///   - a big Motion Shard floating and bobbing in the arch above the painted altar, with a pulsing cyan light;
///   - P1 and P2 on either side of the altar, facing each other around the shard (tinted to the painting, cyan rim light,
///     soft shadows); glowing motes drifting up (TitleAmbience);
///   - on the left: "SHARDS / OF MOTION" in Jersey 10 with a soft cyan glow, and a plain-word menu
///     (Continue / New Game / Settings / Quit): dim words, the selected one bright with a small shard marker;
///   - the Settings panel (journal style) and an EventSystem; a small controls hint at the bottom right.
/// Also sets the build's scene list: Title first, then the Village (if it exists) and Region 1. Re-runnable.
/// </summary>
public static class TitleSceneBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/Title.unity";
    private const string RegionScene = "Assets/_Project/Scenes/Regions/Region1_ShatteredGrounds.unity";
    private const string VillageScene = "Assets/_Project/Scenes/Village.unity";
    private const string ArtFolder = "Assets/_Project/Art/Title";
    // Kept in Art/Title (not Art/Backgrounds) so Backgrounds > Process Backgrounds doesn't treat it as a source to shrink.
    private const string Background = ArtFolder + "/Title_Background.png";
    private const string FontPath = "Assets/_Project/Art/Fonts/Jersey10 SDF.asset";
    private const float FeetBelowCentre = 0.75f; // character sprites: feet 24 px below the player's position
    // Places in the painting (pixel x right / y down, 344 x 192, 16 px per unit, centred on the camera).
    private static Vector2 Painted(float px, float py) => new Vector2((px - 172f) / 16f, (96f - py) / 16f);
    private static readonly float Ground = Painted(0f, 162f).y;          // the lit clearing in front of the altar
    private static readonly Vector2 ShardHome = Painted(234f, 90f);      // inside the arch, above the glowing altar

    [MenuItem("Tools/Shards of Motion/Game Shell/Build Title Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(ArtFolder);
        Sprite vignette = SaveSprite("Title_Vignette", Vignette(320, 180), 320, 180, 16f);
        Sprite shaft = SaveSprite("Title_LightShaft", Shaft(24, 160), 24, 160, 16f);
        Sprite shadow = SaveSprite("Title_Shadow", Shadow(22, 5), 22, 5, 32f);
        Material glow = GlowMaterial();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---- Camera, light, background (room framing: 640 x 360 at 32 PPU)
        GameObject camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.625f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.08f, 0.07f);
        PixelPerfectCamera ppc = camGo.AddComponent<PixelPerfectCamera>();
        ppc.assetsPPU = 32; ppc.refResolutionX = 640; ppc.refResolutionY = 360;
        ppc.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;
        Light2D global = new GameObject("Global Light 2D").AddComponent<Light2D>();
        global.lightType = Light2D.LightType.Global;
        global.intensity = 0.85f;

        Sprite bg = ImportBackground();
        if (bg != null) SR("Background", bg, Vector3.zero, "Background", -100);
        else Debug.LogWarning("Title: " + Background + " not found; the title has no background.");
        SpriteRenderer vig = SR("Vignette", vignette, Vector3.zero, "Foreground", 100);
        vig.color = new Color(0.02f, 0.04f, 0.03f, 1f);

        List<SpriteRenderer> shafts = new List<SpriteRenderer>();
        foreach ((float x, float a) in new[] { (-6.5f, 0.16f), (-4.2f, 0.11f), (-1.6f, 0.08f) })
        {
            SpriteRenderer s = SR("Light Shaft", shaft, new Vector3(x, 1.5f, 0f), "FX", -10);
            s.transform.rotation = Quaternion.Euler(0f, 0f, 28f);
            s.color = new Color(1f, 0.97f, 0.82f, a);
            shafts.Add(s);
        }

        // ---- The landmark: a big Motion Shard floating in the arch above the painted altar
        SpriteRenderer stoneGlow = null; // (the painted altar already glows)
        // The same crystal as the village's shard shrine (Art/Village, made by Build Village), at its native pixel size.
        Sprite shardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Village/Shrine_Crystal.png");
        bool villageCrystal = shardSprite != null;
        if (!villageCrystal)
        {
            Debug.LogWarning("Title: Art/Village/Shrine_Crystal.png not found (run Game Shell > Build Village first); using the Motion Shard pickup sprite.");
            GameObject shardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Interactables/MotionShard.prefab");
            shardSprite = shardPrefab != null ? shardPrefab.GetComponentInChildren<SpriteRenderer>()?.sprite : null;
        }
        Transform shardT = null;
        Light2D shardLight = null;
        if (shardSprite != null)
        {
            SpriteRenderer big = SR("Great Shard", shardSprite, ShardHome, "Props", 5);
            if (villageCrystal) big.transform.position += (Vector3)ShardHome - big.bounds.center; // its pivot is at the bottom: centre it
            else big.transform.localScale = new Vector3(3f, 3f, 1f);
            shardT = big.transform;
            shardLight = new GameObject("Shard Light").AddComponent<Light2D>();
            shardLight.transform.SetParent(shardT, false);
            shardLight.transform.position = big.bounds.center;
            shardLight.lightType = Light2D.LightType.Point;
            shardLight.color = new Color(0.45f, 0.95f, 1f);
            shardLight.pointLightOuterRadius = 4.5f;
            shardLight.pointLightInnerRadius = 0.4f;
            shardLight.intensity = 1.2f;
        }

        // ---- P1 and P2 on either side of the altar, facing each other around the shard. Blended into the painting:
        // tinted toward its dark, cool palette, a cyan rim light from the shard between them (lights only the Player layer),
        // and a soft shadow at their feet.
        foreach ((string who, float x, bool faceLeft) in new[] { ("P1", 2.3f, false), ("P2", 5.5f, true) })
        {
            CharacterAnimationSet set = AssetDatabase.LoadAssetAtPath<CharacterAnimationSet>($"Assets/_Project/Art/Characters/{who}/{who}_Animations.asset");
            CharacterAnimationSet.Clip idle = set != null ? set.Get("Idle") : null;
            if (idle == null || idle.frames.Length == 0) continue;
            SpriteRenderer sr = SR(who, idle.frames[0], new Vector3(x, Ground + FeetBelowCentre, 0f), "Player", 0);
            sr.color = new Color(0.72f, 0.78f, 0.8f);
            sr.flipX = faceLeft;
            sr.gameObject.AddComponent<SpriteFlipbook>().EditorSetup(idle.frames, idle.framesPerSecond);
            if (shadow != null) SR(who + " Shadow", shadow, new Vector3(x, Ground + 1f / 32f, 0f), "Player", -1).color = new Color(0f, 0.02f, 0.02f, 0.5f);
        }
        Light2D rim = new GameObject("Player Rim Light").AddComponent<Light2D>();
        rim.lightType = Light2D.LightType.Point;
        rim.transform.position = new Vector3(ShardHome.x, Ground + 1.4f, 0f);
        rim.color = new Color(0.45f, 0.95f, 1f);
        rim.intensity = 0.9f;
        rim.pointLightOuterRadius = 2.6f;
        rim.pointLightInnerRadius = 0.2f;
        SerializedObject rimSo = new SerializedObject(rim);
        SerializedProperty layers = rimSo.FindProperty("m_ApplyToSortingLayers");
        if (layers != null)
        {
            layers.arraySize = 1;
            layers.GetArrayElementAtIndex(0).intValue = SortingLayer.NameToID("Player");
            rimSo.ApplyModifiedPropertiesWithoutUndo();
        }

        GameObject ambience = new GameObject("Title Ambience");
        ambience.AddComponent<TitleAmbience>().EditorSetup(shardT, shardLight, shafts.ToArray(),
            AssetDatabase.LoadAssetAtPath<FxLibrary>("Assets/_Project/Art/FX/FX_Library.asset"), stoneGlow);

        // ---- UI: title, plain-word menu, hint, settings
        GameObject canvasGo = new GameObject("Title UI", typeof(RectTransform));
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        canvasGo.AddComponent<GraphicRaycaster>();
        Transform c = canvasGo.transform;

        TMP_Text title = MenuUIBuilder.Text(c, "Title", "SHARDS\nOF MOTION", 150f, new Color(1f, 0.97f, 0.86f), new Vector2(-410f, 250f),
            new Vector2(900f, 330f), TextAlignmentOptions.Left);
        title.lineSpacing = -30f;
        if (glow != null) title.fontSharedMaterial = glow;
        MenuUIBuilder.Text(c, "Subtitle", "A two-player physics adventure", 38f, new Color(0.75f, 0.95f, 1f, 0.9f),
            new Vector2(-410f, 110f), new Vector2(900f, 50f), TextAlignmentOptions.Left);

        RectTransform menu = MenuUIBuilder.Rect(c, "Menu", new Vector2(-610f, -190f), new Vector2(500f, 320f));
        VerticalLayoutGroup layout = menu.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = 6f;
        layout.childControlWidth = false; layout.childControlHeight = false;
        layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;

        RectTransform marker = MenuUIBuilder.Rect(c, "Marker", Vector2.zero, new Vector2(24f, 36f));
        Image markerImage = marker.gameObject.AddComponent<Image>();
        markerImage.sprite = shardSprite;
        markerImage.preserveAspect = true;
        markerImage.raycastTarget = false;
        marker.gameObject.SetActive(false);

        Button cont = Word(menu, "CONTINUE", marker, out _);
        Button newGame = Word(menu, "NEW GAME", marker, out TMP_Text newGameLabel);
        Button settings = Word(menu, "SETTINGS", marker, out _);
        Button quit = Word(menu, "QUIT", marker, out _);

        TMP_Text hint = MenuUIBuilder.Text(c, "Hint", "Arrows / Stick: choose     Enter / A: confirm", 26f, new Color(1f, 1f, 1f, 0.55f),
            Vector2.zero, new Vector2(800f, 40f), TextAlignmentOptions.Right);
        RectTransform hr = hint.rectTransform;
        hr.anchorMin = hr.anchorMax = new Vector2(1f, 0f);
        hr.pivot = new Vector2(1f, 0f);
        hr.anchoredPosition = new Vector2(-40f, 30f);

        SettingsPanel settingsPanel = MenuUIBuilder.SettingsPanel(c);
        canvasGo.AddComponent<TitleMenu>().EditorSetup(cont, newGame, settings, quit, newGameLabel, settingsPanel, menu.gameObject);
        MenuUIBuilder.EventSystemObject(null);

        EditorSceneManager.SaveScene(scene, ScenePath);
        SetBuildScenes();
        Debug.Log("Game shell: Title scene saved to " + ScenePath + " and set as the first scene in the build.");
    }

    // A plain-word menu entry: the label is the button's graphic (so it can be clicked), no box behind it.
    private static Button Word(Transform menu, string text, RectTransform marker, out TMP_Text label)
    {
        RectTransform rt = MenuUIBuilder.Rect(menu, text, Vector2.zero, new Vector2(420f, 70f));
        label = MenuUIBuilder.Text(rt, "Label", text, 58f, new Color(1f, 0.96f, 0.86f, 0.55f), Vector2.zero, new Vector2(420f, 70f), TextAlignmentOptions.Left);
        label.raycastTarget = true;
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = label;
        b.transition = Selectable.Transition.None; // TextMenuItem does the highlight
        rt.gameObject.AddComponent<TextMenuItem>().EditorSetup(label, marker);
        return b;
    }

    private static SpriteRenderer SR(string name, Sprite sprite, Vector3 pos, string layer, int order)
    {
        SpriteRenderer sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.transform.position = pos;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return sr;
    }

    private static Sprite ImportBackground()
    {
        TextureImporter importer = AssetImporter.GetAtPath(Background) as TextureImporter;
        if (importer == null) return null;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16f; // like the room backgrounds: each painting pixel = 2 x 2 game pixels
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(Background);
    }

    // Soft cyan underlay ("glow") on a copy of the Jersey 10 material, for the title.
    private static Material GlowMaterial()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) return null;
        string path = "Assets/_Project/Art/Fonts/Jersey10 Title Glow.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(font.material); AssetDatabase.CreateAsset(mat, path); }
        else mat.CopyPropertiesFromMaterial(font.material);
        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor("_UnderlayColor", new Color(0.3f, 0.85f, 1f, 0.55f));
        mat.SetFloat("_UnderlaySoftness", 0.8f);
        mat.SetFloat("_UnderlayDilate", 0.6f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ------------------------------------------------------------------ generated pixel art (16 PPU, like the backgrounds)

    // Dark corners and edges, in 5 alpha steps (pixel-art banding, no smooth gradient).
    private static Color32[] Vignette(int w, int h)
    {
        Color32[] p = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x - w * 0.5f) / (w * 0.5f), dy = (y - h * 0.5f) / (h * 0.5f);
                float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f);
                float a = Mathf.Clamp01((d - 0.7f) / 0.6f);
                a = Mathf.Floor(a * 5f) / 5f * 0.75f;
                p[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        return p;
    }

    // A soft beam: brightest in the middle, fading to the sides and to both ends (stepped).
    private static Color32[] Shaft(int w, int h)
    {
        Color32[] p = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float across = 1f - Mathf.Abs(x - (w - 1) * 0.5f) / (w * 0.5f);
                float along = Mathf.Sin(Mathf.PI * y / (h - 1));
                float a = Mathf.Floor(Mathf.Clamp01(across * along) * 4f) / 4f;
                p[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        return p;
    }

    // A flat oval, stepped (pixel-art): for the players' ground shadows.
    private static Color32[] Shadow(int w, int h)
    {
        Color32[] p = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x - (w - 1) * 0.5f) / (w * 0.5f), dy = (y - (h - 1) * 0.5f) / (h * 0.5f);
                float d = dx * dx + dy * dy;
                byte a = d <= 0.45f ? (byte)255 : d <= 1f ? (byte)140 : (byte)0;
                p[y * w + x] = new Color32(255, 255, 255, a);
            }
        return p;
    }

    private static Sprite SaveSprite(string name, Color32[] pixels, int w, int h, float ppu)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        string path = $"{ArtFolder}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void SetBuildScenes()
    {
        List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
        if (File.Exists(VillageScene)) list.Add(new EditorBuildSettingsScene(VillageScene, true));
        list.Add(new EditorBuildSettingsScene(RegionScene, true));
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes) // keep any others (e.g. prototypes), switched off
            if (list.All(x => x.path != s.path)) list.Add(new EditorBuildSettingsScene(s.path, false));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
