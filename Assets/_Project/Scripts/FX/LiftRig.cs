using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The visible machinery of a BalanceLift (purely visual, no physics): the pulley wheel turns as the pans move (one turn per
/// 2·π·radius of rope), each pan's chain runs from the pan up to the wheel and lengthens or shortens as it travels, and a
/// brake lamp on the wheel shows red while braked, green while released. Added by a room's visual pass (Room 6).
/// </summary>
public class LiftRig : MonoBehaviour
{
    [SerializeField] private BalanceLift lift;
    [SerializeField] private Transform wheel;
    [Tooltip("Wheel radius in metres (sets how fast it turns for the rope it pays out).")]
    [SerializeField, Min(0.1f)] private float radius = 0.75f;
    [Tooltip("Chains (tiled sprites, centre pivot): pan A's and pan B's, hanging at these x positions (world).")]
    [SerializeField] private SpriteRenderer chainA;
    [SerializeField] private SpriteRenderer chainB;
    [SerializeField] private Light2D brakeLamp;

    private static readonly Color Braked = new Color(1f, 0.3f, 0.2f), Released = new Color(0.35f, 1f, 0.45f);

    private void LateUpdate()
    {
        if (lift == null || wheel == null) return;
        // Pan A rising pulls its (left) side of the rope up: the wheel turns clockwise.
        wheel.localRotation = Quaternion.Euler(0f, 0f, -lift.Height / radius * Mathf.Rad2Deg);
        float top = wheel.position.y;
        Hang(chainA, lift.PanA, top);
        Hang(chainB, lift.PanB, top);
        if (brakeLamp != null) brakeLamp.color = lift.Braked ? Braked : Released;
    }

    // Stretch a chain from the pan's top up to the wheel's centre height, keeping its x.
    private static void Hang(SpriteRenderer chain, Rigidbody2D pan, float top)
    {
        if (chain == null || pan == null || !pan.TryGetComponent(out BoxCollider2D box)) return;
        float bottom = box.bounds.max.y;
        float length = Mathf.Max(0.05f, top - bottom);
        chain.size = new Vector2(chain.size.x, length);
        // A tiled sprite's box is centred (0.5 - pivot) * size away from its pivot: place the box from bottom to top.
        float pivot01 = chain.sprite != null ? chain.sprite.pivot.y / chain.sprite.rect.height : 0.5f;
        Vector3 p = chain.transform.position;
        chain.transform.position = new Vector3(p.x, bottom + length * 0.5f - (0.5f - pivot01) * length, p.z);
    }

#if UNITY_EDITOR
    public void EditorSetup(BalanceLift balance, Transform wheelTransform, float wheelRadius, SpriteRenderer a, SpriteRenderer b, Light2D lamp)
    {
        lift = balance; wheel = wheelTransform; radius = wheelRadius; chainA = a; chainB = b; brakeLamp = lamp;
    }
#endif
}
