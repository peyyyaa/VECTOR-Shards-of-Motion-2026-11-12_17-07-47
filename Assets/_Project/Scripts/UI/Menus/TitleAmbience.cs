using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Title screen life: the big Motion Shard bobs above its pedestal with a pulsing light, light shafts breathe slowly,
/// and glowing motes (cyan and warm) drift up through the forest. Purely visual; uses unscaled time.
/// </summary>
public class TitleAmbience : MonoBehaviour
{
    [SerializeField] private Transform shard;
    [SerializeField] private Light2D shardLight;
    [SerializeField] private SpriteRenderer[] shafts = new SpriteRenderer[0];
    [SerializeField] private FxLibrary fx;
    [Tooltip("Where motes can appear (world units).")]
    [SerializeField] private Rect moteArea = new Rect(-10f, -5.6f, 20f, 6f);
    [SerializeField] private float motesPerSecond = 3f;

    private Vector3 shardHome;
    private float[] shaftAlpha;
    private float moteTimer;

    private void Start()
    {
        if (shard != null) shardHome = shard.position;
        shaftAlpha = new float[shafts.Length];
        for (int i = 0; i < shafts.Length; i++) if (shafts[i] != null) shaftAlpha[i] = shafts[i].color.a;
    }

    private void Update()
    {
        float time = Time.unscaledTime;
        if (shard != null)
        {
            Vector3 p = shardHome + Vector3.up * (Mathf.Sin(time * 1.6f) * 0.18f);
            shard.position = Fx.Snap(p);
        }
        if (shardLight != null) shardLight.intensity = 1.2f + 0.35f * Mathf.Sin(time * 2.2f);
        for (int i = 0; i < shafts.Length; i++)
        {
            if (shafts[i] == null) continue;
            Color c = shafts[i].color;
            c.a = shaftAlpha[i] * (0.75f + 0.25f * Mathf.Sin(time * 0.5f + i * 1.7f));
            shafts[i].color = c;
        }

        if (fx == null || fx.sparkle == null) return;
        moteTimer -= Time.unscaledDeltaTime;
        if (moteTimer > 0f) return;
        moteTimer = 1f / motesPerSecond;
        Vector3 at = new Vector3(Random.Range(moteArea.xMin, moteArea.xMax), Random.Range(moteArea.yMin, moteArea.yMax), 0f);
        Color tint = Random.value < 0.6f ? new Color(0.6f, 0.95f, 1f, 0.8f) : new Color(1f, 0.9f, 0.6f, 0.7f);
        Fx.Sprite(fx.sparkle, at, tint, Random.Range(2.5f, 4.5f), 2).Move(new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(0.25f, 0.6f)));
    }

#if UNITY_EDITOR
    public void EditorSetup(Transform s, Light2D l, SpriteRenderer[] beams, FxLibrary library) { shard = s; shardLight = l; shafts = beams; fx = library; }
#endif
}
