using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tools > Shards of Motion > Room 5 > 2. Apply Visual Pass.
/// RoomDresser does the graybox automatically (culvert, alcove, west ledge, plate, gate, shrine, journal); this adds Room 5's
/// look, "the old wet causeway". The floor art matches the physics exactly:
///   ruin stone x -10..-7 | mossy tiles x -7..-4.5 (moss, mu 0.6) | wet tiles x -4.5..4 (wet stone, mu 0.05) | stone x 4..10.
/// The graybox Floor box sits 2 cm low (see Room5Builder), so it's skipped and the floor is drawn here, on the floor line.
/// Wet stone: glints and drips from the culvert (WetSurfaceFx), a cool light. Moss: tufts and flowers. The culvert and the
/// alcove get stone inside them (shadowed fill, a darker band under the roof, lit stone edges at the mouths) and a soft light
/// each, so they read as tunnels, not black boxes. The exit light turns on with the moss plate. Physics is never touched.
/// Room-local coordinates: floor top y = -4.5 (see Room5Builder).
/// </summary>
public static class Room5Dressing
{
    private const string Room5Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room05.prefab";
    private const string FxLibraryPath = "Assets/_Project/Art/FX/FX_Library.asset";
    private const float Floor = -4.5f;
    private const int Cobbles = 33; // a fully opaque ground tile of cobblestone: the tunnel walls (69 etc. have a see-through edge)

