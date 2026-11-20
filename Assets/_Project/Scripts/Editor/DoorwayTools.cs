using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Inspector button for Doorway ("Apply Opening"), plus the one-time setup that makes Doorway_Side adjustable:
/// Tools > Shards of Motion > Rooms > Set Up Adjustable Doorway (adds the Doorway component, a Landing collider on
/// the Ground layer and a Landing art holder, and wires the existing parts). Existing doorways keep working:
/// with Opening Bottom 0 nothing changes.
/// </summary>
[CustomEditor(typeof(Doorway))]
public class DoorwayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("After changing the opening, press Apply. A raised opening gets a stone landing under it: " +
                                "make sure the room you leave has a way up to it.", MessageType.Info);
        if (GUILayout.Button("Apply Opening"))
        {
            foreach (Object o in targets)
            {
                Doorway d = (Doorway)o;
                Undo.RegisterFullObjectHierarchyUndo(d.gameObject, "Apply Doorway Opening");
                d.Apply();
                foreach (Component c in d.GetComponentsInChildren<Component>(true))
                    if (c != null && PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                EditorUtility.SetDirty(d);
                if (d.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(d.gameObject.scene);
            }
        }
    }
}

public static class DoorwayTools
{
    private const string DoorwayPrefab = "Assets/_Project/Prefabs/Rooms/Doorway_Side.prefab";
    private const string RuinTexture = "Assets/_Project/Art/Recolors/Ground_RuinStone.png";

    [MenuItem("Tools/Shards of Motion/Rooms/Set Up Adjustable Doorway")]
    public static void SetUp()
    {
        Sprite[] all = AssetDatabase.LoadAllAssetsAtPath(RuinTexture).OfType<Sprite>().ToArray();
        Sprite Tile(int n) => all.FirstOrDefault(s => s.name == $"TX Tileset Ground_{n}");
        Sprite[] stone = { Tile(RoomDresser.TL), Tile(RoomDresser.T), Tile(RoomDresser.TR), Tile(RoomDresser.Le), Tile(RoomDresser.F), Tile(RoomDresser.R) };
        if (stone.Any(s => s == null)) { Debug.LogError("Doorway: Ruin Stone tiles not found. Run Recolor > Build All first."); return; }

        GameObject root = PrefabUtility.LoadPrefabContents(DoorwayPrefab);
        try
        {
            Transform Find(string n) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == n);
            Transform wall = Find("Wall_Above"), barrier = Find("PlayerBarrier");
            if (wall == null || barrier == null) { Debug.LogError("Doorway: Doorway_Side is missing Wall_Above or PlayerBarrier."); return; }

            Transform landing = Find("Landing");
            if (landing == null)
            {
                landing = new GameObject("Landing").transform;
                landing.SetParent(root.transform, false);
                landing.gameObject.layer = wall.gameObject.layer; // Ground: players stand on it, blocks rest on it
                landing.gameObject.AddComponent<BoxCollider2D>();
                landing.gameObject.SetActive(false);
            }
            Transform landingArt = Find("Landing_Art");
            if (landingArt == null)
            {
                landingArt = new GameObject("Landing_Art").transform; // not under Landing: its scale would stretch the tiles
                landingArt.SetParent(root.transform, false);
                landingArt.gameObject.SetActive(false);
            }

            Transform glow = Find("Exit_Glow");
            if (glow == null)
            {
                glow = new GameObject("Exit_Glow").transform;
                glow.SetParent(root.transform, false);
                UnityEngine.Rendering.Universal.Light2D l = glow.gameObject.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
                l.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
                l.color = new Color(0.45f, 0.9f, 1f);
                l.intensity = 0.9f;
                l.pointLightOuterRadius = 2.6f;
                l.pointLightInnerRadius = 0.3f;
            }

            Doorway d = root.GetComponent<Doorway>();
            if (d == null) d = root.AddComponent<Doorway>();
            BoxCollider2D[] exits = root.GetComponentsInChildren<RoomExit>(true).Select(e => e.GetComponent<BoxCollider2D>()).Where(c => c != null).ToArray();
            Transform[] arrivals = root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Arrive_")).ToArray();
            d.EditorSetup(wall, Find("Wall_Above_Visual"), barrier.GetComponent<BoxCollider2D>(), exits, arrivals, landing, landingArt, stone, glow);
            d.Apply(); // opening bottom 0: same layout as before
            PrefabUtility.SaveAsPrefabAsset(root, DoorwayPrefab);
            Debug.Log($"Doorway: Doorway_Side is adjustable ({exits.Length} exit zones, {arrivals.Length} arrival points). " +
                      "Select a doorway under Connections, set Opening Bottom, press Apply Opening.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
