using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Shards of Motion > Room 3 > 2. Apply Visual Pass.
/// RoomDresser dresses the graybox automatically (floors, the arch, the acid, the bridge deck, plate, shrine, sign);
/// this adds Room 3's decoration: "Together We Move" - a broken causeway under a mossy stone arch, a cargo ferry, green acid.
/// Darkened Village props in the background, plants on the arch and landing, moss hanging from the arch.
/// Nothing sits on a puzzle path. Room-local coordinates: floor top y = -4.5, arch top y = 0, exit landing top y = -1.5.
/// </summary>
public static class Room3Dressing
{
    private const string Room3Path = "Assets/_Project/Prefabs/Rooms/Region1/R1_Room03.prefab";

    [MenuItem("Tools/Shards of Motion/Room 3/2. Apply Visual Pass")]
    public static void Apply()
    {
        bool ok = RoomDresser.Dress(Room3Path, ctx =>
        {
            // Background: ruins of the old causeway town.
            ctx.BackProp("TX Village Props - Brick Wall", -7.2f, -4.5f);
            ctx.Prop(ctx.Village, "TX Village Props - Road Sign 01", 8.7f, -1.5f);      // signpost on the exit landing: "this way"
            ctx.BackProp("TX Village Props - Wood Logs", -9.6f, -4.5f, 0.55f);

            ctx.Lamp(-1.6f, -4.5f); // the ferry dock lamp

            // Plants.
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 02", -7.6f, -4.5f);
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 04", 0.6f, 0f);        // arch top
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 01", 3.2f, 0f);        // arch top
            ctx.Prop(ctx.Forest, "TX Village Props - Grass 05", 9.4f, -1.5f);     // exit landing
            ctx.Hanging("TX Village Props - Grass 08", -0.7f, -2.7f);              // under the arch, at the tunnel mouths
            ctx.Hanging("TX Village Props - Grass 07", 3.6f, -2.7f);

            ctx.Prop(ctx.Village, "TX Village Props - Rock 01", 1.1f, 0f, "Props", -1); // arch top
        });
        if (ok) Debug.Log("Room 3 visual pass applied to " + Room3Path + ".");
    }
}