    [MenuItem("Tools/Shards of Motion/Room 5/2. Apply Visual Pass")]
    public static void Apply()
    {
        FxLibrary fx = AssetDatabase.LoadAssetAtPath<FxLibrary>(FxLibraryPath);
        bool ok = RoomDresser.Dress(Room5Path, ctx =>
        {
            // ---- Floors, matched to the surfaces (Room5Builder: moss -7..-4.5, wet stone -4.5..4).
            float wetRight = Room5Builder.AlcoveLeft, back = Room5Builder.AlcoveBack;
            ctx.Floor(null, -10f, -7f, Floor);
            ctx.Floor("Ground_Mossy", -7f, -4.5f, Floor);
            ctx.Floor("Ground_Wet", -4.5f, wetRight, Floor);
            ctx.Floor(null, wetRight, 10f, Floor);
            foreach (string band in new[] { "Strip_Moss", "Strip_Wet" }) // graybox colour bands
            {
                Transform t = RoomBuildKit.Find(ctx.Room, band);
                if (t != null && t.TryGetComponent(out SpriteRenderer sr)) sr.enabled = false;
            }

            // ---- Hollows: stone inside the culvert and the alcove, in shadow; darker just under the roof; the mouths framed by
            //      lit edge stones; a soft light inside each (cool in the wet culvert, warm in the alcove by the Bearing).
            Tunnel(ctx, "Culvert", -3.5f, 1.5f, Floor + 1.3f, true, true);
            Tunnel(ctx, "Alcove", wetRight, back, Floor + 1.8f, true, false);
            ctx.Light("Culvert Light", new Vector3(-1f, Floor + 0.5f, 0f), new Color(0.5f, 0.85f, 1f), 0.6f, 2.8f);
            ctx.Light("Alcove Light", new Vector3(back - 0.6f, Floor + 0.9f, 0f), new Color(1f, 0.8f, 0.5f), 0.45f, 1.8f);
            ctx.Hanging("TX Village Props - Grass 08", -1.2f, Floor + 1.3f);   // a few strands inside the culvert

            // ---- Wet stone: glints, drips from the culvert's underside, a cool light.
            GameObject wet = new GameObject("Wet_Surface" + RoomDresser.VisualTag);
            wet.transform.SetParent(ctx.Decor, false);
            wet.AddComponent<WetSurfaceFx>().EditorSetup(fx, -4.5f, wetRight, Floor,
                new[] { new Vector2(-3.1f, Floor + 1.25f), new Vector2(-1.2f, Floor + 1.25f), new Vector2(0.9f, Floor + 1.25f) });
            ctx.Light("Wet Glow", new Vector3(0.5f, Floor + 0.6f, 0f), new Color(0.55f, 0.8f, 1f), 0.45f, 6f);

            // ---- Moss: tufts and a flower on the bed (clear of the plate at -6.5..-5), moss hanging from the stone edges.
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 02", -6.8f, Floor);
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 05", -4.8f, Floor);
            ctx.Prop(ctx.Forest, "TX Village Props - Flower 01", -4.6f, Floor, "Props", 1);
            ctx.Hanging("TX Village Props - Grass 08", -3.3f, Floor + 1.3f);   // culvert, west mouth
            ctx.Hanging("TX Village Props - Grass 07", 1.3f, Floor + 1.3f);    // culvert, east mouth
            ctx.Hanging("TX Village Props - Grass 07", wetRight + 0.3f, Floor + 1.8f); // alcove mouth
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 03", -9.4f, Floor + 0.65f); // on the fallen column
            ctx.Hanging("TX Village Props - Grass 08", -7.7f, Floor + 3.2f);   // under the west ledge

            // ---- Background: broken causeway pillars, the old spring's well, rocks and a bush.
            int T = RoomDresser.T, F = RoomDresser.F;
            ctx.BackWall(-6, -3, T); ctx.BackWall(-6, -4, F); ctx.BackWall(-6, -5, F);
            ctx.BackWall(3, -2, T); ctx.BackWall(3, -3, F); ctx.BackWall(3, -4, F); ctx.BackWall(3, -5, F);
            ctx.BackProp("TX Village Props - Well", -1f, Floor, 0.55f);         // behind the culvert: the spring that wets the stone
            ctx.BackProp("TX Village Props - Rock 01", -8.9f, Floor, 0.5f);
            ctx.BackProp("TX Village Props - Bush 02", 8.6f, Floor, 0.5f);

            // ---- Exit light: on while the moss plate holds the crate (the gate is driven by the plate directly).
            Transform plateT = RoomBuildKit.Find(ctx.Room, "Plate_Moss_80N"), gate = RoomBuildKit.Find(ctx.Room, "Gate_Exit");
            if (plateT != null && gate != null && plateT.TryGetComponent(out PressurePlate plate))
            {
                Light2D light = ctx.Light("Exit Light", new Vector3(gate.position.x - 0.5f, Floor + 2f, 0f), new Color(0.35f, 0.9f, 1f), 1.2f, 3.5f);
                light.enabled = false;
                SignalActivator activator = light.gameObject.AddComponent<SignalActivator>();
                SerializedObject a = new SerializedObject(activator);
                a.FindProperty("signal").objectReferenceValue = plate;
                SerializedProperty list = a.FindProperty("behaviours");
                list.arraySize = 1;
                list.GetArrayElementAtIndex(0).objectReferenceValue = light;
                a.ApplyModifiedPropertiesWithoutUndo();
            }
        }, "Floor");
        if (ok) Debug.Log("Room 5 visual pass applied to " + Room5Path + ".");
    }

    // The inside of an opening (floor .. roofBottom between x0 and x1): shadowed stone fill, a darker band under the roof,
    // and lit edge stones at the open mouths, so it reads as a stone tunnel with depth.
    private static void Tunnel(RoomDresser.Context ctx, string name, float x0, float x1, float roofBottom, bool westMouth, bool eastMouth)
    {
        ctx.Hollow(name, x0, Floor, x1, roofBottom, 0.5f, Cobbles);
        ctx.Hollow(name + " Shade", x0, roofBottom - 0.35f, x1, roofBottom, 0.75f, Cobbles, 31);
        if (westMouth) ctx.Hollow(name + " West Edge", x0, Floor, x0 + 0.4f, roofBottom, 0.3f, RoomDresser.Le, 32);
        if (eastMouth) ctx.Hollow(name + " East Edge", x1 - 0.4f, Floor, x1, roofBottom, 0.3f, RoomDresser.R, 32);
    }
}
