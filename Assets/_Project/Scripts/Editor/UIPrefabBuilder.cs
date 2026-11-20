using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor tool: Tools > Shards of Motion > Create UI Prefabs (Add To Scene).
/// Builds (or rebuilds) two prefabs in Assets/_Project/Prefabs/UI and puts the canvas in the open scene:
///   Label      - a dark rounded panel with centered text; every world-pinned label is a copy.
///   UI_Canvas  - screen-space canvas (1920x1080 reference, pixel perfect) with a Labels container
///                and the Motion Journal notebook (centred, opens when a Journal stone is read).
/// After building, restyle the prefabs freely in the editor (font, colors, sprite, sizes);
/// re-running the tool would overwrite those changes, so only run it again to reset them.
/// </summary>
public static class UIPrefabBuilder
{
    private const string Folder = "Assets/_Project/Prefabs/UI";
    private const string LabelPath = Folder + "/Label.prefab";
    private const string CanvasPath = Folder + "/UI_Canvas.prefab";

    [MenuItem("Tools/Shards of Motion/Create UI Prefabs (Add To Scene)")]
    public static void Build()
    {
        GameObject canvasPrefab = BuildPrefabs();
        AddToScene(canvasPrefab);
        Debug.Log("UI prefabs built in " + Folder + ". UI_Canvas is in the scene under _Managers; save the scene (Ctrl+S).");
    }

