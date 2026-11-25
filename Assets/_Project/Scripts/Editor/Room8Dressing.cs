using UnityEditor;
using UnityEngine;
using static Room9Dressing;

/// <summary>
/// Tools > Shards of Motion > Room 8 > 2. Apply Visual Pass (after 1. Build Graybox (Part 1: The Descent)). Re-runnable.
/// The finale's first part, "The Descent", in the Heart Engine hall's style (Room9Dressing): an old mine loading yard
/// grown over by the forest. RoomDresser does the graybox automatically (stone floors, the chock plate's slab, the gates'
/// bars, shrine, Wren's journal stone, exit light); this adds:
///   - the LOADING DECK as planks on two end posts (the chock plate under it stays open to see), a fence along its back,
///     a lantern lighting the plate; the climb-up step and the slab over the cart chute as planks;
///   - the ORE CHUTE as a plank chute from the roof to its mouth over the cart, its handle drawn as a lever;
///   - the RAMP as a plank track on a trestle (as in part 2);
///   - the ROOT WALL as roots: the dresser's rubble tinted earth-brown, root strands and hanging moss over it. They are
///     the wall's children, so they vanish with it when it breaks (BreakableWall hides all its child sprites);
///   - the CART CHUTE as an iron grate in the track with a cyan glow from below and a chevron pointing down into it;
///   - Legacy Fantasy dressing under the ramp (mossy rocks, fern, mushroom), a fern by the entry, two hall lanterns,
///     a dark bush framing the bottom-right corner.
/// Visual only: no collider, mass or puzzle value is touched. Room-local coordinates (floor top y = -4.5), as Room8Builder.
/// </summary>
public static class Room8Dressing
{
    private const string Room8Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room08.prefab";
    private const string LeverSprite = "Assets/_Project/Art/Sprites/Props/SP_Lever.png";
    private const float Floor = FinaleKit.Floor, Ceiling = FinaleKit.Ceiling;
    // As Room8Builder.
    private const float DeckTop = Floor + 3f, DeckLeft = -8f, DeckRight = -3f, RampFoot = 4f;
    private const float WallLeft = 4.6f, WallRight = 5.2f, ChuteLeft = 5.4f, ChuteRight = 6.4f;
    private static readonly Vector2 OreHandle = new Vector2(-7.4f, DeckTop + 0.8f), OreMouth = new Vector2(-6f, Floor + 5f);

