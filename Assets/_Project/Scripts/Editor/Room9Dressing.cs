using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static Room9Builder;

/// <summary>
/// Tools > Shards of Motion > Room 8 Part 2 > 2. Apply Visual Pass (after 1. Build Graybox). Re-runnable.
/// RoomDresser does the graybox automatically (stone floors, the alcove lift, the exit gate's bars, shrine, journal);
/// this adds the Heart Engine hall's look: an old machine hall grown over by the forest.
///   - P1's BALCONY: a plank deck with a fence along its back, the roof beam at its east end (a banner on it), a knee
///     strut down to the landing, a lantern underneath; the ORE CHUTE runs from the hopper down to the mouth over the cart.
///   - the RAMP and the LOW RAIL as plank tracks; the ramp on a wooden trestle (the cart's way back is plain to see).
///   - the CHAIN LIFT's shaft: a walkable grate over it, the right post (above head height), a crossbeam over the two sprockets, the hammer's hanger beam.
///   - the hall's lamps as hanging lanterns (their lights are the engine's 30 kg·m/s stage);
///   - Legacy Fantasy dressing under the ramp and on the east floor: a rune stone (cyan glow), mossy rocks, fern,
///     crystal flowers, a mushroom; a dark bush framing the bottom-left corner. The east floor stays clear for P2.
/// Visual only: no collider, mass or puzzle value is touched (the balcony, its post and the striker paddle are left to
/// this pass: their graybox is hidden, colliders kept). Room-local coordinates (floor top y = -4.5), as Room9Builder.
/// </summary>
public static class Room9Dressing
{
    private const string Room9Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room09.prefab";
    private const float Floor = FinaleKit.Floor, Ceiling = FinaleKit.Ceiling;
    private static readonly float[] HallLampX = { -8f, -2.5f, 3.5f, 8.5f }; // as Room9Builder's Hall_Lamps
    private const float HallLampY = Floor + 8.6f;

