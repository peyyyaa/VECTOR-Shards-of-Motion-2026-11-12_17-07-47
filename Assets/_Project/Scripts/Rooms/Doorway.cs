using UnityEngine;

/// <summary>
/// Lays out a side doorway (Doorway_Side prefab) for an opening at any height, so exits don't always have to
/// be at floor level. Set Opening Bottom (metres above the room floor) and Opening Height, then press
/// "Apply Opening" in the Inspector (Doorway editor). It moves and sizes:
///   - the wall above the opening (and its art),
///   - both exit triggers and the player barrier (to the opening's height),
///   - the four arrival points (0.75 m above the opening bottom),
///   - a stone LANDING under a raised opening: solid from the floor up to the opening, wide enough that both
///     rooms' arrival points stand on it (Landing Half Width from the doorway centre). Players climb to it in
///     the room they leave and arrive on it in the next room.
/// The doorway's own position stays at floor level (y = -4.5 in Region 1). Layout only: nothing runs in play.
/// </summary>
public class Doorway : MonoBehaviour
{
    [Tooltip("Height of the bottom of the opening above the room floor, in metres. 0 = a floor-level doorway.")]
    [SerializeField, Min(0f)] private float openingBottom = 0f;
    [Tooltip("Height of the opening, in metres (players are 1.5 m tall).")]
    [SerializeField, Min(2f)] private float openingHeight = 4f;
    [Tooltip("How far the landing reaches into each room from the doorway centre (arrival points are at 2 and 3 m).")]
    [SerializeField, Min(1f)] private float landingHalfWidth = 3.5f;

    [Header("Parts (filled in by Tools > Shards of Motion > Rooms > Set Up Adjustable Doorway)")]
    [SerializeField] private Transform wallAbove;
    [SerializeField] private Transform wallAboveArt;
    [SerializeField] private BoxCollider2D barrier;
    [SerializeField] private BoxCollider2D[] exitZones = new BoxCollider2D[0];
    [SerializeField] private Transform[] arrivals = new Transform[0];
    [SerializeField] private Transform landing;
    [SerializeField] private Transform landingArt;
    [Tooltip("Soft light in the opening, so the way out is easy to spot.")]
    [SerializeField] private Transform exitGlow;
    [Tooltip("Ruin Stone tiles: top-left, top, top-right, left, fill, right.")]
    [SerializeField] private Sprite[] stone = new Sprite[6];

    private const float FloorThickness = 1f, DefaultHeight = 4f, WallAboveHeight = 6.5f;

    public float OpeningBottom => openingBottom;

    /// <summary>Lays everything out for the current settings (called by the Inspector button).</summary>
    public void Apply()
    {
        float b = openingBottom, h = openingHeight, mid = b + h * 0.5f;
        if (wallAbove != null) SetY(wallAbove, b + h + WallAboveHeight * 0.5f);
        if (wallAboveArt != null) SetY(wallAboveArt, b + (h - DefaultHeight)); // its art was built for a 4 m floor-level opening
        if (barrier != null) { SetY(barrier.transform, mid); barrier.size = new Vector2(barrier.size.x, h); }
        foreach (BoxCollider2D zone in exitZones)
            if (zone != null) { SetY(zone.transform, mid); zone.size = new Vector2(zone.size.x, h); }
        foreach (Transform a in arrivals)
            if (a != null) SetY(a, b + 0.75f);
        if (exitGlow != null) SetY(exitGlow, mid);

        if (landing != null)
        {
            bool raised = b > 0.01f;
            landing.gameObject.SetActive(raised);
            landing.localPosition = new Vector3(0f, (b - FloorThickness) * 0.5f, 0f);
            landing.localScale = new Vector3(landingHalfWidth * 2f, b + FloorThickness, 1f);
            if (landingArt != null)
            {
                landingArt.gameObject.SetActive(raised);
                if (raised) BuildLandingArt(-landingHalfWidth, landingHalfWidth, -FloorThickness, b);
            }
        }
    }

    private static void SetY(Transform t, float y) => t.localPosition = new Vector3(t.localPosition.x, y, t.localPosition.z);

    // Stone block art for the landing: grass-topped edge on top, stone fill below, rounded side caps.
    private void BuildLandingArt(float xMin, float xMax, float yMin, float yMax)
    {
        for (int i = landingArt.childCount - 1; i >= 0; i--)
        {
            GameObject child = landingArt.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
        if (stone == null || stone.Length < 6) return;
        float topRow = Mathf.Min(1f, yMax - yMin);
        float[,] cols = { { xMin, xMin + 1f }, { xMin + 1f, xMax - 1f }, { xMax - 1f, xMax } };
        for (int c = 0; c < 3; c++)
        {
            Piece(stone[c], cols[c, 0], yMax - topRow, cols[c, 1], yMax);                // TL, T, TR
            if (yMax - topRow > yMin) Piece(stone[3 + c], cols[c, 0], yMin, cols[c, 1], yMax - topRow); // L, F, R
        }
    }

    private void Piece(Sprite sprite, float x0, float y0, float x1, float y1)
    {
        if (sprite == null || x1 <= x0 || y1 <= y0) return;
        GameObject go = new GameObject("Piece");
        go.transform.SetParent(landingArt, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Midground";
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(x1 - x0, y1 - y0);
        // Tiled sprites stretch from their pivot: offset so the drawn area is centred on the piece.
        Vector2 pivot01 = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
        Vector2 centre = new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
        go.transform.localPosition = new Vector3(centre.x - (0.5f - pivot01.x) * sr.size.x, centre.y - (0.5f - pivot01.y) * sr.size.y, 0f);
    }

#if UNITY_EDITOR
    public void EditorSetup(Transform above, Transform aboveArt, BoxCollider2D playerBarrier, BoxCollider2D[] exits,
        Transform[] arrivalPoints, Transform landingBlock, Transform landingVisual, Sprite[] stoneTiles, Transform glow)
    {
        wallAbove = above; wallAboveArt = aboveArt; barrier = playerBarrier; exitZones = exits;
        arrivals = arrivalPoints; landing = landingBlock; landingArt = landingVisual; stone = stoneTiles; exitGlow = glow;
    }
#endif
}
