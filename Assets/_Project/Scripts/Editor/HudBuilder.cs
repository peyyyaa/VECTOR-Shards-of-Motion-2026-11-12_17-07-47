using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds two pieces of game-flow UI, called by UIPrefabBuilder (Create UI Prefabs):
///   - the "room solved" ribbon (SolvedBanner) inside UI_Canvas: a journal ribbon at the top of the screen with a
///     small paper card under it;
///   - the scene transition (SceneTransition) as its own prefab in Assets/_Project/Resources, so GameFlow can load it
///     from any scene: black screen, region name in the title glow font, subtitle, ink flourish, progress line.
/// </summary>
public static class HudBuilder
{
    private const string Kit = "Assets/_Project/Art/Sprites/UI";
    private const string FontPath = "Assets/_Project/Art/Fonts/Jersey10 SDF.asset";
    private const string GlowMaterial = "Assets/_Project/Art/Fonts/Jersey10 Title Glow.mat";
    private const string ResourcesFolder = "Assets/_Project/Resources";
    private const string TransitionPath = ResourcesFolder + "/SceneTransition.prefab";

    private static readonly Color32 Ink = new Color32(74, 47, 28, 255), InkSoft = new Color32(107, 74, 46, 255), Paper = new Color32(255, 251, 240, 255);
    private static readonly Color32 Parchment = new Color32(232, 214, 176, 255), Gold = new Color32(214, 170, 90, 255);

    private static Sprite KitSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Kit}/{name}.png");

    // ------------------------------------------------------------------ the room-solved ribbon

    public static SolvedBanner SolvedBanner(Transform canvas, RegionMapData map)
    {
        RectTransform root = MenuUIBuilder.Rect(canvas, "SolvedBanner", Vector2.zero, Vector2.zero);
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;

        RectTransform panel = MenuUIBuilder.Rect(root, "Panel", Vector2.zero, new Vector2(900f, 170f));
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        CanvasGroup group = panel.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        RectTransform card = MenuUIBuilder.Card(panel, "Card", new Vector2(0f, -112f), new Vector2(820f, 104f));
        TMP_Text idea = MenuUIBuilder.Text(card, "Idea", "", 26f, Ink, new Vector2(0f, 14f), new Vector2(760f, 50f));
        idea.enableAutoSizing = true; idea.fontSizeMin = 20f; idea.fontSizeMax = 26f;
        TMP_Text shards = MenuUIBuilder.Text(card, "Shards", "", 24f, InkSoft, new Vector2(0f, -28f), new Vector2(760f, 30f));

        RectTransform ribbon = MenuUIBuilder.Rect(panel, "Ribbon", new Vector2(0f, -40f), new Vector2(900f, 72f));
        Image r = ribbon.gameObject.AddComponent<Image>();
        r.sprite = KitSprite("UI_Ribbon");
        r.type = r.sprite != null && r.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        r.raycastTarget = false;
        TMP_Text title = MenuUIBuilder.Text(ribbon, "Title", "Solved!", 40f, Paper, new Vector2(0f, 2f), new Vector2(820f, 56f));

        SolvedBanner banner = root.gameObject.AddComponent<SolvedBanner>();
        banner.EditorSetup(map, panel, group, title, idea, shards);
        return banner;
    }

    // ------------------------------------------------------------------ the scene transition (Resources prefab)

    public static void SceneTransitionPrefab(RegionMapData[] regions)
    {
        if (!AssetDatabase.IsValidFolder(ResourcesFolder)) { Directory.CreateDirectory(ResourcesFolder); AssetDatabase.Refresh(); }
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        GameObject root = new GameObject("SceneTransition", typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // above every scene's UI
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        CanvasGroup all = root.AddComponent<CanvasGroup>();
        all.blocksRaycasts = false;
        all.interactable = false;

        RectTransform black = MenuUIBuilder.Rect(root.transform, "Black", Vector2.zero, Vector2.zero);
        black.anchorMin = Vector2.zero; black.anchorMax = Vector2.one; black.offsetMin = black.offsetMax = Vector2.zero;
        Image bg = black.gameObject.AddComponent<Image>();
        bg.color = new Color32(14, 11, 9, 255); // warm near-black, like dark parchment in shadow
        bg.raycastTarget = false;

        RectTransform card = MenuUIBuilder.Rect(root.transform, "Card", Vector2.zero, new Vector2(1400f, 400f));
        CanvasGroup cardGroup = card.gameObject.AddComponent<CanvasGroup>();
        TMP_Text title = MenuUIBuilder.Text(card, "Title", "The Shattered Grounds", 104f, Parchment, new Vector2(0f, 60f), new Vector2(1400f, 130f));
        Material glow = AssetDatabase.LoadAssetAtPath<Material>(GlowMaterial);
        if (glow != null) title.fontSharedMaterial = glow;
        TMP_Text subtitle = MenuUIBuilder.Text(card, "Subtitle", "Region 1", 36f, Gold, new Vector2(0f, -30f), new Vector2(1200f, 50f));

        RectTransform line = MenuUIBuilder.Rect(card, "Flourish", new Vector2(0f, -70f), new Vector2(560f, 4f));
        Image flourish = line.gameObject.AddComponent<Image>();
        flourish.sprite = KitSprite("UI_Dot");
        flourish.color = Gold;
        flourish.type = Image.Type.Filled;
        flourish.fillMethod = Image.FillMethod.Horizontal;
        flourish.fillOrigin = (int)Image.OriginHorizontal.Left;
        flourish.fillAmount = 0f;
        flourish.raycastTarget = false;

        TMP_Text progress = MenuUIBuilder.Text(card, "Progress", "", 30f, new Color32(200, 186, 156, 255), new Vector2(0f, -115f), new Vector2(1300f, 44f));

        SceneTransition transition = root.AddComponent<SceneTransition>();
        transition.EditorSetup(all, cardGroup, title, subtitle, progress, flourish, regions);
        if (font != null) foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>(true)) if (t.font == null) t.font = font;

        PrefabUtility.SaveAsPrefabAsset(root, TransitionPath);
        Object.DestroyImmediate(root);
    }
}