    /// <summary>Returns the UI_Canvas prefab, building both UI prefabs first if they don't exist yet.</summary>
    public static GameObject EnsurePrefabs()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPath);
        return existing != null ? existing : BuildPrefabs();
    }

    private static GameObject BuildPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
        }

        Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        ScreenLabel labelPrefab = BuildLabel(rounded);
        return BuildCanvas(rounded, labelPrefab);
    }

    private static ScreenLabel BuildLabel(Sprite rounded)
    {
        GameObject root = new GameObject("Label", typeof(RectTransform), typeof(CanvasGroup));
        Image panel = root.AddComponent<Image>();
        panel.sprite = rounded;
        panel.type = Image.Type.Sliced;
        panel.color = WorldText.PanelColor;
        panel.raycastTarget = false;
        ((RectTransform)root.transform).sizeDelta = new Vector2(160f, 44f);

        TextMeshProUGUI text = NewText(root.transform, "Text", 26f, TextAlignmentOptions.Center, FontStyles.Normal);
        text.text = "Label";

        ScreenLabel label = root.AddComponent<ScreenLabel>();
        label.EditorSetup(text, panel);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, LabelPath);
        Object.DestroyImmediate(root);
        return saved.GetComponent<ScreenLabel>();
    }

    private static GameObject BuildCanvas(Sprite rounded, ScreenLabel labelPrefab)
    {
        GameObject root = new GameObject("UI_Canvas", typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvas.pixelPerfect = true;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, WorldText.ReferenceHeight);
        scaler.matchWidthOrHeight = 1f;
        root.AddComponent<GraphicRaycaster>(); // mouse clicks on menu buttons

        // Labels container: fills the screen; labels are positioned inside it at runtime.
        RectTransform labels = NewRect(root.transform, "Labels");
        Stretch(labels);
        labels.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false; // UIRoot fades labels while the journal is open

        JournalPanel panel = BuildJournal(root.transform);
        panel.gameObject.SetActive(false); // shown when a Journal stone is read

        // Game shell: pause menu (Esc / Start) with its settings panel, and the EventSystem menus need.
        SettingsPanel settingsPanel = MenuUIBuilder.SettingsPanel(root.transform);
        MenuUIBuilder.PauseMenu(root.transform, settingsPanel);
        MenuUIBuilder.EventSystemObject(root.transform);

        UIRoot ui = root.AddComponent<UIRoot>();
        ui.EditorSetup(labels, labelPrefab, panel);
        ui.EditorSetupPaper(KitSprite("UI_Card"), AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(JerseyFont)); // speech & notice boards

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, CanvasPath);
        Object.DestroyImmediate(root);
        return saved;
    }

    // ------------------------------------------------------------------ the notebook journal

    private const string Kit = "Assets/_Project/Art/Sprites/UI";
    private const string JerseyFont = "Assets/_Project/Art/Fonts/Jersey10 SDF.asset";
    private static readonly Color32 Ink = new Color32(74, 47, 28, 255), InkSoft = new Color32(107, 74, 46, 255);
    private static readonly Color32 Paper = new Color32(255, 251, 240, 255), GreenInk = new Color32(62, 98, 48, 255);
    private static readonly Color32 P1Color = new Color32(200, 104, 30, 255), P2Color = new Color32(40, 140, 160, 255);

    // Field-journal spread (see Art_UIKit_Preview): leather cover, two parchment pages with a spiral binding,
    // title ribbon; left page = force diagram card + margin notes, right page = Big Idea, notes, Look Closer, fun fact.
    private static JournalPanel BuildJournal(Transform canvas)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(JerseyFont);
        RectTransform j = NewRect(canvas, "JournalPanel");
        j.sizeDelta = new Vector2(1800f, 780f);
        CanvasGroup group = j.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        Img(j, "Cover_Shadow", "UI_Shadow", new Vector2(18, -24), new Vector2(1800, 780));
        Img(j, "Cover", "UI_Cover", Vector2.zero, new Vector2(1800, 780), Image.Type.Tiled);
        Img(j, "PageShadow_Left", "UI_Shadow", new Vector2(-428, -12), new Vector2(840, 680));
        Img(j, "PageShadow_Right", "UI_Shadow", new Vector2(452, -12), new Vector2(840, 680));
        RectTransform left = Img(j, "Page_Left", "UI_Page", new Vector2(-440, 0), new Vector2(840, 680), Image.Type.Tiled);
        RectTransform right = Img(j, "Page_Right", "UI_Page", new Vector2(440, 0), new Vector2(840, 680), Image.Type.Tiled);
        Img(left, "Gutter", "UI_Gutter", new Vector2(372, 0), new Vector2(96, 668));
        Img(right, "Gutter", "UI_Gutter", new Vector2(-372, 0), new Vector2(96, 668)).localScale = new Vector3(-1, 1, 1);

        // Page contents live in their own groups so they can fade while a page turns (the paper stays).
        RectTransform lc = NewRect(left, "Content");
        CanvasGroup leftGroup = lc.gameObject.AddComponent<CanvasGroup>();
        RectTransform rc = NewRect(right, "Content");
        CanvasGroup rightGroup = rc.gameObject.AddComponent<CanvasGroup>();

        // The page that folds over the spine when turning (pivot/scale animated by JournalPanel).
        RectTransform flap = Img(j, "TurningPage", "UI_Page", Vector2.zero, new Vector2(840, 680), Image.Type.Tiled);
        flap.pivot = new Vector2(0f, 0.5f);
        flap.anchoredPosition = new Vector2(20, 0);
        flap.gameObject.SetActive(false);

        for (int i = 0; i < 8; i++) // eight separate metal rings down the spine (art pixel = 3 canvas px)
            Img(j, "Ring", "UI_Rings", new Vector2(0f, 262f - i * 75f), new Vector2(48, 30));
        foreach ((string tab, float y) in new[] { ("UI_Tab_Green", 240f), ("UI_Tab_Gold", 144f), ("UI_Tab_Cyan", 48f) })
            Img(j, tab, tab, new Vector2(890, y), new Vector2(48, 84));

        Img(j, "Ribbon_Shadow", "UI_Shadow", new Vector2(10, 318), new Vector2(760, 64));
        RectTransform ribbon = Img(j, "Ribbon", "UI_Ribbon", new Vector2(0, 330), new Vector2(760, 72));
        TMP_Text title = Txt(ribbon, "Title", font, 44, Paper, Vector2.zero, new Vector2(680, 60), TextAlignmentOptions.Center);

        // Left page (680 tall: +340 .. -340): header with two of Wren's doodles, diagram card + caption, margin notes.
        Txt(lc, "Header", font, 28, InkSoft, new Vector2(0, 262), new Vector2(560, 40), TextAlignmentOptions.Center).text = "From the field journal of Wren Alder";
        Img(lc, "Doodle_Crate", "UI_Doodle_Crate", new Vector2(-325, 264), new Vector2(48, 27)).GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.75f);
        Img(lc, "Doodle_Sparkle", "UI_Doodle_Sparkle", new Vector2(320, 266), new Vector2(27, 21)).GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.75f);
        Img(lc, "Divider", "UI_Divider", new Vector2(0, 236), new Vector2(640, 9), Image.Type.Tiled);
        RectTransform card = Img(lc, "Diagram_Card", "UI_Card", new Vector2(0, -8), new Vector2(680, 440), Image.Type.Tiled);
        Img(card, "Tape", "UI_Tape", new Vector2(0, 218), new Vector2(120, 48));
        RectTransform area = NewRect(card, "Diagram");
        area.anchoredPosition = new Vector2(0, 30);
        area.sizeDelta = new Vector2(620, 340);
        ForceDiagramView diagram = area.gameObject.AddComponent<ForceDiagramView>();
        diagram.EditorSetup(KitSprite("UI_Block"), KitSprite("UI_ArrowShaft"), KitSprite("UI_ArrowHead"), font);
        TMP_Text caption = Txt(card, "Caption", font, 28, Ink, new Vector2(0, -175), new Vector2(620, 60), TextAlignmentOptions.Center);
        // Room sketches (300 x 170 px) shown at exactly 2x, so their pixels stay square.
        RectTransform sketchRt = NewRect(card, "Sketch");
        sketchRt.anchoredPosition = new Vector2(0, 30);
        sketchRt.sizeDelta = new Vector2(600, 340);
        Image sketch = sketchRt.gameObject.AddComponent<Image>();
        sketch.raycastTarget = false;
        sketch.preserveAspect = true;
        sketchRt.gameObject.SetActive(false);

        RectTransform p1Row = NewRect(lc, "Margin_P1");
        Img(p1Row, "Icon", "UI_Icon_Burst", new Vector2(-330, -260), new Vector2(48, 48));
        TMP_Text marginP1 = Txt(p1Row, "Text", font, 28, P1Color, new Vector2(40, -260), new Vector2(660, 40), TextAlignmentOptions.Left);
        RectTransform p2Row = NewRect(lc, "Margin_P2");
        Img(p2Row, "Icon", "UI_Icon_Tether", new Vector2(-330, -300), new Vector2(48, 48));
        TMP_Text marginP2 = Txt(p2Row, "Text", font, 28, P2Color, new Vector2(40, -300), new Vector2(660, 40), TextAlignmentOptions.Left);

        // Right page. Cards: a heading row (icon + label) on top, body text below it, indented to the heading.
        RectTransform idea = Img(rc, "BigIdea_Card", "UI_Card", new Vector2(-15, 225), new Vector2(700, 160), Image.Type.Tiled);
        Img(idea, "Icon", "UI_Icon_Bulb", new Vector2(-300, 46), new Vector2(42, 42));
        Txt(idea, "Heading", font, 28, GreenInk, new Vector2(-110, 46), new Vector2(330, 36), TextAlignmentOptions.Left).text = "BIG IDEA";
        TMP_Text bigIdea = Txt(idea, "Text", font, 32, Ink, new Vector2(30, -22), new Vector2(610, 88), TextAlignmentOptions.TopLeft);
        TMP_Text notes = Txt(rc, "Notes", font, 29, Ink, new Vector2(0, 55), new Vector2(730, 150), TextAlignmentOptions.TopLeft);
        RectTransform closer = Img(rc, "LookCloser_Card", "UI_Card", new Vector2(0, -125), new Vector2(730, 180), Image.Type.Tiled);
        Img(closer, "Icon", "UI_Icon_Magnifier", new Vector2(-315, 56), new Vector2(42, 42));
        Txt(closer, "Heading", font, 26, GreenInk, new Vector2(-125, 56), new Vector2(330, 36), TextAlignmentOptions.Left).text = "LOOK CLOSER";
        TMP_Text lookCloser = Txt(closer, "Text", font, 26, Ink, new Vector2(30, -28), new Vector2(640, 112), TextAlignmentOptions.TopLeft);
        RectTransform fun = NewRect(rc, "FunFact");
        Img(fun, "Icon", "UI_Icon_Book", new Vector2(-345, -236), new Vector2(42, 42));
        Txt(fun, "Heading", font, 24, GreenInk, new Vector2(-150, -236), new Vector2(330, 32), TextAlignmentOptions.Left).text = "FUN FACT";
        TMP_Text funFact = Txt(fun, "Text", font, 24, InkSoft, new Vector2(15, -268), new Vector2(660, 50), TextAlignmentOptions.TopLeft);
        Img(right, "Seal", "UI_Seal", new Vector2(378, 300), new Vector2(54, 54));

        // Folded corner (bottom right): wiggles while there's another page. Pivot on the corner so it grows from it.
        RectTransform curl = Img(right, "PageCurl", "UI_PageCurl", Vector2.zero, new Vector2(36, 36));
        curl.pivot = new Vector2(1f, 0f);
        curl.anchoredPosition = new Vector2(420, -340);

        // Bottom bar:  hold to close   * o o        [E] [Enter]  Next page >
        const float barY = -314f;
        TMP_Text hint = Txt(rc, "HoldHint", font, 22, InkSoft, new Vector2(-255, barY), new Vector2(190, 30), TextAlignmentOptions.Left);
        hint.text = "hold to close";
        RectTransform fillRt = Img(hint.rectTransform, "HoldFill", "UI_ArrowShaft", new Vector2(0, -18), new Vector2(190, 6));
        Image fill = fillRt.GetComponent<Image>();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        fill.color = new Color32(178, 68, 58, 255);
        fill.enabled = false;

        Image[] dots = new Image[6];
        for (int i = 0; i < dots.Length; i++)
            dots[i] = Img(rc, "Dot", "UI_Dot", new Vector2(-110 + i * 24, barY), new Vector2(15, 15)).GetComponent<Image>();

        RectTransform[] badges = new RectTransform[2];
        TMP_Text[] keys = new TMP_Text[2];
        for (int i = 0; i < 2; i++)
        {
            badges[i] = Img(rc, "Key_P" + (i + 1), "UI_Button", Vector2.zero, new Vector2(60, 42));
            badges[i].pivot = new Vector2(1f, 0.5f);
            badges[i].anchoredPosition = new Vector2(200 - i * 70, barY);
            keys[i] = Txt(badges[i], "Key", font, 26, i == 0 ? P1Color : P2Color, Vector2.zero, new Vector2(60, 36), TextAlignmentOptions.Center);
            keys[i].enableAutoSizing = false;
            keys[i].textWrappingMode = TextWrappingModes.NoWrap;
            Stretch(keys[i].rectTransform);
            keys[i].rectTransform.offsetMin = new Vector2(0, 3); // sit on the key cap, above its darker bottom edge
        }
        TMP_Text prompt = Txt(rc, "Prompt", font, 30, Ink, Vector2.zero, new Vector2(140, 36), TextAlignmentOptions.Right);
        prompt.enableAutoSizing = false;
        prompt.textWrappingMode = TextWrappingModes.NoWrap;
        prompt.rectTransform.pivot = new Vector2(1f, 0.5f);
        prompt.rectTransform.anchoredPosition = new Vector2(303, barY);
        RectTransform arrow = Img(rc, "PromptArrow", "UI_ArrowHead", new Vector2(323, barY), new Vector2(18, 18));
        arrow.GetComponent<Image>().color = Ink;

        JournalPanel panel = j.gameObject.AddComponent<JournalPanel>();
        panel.EditorSetup(title, bigIdea, notes, lookCloser, funFact, marginP1, marginP2, caption,
            idea.gameObject, closer.gameObject, fun.gameObject, p1Row.gameObject, p2Row.gameObject, card.gameObject, diagram,
            group, leftGroup, rightGroup, flap, curl);
        panel.EditorSetupBar(dots, badges, keys, prompt, arrow.gameObject, hint, fill);
        panel.EditorSetupSketch(sketch);
        return panel;
    }

    private static Sprite KitSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Kit}/{name}.png");

    private static RectTransform Img(RectTransform parent, string name, string sprite, Vector2 pos, Vector2 size, Image.Type type = Image.Type.Sliced)
    {
        RectTransform rt = NewRect(parent, name);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = KitSprite(sprite);
        img.type = img.sprite != null && img.sprite.border != Vector4.zero ? type : Image.Type.Simple;
        img.raycastTarget = false;
        return rt;
    }

    private static TMP_Text Txt(RectTransform parent, string name, TMP_FontAsset font, float size, Color32 color, Vector2 pos, Vector2 box, TextAlignmentOptions align)
    {
        TextMeshProUGUI t = NewText(parent, name, size, align, FontStyles.Normal);
        if (font != null) t.font = font;
        t.color = color;
        RectTransform rt = t.rectTransform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        t.enableAutoSizing = true; // long entries shrink to fit their box instead of spilling off the page
        t.fontSizeMax = size;
        t.fontSizeMin = Mathf.Max(16f, size * 0.7f);
        return t;
    }

    private static void AddToScene(GameObject canvasPrefab)
    {
        if (Object.FindFirstObjectByType<UIRoot>(FindObjectsInactive.Include) != null) return; // already there

        GameObject managers = GameObject.Find("_Managers");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(canvasPrefab);
        Undo.RegisterCreatedObjectUndo(instance, "Add UI_Canvas");
        if (managers != null) Undo.SetTransformParent(instance.transform, managers.transform, false, "Add UI_Canvas");
        EditorSceneManager.MarkSceneDirty(instance.scene);
    }

    private static RectTransform NewRect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI NewText(Transform parent, string name, float size, TextAlignmentOptions align, FontStyles style)
    {
        RectTransform rt = NewRect(parent, name);
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }
}
