using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

/// <summary>
/// Reusable visual pass for room prefabs (Village Props art, recoloured; see Docs/Region1_Art_Direction.md).
/// Dresses a room AUTOMATICALLY from its graybox, by object type, then runs a short per-room decoration list:
///   solid boxes      - floors: grass edge + stone fill (end caps at pits); thin (<= 0.6 m): platform strips;
///                      others: stone blocks in 3x3 tile pieces. Art matches the collider exactly.
///   PressurePlate    - recessed slab that sinks with weight (PressurePlate.slab).
///   Mover            - "Gate..." gets bars; anything else (lifts) gets a plank on the collider top + chains.
///   ResetShrine      - Stone of Recall + glow + cyan light.   JournalStone - a sign post.
///   HazardZone       - acid pixel water (Hazard_Acid prefab) sized to the zone + a green glow.
///   Relay_Exit       - a cyan light at the exit gate that turns on while it's open.
/// Graybox sprites are hidden; colliders, masses and puzzle settings are never touched.
/// Re-runnable: everything it creates ends with "_Visual" and is removed before rebuilding.
/// Use: RoomDresser.Dress(prefabPath, ctx => { ctx.Prop(...); ... }) from a per-room menu (see Room2Dressing).
/// </summary>
public static class RoomDresser
{
    public const string VisualTag = "_Visual";
    private const string RuinTexture = "Assets/_Project/Art/Recolors/Ground_RuinStone.png";
    private const string RuinTiles = "Assets/_Project/Art/Recolors/Palettes/Ground_RuinStone";
    private const string ForestTexture = "Assets/_Project/Art/Recolors/Props_Forest.png";
    private const string InteractiveTexture = "Assets/_Project/Art/Recolors/Props_Interactive.png";
    private const string VillageTexture = "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Village Props.png";
    private const string AcidPrefab = "Assets/_Project/Prefabs/Environment/Hazard_Acid.prefab";

    // Ground tile numbers (edge analysis of TX Tileset Ground), as in Room1VisualPass.
    public const int TL = 0, T = 1, TR = 2, Le = 3, F = 4, R = 5, BL = 6, B = 7, BR = 8;
    private const int StripL = 157, StripM = 158, StripR = 164;
    private const float RoomHalfWidth = 10f, CameraBottom = -5.625f, FloorTop = -4.5f;

    /// <summary>What a per-room decoration list can use.</summary>
    public class Context
    {
        public Transform Room, Decor;
        public Dictionary<string, Sprite> Forest, Village, Interactive;
        internal Tilemap BackWallMap;

        public SpriteRenderer Prop(Dictionary<string, Sprite> sheet, string sprite, float x, float groundY, string layer = "Props", int order = 0)
        {
            if (!sheet.TryGetValue(sprite, out Sprite s)) { Debug.LogWarning("Room dresser: no sprite " + sprite); return null; }
            SpriteRenderer sr = Visual(Decor, sprite.Replace("TX Village Props - ", ""), s, layer, order, false);
            PlaceBottom(sr, new Vector2(x, groundY));
            return sr;
        }

        /// <summary>A Village prop pushed into the background: darker and cooler, drawn behind the play space
        /// (BackDecor), so it adds depth without looking like something to stand on or use.</summary>
        public SpriteRenderer BackProp(string sprite, float x, float groundY, float darkness = 0.5f, bool flip = false)
        {
            SpriteRenderer sr = Prop(Village, sprite, x, groundY, "BackDecor", 5);
            if (sr == null) return null;
            float v = 1f - darkness;
            sr.color = new Color(v * 0.9f, v * 0.95f, v * 1.1f); // darker, slightly blue: atmospheric distance
            sr.flipX = flip;
            return sr;
        }

        /// <summary>Grass flipped upside down under a ledge: reads as hanging moss.</summary>
        public void Hanging(string sprite, float x, float ceilingY)
        {
            if (!Forest.TryGetValue(sprite, out Sprite s)) return;
            SpriteRenderer sr = Visual(Decor, "Hanging " + sprite.Replace("TX Village Props - ", ""), s, "Props", 1, false);
            sr.flipY = true;
            Bounds b = s.bounds;
            sr.transform.position = new Vector3(x - b.center.x, ceilingY + b.center.y - b.extents.y, 0f);
        }

