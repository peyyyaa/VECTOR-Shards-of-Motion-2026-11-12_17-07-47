using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the Momentum Engine assembly screen (EngineAssemblyPanel) into UI_Canvas, in the journal's parchment style:
/// the empty engine as a 4x blueprint (Engine_Base) with faint marks where the parts go, a tray with the three parts
/// (bearing, flywheel, counterweight from the parts sheet), two ring cursors (P1 orange, P2 teal), a hint line and the
/// controls. Socket positions come from EngineArtTool (the art's wheel centre and hook), so they line up with the drawing.
/// Called by UIPrefabBuilder (Create UI Prefabs). Needs Room 8 > 0. Import Engine Art first; without the art it builds
/// nothing and the engine installs its parts directly.
/// </summary>
public static class EngineAssemblyBuilder
{
    private const float Scale = 4f;                                   // blueprint pixels per art pixel
    private static readonly Vector2 BlueprintCentre = new Vector2(-230f, 10f);
    private static readonly Color32 InkSoft = new Color32(107, 74, 46, 255);
    private static readonly Color32 P1 = new Color32(232, 137, 58, 255), P2 = new Color32(73, 194, 208, 255);
    private static readonly Color Mark = new Color(0.29f, 0.18f, 0.11f, 0.3f);

    public static EngineAssemblyPanel Build(Transform canvas)
    {
        Sprite baseSprite = EngineArtTool.Load("Engine_Base"), loaded = EngineArtTool.Load("Engine_Base_Loaded"),
               wheel = EngineArtTool.Load("Engine_Wheel"), flywheel = EngineArtTool.Load("Part_Flywheel"),
               bearing = EngineArtTool.Load("Part_Bearing"), weight = EngineArtTool.Load("Part_Counterweight"),
               ring = EngineArtTool.Load("Cursor_Ring");
        if (baseSprite == null || loaded == null || wheel == null || flywheel == null || bearing == null || weight == null || ring == null)
        {
            Debug.LogWarning("Engine assembly screen: engine art not found (Room 8 > 0. Import Engine Art). Skipped: the engine will install its parts directly.");
            return null;
        }

        RectTransform root = MenuUIBuilder.Fullscreen(canvas, "EngineAssemblyPanel", new Color(0f, 0f, 0f, 0.6f));
        RectTransform card = MenuUIBuilder.Card(root, "Card", Vector2.zero, new Vector2(1500f, 960f), "UI_Page");
        MenuUIBuilder.Text(card, "Heading", "Rebuild the Momentum Engine", 50f, MenuUIBuilder.Green, new Vector2(0f, 410f), new Vector2(1000f, 64f));
        // Bottom: the numbered checklist under the blueprint, hints under the tray, the controls along the very bottom.
        TMP_Text steps = MenuUIBuilder.Text(card, "Steps", "", 25f, MenuUIBuilder.Ink, new Vector2(-230f, -385f), new Vector2(860f, 100f),
            TextAlignmentOptions.Left);
        TMP_Text hint = MenuUIBuilder.Text(card, "Hint", "", 21f, MenuUIBuilder.Ink, new Vector2(470f, -385f), new Vector2(420f, 110f));
        TMP_Text controls = MenuUIBuilder.Text(card, "Controls", "", 22f, InkSoft, new Vector2(0f, -448f), new Vector2(1380f, 34f));

        RectTransform board = MenuUIBuilder.Rect(card, "Board", Vector2.zero, new Vector2(1440f, 880f));

        // Blueprint: the empty engine at 4x on a paper card.
        Vector2 size = new Vector2(baseSprite.rect.width, baseSprite.rect.height) * Scale;
        MenuUIBuilder.Card(board, "Blueprint_Card", BlueprintCentre, size + new Vector2(60f, 50f));
        Image blueprint = Img(board, "Blueprint", baseSprite, BlueprintCentre, size, Color.white);
        Vector2 basePivot = BlueprintCentre + new Vector2(0f, -size.y * 0.5f);
        Vector2 hub = basePivot + EngineArtTool.FromBase(EngineArtTool.WheelCX, EngineArtTool.WheelCY) * Scale;
        Vector2 hook = basePivot + EngineArtTool.FromBase(EngineArtTool.WeightCX, EngineArtTool.WeightCY) * Scale;
        Vector2 wheelSize = new Vector2(wheel.rect.width, wheel.rect.height) * Scale, hubSize = new Vector2(112f, 112f), weightSize = new Vector2(120f, 120f);

        // Socket marks (faint outlines of what goes where).
        Image markWheel = Img(board, "Socket_Flywheel", wheel, hub, wheelSize, Mark);
        Image markBearing = Img(board, "Socket_Bearing", bearing, hub, hubSize, Mark);
        Image markWeight = Img(board, "Socket_Counterweight", weight, hook, weightSize, Mark);

        // Tray.
        RectTransform tray = MenuUIBuilder.Card(board, "Tray", new Vector2(470f, 20f), new Vector2(340f, 660f));
        MenuUIBuilder.Text(tray, "Heading", "Parts", 30f, MenuUIBuilder.Green, new Vector2(0f, 290f), new Vector2(300f, 40f));
        Vector2[] slots = { new Vector2(470f, 200f), new Vector2(470f, 10f), new Vector2(470f, -190f) };
        Vector2 traySize = new Vector2(150f, 150f);
        string[] trayLabels = { "Bearing", "Flywheel  (heavy: together)", "Counterweight  (heavy: together)" };
        for (int i = 0; i < 3; i++)
            MenuUIBuilder.Text(board, "Label_" + (i + 1), trayLabels[i], 20f, InkSoft, slots[i] + new Vector2(0f, -88f), new Vector2(320f, 26f));

        EngineAssemblyPanel.Part pBearing = new EngineAssemblyPanel.Part
        {
            name = "bearing", step = "Bearing  ->  onto the axle, in the centre of the ring  (one of you)", heavy = false, trayPosition = slots[0], socketPosition = hub,
            fittedSprite = bearing, fittedSize = hubSize, socketMark = markBearing,
            fittedHint = "Bearing in! Its steel balls roll instead of rubbing, so the flywheel will lose very little momentum to friction.",
        };
        EngineAssemblyPanel.Part pFlywheel = new EngineAssemblyPanel.Part
        {
            name = "flywheel", step = "Flywheel  ->  over the bearing  (heavy: grab it together)", heavy = true, trayPosition = slots[1], socketPosition = hub,
            fittedSprite = wheel, fittedSize = wheelSize, socketMark = markWheel, requires = 0,
            requiresHint = "The flywheel grinds on the bare axle: far too much friction. Fit the bearing first.",
            fittedHint = "Flywheel on! A heavy spinning wheel can store a lot of momentum.",
        };
        EngineAssemblyPanel.Part pWeight = new EngineAssemblyPanel.Part
        {
            name = "counterweight", step = "Counterweight  ->  on the hook, top right  (heavy: grab it together)", heavy = true, trayPosition = slots[2], socketPosition = hook,
            blueprintAfter = loaded, socketMark = markWeight,
            fittedHint = "Counterweight hung. Its weight, W = m·g = 3 kg × 29.43 N/kg = 88.3 N, will pull the lift.",
        };
        (EngineAssemblyPanel.Part part, Sprite sprite)[] all = { (pBearing, bearing), (pFlywheel, flywheel), (pWeight, weight) };
        foreach ((EngineAssemblyPanel.Part part, Sprite sprite) in all)
        {
            part.image = Img(board, "Part_" + char.ToUpper(part.name[0]) + part.name.Substring(1), sprite, part.trayPosition, traySize, Color.white);
            part.rect = part.image.rectTransform;
        }

        // Cursors.
        RectTransform c1 = Cursor(board, "Cursor_P1", ring, P1, "P1"), c2 = Cursor(board, "Cursor_P2", ring, P2, "P2");

        EngineAssemblyPanel panel = root.gameObject.AddComponent<EngineAssemblyPanel>();
        panel.EditorSetup(board, blueprint, new[] { pBearing, pFlywheel, pWeight }, new[] { c1, c2 }, hint, controls, steps, new Vector2(700f, 400f));
        root.gameObject.SetActive(false);
        return panel;
    }

    private static Image Img(RectTransform parent, string name, Sprite sprite, Vector2 pos, Vector2 size, Color color)
    {
        RectTransform rt = MenuUIBuilder.Rect(parent, name, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    private static RectTransform Cursor(RectTransform board, string name, Sprite ring, Color32 color, string label)
    {
        Image img = Img(board, name, ring, Vector2.zero, new Vector2(64f, 64f), color);
        TMP_Text t = MenuUIBuilder.Text(img.rectTransform, "Label", label, 24f, color, new Vector2(38f, -34f), new Vector2(60f, 30f));
        t.fontStyle = FontStyles.Bold;
        return img.rectTransform;
    }
}
