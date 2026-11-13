using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Horizontal dash: a short, fixed-distance burst of sideways movement in the
/// direction the player faces, with gravity switched off for the duration.
/// One dash per airtime (refilled on landing), plus a cooldown between dashes.
///
/// Physics (constant-velocity motion):
///   distance = speed * time   →   speed = distance / time
/// The script picks the speed from Dash Distance and Dash Duration.
///
/// Unity detail: physics runs in fixed steps (0.02 s by default). The dash lasts a
/// whole number of steps, so the speed is calculated from the actual stepped duration.
/// That keeps the distance correct even when Dash Duration isn't a multiple of 0.02 s.
///
/// The dash sets velocity directly (not a force), so it's identical every time
/// regardless of the player's mass. That's a deliberate game-feel choice.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerDash : MonoBehaviour
{
    [Header("Input (drag InputSystem_Actions > Player/Dash)")]
    [SerializeField] private InputActionReference dashAction;

    [Header("Dash")]
    [Tooltip("How far one dash travels, in units (meters), if nothing blocks it.")]
    [SerializeField] private float dashDistance = 3f;
    [Tooltip("How long the dash lasts, in seconds. Rounded to whole physics steps.")]
    [SerializeField] private float dashDuration = 0.15f;
    [Tooltip("Seconds after a dash ends before another dash is allowed.")]
    [SerializeField] private float dashCooldown = 0.5f;

    private Rigidbody2D body;
    private PlayerMovement movement;

    private float normalGravityScale;
    private bool dashRequested;     // set in Update, consumed in FixedUpdate
    private bool isDashing;
    private int dashStepsRemaining;
    private float dashSpeed;
    private int dashDirection;      // +1 right, -1 left
    private float cooldownRemaining;
    private bool airDashAvailable = true;

    /// <summary>True while the dash is moving the player (useful later for animation).</summary>
    public bool IsDashing => isDashing;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        normalGravityScale = body.gravityScale;
    }

    private void OnEnable()
    {
        dashAction?.action.Enable();
    }

    private void OnDisable()
    {
        if (isDashing)
        {
            EndDash();
        }
    }

    private void Update()
    {
        if (dashAction != null && dashAction.action.WasPressedThisFrame())
        {
            dashRequested = true;
        }
    }

    private void FixedUpdate()
    {
        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.fixedDeltaTime;
        }

        if (isDashing)
        {
            dashStepsRemaining--;
            if (dashStepsRemaining <= 0)
            {
                EndDash();
            }
            else
            {
                MoveDashStep();
            }
        }
        else
        {
            if (movement.IsGrounded)
            {
                airDashAvailable = true; // landing refills the air dash
            }

            // Don't start while another ability (e.g., Kinetic Burst recoil) controls the player.
            bool canDash = !movement.ControlLocked
                && cooldownRemaining <= 0f
                && (movement.IsGrounded || airDashAvailable);
            if (dashRequested && canDash)
            {
                StartDash();
            }
        }

        // Clear the request either way, so a press during cooldown isn't saved for later.
        dashRequested = false;
    }

    private void StartDash()
    {
        float step = Time.fixedDeltaTime;
        dashStepsRemaining = Mathf.Max(1, Mathf.RoundToInt(dashDuration / step));
        dashSpeed = dashDistance / (dashStepsRemaining * step); // v = d / t

        dashDirection = movement.FacingDirection;
        if (!movement.IsGrounded)
        {
            airDashAvailable = false;
        }

        isDashing = true;
        movement.ControlLocked = true;
        body.gravityScale = 0f;
        MoveDashStep();
    }

    /// <summary>
    /// Sets this physics step's dash velocity: a perfectly horizontal line at dash speed,
    /// unless a pushable object is in the way. Then the player moves just up to it and the
    /// dash ends — the dash isn't a battering ram (Kinetic Burst is how to launch objects).
    /// </summary>
    private void MoveDashStep()
    {
        float step = Time.fixedDeltaTime;
        RaycastHit2D hit = movement.FindPushableAhead(dashDirection, dashSpeed * step);

        if (hit)
        {
            const float skin = 0.01f; // stop a hair short so we arrive touching, not overlapping
            float speedToContact = Mathf.Max(0f, hit.distance - skin) / step;
            body.linearVelocity = new Vector2(dashDirection * speedToContact, 0f);
            dashStepsRemaining = Mathf.Min(dashStepsRemaining, 1); // end on the next step
        }
        else
        {
            body.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
        }
    }

    private void EndDash()
    {
        isDashing = false;
        body.gravityScale = normalGravityScale;

        // Stop the dash's speed; PlayerMovement applies normal input speed from the next step.
        body.linearVelocity = new Vector2(0f, body.linearVelocity.y);

        movement.ControlLocked = false;
        cooldownRemaining = dashCooldown;
    }
}