    [MenuItem("Tools/Shards of Motion/Room 8 Part 2/2. Apply Visual Pass")]
    public static void Apply()
    {
        if (!HeartEngineArt.Ensure()) { Debug.LogError("Room 8 part 2 visuals: Heart Engine art is incomplete (see above). Nothing was changed."); return; }
        bool ok = RoomDresser.Dress(Room9Path, ctx =>
        {
            Transform d = ctx.Decor;
            Sprite plank = HeartEngineArt.Get("Pipe"), beam = HeartEngineArt.Get("Beam");
            Color back = new Color(0.62f, 0.66f, 0.78f), backWood = new Color(0.78f, 0.78f, 0.84f);

            // ---- Graybox pieces this pass draws itself.
            Hide(ctx.Room, "Ramp_Top");
            Hide(ctx.Room, "Rail_Low_Top");
            Hide(ctx.Room, "Ore_Chute_Pipe");

            // ---- The ramp: a plank track on a trestle; the low rail: a plank rail held out from the shaft by a knee brace.
            Vector2 rampTop = new Vector2(-6.5f, Floor + Landing), rampFoot = new Vector2(RampFoot, Floor);
            Along(d, "Ramp Track", plank, rampTop, rampFoot, 12f / 32f, false, "Midground", 3, Color.white);
            foreach (float x in new[] { -6.1f, -4.7f, -3.3f, -1.9f })
            {
                float y = Floor + Landing * (RampFoot - x) / (RampFoot + 6.5f); // the ramp's surface at x
                Along(d, "Trestle Post", beam, new Vector2(x, Floor), new Vector2(x, y - 0.3f), 8f / 32f, true, "BackDecor", 8, backWood);
            }
            Along(d, "Trestle Brace", beam, new Vector2(-6.1f, Floor + 0.4f), new Vector2(-3.3f, Floor + 1.4f), 6f / 32f, true, "BackDecor", 7, back);
            Along(d, "Low Rail", plank, new Vector2(RailLeft, LowStop - 0.2f), new Vector2(ShaftLeft, LowStop), 6f / 32f, false, "Midground", 3, Color.white);
            // (no brace under the rail: at head height over the track it read as a wall across the way east)

            // ---- P1's balcony: deck, fence, the roof beam (and its banner), a knee strut, a lantern underneath.
            Along(d, "Balcony Deck", plank, new Vector2(BalconyLeft, BalconyTop), new Vector2(PostRight, BalconyTop), 12f / 32f, false, "Midground", 4, Color.white);
            Along(d, "Balcony Post", beam, new Vector2((PostLeft + PostRight) * 0.5f, BalconyTop - 0.3f), new Vector2((PostLeft + PostRight) * 0.5f, Ceiling), 8f / 32f, true, "Midground", 2, Color.white);
            Along(d, "Balcony Strut", beam, new Vector2(-6.4f, Floor + Landing), new Vector2(BalconyLeft + 0.3f, BalconyTop - 0.3f), 8f / 32f, true, "BackDecor", 11, backWood);
            for (int i = 0; i < 3; i++) ctx.Prop(ctx.Village, "TX Village Props - Fence 01 Pillar 01", -3.65f + i * 1.25f, BalconyTop, "BackDecor", 13);
            for (int i = 0; i < 2; i++)
            {
                SpriteRenderer rail = ctx.Prop(ctx.Village, "TX Village Props - Fence 01 Rail 01", -3.65f + 0.625f + i * 1.25f, BalconyTop + 0.55f, "BackDecor", 12);
                if (rail != null) rail.color = new Color(0.9f, 0.9f, 0.9f);
            }
            SpriteRenderer banner = ctx.Prop(ctx.Village, "TX Village Props - Banner", (PostLeft + PostRight) * 0.5f, Floor + 6.1f, "Midground", 3);
            if (banner != null) banner.color = new Color(0.85f, 0.85f, 0.9f);
            Lantern(ctx, -2.0f, BalconyTop - 0.3f, 0.35f, true);
            ctx.Hanging("TX Village Props - Grass 08", -3.3f, BalconyTop - 0.3f);
            ctx.Hanging("TX Village Props - Grass 07", -1.4f, BalconyTop - 0.3f);
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 03", -3.4f, BalconyTop, "Props", 2);

            // ---- The ore chute: from the hopper's side, down over the shaft, to its mouth above the cart's stop.
            Along(d, "Ore Chute", plank, new Vector2(HopperX + 0.45f, BalconyTop + 0.35f), new Vector2(ChuteMouthX + 0.2f, ChuteMouthY + 0.15f), 12f / 32f, false, "BackDecor", 20, Color.white);
            Along(d, "Ore Chute Spout", plank, new Vector2(ChuteMouthX, ChuteMouthY + 0.3f), new Vector2(ChuteMouthX, ChuteMouthY - 0.1f), 12f / 32f, false, "BackDecor", 21, Color.white);

            // ---- The chain lift's shaft: right post (above the striker), a crossbeam over the sprockets, the hammer's hanger.
            // The post starts above head height (over the cart at its stop), so the hop over the striker looks as open as it is.
            Along(d, "Shaft Post", beam, new Vector2(ShaftRight + 0.13f, LowStop + 1.2f), new Vector2(ShaftRight + 0.13f, ChainTop + 0.55f), 8f / 32f, true, "BackDecor", 10, Color.white);
            // The grate over the shaft (its collider: Shaft_Cover), drawn behind the lift decks so a parked deck shows on top.
            Along(d, "Shaft Grate", HeartEngineArt.Get("Grate"), new Vector2(ShaftLeft, Floor - 0.02f), new Vector2(ShaftRight, Floor - 0.02f), 8f / 32f, false, "Midground", 1, Color.white);
            Along(d, "Shaft Crossbeam", beam, new Vector2(PostRight, ChainTop + 0.6f), new Vector2(ReturnX + 0.6f, ChainTop + 0.6f), 8f / 32f, true, "BackDecor", 10, Color.white);
            Along(d, "Hammer Hanger", beam, new Vector2(HammerX, PivotY), new Vector2(HammerX, ChainTop + 0.6f), 8f / 32f, true, "BackDecor", 10, Color.white);

            // ---- The hall's lamps: lanterns hung from the roof (their lights come on with the engine's first stage).
            var litLooks = new System.Collections.Generic.List<GameObject>();
            foreach (float x in HallLampX)
            {
                GameObject on = Lantern(ctx, x, Ceiling, Ceiling - HallLampY - 0.3f, false);
                if (on != null) litLooks.Add(on);
            }
            Transform lampsStage = RoomBuildKit.Find(ctx.Room, "Stage_Lamps");
            if (lampsStage != null && lampsStage.TryGetComponent(out EngineStage stage))
            {
                SerializedObject so = new SerializedObject(stage);
                SerializedProperty list = so.FindProperty("showOnReach");
                list.arraySize = litLooks.Count;
                for (int i = 0; i < litLooks.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = litLooks[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("Room 8 part 2 visuals: no Stage_Lamps (run 1. Build Graybox first): the lanterns won't light.");

            // ---- The chain lift's direction: faint cyan chevrons, up the shaft and down the return behind it.
            Sprite chevron = HeartEngineArt.Get("Chevron");
            for (int i = 0; i < 3; i++)
            {
                SpriteRenderer up = Put(d, "Lift Up", chevron, new Vector2(ShaftX, Floor + 1.6f + i * 1.5f), "BackDecor", 3, new Color(1f, 1f, 1f, 0.55f));
                SpriteRenderer down = Put(d, "Lift Down", chevron, new Vector2(ReturnX, Floor + 4.6f - i * 1.5f), "BackDecor", 3, new Color(1f, 1f, 1f, 0.45f));
                if (down != null) down.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            }

            // ---- Under the ramp: a rune stone glowing faintly cyan, mossy rocks, a fern, a mushroom (all behind the trestle).
            SpriteRenderer stone = Put(d, "Rune Stone", HeartEngineArt.Get("LF_Stone_Tall"), new Vector2(-5.3f, Floor), "BackDecor", 5, new Color(0.7f, 0.74f, 0.85f));
            if (stone != null)
            {
                Vector2 runeAt = new Vector2(-5.35f, Floor + 1.35f);
                Put(d, "Rune Glyph", HeartEngineArt.Get("LF_Rune_A"), runeAt, "BackDecor", 6, Color.white);
                ctx.Light("Rune Glow", runeAt, new Color(0.45f, 0.9f, 1f), 0.5f, 1.8f);
            }
            Put(d, "Moss Rock", HeartEngineArt.Get("LF_Rock_Moss_A"), new Vector2(-2.9f, Floor), "BackDecor", 6, back);
            Put(d, "Moss Rock Small", HeartEngineArt.Get("LF_Rock_Moss_Small"), new Vector2(-1.95f, Floor), "BackDecor", 6, back);
            Put(d, "Fern", HeartEngineArt.Get("LF_Fern"), new Vector2(-4.2f, Floor), "BackDecor", 7, back);
            Put(d, "Mushroom", HeartEngineArt.Get("LF_Mushroom"), new Vector2(-6.05f, Floor), "BackDecor", 7, back);

            // ---- East floor: crystal plants by the engine's foot, left clear where P2 works the winch (x 5..9).
            Put(d, "Blue Plant", HeartEngineArt.Get("LF_Blue_Plant"), new Vector2(3.0f, Floor), "BackDecor", 7, Color.white);
            Put(d, "Crystal Flower", HeartEngineArt.Get("LF_Crystal_Flower"), new Vector2(4.1f, Floor), "BackDecor", 7, Color.white);
            ctx.Light("Crystal Glow", new Vector3(4.1f, Floor + 1.1f, 0f), new Color(0.6f, 0.95f, 1f), 0.45f, 1.5f);

            // ---- Foreground: a dark bush framing the bottom-left corner (in front of everything, over no play space).
            Put(d, "Foreground Bush", HeartEngineArt.Get("LF_Bush_Dark"), new Vector2(-9.1f, Floor - 1f), "Foreground", 5, new Color(0.55f, 0.6f, 0.62f));
        }, "Balcony", "Balcony_Post", "Paddle", "Shaft_Cover", "Cart_Stop");
        if (ok) Debug.Log("Room 8 part 2: visual pass applied to " + Room9Path + ".");
    }

    // A lantern hanging on a chain of 'chain' metres from 'hangY'; 'lit' = its own warm light (the hall lamps' are the engine's).
    // Returns the lit sprite's object (for an unlit lantern: its hidden lit look, to switch on later), or null.
    private static GameObject Lantern(RoomDresser.Context ctx, float x, float hangY, float chain, bool lit)
    {
        Sprite links = ctx.Village.TryGetValue("TX Village Props - Chain 01", out Sprite c) ? c : null;
        if (links != null && chain > 0.05f)
        {
            SpriteRenderer sr = Put(ctx.Decor, "Lantern Chain", links, new Vector2(x, hangY), "BackDecor", 14, Color.white, false);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(links.bounds.size.x, chain);
            sr.transform.position += new Vector3(x, hangY - chain * 0.5f, 0f) - sr.bounds.center; // centred, whatever the pivot
        }
        string sprite = lit ? "TX Village Props - Road Lamp Light On" : "TX Village Props - Road Lamp Light Off";
        if (!ctx.Village.TryGetValue(sprite, out Sprite lamp)) return null;
        SpriteRenderer l = ctx.Prop(ctx.Village, sprite, x, hangY - chain - lamp.bounds.size.y, "Props", 1); // its top on the chain's end
        if (l == null) return null;
        if (lit) { ctx.Light("Lantern Light", l.bounds.center, new Color(1f, 0.78f, 0.5f), 0.9f, 3.5f); return l.gameObject; }
        // An unlit lantern also gets its lit look on top, hidden until something switches it on (the hall's 30 stage).
        SpriteRenderer on = ctx.Prop(ctx.Village, "TX Village Props - Road Lamp Light On", x, hangY - chain - lamp.bounds.size.y, "Props", 2);
        if (on == null) return null;
        on.gameObject.SetActive(false);
        return on.gameObject;
    }

    // A sprite whose pivot sits at 'at' (bottom-pivot art: its base; centre-pivot art: its centre).
    private static SpriteRenderer Put(Transform parent, string name, Sprite sprite, Vector2 at, string layer, int order, Color color, bool warn = true)
    {
        if (sprite == null) { if (warn) Debug.LogWarning("Room 8 part 2 visuals: missing sprite for " + name); return null; }
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = at;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        sr.color = color;
        return sr;
    }

    // A tiled strip from a to b, 'thickness' wide. alongY: the tile runs along its height (Beam), else along its width (Pipe).
    // Plank tracks (alongY false) hang below the line a-b, so the line is their walking/rolling surface.
    private static SpriteRenderer Along(Transform parent, string name, Sprite tile, Vector2 a, Vector2 b, float thickness, bool alongY,
                                        string layer, int order, Color color)
    {
        SpriteRenderer sr = Put(parent, name, tile, a, layer, order, color);
        if (sr == null) return null;
        Vector2 dir = b - a;
        float length = dir.magnitude, angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (length < 0.01f) { Object.DestroyImmediate(sr.gameObject); return null; }
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = alongY ? new Vector2(thickness, length) : new Vector2(length, thickness);
        Vector2 normal = new Vector2(-dir.y, dir.x) / length;            // left of a->b
        if (normal.y < 0f) normal = -normal;                              // the upper side
        if (Mathf.Abs(normal.y) < 0.01f) normal = Vector2.zero;           // vertical: no upper side, centred on the line
        Vector2 centre = (a + b) * 0.5f - (alongY ? Vector2.zero : normal * thickness * 0.5f);
        sr.transform.SetPositionAndRotation(centre, Quaternion.Euler(0f, 0f, alongY ? angle - 90f : angle));
        return sr;
    }

    private static void Hide(Transform room, string name)
    {
        Transform t = RoomBuildKit.Find(room, name);
        if (t != null && t.TryGetComponent(out SpriteRenderer sr)) sr.enabled = false;
    }
}
