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
///                and the Motion Journal panel docked at the bottom of the screen.
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
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
        }

        Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        ScreenLabel labelPrefab = BuildLabel(rounded);
        GameObject canvasPrefab = BuildCanvas(rounded, labelPrefab);
        AddToScene(canvasPrefab);

        Debug.Log("UI prefabs built in " + Folder + ". UI_Canvas is in the scene under _Managers; save the scene (Ctrl+S).");
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

        // Labels container: fills the screen; labels are positioned inside it at runtime.
        RectTransform labels = NewRect(root.transform, "Labels");
        Stretch(labels);

        // Journal panel: bottom-centre, 1100 px wide, grows to fit its text.
        RectTransform journal = NewRect(root.transform, "JournalPanel");
        journal.anchorMin = journal.anchorMax = new Vector2(0.5f, 0f);
        journal.pivot = new Vector2(0.5f, 0f);
        journal.anchoredPosition = new Vector2(0f, 36f);
        journal.sizeDelta = new Vector2(1100f, 200f);
        Image bg = journal.gameObject.AddComponent<Image>();
        bg.sprite = rounded;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.06f, 0.07f, 0.1f, 0.9f);
        bg.raycastTarget = false;
        VerticalLayoutGroup layout = journal.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 22, 18);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = journal.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI title = NewText(journal, "Title", 34f, TextAlignmentOptions.Left, FontStyles.Bold);
        title.color = new Color(1f, 0.86f, 0.55f);
        title.text = "Journal title";
        TextMeshProUGUI body = NewText(journal, "Body", 27f, TextAlignmentOptions.Left, FontStyles.Normal);
        body.text = "Explanation text.";
        body.lineSpacing = 8f;
        TextMeshProUGUI hint = NewText(journal, "Hint", 20f, TextAlignmentOptions.Right, FontStyles.Italic);
        hint.color = new Color(1f, 1f, 1f, 0.55f);
        hint.text = "Interact again to close";

        JournalPanel panel = journal.gameObject.AddComponent<JournalPanel>();
        panel.EditorSetup(title, body, hint);
        journal.gameObject.SetActive(false); // shown when a Journal stone is read

        UIRoot ui = root.AddComponent<UIRoot>();
        ui.EditorSetup(labels, labelPrefab, panel);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, CanvasPath);
        Object.DestroyImmediate(root);
        return saved;
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
