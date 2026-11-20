using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes a wet floor look wet (purely visual, no physics): small glints blink along the surface, and drops fall from
/// drip points (e.g. the underside of a culvert) and splash on the floor. Drops fall with the world's gravity
/// (Physics2D.gravity, 29.43 m/s^2 here), so they land in t = sqrt(2h / g), like everything else in the game.
/// Uses the FX library's sparkle and dot sprites. Added by a room's visual pass (Room 5: the wet causeway).
/// </summary>
public class WetSurfaceFx : MonoBehaviour
{
    [SerializeField] private FxLibrary fx;
    [Tooltip("X range of the wet surface and its top y, local to this object (the room's own coordinates).")]
    [SerializeField] private float xMin = -4.5f, xMax = 5.6f, surfaceY = -4.5f;
    [Tooltip("Points drops fall from, local to this object.")]
    [SerializeField] private Vector2[] dripPoints = new Vector2[0];
    [SerializeField] private Vector2 glintInterval = new Vector2(0.2f, 0.6f);
    [SerializeField] private Vector2 dripInterval = new Vector2(1.2f, 2.8f);

    private static readonly Color Glint = new Color(0.8f, 0.97f, 1f, 0.9f), Drop = new Color(0.6f, 0.85f, 1f, 0.9f);
    private readonly List<(float at, Vector2 where)> splashes = new List<(float, Vector2)>();
    private float glintTimer, dripTimer;

    private void Update()
    {
        if (fx == null) return;
        glintTimer -= Time.deltaTime;
        if (glintTimer <= 0f && fx.sparkle != null)
        {
            glintTimer = Random.Range(glintInterval.x, glintInterval.y);
            Vector3 p = transform.TransformPoint(new Vector3(Random.Range(xMin, xMax), surfaceY + 0.06f, 0f));
            Fx.Sprite(fx.sparkle, p, Glint, Random.Range(0.4f, 0.8f), 2).Grow(0.35f, 0.6f).FadeInOut();
        }

        dripTimer -= Time.deltaTime;
        if (dripTimer <= 0f && dripPoints.Length > 0 && fx.dot != null)
        {
            dripTimer = Random.Range(dripInterval.x, dripInterval.y);
            Vector2 local = dripPoints[Random.Range(0, dripPoints.Length)];
            Vector2 from = transform.TransformPoint(local);
            float g = Mathf.Abs(Physics2D.gravity.y), h = local.y - surfaceY;
            float fall = h > 0f && g > 0f ? Mathf.Sqrt(2f * h / g) : 0.1f;
            Fx.Sprite(fx.dot, from, Drop, fall, 3).Move(Vector2.zero, g);
            splashes.Add((Time.time + fall, transform.TransformPoint(new Vector3(local.x, surfaceY, 0f))));
        }

        for (int i = splashes.Count - 1; i >= 0; i--)
        {
            if (Time.time < splashes[i].at) continue;
            Vector2 at = splashes[i].where;
            splashes.RemoveAt(i);
            for (int s = -1; s <= 1; s += 2)
                Fx.Sprite(fx.dot, new Vector3(at.x, at.y + 0.05f, 0f), Drop, 0.22f, 3).Move(new Vector2(s * Random.Range(0.8f, 1.4f), 2.4f), Mathf.Abs(Physics2D.gravity.y)).Grow(0.8f, 0.5f);
        }
    }

#if UNITY_EDITOR
    public void EditorSetup(FxLibrary library, float left, float right, float top, Vector2[] drips)
    {
        fx = library; xMin = left; xMax = right; surfaceY = top; dripPoints = drips;
    }
#endif
}
