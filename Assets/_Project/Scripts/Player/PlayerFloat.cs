using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Temporary floating: while airborne and falling, holding Jump lowers the
/// player's gravity and caps fall speed, for a limited time per airtime.
/// Landing refills the float timer.
///
/// Physics:
/// - Newton's 2nd law, a = F / m. Gravity's force is F = m * g, so its acceleration is just g.
///   Lowering the Rigidbody2D's Gravity Scale lowers g, so the player speeds up downward more slowly.
/// - The fall-speed cap stands in for air resistance. Real drag grows with speed until it
///   balances gravity (terminal velocity). Here we simply clamp the speed — a game simplification,
///   not a real force.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerFloat : MonoBehaviour
{
    [Header("Input (drag InputSystem_Actions > Player/Jump)")]
    [SerializeField] private InputActionReference jumpAction;

    [Header("Float")]
    [Tooltip("Gravity Scale used while floating. The player's normal value is set on the Rigidbody2D.")]
    [SerializeField] private float floatGravityScale = 0.5f;
    [Tooltip("Fastest downward speed while floating, in units (meters) per second.")]
    [SerializeField] private float maxFloatFallSpeed = 2f;
    [Tooltip("Total seconds of floating allowed before landing again.")]
    [SerializeField] private float floatDuration = 1f;

    private Rigidbody2D body;
    private PlayerMovement movement;

    private float normalGravityScale;
    private float floatTimeRemaining;
    private bool isFloating;

    /// <summary>True while the float is active (useful later for animation).</summary>
    public bool IsFloating => isFloating;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        normalGravityScale = body.gravityScale;
        floatTimeRemaining = floatDuration;
    }

    private void OnEnable()
    {
        jumpAction?.action.Enable();
    }

    private void OnDisable()
    {
        // Never leave the player stuck with low gravity if this script is turned off.
        // (We don't disable the Jump action here: PlayerMovement uses it too.)
        StopFloating();
    }

    private void FixedUpdate()
    {
        if (movement.IsGrounded)
        {
            floatTimeRemaining = floatDuration; // landing refills the float
            StopFloating();
            return;
        }

        bool holdingJump = jumpAction != null && jumpAction.action.IsPressed();
        bool falling = body.linearVelocity.y < 0f;
        bool canFloat = holdingJump && falling && floatTimeRemaining > 0f;

        if (canFloat)
        {
            isFloating = true;
            body.gravityScale = floatGravityScale;

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
