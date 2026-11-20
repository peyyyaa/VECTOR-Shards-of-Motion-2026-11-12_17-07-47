using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tiny effect toolkit: short-lived sprites (sparks, dust, rings, after-images) and light flashes.
/// Effects are purely visual: no colliders, no physics. They are drawn on the FX sorting layer,
/// snapped to the 32-PPU pixel grid so they stay crisp, and destroy themselves when done.
///
/// Usage:  Fx.Sprite(sprite, position, color, lifetime).Move(velocity, gravity).Grow(1f, 1.5f);
/// </summary>
public static class Fx
{
    public const float PixelsPerUnit = 32f;

    public static FxSprite Sprite(Sprite sprite, Vector3 position, Color color, float lifetime, int order = 0)
        => Frames(new[] { sprite }, position, color, lifetime, order);

    /// <summary>Plays the frames once over the lifetime.</summary>
    public static FxSprite Frames(Sprite[] frames, Vector3 position, Color color, float lifetime, int order = 0)
    {
        GameObject go = new GameObject("FX");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "FX";
        sr.sortingOrder = order;
        FxSprite fx = go.AddComponent<FxSprite>();
        fx.Init(sr, frames, position, color, lifetime);
        return fx;
    }

    /// <summary>A point light that fades out (URP 2D).</summary>
    public static void Flash(Vector3 position, Color color, float radius, float intensity, float lifetime)
    {
        GameObject go = new GameObject("FX Light");
        go.transform.position = position;
        Light2D light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * 0.2f;
        light.intensity = intensity;
        go.AddComponent<FxLight>().Init(light, intensity, lifetime);
    }

    public static Vector3 Snap(Vector3 p)
        => new Vector3(Mathf.Round(p.x * PixelsPerUnit) / PixelsPerUnit, Mathf.Round(p.y * PixelsPerUnit) / PixelsPerUnit, p.z);
}

/// <summary>One short-lived effect sprite (see Fx). Moves, falls, grows and fades over its lifetime.</summary>
public class FxSprite : MonoBehaviour
{
    private SpriteRenderer sr;
    private Sprite[] frames;
    private Color color;
    private float lifetime, age;
    private Vector3 position;
    private Vector2 velocity;
    private float gravity, drag;
    private float startScale = 1f, endScale = 1f;
    private bool fade = true;
    private Transform follow;
    private Vector3 followOffset;

    public void Init(SpriteRenderer renderer, Sprite[] spriteFrames, Vector3 pos, Color tint, float life)
    {
        sr = renderer;
        frames = spriteFrames;
        color = tint;
        lifetime = Mathf.Max(0.01f, life);
        position = pos;
        Apply();
    }

    public FxSprite Move(Vector2 v, float gravityAccel = 0f, float dragPerSecond = 0f) { velocity = v; gravity = gravityAccel; drag = dragPerSecond; return this; }
    public FxSprite Grow(float from, float to) { startScale = from; endScale = to; Apply(); return this; }
    public FxSprite Rotate(float degrees) { transform.rotation = Quaternion.Euler(0f, 0f, degrees); return this; }
    public FxSprite Flip(bool flipX) { sr.flipX = flipX; return this; }
    public FxSprite Hold() { fade = false; return this; }
    public FxSprite Follow(Transform target) { follow = target; followOffset = position - (target != null ? target.position : Vector3.zero); return this; }
    public FxSprite Order(string layer, int order) { sr.sortingLayerName = layer; sr.sortingOrder = order; return this; }

    private void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime) { Destroy(gameObject); return; }

        velocity += Vector2.down * gravity * Time.deltaTime;
        velocity *= Mathf.Max(0f, 1f - drag * Time.deltaTime);
        if (follow != null) position = follow.position + followOffset + (Vector3)(velocity * age);
        else position += (Vector3)(velocity * Time.deltaTime);
        Apply();
    }

    private void Apply()
    {
        if (sr == null || frames == null || frames.Length == 0) return;
        float t = Mathf.Clamp01(age / lifetime);
        sr.sprite = frames[Mathf.Min(frames.Length - 1, (int)(t * frames.Length))];
        Color c = color;
        if (fade) c.a *= 1f - t * t; // holds longer, then fades quickly
        sr.color = c;
        float s = Mathf.Lerp(startScale, endScale, t);
        transform.localScale = new Vector3(s, s, 1f);
        transform.position = Fx.Snap(position);
    }
}

/// <summary>Fades a Light2D out and removes it (see Fx.Flash).</summary>
public class FxLight : MonoBehaviour
{
    private Light2D light2D;
    private float start, lifetime, age;

    public void Init(Light2D light, float intensity, float life)
    {
        light2D = light;
        start = intensity;
        lifetime = Mathf.Max(0.01f, life);
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime) { Destroy(gameObject); return; }
        light2D.intensity = start * (1f - age / lifetime);
    }
}

/// <summary>
/// Optional tiny camera kick (Kinetic Burst "Camera Kick"). Each frame of the kick, the camera is offset
/// after all other camera scripts have run and put back right after the frame is drawn, so it never drifts
/// and never fights the room camera.
/// </summary>
[DefaultExecutionOrder(10000)]
public class FxCameraKick : MonoBehaviour
{
    private Vector2 kick;
    private float remaining;
    private readonly WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();

    public static void Kick(Vector2 offset, float duration)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        FxCameraKick k = cam.GetComponent<FxCameraKick>();
        if (k == null) k = cam.gameObject.AddComponent<FxCameraKick>();
        k.kick = offset;
        k.remaining = duration;
    }

    private void LateUpdate()
    {
        if (remaining <= 0f) return;
        remaining -= Time.deltaTime;
        StartCoroutine(OffsetThisFrame(new Vector3(kick.x, kick.y, 0f)));
    }

    private System.Collections.IEnumerator OffsetThisFrame(Vector3 offset)
    {
        transform.position += offset;
        yield return endOfFrame;
        transform.position -= offset;
    }
}
