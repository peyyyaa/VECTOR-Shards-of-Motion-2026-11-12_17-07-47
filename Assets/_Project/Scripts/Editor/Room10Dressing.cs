using UnityEditor;
using UnityEngine;
using static Room9Dressing;

/// <summary>
/// Tools > Shards of Motion > Room 8 Part 3 > 2. Apply Visual Pass (after 1. Build Graybox (The Great Shard)). Re-runnable.
/// The finale's last part, "The Great Shard", in the Heart Engine hall's style (Room9Dressing): a rune chamber deep in the
/// roots. RoomDresser does the graybox automatically (stone floor, the ledges and pedestals as stone platforms, the two
/// lifts as planks on chains, shrine, exit light); this adds:
///   - the THORNS as purple brambles with a faint purple glow. The dresser turns every hazard into acid water (a prefab
///     with its own hazard trigger): that water and its green glow are removed here; the builder's Thorns hazard stays;
///   - faint cyan chevrons up each lift's shaft (they run once the engine has woken);
///   - a cyan rune glowing behind each pedestal pad: where each player stands;
///   - the GREAT SHARD bound in a rune ring (a dotted circle, four glyphs) with roots reaching down from the roof;
///   - two rune standing stones flanking the thorns, crystal flowers and ferns, moss under the pedestals, two lanterns,
///     a dark bush framing the bottom-right corner (the entry from part 2 is on the left).
/// Visual only: no collider, mass or puzzle value is touched; the GreatShard and ShardPedestal scripts are untouched.
/// Room-local coordinates (floor top y = -4.5), as Room10Builder.
/// </summary>
public static class Room10Dressing
{
    private const string Room10Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room10.prefab";
    private const float Floor = FinaleKit.Floor, Ceiling = FinaleKit.Ceiling;
    // As Room10Builder.
    private const float PedestalTop = Floor + 5.9f, ThornsLeft = -3f, ThornsRight = 3f, LiftX = 7f;
    private static readonly Vector2 ShardCentre = new Vector2(0f, Floor + 6.4f); // the shard's light / trigger centre