        public Light2D Light(string name, Vector3 position, Color color, float intensity, float radius)
            => PointLight(Decor, name, position, color, intensity, radius);

        /// <summary>A tile on the dimmed back-wall tilemap. Cell (x, y) covers world x..x+1, y+0.5..y+1.5 (floor top = row -6's top).</summary>
        public void BackWall(int x, int y, int tile)
        {
            if (BackWallMap != null) BackWallMap.SetTile(new Vector3Int(x, y, 0), Tile(tile));
        }

        /// <summary>A lamp post with its warm light.</summary>
        public void Lamp(float x, float groundY)
        {
            SpriteRenderer pillar = Prop(Village, "TX Village Props - Road Lamp Pillar", x, groundY);
            if (pillar == null) return;
            Vector3 top = new Vector3(pillar.bounds.center.x, pillar.bounds.max.y, 0f);
            SpriteRenderer lamp = Prop(Village, "TX Village Props - Road Lamp Light On", top.x, top.y - 0.3f, "Props", 1);
            if (lamp != null) Light("Lamp Light", lamp.bounds.center, new Color(1f, 0.78f, 0.5f), 1.1f, 5f);
        }
    }

    private static Dictionary<string, Sprite> ruin;
    private static readonly List<Rect> solids = new List<Rect>(); // walkable surfaces, for placing shrines and signs

