using UnityEngine;

/// <summary>
/// Temporary floating: while airborne and falling, holding Jump lowers the
/// player's gravity and caps fall speed, for a limited time per airtime.
/// Landing refills the float timer.
///
/// Physics:
/// - Newton's 2nd law, a = F / m. Gravity's force is F = m * g, so its acceleration is just g.
///   Lowering the Rigidbody2D's Gravity Scale lowers g, so the player speeds up downward more slowly.
///   Default: 1/6 of normal gravity (29.43 / 6 ≈ 4.9 m/s²) — roughly how the Moon compares to Earth.
/// - The fall-speed cap stands in for air resistance. Real drag grows with speed until it
///   balances gravity (terminal velocity). Here we simply clamp the speed — a game simplification,
///   not a real force.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerFloat : MonoBehaviour
{
    [Header("Float")]
    [Tooltip("Gravity while floating, as a fraction of the player's normal gravity. 0.1667 = 1/6 (about the Moon vs. Earth).")]
    [SerializeField, Range(0f, 1f)] private float floatGravityFraction = 1f / 6f;
    [Tooltip("Fastest downward speed while floating, in units (meters) per second.")]
    [SerializeField] private float maxFloatFallSpeed = 2f;
    [Tooltip("Total seconds of floating allowed before landing again.")]
    [SerializeField] private float floatDuration = 1f;

    private Rigidbody2D body;
    private PlayerMovement movement;
    private PlayerInputReader input;

    private float normalGravityScale;
    private float floatTimeRemaining;
    private bool isFloating;

    /// <summary>True while the float is active (useful later for animation).</summary>
    public bool IsFloating => isFloating;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        input = GetComponent<PlayerInputReader>();
        normalGravityScale = body.gravityScale;
        floatTimeRemaining = floatDuration;
    }

    private void OnDisable()
    {
        // Never leave the player stuck with low gravity if this script is turned off.
        StopFloating();
    }

    private void FixedUpdate()
    {
        if (movement.ControlLocked)
        {
            // Another ability (e.g., dash) owns gravity right now; don't touch it.
            isFloating = false;
            return;
        }

        if (movement.IsGrounded)
        {
            floatTimeRemaining = floatDuration; // landing refills the float
            StopFloating();
            return;
        }

        bool holdingJump = input.JumpHeld;
        bool falling = body.linearVelocity.y < 0f;
        bool canFloat = holdingJump && falling && floatTimeRemaining > 0f;

        if (canFloat)
        {
            isFloating = true;
            body.gravityScale = normalGravityScale * floatGravityFraction;

            // Cap the downward speed (never slower than -maxFloatFallSpeed).
            if (body.linearVelocity.y < -maxFloatFallSpeed)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, -maxFloatFallSpeed);
            }

            floatTimeRemaining -= Time.fixedDeltaTime;
        }
        else
        {
            StopFloating();
        }
    }

    private void StopFloating()
    {
        isFloating = false;
        if (body != null)
        {
            body.gravityScale = normalGravityScale;
        }
    }
}
