using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tools > Shards of Motion > Room 6 > 2. Apply Visual Pass.
/// RoomDresser does the graybox automatically (gallery, exit ledge, pulley tower, hold plate, the west lift's deck and chains,
/// shrine, journal); this adds Room 6's look, "the old pulley works", built so the machine reads at a glance:
///   - a big wooden pulley wheel on the tower that TURNS as the lift moves, chains from each pan up to it that lengthen and
///     shorten as the pans travel, and a brake lamp on its hub (red braked, green released): LiftRig, visual only;
///   - the pans as wooden platforms (pan A's rim a wooden lip);
///   - a dim pulley-house background: stone pillars, old hanging chains, barrels and crates, moss under the ledges, a lamp.
/// Physics is never touched. Room-local coordinates: floor top y = -4.5 (see Room6Builder).
/// </summary>
public static class Room6Dressing
{
    private const string Room6Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room06.prefab";
    private const string WheelPath = "Assets/_Project/Art/Sprites/Props/SP_PulleyWheel.png";
    private const float Floor = -4.5f, WheelRadius = 0.75f;
    private static readonly Vector2 WheelCentre = new Vector2(0.8f, Floor + 6.8f); // on the pulley tower (x 0.2..1.4)
    private const float ChainAX = 0.1f, ChainBX = 1.5f;                            // straight down to pan A's lip and pan B's edge
    private static readonly Color Wood = new Color(0.62f, 0.45f, 0.28f);

    [MenuItem("Tools/Shards of Motion/Room 6/2. Apply Visual Pass")]
    public static void Apply()
    {
        Sprite wheelSprite = WheelSprite();
        bool ok = RoomDresser.Dress(Room6Path, ctx =>
        {
            Sprite plank = Pack(ctx, "Elevator Platform"), chain = Pack(ctx, "Chain 01");

            // ---- The pans: wooden platforms riding on the kinematic pans (children, so they move with them).
            foreach (string name in new[] { "Pan_A", "Pan_B" })
            {
                Transform pan = RoomBuildKit.Find(ctx.Room, name);
                if (pan == null || !pan.TryGetComponent(out BoxCollider2D box)) continue;
                if (pan.TryGetComponent(out SpriteRenderer own)) own.enabled = false;
                Rect r = BoxRect(box);
                SpriteRenderer deck = Child(pan, "Deck", plank, "Interactables", 0);
                deck.drawMode = SpriteDrawMode.Tiled;
                deck.size = new Vector2(r.width, plank != null ? plank.bounds.size.y : 0.3f);
                Place(deck, new Vector2(r.center.x, r.yMax - 7f / 32f), bottom: true); // plank top on the pan's top (as the dresser's lifts)
            }
            Transform rim = RoomBuildKit.Find(ctx.Room, "Rim_A");
            if (rim != null && rim.TryGetComponent(out SpriteRenderer rimSr)) rimSr.color = Wood;

            // ---- The pulley: wheel, chains, brake lamp, driven by LiftRig.
            GameObject rigGo = new GameObject("Pulley_Rig" + RoomDresser.VisualTag);
            rigGo.transform.SetParent(ctx.Decor, false);
            GameObject wheelGo = new GameObject("Wheel" + RoomDresser.VisualTag);
            wheelGo.transform.SetParent(rigGo.transform, false);
            wheelGo.transform.position = WheelCentre;
            SpriteRenderer wheel = wheelGo.AddComponent<SpriteRenderer>();
            wheel.sprite = wheelSprite;
            wheel.sortingLayerName = "Props";
            wheel.sortingOrder = 5;
            SpriteRenderer chainA = Chain(rigGo.transform, "Chain_A", chain, ChainAX);
            SpriteRenderer chainB = Chain(rigGo.transform, "Chain_B", chain, ChainBX);
            Light2D brake = ctx.Light("Brake Lamp", new Vector3(WheelCentre.x, WheelCentre.y, 0f), new Color(1f, 0.3f, 0.2f), 0.9f, 1.4f);
            Transform liftT = RoomBuildKit.Find(ctx.Room, "Balance_Lift");
            BalanceLift lift = liftT != null ? liftT.GetComponent<BalanceLift>() : null;
            rigGo.AddComponent<LiftRig>().EditorSetup(lift, wheelGo.transform, WheelRadius, chainA, chainB, brake);
            if (lift == null) Debug.LogWarning("Room 6 visual pass: no Balance_Lift found; run Room 6 > 1. Build Graybox first.");

            // ---- Background: the pulley house.
            int T = RoomDresser.T, F = RoomDresser.F;
            for (int y = -5; y <= -1; y++) ctx.BackWall(4, y, y == -1 ? T : F);       // a pillar under the exit ledge
            for (int y = -5; y <= 0; y++) ctx.BackWall(-1, y, y == 0 ? T : F);       // a pillar behind the pans
            BackChain(ctx, chain, -4.3f, 3.2f);
            BackChain(ctx, chain, 5.6f, 4.4f);
            ctx.BackProp("TX Village Props - Barrel", 5.8f, Floor, 0.5f);
            ctx.BackProp("TX Village Props - Crate Large", 4.6f, Floor, 0.5f);
            ctx.BackProp("TX Village Props - Barrel", -6.9f, Floor, 0.55f);
            ctx.Hanging("TX Village Props - Grass 08", -6.4f, Floor + 4.6f);         // under the gallery
            ctx.Hanging("TX Village Props - Grass 07", -3.3f, Floor + 4.6f);
            ctx.Hanging("TX Village Props - Grass 08", 4.4f, Floor + 5.6f);          // under the exit ledge
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 04", -4.2f, Floor + 5f);  // tufts on the gallery and exit ledge
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 02", 5.2f, Floor + 6f);
            ctx.Lamp(-7.1f, Floor);
        });
        if (ok) Debug.Log("Room 6 visual pass applied to " + Room6Path + ".");
    }

