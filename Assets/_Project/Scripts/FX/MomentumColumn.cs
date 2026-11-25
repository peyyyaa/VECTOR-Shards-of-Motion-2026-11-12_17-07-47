using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The Momentum Column beside the Heart Engine (finale, part 2): a tall glass tube whose glowing liquid rises with the
/// momentum stored in the flywheel, so players can watch it build. Each hit makes the surface slosh and sends bubbles
/// up; notches (drawn on the glass) mark the engine's stages; near the top it glows brighter and pulses, and when full
/// it overflows in a burst of light. Purely visual: reads MomentumEngine.Momentum / MaxMomentum.
/// Setup: Fill = a SpriteRenderer with a bottom-pivot 1-pixel-wide sprite (scaled to the liquid height); Surface = the
/// bright line on top; Inner Height = the glass's inside height (m); a Light2D that follows the surface.
/// </summary>
public class MomentumColumn : MonoBehaviour
{
    [SerializeField] private MomentumEngine engine;
    [SerializeField] private SpriteRenderer fill;
    [SerializeField] private SpriteRenderer surface;
    [SerializeField] private Light2D glow;
    [SerializeField, Min(0.1f)] private float innerHeight = 5f;
    [SerializeField, Min(0.01f)] private float innerWidth = 0.4f;
    [SerializeField] private Color lowColor = new Color(0.2f, 0.55f, 0.75f, 0.9f);
    [SerializeField] private Color highColor = new Color(0.55f, 0.97f, 1f, 1f);
    [SerializeField] private FxLibrary fx;

    private float shown;     // 0..1, catches up with the engine quickly
    private float slosh;
    private float lastMomentum;
    private bool overflowed;

    private void Update()
    {
        if (engine == null || fill == null) return;
        float target = engine.Installed ? Mathf.Clamp01(engine.Momentum / engine.MaxMomentum) : 0f;
        if (engine.Momentum > lastMomentum + 0.5f)
        {
            slosh = Mathf.Min(0.25f, slosh + (engine.Momentum - lastMomentum) * 0.01f);
            Bubbles(6);
        }
        lastMomentum = engine.Momentum;
        shown = Mathf.MoveTowards(shown, target, Mathf.Max(0.3f, Mathf.Abs(target - shown) * 5f) * Time.deltaTime);
        slosh = Mathf.MoveTowards(slosh, 0f, 0.3f * Time.deltaTime);

        float h = shown * innerHeight + Mathf.Sin(Time.time * 14f) * slosh * shown;
        Vector3 bottom = transform.position;
        fill.transform.position = bottom;
        fill.transform.localScale = new Vector3(innerWidth * 32f, Mathf.Max(0f, h) * 32f, 1f); // the sprite is 1 px = 1/32 m
        Color c = Color.Lerp(lowColor, highColor, shown);
        if (shown > 0.85f) c = Color.Lerp(c, Color.white, 0.25f + 0.25f * Mathf.Sin(Time.time * 6f));
        fill.color = c;
        fill.enabled = h > 0.01f;
        if (surface != null)
        {
            surface.enabled = fill.enabled;
            surface.transform.position = bottom + Vector3.up * h;
            surface.color = new Color(1f, 1f, 1f, 0.85f);
        }
        if (glow != null)
        {
            glow.enabled = h > 0.05f;
            glow.transform.position = bottom + Vector3.up * h;
            glow.intensity = 0.4f + 1.4f * shown;
        }
        if (Random.value < shown * 2f * Time.deltaTime) Bubbles(1);

        if (target >= 0.999f && !overflowed)
        {
            overflowed = true;
            Vector3 top = bottom + Vector3.up * innerHeight;
            Fx.Flash(top, new Color(0.6f, 0.97f, 1f), 5f, 3f, 1f);
            Bubbles(24, burst: true);
        }
        else if (target < 0.9f) overflowed = false;
    }

    private void Bubbles(int n, bool burst = false)
    {
        if (fx == null || fx.dot == null) return;
        float h = shown * innerHeight;
        for (int i = 0; i < n; i++)
        {
            Vector3 p = transform.position + new Vector3(Random.Range(-innerWidth * 0.4f, innerWidth * 0.4f), Random.Range(0f, Mathf.Max(0.1f, h)), 0f);
            Vector2 v = burst ? new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(2f, 5f)) : new Vector2(0f, Random.Range(0.6f, 1.4f));
            float life = burst ? 0.8f : Mathf.Clamp((h - (p.y - transform.position.y)) / Mathf.Max(0.1f, v.y), 0.2f, 1.5f);
            Fx.Sprite(fx.dot, p, new Color(0.85f, 1f, 1f, 0.9f), life, 9).Move(v, burst ? 6f : 0f);
        }
    }

#if UNITY_EDITOR
    public void EditorSetup(MomentumEngine target, SpriteRenderer fillRenderer, SpriteRenderer surfaceRenderer, Light2D light, float height, float width, FxLibrary library)
    {
        engine = target; fill = fillRenderer; surface = surfaceRenderer; glow = light; innerHeight = height; innerWidth = width; fx = library;
    }
#endif
}
