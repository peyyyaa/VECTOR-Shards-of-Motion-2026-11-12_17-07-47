using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the Village Fund ledger (FundPanel) into UI_Canvas, in the journal's parchment style: Bram's line, the shards
/// to spend, one row per project in VillageFund.All (name, description, Fund button), a status line and Back.
/// Called by UIPrefabBuilder (Create UI Prefabs). Names and prices live in VillageFund, not here.
/// </summary>
public static class FundUIBuilder
{
    private static readonly Color32 InkSoft = new Color32(107, 74, 46, 255);

    public static FundPanel Build(Transform canvas)
    {
        RectTransform root = MenuUIBuilder.Fullscreen(canvas, "FundPanel", new Color(0f, 0f, 0f, 0.6f));
        RectTransform card = MenuUIBuilder.Card(root, "Card", Vector2.zero, new Vector2(1100f, 880f), "UI_Page");
        MenuUIBuilder.Text(card, "Heading", "Village Fund", 56f, MenuUIBuilder.Green, new Vector2(0f, 375f), new Vector2(700f, 70f));
        TMP_Text speech = MenuUIBuilder.Text(card, "Speech", "", 28f, MenuUIBuilder.Ink, new Vector2(0f, 300f), new Vector2(960f, 70f));
        TMP_Text balance = MenuUIBuilder.Text(card, "Balance", "", 34f, MenuUIBuilder.Ink, new Vector2(0f, 235f), new Vector2(960f, 44f));

        int count = VillageFund.All.Length;
        FundRow[] rows = new FundRow[count];
        for (int i = 0; i < count; i++)
        {
            float y = 140f - i * 110f;
            RectTransform row = MenuUIBuilder.Card(card, "Project " + (i + 1), new Vector2(0f, y), new Vector2(980f, 96f));
            row.GetComponent<Image>().raycastTarget = true; // hovering the row selects its button
            TMP_Text title = MenuUIBuilder.Text(row, "Name", VillageFund.All[i].name, 32f, MenuUIBuilder.Ink, new Vector2(-150f, 17f), new Vector2(620f, 40f),
                TextAlignmentOptions.Left);
            TMP_Text description = MenuUIBuilder.Text(row, "Description", VillageFund.All[i].description, 24f, InkSoft, new Vector2(-150f, -20f),
                new Vector2(620f, 34f), TextAlignmentOptions.Left);
            Button fund = MenuUIBuilder.Button(row, "Fund", "Fund", new Vector2(330f, 0f), 260f, out TMP_Text fundLabel);
            fundLabel.fontSize = 28f;
            ColorBlock c = fund.colors;
            c.disabledColor = new Color(0.75f, 0.75f, 0.72f, 0.6f); // funded: greyed out
            fund.colors = c;
            FundRow r = row.gameObject.AddComponent<FundRow>();
            r.EditorSetup(title, description, fund, fundLabel);
            rows[i] = r;
        }

        TMP_Text status = MenuUIBuilder.Text(card, "Status", "", 28f, MenuUIBuilder.Ink, new Vector2(0f, -300f), new Vector2(980f, 40f));
        Button back = MenuUIBuilder.Button(card, "Back", "Back", new Vector2(0f, -370f), 260f);

        FundPanel panel = root.gameObject.AddComponent<FundPanel>();
        panel.EditorSetup(speech, balance, status, rows, back);
        root.gameObject.SetActive(false);
        return panel;
    }
}
