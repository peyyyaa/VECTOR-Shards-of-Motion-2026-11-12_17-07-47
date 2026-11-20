using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Wind Tether visuals (Player 2). Reads WindTether and only draws; it never changes physics.
/// - Wind ribbon: a wavy line of pixel dashes from P2's hand to the object. The dashes flow TOWARD P2,
///   showing the direction of the pull (a continuous force toward the player).
/// - Wisps drifting along the ribbon toward P2.
/// - Attach: a "snap" ring on the object, which glows softly (cyan light) while held.
/// - Release: the ribbon breaks up into wisps. Miss (nothing in range): a small puff at the hand.
/// The tether's own LineRenderer is hidden while this component is active.
/// </summary>
[RequireComponent(typeof(WindTether))]
public class TetherEffects : MonoBehaviour
{
    [SerializeField] private FxLibrary fx;
    [SerializeField] private Color color = new Color32(73, 194, 208, 255);
    [SerializeField] private Color lightColor = new Color32(191, 238, 242, 255);
    [SerializeField] private Vector2 handOffset = new Vector2(0.4f, 0.05f);
    [Tooltip("Wave height in units.")]
    [SerializeField] private float waveAmplitude = 0.12f;
    [Tooltip("How fast the dashes flow toward the player.")]
    [SerializeField] private float flowSpeed = 14f;
    [SerializeField] private float wispInterval = 0.07f;

    private const int MaxDots = 64;
    private const float DotSpacingPx = 3f;

    private WindTether tether;
    private PlayerMovement movement;
    private LineRenderer line;
    private readonly List<SpriteRenderer> dots = new List<SpriteRenderer>();
    private readonly List<Vector3> lastPoints = new List<Vector3>();
    private Transform ribbonRoot;
    private Light2D glow;
    private float wispTimer;

    private void Awake()
    {
        tether = GetComponent<WindTether>();
        movement = GetComponent<PlayerMovement>();
        line = GetComponentInChildren<LineRenderer>(true);
    }

    private void OnEnable()
    {
        tether.Attached += OnAttached;
        tether.Released += OnReleased;
        tether.Missed += OnMissed;
        if (line != null) line.forceRenderingOff = true;
    }

    private void OnDisable()
    {
        tether.Attached -= OnAttached;
        tether.Released -= OnReleased;
        tether.Missed -= OnMissed;
        if (line != null) line.forceRenderingOff = false;
        HideRibbon();
        if (glow != null) { Destroy(glow.gameObject); glow = null; }
    }

    private void OnDestroy()
    {
        if (ribbonRoot != null) Destroy(ribbonRoot.gameObject);
    }

    private Vector3 Hand => transform.position + new Vector3(handOffset.x * movement.FacingDirection, handOffset.y, 0f);

    private void LateUpdate()
    {
        Rigidbody2D target = tether.Target;
        if (target == null || fx == null) { HideRibbon(); return; }

        Vector3 a = Hand, b = target.worldCenterOfMass;
        DrawRibbon(a, b);

        wispTimer -= Time.deltaTime;
        if (wispTimer <= 0f)
        {
            wispTimer = wispInterval;
            Vector3 p = Vector3.Lerp(a, b, Random.Range(0.2f, 1f));
            Vector2 toPlayer = (a - b).normalized;
            Fx.Sprite(fx.wisp, p, new Color(lightColor.r, lightColor.g, lightColor.b, 0.8f), 0.35f, 3)
              .Move(toPlayer * Random.Range(2f, 3.5f) + Random.insideUnitCircle * 0.3f, 0f, 1f);
        }

        if (glow != null)
        {
            glow.transform.position = b;
            glow.intensity = 0.7f + 0.2f * Mathf.Sin(Time.time * 8f);
        }
    }

    private void DrawRibbon(Vector3 a, Vector3 b)
    {
        EnsureDots();
        Vector3 d = b - a;
        float length = d.magnitude;
        Vector3 along = length > 0.0001f ? d / length : Vector3.right;
        Vector3 perp = new Vector3(-along.y, along.x, 0f);
        int n = Mathf.Clamp(Mathf.RoundToInt(length * Fx.PixelsPerUnit / DotSpacingPx), 2, MaxDots);
        int shift = Mathf.FloorToInt(Time.time * flowSpeed);
        lastPoints.Clear();

        for (int i = 0; i < dots.Count; i++)
        {
            bool used = i < n;
            if (!used) { dots[i].enabled = false; continue; }
            float s = i / (float)(n - 1);
            // Wave rides along the ribbon toward the player; zero at both ends (pinned at hand and object).
            float wave = Mathf.Sin(s * length * 5f + Time.time * 9f) * waveAmplitude * Mathf.Sin(Mathf.PI * s);
            Vector3 p = a + d * s + perp * wave;
            lastPoints.Add(p);
            // Dashes: every 4th dot is a gap, and the gaps move toward index 0 (the player) as time passes.
            bool gap = ((i + shift) % 4) == 3;
            dots[i].enabled = !gap;
            dots[i].transform.position = Fx.Snap(p);
            dots[i].color = ((i + shift) % 4) == 0 ? lightColor : color;
        }
    }

    private void EnsureDots()
    {
        if (ribbonRoot == null)
        {
            // Not parented to the player: its 0.8 x 1.5 scale would stretch the dots.
            ribbonRoot = new GameObject($"{name} Tether Ribbon").transform;
        }
        while (dots.Count < MaxDots)
        {
            GameObject go = new GameObject("Dot");
            go.transform.SetParent(ribbonRoot, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = fx.dot;
            sr.sortingLayerName = "FX";
            sr.sortingOrder = 2;
            sr.enabled = false;
            dots.Add(sr);
        }
    }

    private void HideRibbon()
    {
        foreach (SpriteRenderer d in dots) if (d != null) d.enabled = false;
    }

    private void OnAttached(Rigidbody2D target)
    {
        if (fx == null) return;
        if (fx.ring.Length > 0) Fx.Frames(fx.ring, target.worldCenterOfMass, lightColor, 0.25f, 5).Grow(1f, 1.6f);
        Fx.Flash(Hand, color, 1.2f, 1f, 0.15f);
        if (glow == null)
        {
            GameObject g = new GameObject($"{name} Tether Glow");
            glow = g.AddComponent<Light2D>();
            glow.lightType = Light2D.LightType.Point;
            glow.color = lightColor;
            glow.pointLightOuterRadius = 1.3f;
            glow.pointLightInnerRadius = 0.2f;
        }
        glow.transform.position = target.worldCenterOfMass;
        glow.intensity = 0.8f;
    }

    private void OnReleased(Rigidbody2D target)
    {
        if (glow != null) { Destroy(glow.gameObject); glow = null; }
        if (fx == null) return;
        for (int i = 0; i < lastPoints.Count; i += 4) // the ribbon breaks up into wisps
            Fx.Sprite(fx.wisp, lastPoints[i], lightColor, Random.Range(0.25f, 0.4f), 3).Move(Random.insideUnitCircle * 1.2f, -0.5f, 2f);
        HideRibbon();
    }

    private void OnMissed()
    {
        if (fx == null) return;
        int dir = movement.FacingDirection;
        for (int i = 0; i < 5; i++)
            Fx.Sprite(fx.wisp, Hand, lightColor, Random.Range(0.2f, 0.3f), 3).Move(new Vector2(dir * Random.Range(1.5f, 2.5f), Random.Range(-1f, 1f)), 0f, 6f);
    }

#if UNITY_EDITOR
    public void EditorSetup(FxLibrary library) => fx = library;
#endif
}
