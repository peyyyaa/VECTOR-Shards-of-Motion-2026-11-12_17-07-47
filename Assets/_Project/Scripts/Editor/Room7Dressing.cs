using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tools > Shards of Motion > Room 7 > 2. Apply Visual Pass.
/// RoomDresser does the graybox automatically (floors at the pit, culvert roof, pillar, acid water, plate slab, the gates'
/// bars, shrine, journal); this adds Room 7's look, "the old mill race":
///   - the mid shelf drawn as WET STONE (it's skipped by the dresser: its collider sits 2 cm low), glints and drips (WetSurfaceFx);
///   - the culvert's inside as shadowed cobblestone with lit edges and a cool light, so it reads as a tunnel;
///   - the stop block drawn BEHIND the shelf's stone, so it stays hidden until it pops up into the culvert;
///   - an exit light that comes on with the brake plate;
///   - background: an old cart wheel, a barrel and a rock on the east floor, moss under the roof, a lamp; a strong glow over the acid.
/// Physics is never touched. Room-local coordinates: floor top y = -4.5, shelf top -1.5 (see Room7Builder).
/// </summary>
public static class Room7Dressing
{
    private const string Room7Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room07.prefab";
    private const string FxLibraryPath = "Assets/_Project/Art/FX/FX_Library.asset";
    private const float Floor = -4.5f, ShelfTop = -1.5f, RoofBottom = ShelfTop + 1.3f, RoofTop = RoofBottom + 0.7f;
    private const int Cobbles = 33; // a fully opaque cobblestone ground tile

    [MenuItem("Tools/Shards of Motion/Room 7/2. Apply Visual Pass")]
    public static void Apply()
    {
        FxLibrary fx = AssetDatabase.LoadAssetAtPath<FxLibrary>(FxLibraryPath);
        bool ok = RoomDresser.Dress(Room7Path, ctx =>
        {
            // ---- The mid shelf: wet stone from its top down to the floor.
            ctx.Floor("Ground_Wet", -6.5f, 3f, ShelfTop, false, true, 3f);
            Transform shelf = RoomBuildKit.Find(ctx.Room, "Shelf");
            if (shelf != null && shelf.TryGetComponent(out SpriteRenderer shelfSr)) shelfSr.enabled = false;

            // ---- The culvert's inside: cobbles in shadow, darker under the roof, lit stone edges at both mouths, a cool light.
            ctx.Hollow("Culvert", -4.5f, ShelfTop, 2f, RoofBottom, 0.5f, Cobbles);
            ctx.Hollow("Culvert Shade", -4.5f, RoofBottom - 0.35f, 2f, RoofBottom, 0.75f, Cobbles, 31);
            ctx.Hollow("Culvert West Edge", -4.5f, ShelfTop, -4.1f, RoofBottom, 0.3f, RoomDresser.Le, 32);
            ctx.Hollow("Culvert East Edge", 1.6f, ShelfTop, 2f, RoofBottom, 0.3f, RoomDresser.R, 32);
            ctx.Light("Culvert Light", new Vector3(-1.2f, ShelfTop + 0.5f, 0f), new Color(0.5f, 0.85f, 1f), 0.6f, 3f);

            // ---- The stop block hides behind the shelf's stone and shows only once it pops up into the culvert.
            Transform stop = RoomBuildKit.Find(ctx.Room, "Gate_Stop");
            if (stop != null)
                foreach (SpriteRenderer sr in stop.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    sr.sortingLayerName = "BackDecor";
                    sr.sortingOrder = 35; // in front of the culvert's hollow (30-32), behind the shelf (Midground)
                    sr.color = new Color(0.85f, 0.8f, 0.72f);
                }

            // ---- Wet stone: glints along the shelf, drips from the culvert roof.
            GameObject wet = new GameObject("Wet_Surface" + RoomDresser.VisualTag);
            wet.transform.SetParent(ctx.Decor, false);
            wet.AddComponent<WetSurfaceFx>().EditorSetup(fx, -6.5f, 3f, ShelfTop,
                new[] { new Vector2(-3.4f, RoofBottom - 0.05f), new Vector2(-1.6f, RoofBottom - 0.05f), new Vector2(0.4f, RoofBottom - 0.05f) });

            // ---- Exit light: on while the ram rests on the brake plate (the plate drives the gate directly).
            Transform plateT = RoomBuildKit.Find(ctx.Room, "Plate_Brake_80N"), gate = RoomBuildKit.Find(ctx.Room, "Gate_Exit");
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

            // ---- The acid: a stronger glow over the pit so it reads as a hazard, not an empty gap.
            ctx.Light("Acid Glow", new Vector3(4.5f, Floor - 0.2f, 0f), new Color(0.55f, 1f, 0.35f), 1.1f, 3f);

            // ---- Plants and moss.
            ctx.Hanging("TX Village Props - Grass 08", -4.3f, RoofBottom);     // culvert, west mouth
            ctx.Hanging("TX Village Props - Grass 07", 1.8f, RoofBottom);      // culvert, east mouth
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 03", -3f, RoofTop); // tufts on the culvert roof
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 05", 0.2f, RoofTop);
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 02", 7.4f, Floor);

            // ---- Background: the old mill race. (No back-wall pillars: dimmed tiles with grass tops read as ledges.)
            ctx.BackProp("TX Village Props - Wheelbarrow Wheel", 6.3f, Floor, 0.55f); // an old cart wheel, resting on the east floor
            ctx.BackProp("TX Village Props - Barrel", 6.9f, Floor, 0.5f);
            ctx.BackProp("TX Village Props - Rock 01", 9.6f, Floor, 0.5f);
            ctx.Lamp(7.6f, Floor);
        }, "Shelf");
        if (ok) Debug.Log("Room 7 visual pass applied to " + Room7Path + ".");
    }
}