    public static bool Dress(string prefabPath, Action<Context> decorate)
    {
        ruin = Sprites(RuinTexture);
        Context ctx = new Context { Forest = Sprites(ForestTexture), Village = Sprites(VillageTexture), Interactive = Sprites(InteractiveTexture) };
        if (ruin.Count == 0 || ctx.Forest.Count == 0 || ctx.Interactive.Count == 0)
        {
            Debug.LogError("Room dresser: run Tools > Shards of Motion > Recolor > Build All first.");
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Transform layout = Find(root.transform, "Layout");
            if (layout == null) { Debug.LogError($"Room dresser: {prefabPath} has no Layout group. Nothing was changed."); return false; }
            Clear(root.transform);
            ctx.Room = root.transform;
            Transform visuals = NewChild(layout, "Visuals" + VisualTag);
            ctx.Decor = NewChild(visuals, "Decor" + VisualTag);

            DressSolids(root.transform, visuals);
            DressPlates(root.transform, ctx);
            DressMovers(root.transform, ctx);
            DressShrinesAndJournals(root.transform, ctx);
            DressHazards(root.transform, visuals);
            ExitLight(root.transform, ctx);

            Transform grid = Find(layout, "Grid");
            if (grid != null)
            {
                grid.localPosition = new Vector3(0f, 0.5f, 0f); // cells line up with the floor top (-4.5)
                Transform back = Find(grid, "TM_BackWall");
                if (back != null && back.TryGetComponent(out Tilemap map))
                {
                    map.ClearAllTiles();
                    map.color = new Color(0.62f, 0.68f, 0.76f); // darker and cooler: sits behind the play space
                    ctx.BackWallMap = map;
                }
                Transform terrain = Find(grid, "TM_Terrain");
                if (terrain != null) foreach (Collider2D c in terrain.GetComponents<Collider2D>()) c.enabled = false; // visual only
            }

            decorate?.Invoke(ctx);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ------------------------------------------------------------------ automatic dressing

    private static bool IsSpecial(GameObject go) =>
        go.GetComponent<Rigidbody2D>() != null || go.GetComponent<PressurePlate>() != null || go.GetComponent<Mover>() != null ||
        go.GetComponent<ResetShrine>() != null || go.GetComponent<JournalStone>() != null || go.GetComponent<HazardZone>() != null;

    private static void DressSolids(Transform room, Transform visuals)
    {
        solids.Clear();
        foreach (BoxCollider2D box in room.GetComponentsInChildren<BoxCollider2D>(true))
        {
            GameObject go = box.gameObject;
            if (box.isTrigger || IsSpecial(go) || go.name.EndsWith(VisualTag) || PrefabUtility.IsPartOfPrefabInstance(go)) continue;
            if (!go.TryGetComponent(out SpriteRenderer sr)) continue;
            Rect r = BoxRect(box);
            solids.Add(r);
            sr.enabled = false; // graybox shape hidden (collider untouched)
            if (r.yMax < CameraBottom) continue; // entirely below the screen (e.g. a pit's safety floor)

            bool leftCap = r.xMin > -RoomHalfWidth + 0.6f, rightCap = r.xMax < RoomHalfWidth - 0.6f;
            if (go.name.StartsWith("Floor")) FloorStrip(visuals, go.name, r, leftCap, rightCap);
            else if (r.height <= 0.6f) Strip(visuals, go.name, r.xMin, r.xMax, r.yMax, leftCap, rightCap);
            else Block(visuals, go.name, r.xMin, r.yMin, r.xMax, r.yMax, grounded: r.yMin <= FloorTop + 0.01f);
        }
    }

    private static void DressPlates(Transform room, Context ctx)
    {
        foreach (PressurePlate plate in room.GetComponentsInChildren<PressurePlate>(true))
        {
            Transform t = plate.transform;
            if (t.TryGetComponent(out SpriteRenderer own)) own.enabled = false;
            SpriteRenderer slab = Visual(t, "Slab", ctx.Interactive["TX Village Props - Fence 01 Rail 01"], "BackDecor", 20);
            Tiled(slab, new Vector2(BoxRect(t.GetComponent<BoxCollider2D>()).width, 9f / 32f));
            // Recessed: drawn behind the ground with its top 4 px above the floor; PressurePlate sinks it up to 3 px.
            float floor = BoxEdgeY(t, top: false);
            PlaceBottom(slab, new Vector2(t.position.x, floor + 4f / 32f - 9f / 32f));
            SerializedObject p = new SerializedObject(plate);
            p.FindProperty("plateRenderer").objectReferenceValue = slab;
            p.FindProperty("slab").objectReferenceValue = slab.transform;
            p.FindProperty("releasedColor").colorValue = Color.white;
            p.FindProperty("pressedColor").colorValue = new Color(0.75f, 1f, 0.8f);
            p.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void DressMovers(Transform room, Context ctx)
    {
        foreach (Mover mover in room.GetComponentsInChildren<Mover>(true))
        {
            Transform t = mover.transform;
            if (t.TryGetComponent(out SpriteRenderer own)) own.enabled = false;
            Rect r = BoxRect(t.GetComponent<BoxCollider2D>());
            Vector2 travel = new SerializedObject(mover).FindProperty("travel").vector2Value;
            if (t.name.StartsWith("Gate"))
            {
                SpriteRenderer bars = Visual(t, "Bars", ctx.Interactive["TX Village Props - Ladder 01 M"], "Interactables", 0);
                Tiled(bars, r.size);
                PlaceCenter(bars, r.center);
            }
            else if (Mathf.Abs(travel.y) < 0.01f)
            {
                // Sideways mover = a bridge: wooden planks across its full width, top on the collider top. No chains.
                Sprite planks = ctx.Village["TX Village Props - Wooden Bridge Part 01"];
                SpriteRenderer deck = Visual(t, "Deck", planks, "Interactables", 0);
                Tiled(deck, new Vector2(r.width, planks.bounds.size.y));
                PlaceBottom(deck, new Vector2(r.center.x, r.yMax - planks.bounds.size.y));
            }
            else
            {
                // Elevator Platform: 10 px tall, but only the bottom 7 rows are plank. Plank top = collider top.
                SpriteRenderer plank = Visual(t, "Platform", ctx.Village["TX Village Props - Elevator Platform"], "Interactables", 0);
                PlaceBottom(plank, new Vector2(r.center.x, r.yMax - 7f / 32f));
                foreach (float side in new[] { -0.85f, 0.85f })
                {
                    SpriteRenderer chain = Visual(t, "Chain", ctx.Village["TX Village Props - Chain 01"], "Interactables", -1);
                    Tiled(chain, new Vector2(3f / 32f, 11f));
                    PlaceBottom(chain, new Vector2(r.center.x + side, r.yMax));
                }
            }
        }
    }

    private static void DressShrinesAndJournals(Transform room, Context ctx)
    {
        foreach (ResetShrine shrine in room.GetComponentsInChildren<ResetShrine>(true))
        {
            Transform t = shrine.transform;
            if (t.TryGetComponent(out SpriteRenderer own)) own.enabled = false;
            SpriteRenderer stone = Visual(t, "Stone", ctx.Village["TX Village Props - Stone of Recall"], "Interactables", 0);
            PlaceBottom(stone, new Vector2(t.position.x, GroundBelow(t)));
            SpriteRenderer glow = Visual(t, "Glow", ctx.Village["TX Village Props - Stone of Recall - Glow"], "Interactables", 1);
            glow.transform.position = stone.bounds.center;
            PointLight(t, "Light", stone.bounds.center, new Color(0.4f, 0.9f, 1f), 0.8f, 2.5f);
        }
        foreach (JournalStone journal in room.GetComponentsInChildren<JournalStone>(true))
        {
            Transform t = journal.transform;
            if (t.TryGetComponent(out SpriteRenderer own)) own.enabled = false;
            SpriteRenderer sign = Visual(t, "Sign", ctx.Village["TX Village Props - Sign 01"], "Props", 0);
            PlaceBottom(sign, new Vector2(t.position.x, GroundBelow(t)));
        }
    }

    private static void DressHazards(Transform room, Transform visuals)
    {
        GameObject acidPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AcidPrefab);
        foreach (HazardZone hazard in room.GetComponentsInChildren<HazardZone>(true))
        {
            if (PrefabUtility.IsPartOfPrefabInstance(hazard.gameObject)) continue; // already real water
            Transform t = hazard.transform;
            BoxCollider2D box = t.GetComponent<BoxCollider2D>();
            if (box == null) continue;
            Rect r = BoxRect(box);
            if (t.TryGetComponent(out SpriteRenderer own)) own.enabled = false;
            if (acidPrefab != null)
            {
                // The water prefab has its own trigger + HazardZone; both do the same thing (respawn / return home).
                GameObject water = (GameObject)PrefabUtility.InstantiatePrefab(acidPrefab, visuals);
                water.name = t.name + " Acid" + VisualTag;
                water.transform.position = new Vector3(r.center.x, r.yMin, 0f); // the water's origin is its bottom centre
                Cainos.InteractivePixelWater.PixelWater pw = water.GetComponent<Cainos.InteractivePixelWater.PixelWater>();
                if (pw != null)
                {
                    pw.Size = r.size;
                    pw.ambientWaveMul = 2.5f;  // livelier surface (Cainos default 1)
                    pw.waveLimit = 0.8f;       // taller waves allowed (default 0.5)
                    pw.distortionStrength = 0.8f;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(pw); // keep these as overrides on the instance
                }
            }
            PointLight(visuals, t.name + " Glow", new Vector3(r.center.x, r.yMax, 0f), new Color(0.55f, 1f, 0.35f), 0.7f, Mathf.Max(1.5f, r.width));
        }
    }

    private static void ExitLight(Transform room, Context ctx)
    {
        Transform relay = Find(room, "Relay_Exit"), gate = Find(room, "Gate_Exit");
        if (relay == null || gate == null || !relay.TryGetComponent(out PuzzleRelay pr)) return;
        Rect g = BoxRect(gate.GetComponent<BoxCollider2D>());
        Light2D light = ctx.Light("Exit Light", new Vector3(g.center.x - 0.5f, FloorTop + 2f, 0f), new Color(0.35f, 0.9f, 1f), 1.2f, 3.5f);
        light.enabled = false;
        SignalActivator activator = light.gameObject.AddComponent<SignalActivator>();
        SerializedObject a = new SerializedObject(activator);
        a.FindProperty("signal").objectReferenceValue = pr;
        SerializedProperty list = a.FindProperty("behaviours");
        list.arraySize = 1;
        list.GetArrayElementAtIndex(0).objectReferenceValue = light;
        a.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------ shapes

    // A floor: grass-topped edge row on the collider top, stone fill below (past the bottom of the screen).
    private static void FloorStrip(Transform parent, string name, Rect r, bool leftCap, bool rightCap)
    {
        Transform group = NewChild(parent, name + VisualTag);
        float top = r.yMax;
        foreach ((int row, float y0, float y1) in new[] { (0, top - 1f, top), (1, top - 2f, top - 1f) })
        {
            int[] pieces = row == 0 ? new[] { TL, T, TR } : new[] { Le, F, R };
            float a = leftCap ? r.xMin + 1f : r.xMin, b = rightCap ? r.xMax - 1f : r.xMax;
            if (b > a) Piece(group, pieces[1], a, y0, b, y1);
            if (leftCap) Piece(group, pieces[0], r.xMin, y0, Mathf.Min(r.xMin + 1f, r.xMax), y1);
            if (rightCap) Piece(group, pieces[2], Mathf.Max(r.xMax - 1f, r.xMin), y0, r.xMax, y1);
        }
    }

    // A rectangular block in 3x3 tile pieces. "grounded" blocks use side tiles at the bottom (no rounded underside).
    private static void Block(Transform parent, string name, float xMin, float yMin, float xMax, float yMax, bool grounded)
    {
        Transform group = NewChild(parent, name + VisualTag);
        float w = xMax - xMin, h = yMax - yMin;
        (float a, float b, int col)[] cols = w < 2f
            ? new[] { (xMin, xMax, 1) }
            : new[] { (xMin, xMin + 1f, 0), (xMin + 1f, xMax - 1f, 1), (xMax - 1f, xMax, 2) };
        List<(float a, float b, int row)> rows = new List<(float, float, int)>();
        if (h <= 1f) rows.Add((yMin, yMax, 0));
        else
        {
            rows.Add((yMax - 1f, yMax, 0));
            if (h > 2f) rows.Add((yMin + 1f, yMax - 1f, 1));
            rows.Add((yMin, Mathf.Min(yMin + 1f, yMax - 1f), grounded ? 1 : 2));
        }
        int[,] pieces = { { TL, T, TR }, { Le, F, R }, { BL, B, BR } };
        foreach ((float x0, float x1, int col) in cols)
            foreach ((float y0, float y1, int row) in rows)
                if (x1 - x0 > 0f && y1 - y0 > 0f) Piece(group, pieces[row, col], x0, y0, x1, y1);
    }

    // A thin platform from the 1-tile strip pieces, its top on the collider's top.
    private static void Strip(Transform parent, string name, float xMin, float xMax, float top, bool leftCap, bool rightCap)
    {
        Transform group = NewChild(parent, name + VisualTag);
        float y = top - 0.5f;
        float midA = leftCap ? xMin + 1f : xMin, midB = rightCap ? xMax - 1f : xMax;
        if (midB > midA)
        {
            SpriteRenderer mid = Visual(group, "Middle", Ground(StripM), "Midground", 0, false);
            Tiled(mid, new Vector2(midB - midA, 1f));
            PlaceCenter(mid, new Vector2((midA + midB) * 0.5f, y));
        }
        if (leftCap) Visual(group, "Left", Ground(StripL), "Midground", 0, false).transform.position = new Vector3(xMin + 0.5f, y, 0f);
        if (rightCap) Visual(group, "Right", Ground(StripR), "Midground", 1, false).transform.position = new Vector3(xMax - 0.5f, y, 0f);
    }

    private static void Piece(Transform group, int tile, float x0, float y0, float x1, float y1)
    {
        SpriteRenderer sr = Visual(group, "Piece", Ground(tile), "Midground", 0, false);
        Tiled(sr, new Vector2(x1 - x0, y1 - y0));
        PlaceCenter(sr, new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f));
    }

    // ------------------------------------------------------------------ helpers (same rules as Room1VisualPass)

    private static Light2D PointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float radius)
    {
        GameObject go = new GameObject(name + VisualTag);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        Light2D l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.pointLightOuterRadius = radius;
        l.pointLightInnerRadius = radius * 0.2f;
        return l;
    }

    // A sprite child. counterScale undoes the parent's scale so the art shows at its true pixel size.
    private static SpriteRenderer Visual(Transform parent, string name, Sprite sprite, string layer, int order, bool counterScale = true)
    {
        GameObject go = new GameObject(name + VisualTag);
        go.transform.SetParent(parent, false);
        if (counterScale)
        {
            Vector3 s = parent.lossyScale;
            go.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f);
        }
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return sr;
    }

    private static void Tiled(SpriteRenderer sr, Vector2 size)
    {
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;
    }

    private static Bounds LocalBounds(SpriteRenderer sr)
    {
        if (sr.drawMode == SpriteDrawMode.Simple) return sr.sprite.bounds;
        Vector2 pivot01 = new Vector2(sr.sprite.pivot.x / sr.sprite.rect.width, sr.sprite.pivot.y / sr.sprite.rect.height);
        Vector2 center = new Vector2((0.5f - pivot01.x) * sr.size.x, (0.5f - pivot01.y) * sr.size.y);
        return new Bounds(center, sr.size);
    }

    private static void PlaceBottom(SpriteRenderer sr, Vector2 bottomCenter)
    {
        Bounds local = LocalBounds(sr);
        Vector3 scale = sr.transform.lossyScale;
        sr.transform.position = new Vector3(bottomCenter.x - local.center.x * scale.x,
                                            bottomCenter.y - (local.center.y - local.extents.y) * scale.y, 0f);
    }

    private static void PlaceCenter(SpriteRenderer sr, Vector2 center)
    {
        Bounds local = LocalBounds(sr);
        Vector3 scale = sr.transform.lossyScale;
        sr.transform.position = new Vector3(center.x - local.center.x * scale.x, center.y - local.center.y * scale.y, 0f);
    }

    // World rect of a BoxCollider2D (from size/offset/transform, so it works inside an edited prefab).
    private static Rect BoxRect(BoxCollider2D box)
    {
        if (box == null) return new Rect();
        Transform t = box.transform;
        Vector3 a = t.TransformPoint(box.offset - box.size * 0.5f), b = t.TransformPoint(box.offset + box.size * 0.5f);
        return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }

    private static float BoxEdgeY(Transform t, bool top)
    {
        Rect r = BoxRect(t.GetComponent<BoxCollider2D>());
        return r.width == 0f ? t.position.y : (top ? r.yMax : r.yMin);
    }

    // Where an object's art should stand: the top of the highest solid surface below it (floor, ledge).
    private static float GroundBelow(Transform t)
    {
        float x = t.position.x, y = t.position.y + 0.05f, best = float.NegativeInfinity;
        foreach (Rect r in solids)
            if (x >= r.xMin && x <= r.xMax && r.yMax <= y && r.yMax > best) best = r.yMax;
        return float.IsNegativeInfinity(best) ? FloorTop : best;
    }

    private static void Clear(Transform room)
    {
        foreach (Transform t in room.GetComponentsInChildren<Transform>(true).Where(t => t != null && t.name.EndsWith(VisualTag)).ToArray())
            if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    private static Transform NewChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static Transform Find(Transform root, string name) =>
        root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

    private static Sprite Ground(int n) => ruin[$"TX Tileset Ground_{n}"];
    private static TileBase Tile(int n) => AssetDatabase.LoadAssetAtPath<TileBase>($"{RuinTiles}/TX Tileset Ground_{n}.asset");

    private static Dictionary<string, Sprite> Sprites(string path)
    {
        Dictionary<string, Sprite> d = new Dictionary<string, Sprite>();
        foreach (Sprite s in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
            if (!d.ContainsKey(s.name)) d.Add(s.name, s);
        return d;
    }
}