    [MenuItem("Tools/Shards of Motion/Room 8/2. Apply Visual Pass (Part 1: The Descent)")]
    public static void Apply()
    {
        if (!HeartEngineArt.Ensure()) { Debug.LogError("Room 8 part 1 visuals: Heart Engine art is incomplete (see above). Nothing was changed."); return; }
        Sprite leverSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LeverSprite);
        bool ok = RoomDresser.Dress(Room8Path, ctx =>
        {
            Transform d = ctx.Decor;
            Sprite plank = HeartEngineArt.Get("Pipe"), beam = HeartEngineArt.Get("Beam");
            Color back = new Color(0.62f, 0.66f, 0.78f), backWood = new Color(0.78f, 0.78f, 0.84f);

            // ---- Graybox pieces this pass draws itself.
            Hide(ctx.Room, "Ramp_Top");
            Hide(ctx.Room, "Ore_Chute_Pipe");
            Hide(ctx.Room, "Ore_Chute");

            // ---- The loading deck: planks, two end posts (the plate under it stays in view), a fence, a lantern over the plate.
            Along(d, "Deck", plank, new Vector2(DeckLeft, DeckTop), new Vector2(DeckRight, DeckTop), 12f / 32f, false, "Midground", 4, Color.white);
            foreach (float x in new[] { DeckLeft + 0.15f, DeckRight - 0.15f })
                Along(d, "Deck Post", beam, new Vector2(x, Floor), new Vector2(x, DeckTop - 0.35f), 8f / 32f, true, "BackDecor", 10, backWood);
            for (int i = 0; i < 4; i++) ctx.Prop(ctx.Village, "TX Village Props - Fence 01 Pillar 01", DeckLeft + 0.15f + i * 1.25f, DeckTop, "BackDecor", 13);
            for (int i = 0; i < 3; i++)
            {
                SpriteRenderer rail = ctx.Prop(ctx.Village, "TX Village Props - Fence 01 Rail 01", DeckLeft + 0.15f + 0.625f + i * 1.25f, DeckTop + 0.55f, "BackDecor", 12);
                if (rail != null) rail.color = new Color(0.9f, 0.9f, 0.9f);
            }
            Lantern(ctx, -5.4f, DeckTop - 0.35f, 0.3f, true);
            ctx.Hanging("TX Village Props - Grass 08", -6.6f, DeckTop - 0.35f);
            ctx.Hanging("TX Village Props - Grass 07", -4.3f, DeckTop - 0.35f);
            Along(d, "Step", plank, new Vector2(-8.9f, Floor + 1.8f), new Vector2(-8.1f, Floor + 1.8f), 8f / 32f, false, "Midground", 4, Color.white);

            // ---- The ore chute: a plank chute from the roof to its mouth over the cart; the handle on the deck as a lever.
            Along(d, "Ore Chute", plank, new Vector2(OreMouth.x, Ceiling), new Vector2(OreMouth.x, OreMouth.y), 12f / 32f, false, "BackDecor", 20, Color.white);
            if (leverSprite != null)
                Put(d, "Ore Lever", leverSprite, new Vector2(OreHandle.x, DeckTop + leverSprite.bounds.extents.y - leverSprite.bounds.center.y), "Interactables", 0, Color.white);

            // ---- The ramp: a plank track on a trestle (its surface is the line from the deck's edge to the ramp's foot).
            Vector2 rampTop = new Vector2(DeckRight, DeckTop), rampFoot = new Vector2(RampFoot, Floor);
            Along(d, "Ramp Track", plank, rampTop, rampFoot, 12f / 32f, false, "Midground", 3, Color.white);
            foreach (float x in new[] { -2.3f, -0.7f, 0.9f, 2.5f })
            {
                float y = Floor + (DeckTop - Floor) * (RampFoot - x) / (RampFoot - DeckRight); // the ramp's surface at x
                Along(d, "Trestle Post", beam, new Vector2(x, Floor), new Vector2(x, y - 0.3f), 8f / 32f, true, "BackDecor", 8, backWood);
            }
            Along(d, "Trestle Brace", beam, new Vector2(-2.3f, Floor + 0.4f), new Vector2(0.9f, Floor + 1.4f), 6f / 32f, true, "BackDecor", 7, back);

            // ---- The root wall: earth-brown rubble, root strands and moss, all children of the wall (gone when it breaks).
            Transform wall = RoomBuildKit.Find(ctx.Room, "Wall_Roots");
            if (wall != null)
            {
                foreach (SpriteRenderer sr in wall.GetComponentsInChildren<SpriteRenderer>(true))
                    if (sr.enabled) sr.color = new Color(0.5f, 0.38f, 0.28f);
                Transform roots = new GameObject("Roots" + RoomDresser.VisualTag).transform;
                roots.SetParent(wall, false);
                Vector3 s = wall.lossyScale;
                roots.localScale = new Vector3(1f / s.x, 1f / s.y, 1f);
                Color root = new Color(0.42f, 0.3f, 0.2f), rootDark = new Color(0.3f, 0.21f, 0.14f);
                Along(roots, "Root", beam, new Vector2(WallLeft + 0.12f, Ceiling), new Vector2(WallLeft + 0.25f, Floor), 7f / 32f, true, "Props", 5, root);
                Along(roots, "Root", beam, new Vector2(WallRight - 0.1f, Ceiling), new Vector2(WallLeft + 0.35f, Floor + 2.2f), 6f / 32f, true, "Props", 6, rootDark);
                Along(roots, "Root", beam, new Vector2(WallLeft + 0.3f, Floor + 3.4f), new Vector2(WallRight - 0.08f, Floor), 6f / 32f, true, "Props", 6, root);
                Along(roots, "Root", beam, new Vector2(WallLeft + 0.05f, Floor + 6.5f), new Vector2(WallRight - 0.15f, Floor + 4.2f), 5f / 32f, true, "Props", 7, rootDark);
                if (ctx.Forest.TryGetValue("TX Village Props - Grass 08", out Sprite moss))
                    foreach (float y in new[] { Floor + 7.2f, Floor + 4.6f, Floor + 2.4f })
                    {
                        SpriteRenderer m = Put(roots, "Moss", moss, new Vector2((WallLeft + WallRight) * 0.5f, y), "Props", 8, new Color(0.85f, 0.9f, 0.8f));
                        if (m != null) m.flipY = true;
                    }
            }
            else Debug.LogWarning("Room 8 part 1 visuals: no Wall_Roots (run 1. Build Graybox first).");

            // ---- The cart chute: an iron grate in the track, a cyan glow from below, a chevron pointing down into it; the slab
            // over it (players' way up to the landing) as planks.
            Along(d, "Chute Grate", HeartEngineArt.Get("Grate"), new Vector2(ChuteLeft, Floor), new Vector2(ChuteRight, Floor), 8f / 32f, false, "Props", 0, Color.white);
            ctx.Light("Chute Glow", new Vector3((ChuteLeft + ChuteRight) * 0.5f, Floor + 0.2f, 0f), new Color(0.45f, 0.9f, 1f), 0.8f, 1.8f);
            SpriteRenderer down = Put(d, "Chute Arrow", HeartEngineArt.Get("Chevron"), new Vector2((ChuteLeft + ChuteRight) * 0.5f, Floor + 1.15f), "Props", 1, new Color(1f, 1f, 1f, 0.8f));
            if (down != null) down.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            Along(d, "Chute Slab", plank, new Vector2(5.4f, Floor + 1.9f), new Vector2(6.5f, Floor + 1.9f), 8f / 32f, false, "Midground", 4, Color.white);

            // ---- Hall lanterns (lit: this part has no engine yet).
            Lantern(ctx, -1.0f, Ceiling, 1.2f, true);
            Lantern(ctx, 3.0f, Ceiling, 1.2f, true);

            // ---- Under the ramp (behind the trestle): mossy rocks, a fern, a mushroom. By the entry: a fern.
            Put(d, "Moss Rock", HeartEngineArt.Get("LF_Rock_Moss_A"), new Vector2(0.5f, Floor), "BackDecor", 6, back);
            Put(d, "Moss Rock Small", HeartEngineArt.Get("LF_Rock_Moss_Small"), new Vector2(2.0f, Floor), "BackDecor", 6, back);
            Put(d, "Fern", HeartEngineArt.Get("LF_Fern"), new Vector2(-0.6f, Floor), "BackDecor", 7, back);
            Put(d, "Mushroom", HeartEngineArt.Get("LF_Mushroom"), new Vector2(-1.6f, Floor), "BackDecor", 7, back);
            Put(d, "Entry Fern", HeartEngineArt.Get("LF_Fern"), new Vector2(-9.55f, Floor), "BackDecor", 7, Color.white);

            // ---- Foreground: a dark bush framing the bottom-right corner (under the landing, clear of the cart chute).
            Put(d, "Foreground Bush", HeartEngineArt.Get("LF_Bush_Dark"), new Vector2(9.7f, Floor - 1.1f), "Foreground", 5, new Color(0.55f, 0.6f, 0.62f));
        }, "Deck", "Step", "Slab_Chute");
        if (ok) Debug.Log("Room 8 part 1: visual pass applied to " + Room8Path + ".");
    }
}
