using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Builds the game-shell menus in the Motion Journal's field-notebook style (UI kit sprites + Jersey 10):
/// the pause menu and settings panel (added to UI_Canvas by UIPrefabBuilder) and the title screen's menu.
/// (The map screen is built by MapTool.)
/// Buttons work with mouse, keyboard (arrows + Enter) and gamepad (stick/d-pad + South) via the EventSystem.
/// </summary>
public static class MenuUIBuilder
{
    private const string Kit = "Assets/_Project/Art/Sprites/UI";
    private const string FontPath = "Assets/_Project/Art/Fonts/Jersey10 SDF.asset";
    private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
    public static readonly Color32 Ink = new Color32(74, 47, 28, 255), Paper = new Color32(255, 251, 240, 255), Green = new Color32(62, 98, 48, 255);

    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
    private static Sprite Sprite(string n) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Kit}/{n}.png");

    // ------------------------------------------------------------------ pieces

    public static RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform Fullscreen(Transform parent, string name, Color dim)
    {
        RectTransform rt = Rect(parent, name, Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = dim;
        img.raycastTarget = true; // blocks clicks to whatever is behind
        return rt;
    }

    public static RectTransform Card(Transform parent, string name, Vector2 pos, Vector2 size, string sprite = "UI_Card")
    {
        RectTransform rt = Rect(parent, name, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = Sprite(sprite);
        img.type = Image.Type.Tiled;
        img.raycastTarget = false;
        return rt;
    }

    public static TMP_Text Text(Transform parent, string name, string text, float size, Color color, Vector2 pos, Vector2 box,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        RectTransform rt = Rect(parent, name, pos, box);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (Font != null) t.font = Font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }

    /// <summary>A kit button: paper key-cap, tints when selected/hovered, pressed sprite when clicked.</summary>
    public static Button Button(Transform parent, string name, string label, Vector2 pos, float width, out TMP_Text text)
    {
        RectTransform rt = Rect(parent, name, pos, new Vector2(width, 66f));
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = Sprite("UI_Button");
        img.type = Image.Type.Sliced;
        Button b = rt.gameObject.AddComponent<Button>();
        ColorBlock c = b.colors;
        c.normalColor = new Color(0.92f, 0.9f, 0.86f);
        c.highlightedColor = Color.white;
        c.selectedColor = new Color(1f, 0.93f, 0.7f); // keyboard/gamepad focus: warm highlight
        c.pressedColor = new Color(0.85f, 0.8f, 0.7f);
        b.colors = c;
        text = Text(rt, "Label", label, 34f, Paper, new Vector2(0f, 4f), new Vector2(width - 20f, 56f)); // light text: the key-cap is dark wood
        return b;
    }

    public static Button Button(Transform parent, string name, string label, Vector2 pos, float width) => Button(parent, name, label, pos, width, out _);

    public static Slider Slider(Transform parent, string label, Vector2 pos)
    {
        Text(parent, label + " Label", label, 30f, Ink, pos + new Vector2(-250f, 0f), new Vector2(300f, 44f), TextAlignmentOptions.Left);
        GameObject go = DefaultControls.CreateSlider(new DefaultControls.Resources());
        go.name = label + " Slider";
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos + new Vector2(170f, 0f);
        rt.sizeDelta = new Vector2(420f, 24f);
        Slider s = go.GetComponent<Slider>();
        s.minValue = 0f; s.maxValue = 1f;
        foreach (Image img in go.GetComponentsInChildren<Image>(true))
        {
            if (img.name == "Background") img.color = new Color32(176, 150, 110, 255);
            else if (img.name == "Fill") img.color = Green;
            else if (img.name == "Handle") { img.sprite = Sprite("UI_Button"); img.color = Color.white; }
        }
        ColorBlock c = s.colors;
        c.selectedColor = new Color(1f, 0.85f, 0.5f);
        s.colors = c;
        return s;
    }

    public static CycleOption Cycle(Transform parent, string label, Vector2 pos)
    {
        Text(parent, label + " Label", label, 30f, Ink, pos + new Vector2(-250f, 0f), new Vector2(300f, 44f), TextAlignmentOptions.Left);
        Button b = Button(parent, label + " Option", "", pos + new Vector2(170f, 0f), 420f, out TMP_Text value);
        CycleOption cycle = b.gameObject.AddComponent<CycleOption>();
        cycle.EditorSetup(value);
        return cycle;
    }

    public static void EventSystemObject(Transform parent)
    {
        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        es.transform.SetParent(parent, false);
    }

    // ------------------------------------------------------------------ panels

    public static SettingsPanel SettingsPanel(Transform canvas)
    {
        RectTransform root = Fullscreen(canvas, "Settings", new Color(0f, 0f, 0f, 0.55f));
        RectTransform card = Card(root, "Card", Vector2.zero, new Vector2(1180f, 900f), "UI_Page");
        Text(card, "Heading", "Settings", 54f, Green, new Vector2(0f, 395f), new Vector2(600f, 70f));
        Slider master = Slider(card, "Master volume", new Vector2(-150f, 300f));
        Slider music = Slider(card, "Music", new Vector2(-150f, 240f));
        Slider sound = Slider(card, "Sound effects", new Vector2(-150f, 180f));
        CycleOption display = Cycle(card, "Display", new Vector2(-150f, 105f));
        CycleOption resolution = Cycle(card, "Resolution", new Vector2(-150f, 30f));
        CycleOption tips = Cycle(card, "Wren's tips", new Vector2(-150f, -45f));
        RectTransform controlsCard = Card(card, "Controls", new Vector2(0f, -225f), new Vector2(1060f, 270f));
        Text(controlsCard, "Heading", "CONTROLS", 26f, Green, new Vector2(-400f, 105f), new Vector2(220f, 36f), TextAlignmentOptions.Left);
        TMP_Text controls = Text(controlsCard, "Text", "", 24f, Ink, new Vector2(0f, -10f), new Vector2(1000f, 200f), TextAlignmentOptions.TopLeft);
        Button back = Button(card, "Back", "Back", new Vector2(0f, -395f), 260f);

        SettingsPanel panel = root.gameObject.AddComponent<SettingsPanel>();
        panel.EditorSetup(master, music, sound, display, resolution, tips, controls, back,
            AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));
        root.gameObject.SetActive(false);
        return panel;
    }

    public static PauseMenu PauseMenu(Transform canvas, SettingsPanel settings)
    {
        RectTransform root = Rect(canvas, "PauseMenu", Vector2.zero, Vector2.zero);
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
        RectTransform panel = Fullscreen(root, "Panel", new Color(0f, 0f, 0f, 0.5f));
        RectTransform card = Card(panel, "Card", Vector2.zero, new Vector2(560f, 800f), "UI_Page");
        Text(card, "Heading", "Paused", 56f, Green, new Vector2(0f, 330f), new Vector2(400f, 70f));
        Button resume = Button(card, "Resume", "Resume", new Vector2(0f, 230f), 400f);
        Button restart = Button(card, "Restart Room", "Restart Room", new Vector2(0f, 150f), 400f);
        Button journal = Button(card, "Journal", "Journal", new Vector2(0f, 70f), 400f);
        Button map = Button(card, "Map", "Map", new Vector2(0f, -10f), 400f);
        Button sett = Button(card, "Settings", "Settings", new Vector2(0f, -90f), 400f);
        Button village = Button(card, "Village", "Return to Village", new Vector2(0f, -170f), 400f);
        Button title = Button(card, "Title", "Quit to Title", new Vector2(0f, -250f), 400f);
        PauseMenu menu = root.gameObject.AddComponent<PauseMenu>();
        menu.EditorSetup(panel.gameObject, resume, restart, journal, map, sett, village, title, settings);
        settings.transform.SetAsLastSibling(); // settings draw on top of the pause card
        return menu;
    }
}
