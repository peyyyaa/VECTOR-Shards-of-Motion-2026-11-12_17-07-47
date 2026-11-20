using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The trailhead at the east end of Forge &amp; East Road: where the road leaves Alderbrook for the regions.
/// West to east (room-local x; the lamp is at 0.5, just past the forge):
///   1.8  trail-map board: the parchment map pinned on a pack Billboard; Interact opens the map screen (WallMap);
///   3.4  signpost with the two closed roads (GRAVITY DIVIDE, MECH. DEPTHS, each with a red CLOSED strip);
///   5.6 - 8.0  the gateway: two posts, a crossbeam, green banners (the pack banner recoloured) and the hanging
///        SHATTERED GROUNDS sign; under it two rune stones (6.3, 7.3) are the RegionGate trigger, lit one per player.
///        It stays clear of the bushes that hide the screen's right edge wall (from about x 8.25).
/// Move things with the constants below.
/// The small pieces are drawn by TrailheadPainter into Art/Village/Trailhead (TrailheadArt, re-drawn on every build). No floating text.
/// </summary>
public static partial class VillageBuilder
{
    private static readonly Color RuneGlow = new Color(0.45f, 0.9f, 1f);
    private const float MapBoardX = 1.8f, SignPostX = 3.4f, GateLeftX = 5.6f, GateRightX = 8.0f;

    private static void BuildTrailhead(Transform exit)
    {
        Dictionary<string, Sprite> art = TrailheadArt.Build();
        float px = 1f / PPU;

        // 1. Trail-map board.
        SpriteRenderer board = PackProp(exit, "Trail_Map", "Billboard", MapBoardX, Floor);
        board.gameObject.layer = LayerMask.NameToLayer("Interactable");
        board.gameObject.AddComponent<BoxCollider2D>().isTrigger = true; // sized to the board (added before the notice)
        board.gameObject.AddComponent<WallMap>();
        Trail(board.transform, "Map_Notice", art, "Map_Notice", MapBoardX, board.bounds.max.y - 29f * px, 1);

        // 2. The closed roads: a signpost with two old planks pointing east.
        Trail(exit, "SignPost", art, "SignPost", SignPostX, Floor, 0);
        Plank(exit, art, "Plank_Region2", SignPostX, Floor + 75f * px);
        Plank(exit, art, "Plank_Region3", SignPostX, Floor + 50f * px);

        // 3. The gateway.
        const float left = GateLeftX, right = GateRightX, mid = (GateLeftX + GateRightX) * 0.5f;
        float beamBottom = Floor + 82f * px; // the beam sits over the post tops
        Trail(exit, "Post_Left", art, "Post", left, Floor, 0);
        Trail(exit, "Post_Right", art, "Post", right, Floor, 0);
        Trail(exit, "Beam", art, "Beam", mid, beamBottom, 1);
        foreach (float x in new[] { left, right })
        {
            Sprite banner = art.TryGetValue("Banner_Green", out Sprite b) ? b : null;
            float h = banner != null ? banner.rect.height * px : 0f;
            Trail(exit, "Banner", art, "Banner_Green", x, beamBottom - h, 2);
        }
        Sprite sign = art.TryGetValue("Sign_Region1", out Sprite sg) ? sg : null;
        Trail(exit, "Sign_Region1", art, "Sign_Region1", mid, beamBottom - (sign != null ? sign.rect.height * px : 0f), 2);

        // 4. The rune stones and the gate trigger over them.
        GameObject gate = new GameObject("Gate_Region1");
        gate.transform.SetParent(exit, false);
        gate.transform.position = new Vector3(mid, Floor + 1.5f, 0f);
        BoxCollider2D trigger = gate.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(right - left - 0.4f, 3f); // both stones, between the posts
        float[] stoneX = { mid - 0.5f, mid + 0.5f };
        SpriteRenderer[] stones = new SpriteRenderer[stoneX.Length];
        Light2D[] glows = new Light2D[stoneX.Length];
        for (int i = 0; i < stoneX.Length; i++)
        {
            stones[i] = Trail(gate.transform, "Rune_Stone_" + (i + 1), art, "Stone_Dark", stoneX[i], Floor - 2f * px, 1);
            glows[i] = PointLight(stones[i].transform, "Glow", new Vector3(stoneX[i], Floor + 0.3f, 0f), RuneGlow, 0f, 1.6f);
        }
        RegionGate regionGate = gate.AddComponent<RegionGate>();
        regionGate.EditorSetup("Region1", "the Shattered Grounds");
        regionGate.EditorSetupStones(stones, glows, art.TryGetValue("Stone_Dark", out Sprite dark) ? dark : null,
            art.TryGetValue("Stone_Lit", out Sprite lit) ? lit : null);
        PointLight(exit, "Gate_Light", new Vector3(mid, Floor + 1.8f, 0f), RuneGlow, 0.5f, 3f);
    }

    // A trailhead sprite (bottom-centre pivot) standing at (x, bottomY) on the Props layer.
    private static SpriteRenderer Trail(Transform parent, string name, Dictionary<string, Sprite> art, string sprite, float x, float bottomY, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, bottomY, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (art.TryGetValue(sprite, out Sprite s)) sr.sprite = s;
        else Debug.LogWarning("Village: missing trailhead art " + sprite);
        sr.sortingLayerName = "Props";
        sr.sortingOrder = order;
        return sr;
    }

    // A plank nailed to the signpost: its left end 2 px inside the post, pointing east.
    private static void Plank(Transform parent, Dictionary<string, Sprite> art, string sprite, float postX, float bottomY)
    {
        float w = art.TryGetValue(sprite, out Sprite s) ? s.rect.width / PPU : 0f;
        Trail(parent, sprite, art, sprite, postX - 2f / PPU + w * 0.5f, bottomY, 2);
    }
}
