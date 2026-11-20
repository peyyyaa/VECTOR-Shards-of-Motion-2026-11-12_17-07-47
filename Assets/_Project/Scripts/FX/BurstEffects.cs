using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kinetic Burst visuals (Player 1). Listens to KineticBurst.Fired and only draws; it never changes physics.
/// - Shockwave arc from the player, drawn at the burst's real range and cone, so players learn its reach
///   (shown on a miss too).
/// - Hand flash (orange light).
/// - For each object hit: sparks, an impact ring, and a short arrow showing the impulse's direction.
/// - Recoil dust behind the feet when the burst pushes the player back (Newton's 3rd law).
/// - Optional 1-pixel camera kick (off by default: on a shared screen it also shakes the other player's view).
/// </summary>
[RequireComponent(typeof(KineticBurst))]
public class BurstEffects : MonoBehaviour
{
    [SerializeField] private FxLibrary fx;
    [SerializeField] private Color color = new Color32(232, 137, 58, 255);
    [SerializeField] private Color hotColor = new Color32(255, 230, 150, 255);
    [Tooltip("Where the hand is, relative to the player's centre (x is flipped with facing).")]
    [SerializeField] private Vector2 handOffset = new Vector2(0.4f, 0.05f);
    [SerializeField] private bool cameraKick = false;
    [SerializeField, Min(1)] private int cameraKickPixels = 1;

    private KineticBurst burst;
    private PlayerMovement movement;
    private Collider2D body;

    private void Awake()
    {
        burst = GetComponent<KineticBurst>();
        movement = GetComponent<PlayerMovement>();
        body = GetComponent<Collider2D>();
    }

    private void OnEnable() => burst.Fired += OnFired;
    private void OnDisable() => burst.Fired -= OnFired;

    private void OnFired(Vector2 origin, int dir, IReadOnlyList<KineticBurst.Hit> hits)
    {
        if (fx == null) return;
        Vector3 hand = (Vector3)origin + new Vector3(handOffset.x * dir, handOffset.y, 0f);

        if (fx.shockwave.Length > 0) Fx.Frames(fx.shockwave, origin, Color.white, 0.22f, 5).Flip(dir < 0);
        Fx.Flash(hand, color, 1.8f, 1.6f, 0.15f);
        for (int i = 0; i < 3; i++) // a few sparks from the hand even on a miss
            Fx.Sprite(fx.spark, hand, hotColor, 0.2f, 6).Move(new Vector2(dir * Random.Range(2f, 4f), Random.Range(-1.5f, 1.5f)), 0f, 6f);

        foreach (KineticBurst.Hit hit in hits)
        {
            if (hit.body == null) continue;
            Vector3 center = hit.body.worldCenterOfMass;
            Vector3 face = center - (Vector3)(hit.direction * 0.45f); // roughly the side the burst struck

            for (int i = 0; i < 7; i++)
            {
                float a = Mathf.Atan2(-hit.direction.y, -hit.direction.x) + Random.Range(-1.2f, 1.2f); // spray back toward P1
                Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(2f, 4.5f);
                Fx.Sprite(fx.spark, face, Color.Lerp(hotColor, color, Random.value), Random.Range(0.25f, 0.4f), 6).Move(v, 9f, 2f);
            }
            if (fx.ring.Length > 0) Fx.Frames(fx.ring, face, color, 0.2f, 5);
            float angle = Mathf.Atan2(hit.direction.y, hit.direction.x) * Mathf.Rad2Deg;
            Fx.Sprite(fx.arrow, center + (Vector3)(hit.direction * 0.3f), color, 0.45f, 7).Rotate(angle).Move(hit.direction * 1.5f, 0f, 3f);
        }

        if (hits.Count > 0 && movement.IsGrounded && fx.dust.Length > 0)
        {
            Vector3 feet = new Vector3(body.bounds.center.x - dir * 0.2f, body.bounds.min.y + 0.1f, 0f);
            for (int i = 0; i < 3; i++)
                Fx.Frames(fx.dust, feet, Color.white, Random.Range(0.3f, 0.45f), 4).Move(new Vector2(-dir * Random.Range(0.6f, 1.4f), Random.Range(0.1f, 0.5f)), 0f, 3f);
        }

        if (cameraKick) FxCameraKick.Kick(new Vector2(dir * cameraKickPixels / Fx.PixelsPerUnit, 0f), 0.06f);
    }

#if UNITY_EDITOR
    public void EditorSetup(FxLibrary library) => fx = library;
#endif
}
