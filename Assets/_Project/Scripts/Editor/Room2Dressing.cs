using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Shards of Motion > Room 2 > 2. Apply Visual Pass.
/// RoomDresser dresses everything from the graybox automatically; this file only adds Room 2's decoration:
/// "Weight of Things" - an old testing hall: darkened Village furniture in the background, a lamp between the pedestals,
/// moss under the high ledges, a few tufts and rocks. Nothing sits on a puzzle path. Edit and re-run freely.
/// Room-local coordinates: floor top y = -4.5 (see Room2Builder for the layout).
/// </summary>
public static class Room2Dressing
{
    private const string Room2Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room02.prefab";

    [MenuItem("Tools/Shards of Motion/Room 2/2. Apply Visual Pass")]
    public static void Apply()
    {
        bool ok = RoomDresser.Dress(Room2Path, ctx =>
        {
            // Background (darkened Village props, behind the play space): the old testing hall's furniture.
            // No crates back here, so nobody mistakes them for the real ones.
            ctx.BackProp("TX Village Props - Billboard", -7.4f, -4.5f);          // notice board behind the test scale
            ctx.BackProp("TX Village Props - Barrel", -4.7f, -4.5f);             // under the high step
            ctx.BackProp("TX Village Props - Pot 01", -4.0f, -4.5f);
            ctx.BackProp("TX Village Props - Statue", 0.9f, -4.5f, 0.55f);       // behind the 3 kg crate's start
            ctx.BackProp("TX Village Props - Weapon Rack", 3.0f, -4.5f, 0.55f);  // behind the 55 N pedestal
            ctx.BackProp("TX Village Props - Training Dummy", 9.4f, -4.5f, 0.55f, flip: true); // past the gate

            // Lamp between the 55 N and 85 N pedestals.
            ctx.Lamp(4.0f, -4.5f);

            // Plants: tufts on ledges, moss hanging under the niche and the high step.
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 02", -8.4f, -4.5f);
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 05", -2.6f, -2.5f);   // low step, left of the crate
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 04", -4.4f, -0.5f);   // high step
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 01", -8.2f, 1.2f);    // niche roof
            ctx.Hanging("TX Village Props - Grass 08", -8.3f, -1.0f);             // under the niche shelf (above the doorway)
            ctx.Hanging("TX Village Props - Grass 07", -4.0f, -1.0f);             // under the high step

            // Rocks.
            ctx.Prop(ctx.Village, "TX Village Props - Rock 03", -3.6f, -4.5f);
            ctx.Prop(ctx.Village, "TX Village Props - Rock 02", 9.6f, -4.5f, "Props", -1); // behind the gate
        });
        if (ok) Debug.Log("Room 2 visual pass applied to " + Room2Path + ".");
    }
}