    [MenuItem("Tools/Shards of Motion/Room 8 Part 3/2. Apply Visual Pass (The Great Shard)")]
    public static void Apply()
    {
        if (!HeartEngineArt.Ensure()) { Debug.LogError("Room 8 part 3 visuals: Heart Engine art is incomplete (see above). Nothing was changed."); return; }
        bool ok = RoomDresser.Dress(Room10Path, ctx =>
        {
            Transform d = ctx.Decor;
            Color back = new Color(0.62f, 0.66f, 0.78f);
            Color rune = new Color(0.45f, 0.9f, 1f);

            // ---- The thorns: brambles, not the dresser's acid water (remove it and its glow; the Thorns hazard stays).
            foreach (Transform t in ctx.Room.GetComponentsInChildren<Transform>(true))
                if (t != null && t.name.StartsWith("Thorns ") && t.name.EndsWith(RoomDresser.VisualTag) || t != null && t.name.StartsWith("Thorns Glow"))
                    Object.DestroyImmediate(t.gameObject);
            SpriteRenderer bramble = Put(d, "Brambles", HeartEngineArt.Get("Bramble"), new Vector2(0f, Floor), "Props", 3, Color.white);
            if (bramble != null)
            {
                bramble.drawMode = SpriteDrawMode.Tiled;
                bramble.tileMode = SpriteTileMode.Continuous;
                bramble.size = new Vector2(ThornsRight - ThornsLeft, bramble.sprite.bounds.size.y);
            }
            ctx.Light("Thorn Glow", new Vector3(0f, Floor + 0.3f, 0f), new Color(0.75f, 0.35f, 0.85f), 0.6f, 3.5f);

            // ---- The lifts' shafts: faint chevrons up each (the lifts run once the engine has woken).
            Sprite chevron = HeartEngineArt.Get("Chevron");
            foreach (float x in new[] { -LiftX, LiftX })
                for (int i = 0; i < 2; i++)
                    Put(d, "Lift Up", chevron, new Vector2(x, Floor + 3.6f + i * 1.3f), "BackDecor", 3, new Color(1f, 1f, 1f, 0.4f));

            // ---- A rune behind each pedestal pad (the pads' own rune lights glow on them): stand here, one each.
            Put(d, "Pad Rune West", HeartEngineArt.Get("LF_Rune_C"), new Vector2(-4.5f, PedestalTop + 0.75f), "BackDecor", 6, new Color(1f, 0.85f, 0.65f, 0.85f));
            Put(d, "Pad Rune East", HeartEngineArt.Get("LF_Rune_D"), new Vector2(4.5f, PedestalTop + 0.75f), "BackDecor", 6, new Color(0.75f, 1f, 1f, 0.85f));
            ctx.Hanging("TX Village Props - Grass 08", -5.0f, PedestalTop - 0.3f);
            ctx.Hanging("TX Village Props - Grass 07", 4.0f, PedestalTop - 0.3f);

            // ---- The Great Shard, bound: a dotted rune ring with four glyphs, roots reaching down from the roof toward it.
            LineRenderer ring = FinaleKit.Line(d, "Shard Ring", new Color(rune.r, rune.g, rune.b, 0.55f), 4f / 32f, "BackDecor", 8);
            Material dots = HeartEngineArt.Mat("Line_Dots");
            if (dots != null) { ring.sharedMaterial = dots; ring.textureMode = LineTextureMode.Tile; }
            ring.useWorldSpace = false;
            ring.loop = true;
            const int points = 40;
            const float radius = 1.7f;
            ring.positionCount = points;
            for (int i = 0; i < points; i++)
            {
                float a = i * Mathf.PI * 2f / points;
                ring.SetPosition(i, (Vector3)(ShardCentre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius));
            }
            string[] glyphs = { "LF_Rune_A", "LF_Rune_B", "LF_Rune_C", "LF_Rune_D" };
            for (int i = 0; i < 4; i++)
            {
                float a = Mathf.PI * 0.25f + i * Mathf.PI * 0.5f;
                Put(d, "Ring Glyph", HeartEngineArt.Get(glyphs[i]), ShardCentre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, "BackDecor", 9, new Color(1f, 1f, 1f, 0.8f));
            }
            Sprite beam = HeartEngineArt.Get("Beam");
            Color root = new Color(0.42f, 0.3f, 0.2f), rootDark = new Color(0.3f, 0.21f, 0.14f);
            Along(d, "Root", beam, new Vector2(-1.6f, Ceiling), new Vector2(-0.6f, ShardCentre.y + 1.9f), 7f / 32f, true, "BackDecor", 7, root);
            Along(d, "Root", beam, new Vector2(1.9f, Ceiling), new Vector2(0.55f, ShardCentre.y + 1.8f), 6f / 32f, true, "BackDecor", 7, rootDark);
            Along(d, "Root", beam, new Vector2(0.2f, Ceiling), new Vector2(0.05f, ShardCentre.y + 2.1f), 5f / 32f, true, "BackDecor", 6, root);

            // ---- Rune standing stones flanking the thorns (behind them), with a faint cyan glow each.
            foreach (float x in new[] { -3.9f, 3.9f })
            {
                if (Put(d, "Rune Stone", HeartEngineArt.Get("LF_Stone_Tall"), new Vector2(x, Floor), "BackDecor", 5, new Color(0.7f, 0.74f, 0.85f)) == null) continue;
                Vector2 at = new Vector2(x - 0.05f, Floor + 1.35f);
                Put(d, "Rune Glyph", HeartEngineArt.Get(x < 0f ? "LF_Rune_B" : "LF_Rune_A"), at, "BackDecor", 6, Color.white);
                ctx.Light("Rune Glow", at, rune, 0.5f, 1.8f);
            }

            // ---- Plants (behind the play space, clear of the ledges, lifts and thorns), lanterns, a foreground bush.
            Put(d, "Crystal Flower", HeartEngineArt.Get("LF_Crystal_Flower"), new Vector2(-6.2f, Floor), "BackDecor", 7, Color.white);
            Put(d, "Crystal Flower", HeartEngineArt.Get("LF_Crystal_Flower"), new Vector2(6.1f, Floor), "BackDecor", 7, Color.white);
            ctx.Light("Crystal Glow", new Vector3(-6.2f, Floor + 1.1f, 0f), new Color(0.6f, 0.95f, 1f), 0.4f, 1.5f);
            ctx.Light("Crystal Glow", new Vector3(6.1f, Floor + 1.1f, 0f), new Color(0.6f, 0.95f, 1f), 0.4f, 1.5f);
            Put(d, "Fern", HeartEngineArt.Get("LF_Fern"), new Vector2(-9.5f, Floor), "BackDecor", 7, back);
            Put(d, "Blue Plant", HeartEngineArt.Get("LF_Blue_Plant"), new Vector2(8.2f, Floor), "BackDecor", 7, Color.white);
            Put(d, "Moss Rock", HeartEngineArt.Get("LF_Rock_Moss_B"), new Vector2(-8.9f, Floor), "BackDecor", 6, back);
            Lantern(ctx, -6.0f, Ceiling, 1.0f, true);
            Lantern(ctx, 6.0f, Ceiling, 1.0f, true);
            Put(d, "Foreground Bush", HeartEngineArt.Get("LF_Bush_Dark"), new Vector2(9.7f, Floor - 1.1f), "Foreground", 5, new Color(0.55f, 0.6f, 0.62f));
        });
        if (ok) Debug.Log("Room 8 part 3: visual pass applied to " + Room10Path + ".");
    }
}
