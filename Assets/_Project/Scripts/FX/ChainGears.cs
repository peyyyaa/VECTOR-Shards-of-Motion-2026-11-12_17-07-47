using UnityEngine;

/// <summary>
/// Turns the chain lift's sprockets (finale, part 2) with its chain: every metre of chain that runs over a sprocket of
/// radius r turns it by 1/r radians (rolling without slipping). The chain runs clockwise (up the shaft, over the top, down
/// the back), so the sprockets turn clockwise. Purely visual: reads ChainLift.Travelled.
/// Setup: the ChainLift; the sprocket transforms (their pivots at the centre); their radius in metres.
/// </summary>
public class ChainGears : MonoBehaviour
{
    [SerializeField] private ChainLift lift;
    [SerializeField] private Transform[] gears = new Transform[0];
    [SerializeField, Min(0.01f)] private float radius = 0.5f;

    private float last;

    private void Start()
    {
        if (lift != null) last = lift.Travelled;
    }

    private void LateUpdate()
    {
        if (lift == null) return;
        float run = lift.Travelled - last;
        last = lift.Travelled;
        if (Mathf.Abs(run) < 0.0001f) return;
        float degrees = -run / radius * Mathf.Rad2Deg; // clockwise
        foreach (Transform g in gears) if (g != null) g.Rotate(0f, 0f, degrees);
    }

#if UNITY_EDITOR
    public void EditorSetup(ChainLift chainLift, Transform[] sprockets, float sprocketRadius)
    {
        lift = chainLift; gears = sprockets; radius = sprocketRadius;
    }
#endif
}
