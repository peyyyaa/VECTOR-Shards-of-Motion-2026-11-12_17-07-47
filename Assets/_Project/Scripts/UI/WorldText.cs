using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates labels pinned to things in the world (prompts, mass labels, plate readouts).
///
/// Normally each label is a copy of the Label prefab inside the scene's UI_Canvas (UIRoot), so its
/// look (font, panel, padding) is set in that prefab. If a scene has no UI_Canvas yet, a plain
/// canvas and label are built in code as a fallback, so nothing breaks.
///
/// Labels are drawn at full screen resolution on a screen-space canvas (crisp over pixel art).
/// Sizes are in pixels at a 1080p screen (the canvas scales with the screen).
/// Note (editor only): Game view "Low Resolution Aspect Ratios" or Scale above 1x renders at
/// reduced resolution and makes all text look soft. Builds are unaffected.
/// </summary>
public static class WorldText
{
    public const float ReferenceHeight = 1080f;
    public static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.1f, 0.72f);

    private static Canvas fallbackCanvas;

    /// <param name="fontSizePx">Text size in 1080p pixels.</param>
    /// <param name="font">Null = keep the Label prefab's font.</param>
    public static ScreenLabel Create(string objectName, TMP_FontAsset font, float fontSizePx, Color color, float widthUnits = 6f)
    {
        ScreenLabel label;
        UIRoot ui = UIRoot.Instance;
        if (ui != null && ui.LabelPrefab != null && ui.LabelContainer != null)
        {
            label = Object.Instantiate(ui.LabelPrefab, ui.LabelContainer);
            label.name = objectName;
        }
        else
        {
            label = CreateFallback(objectName);
        }

        if (font != null) label.Text.font = font;
        label.Text.fontSize = fontSizePx;
        label.Text.color = color;
        label.WidthUnits = widthUnits;
        return label;
    }

    /// <summary>
    /// A label in the journal's paper style (speech, notice boards): paper card background, Jersey 10, dark ink text.
    /// Falls back to the normal label style if the canvas has no paper style set.
    /// </summary>
    public static ScreenLabel CreatePaper(string objectName, float fontSizePx, float widthUnits)
    {
        UIRoot ui = UIRoot.Instance;
        ScreenLabel label = Create(objectName, ui != null ? ui.PaperFont : null, fontSizePx, new Color32(74, 47, 28, 255), widthUnits);
        if (label != null && ui != null && ui.PaperSprite != null && label.Background != null)
        {
            label.Background.sprite = ui.PaperSprite;
            label.Background.type = UnityEngine.UI.Image.Type.Tiled;
            label.Background.color = Color.white;
        }
        return label;
    }

    private static ScreenLabel CreateFallback(string objectName)
    {
        if (fallbackCanvas == null)
        {
            GameObject canvasObject = new GameObject("UI (fallback - add UI_Canvas to this scene)");
            fallbackCanvas = canvasObject.AddComponent<Canvas>();
            fallbackCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fallbackCanvas.sortingOrder = 100;
            fallbackCanvas.pixelPerfect = true;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, ReferenceHeight);
            scaler.matchWidthOrHeight = 1f;
        }

        GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(CanvasGroup));
        panel.transform.SetParent(fallbackCanvas.transform, false);
        Image background = panel.AddComponent<Image>();
        background.color = PanelColor;
        background.raycastTarget = false;

        GameObject textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(panel.transform, false);
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;

        ScreenLabel label = panel.AddComponent<ScreenLabel>();
        label.Init(tmp, background);
        return label;
    }
}
