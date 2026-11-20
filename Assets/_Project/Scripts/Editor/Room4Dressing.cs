using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Shards of Motion > Room 4 > 2. Apply Visual Pass.
/// RoomDresser does the graybox automatically (aqueduct ledge, step, nook, rubble walls, the water channel's current);
/// this adds Room 4's decoration: "Broken Crossing" - a collapsed aqueduct, wood and teal, the old sluice wheel.
/// Room-local coordinates: floor top y = -4.5, aqueduct ledge top y = -1.5 (see Room4Builder).
/// </summary>
public static class Room4Dressing
{
    private const string Room4Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room04.prefab";

    [MenuItem("Tools/Shards of Motion/Room 4/2. Apply Visual Pass")]
    public static void Apply()
    {
        bool ok = RoomDresser.Dress(Room4Path, ctx =>
        {
            // The sluice wheel beside its lever (decoration; the lever is what you use).
            ctx.Prop(ctx.Village, "TX Village Props - Wheelbarrow Wheel", -3.3f, -1.5f, "Props", -1);

            // Background: the old aqueduct town.
            ctx.BackProp("TX Village Props - Well", 4.6f, -4.5f, 0.55f);          // between the walls
            ctx.BackProp("TX Village Props - Barrel", 9.2f, -4.5f, 0.5f);         // past the second wall
            ctx.BackProp("TX Village Props - Wood Logs", -0.2f, -4.5f, 0.55f);

            ctx.Lamp(5.8f, -4.5f);

            // Plants: tufts on the ledge and nook, moss hanging under them.
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 04", -4.1f, -1.5f);
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 01", 9.2f, -2f);       // nook
            ctx.Hanging("TX Village Props - Grass 08", -4.2f, -2f);                // under the aqueduct ledge
            ctx.Hanging("TX Village Props - Grass 07", 8.5f, -2.4f);               // under the nook
            ctx.Prop(ctx.Village, "TX Village Props - Rock 03", 3.4f, -4.5f, "Props", -1);
        });
        if (ok) Debug.Log("Room 4 visual pass applied to " + Room4Path + ".");
    }
}