    private static Sprite Pack(RoomDresser.Context ctx, string name) =>
        ctx.Village.TryGetValue("TX Village Props - " + name, out Sprite s) ? s : null;

    // A sprite child of a (possibly scaled) object, counter-scaled so the art keeps its true pixel size.
    private static SpriteRenderer Child(Transform parent, string name, Sprite sprite, string layer, int order)
    {
        GameObject go = new GameObject(name + RoomDresser.VisualTag);
        go.transform.SetParent(parent, false);
        Vector3 s = parent.lossyScale;
        go.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return sr;
    }

    // A pan's chain: a tiled vertical chain at x; LiftRig sets its length every frame.
    private static SpriteRenderer Chain(Transform parent, string name, Sprite chain, float x)
    {
        GameObject go = new GameObject(name + RoomDresser.VisualTag);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, WheelCentre.y - 1f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = chain;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(3f / 32f, 2f);
        sr.sortingLayerName = "Props";
        sr.sortingOrder = 4;
        return sr;
    }

    // An old chain hanging from the ceiling in the background (dimmed).
    private static void BackChain(RoomDresser.Context ctx, Sprite chain, float x, float length)
    {
        GameObject go = new GameObject("Back Chain" + RoomDresser.VisualTag);
        go.transform.SetParent(ctx.Decor, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = chain;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(3f / 32f, length);
        sr.sortingLayerName = "BackDecor";
        sr.sortingOrder = 6;
        sr.color = new Color(0.45f, 0.48f, 0.55f);
        Place(sr, new Vector2(x, 5.625f - length), bottom: true);
    }

    // Places a (tiled or simple) sprite so its box's bottom-centre (or centre) is at p, whatever the sprite's pivot.
    private static void Place(SpriteRenderer sr, Vector2 p, bool bottom)
    {
        if (sr.sprite == null) return;
        Vector2 size = sr.drawMode == SpriteDrawMode.Simple ? (Vector2)sr.sprite.bounds.size : sr.size;
        Vector2 pivot01 = new Vector2(sr.sprite.pivot.x / sr.sprite.rect.width, sr.sprite.pivot.y / sr.sprite.rect.height);
        Vector2 centreOffset = new Vector2((0.5f - pivot01.x) * size.x, (0.5f - pivot01.y) * size.y);
        Vector2 centre = bottom ? new Vector2(p.x, p.y + size.y * 0.5f) : p;
        sr.transform.position = new Vector3(centre.x - centreOffset.x, centre.y - centreOffset.y, 0f);
    }

    private static Rect BoxRect(BoxCollider2D box)
    {
        Transform t = box.transform;
        Vector3 a = t.TransformPoint(box.offset - box.size * 0.5f), b = t.TransformPoint(box.offset + box.size * 0.5f);
        return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }

    // ------------------------------------------------------------------ the pulley wheel (48 x 48 px, centre pivot)

    private static Sprite WheelSprite()
    {
        const int n = 48;
        Color32 clear = new Color32(0, 0, 0, 0), outline = new Color32(58, 36, 20, 255), rim = new Color32(138, 96, 58, 255),
                rimLight = new Color32(170, 124, 78, 255), spoke = new Color32(120, 82, 48, 255), iron = new Color32(96, 100, 108, 255),
                ironLight = new Color32(150, 156, 166, 255);
        Color32[] px = new Color32[n * n];
        Vector2 c = new Vector2(23.5f, 23.5f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 p = new Vector2(x, y);
                float d = Vector2.Distance(p, c);
                Color32 col = clear;
                if (d <= 23.5f) col = outline;
                if (d <= 22.5f) col = y > 26 ? rimLight : rim;   // the rim, lit from above
                if (d <= 20f) col = outline;
                if (d <= 19f) col = clear;                       // open between the spokes
                // six spokes
                if (d < 19.5f && d > 3.5f)
                {
                    float a = Mathf.Atan2(y - c.y, x - c.x);
                    for (int k = 0; k < 6; k++)
                    {
                        float sa = k * Mathf.PI / 3f;
                        float dist = Mathf.Abs(Mathf.Sin(a - sa)) * d; // distance from the spoke's centre line
                        if (Mathf.Cos(a - sa) > 0f && dist <= 1.6f) col = dist > 1f ? outline : spoke;
                    }
                }
                if (d <= 4.5f) col = outline;
                if (d <= 3.5f) col = y > 24 ? ironLight : iron;  // iron hub
                if (d <= 1f) col = outline;
                px[y * n + x] = col;
            }
        Directory.CreateDirectory(Path.GetDirectoryName(WheelPath));
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        File.WriteAllBytes(WheelPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(WheelPath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(WheelPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(WheelPath);
    }
}
