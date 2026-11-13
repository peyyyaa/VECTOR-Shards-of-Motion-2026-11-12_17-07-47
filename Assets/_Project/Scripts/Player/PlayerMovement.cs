using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Milestone 1: horizontal movement and jumping for the player.
///
/// How it works:
/// - Input is read every frame in Update (so no key press is missed).
/// - Physics changes happen in FixedUpdate (Unity's fixed physics step, 0.02 s by default).
/// - Horizontal movement sets the Rigidbody2D's x-velocity directly, for tight, responsive control.
/// - Jumping applies a single upward impulse sized to reach a chosen jump height.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input (drag actions from InputSystem_Actions > Player)")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Movement")]
    [Tooltip("Horizontal speed in Unity units (meters) per second.")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Jump")]
    [Tooltip("Height of a jump in Unity units (meters). One graybox tile = 1 unit.")]
    [SerializeField] private float jumpHeight = 2.5f;

    [Header("Ground Check")]
    [Tooltip("Layers the player can stand on and jump from (e.g., Ground, PhysicsObject).")]
    [SerializeField] private LayerMask groundLayers;
    [Tooltip("How far below the player's collider to look for ground, in units.")]
    [SerializeField] private float groundCheckDepth = 0.1f;

    private Rigidbody2D body;
    private Collider2D bodyCollider;

    private float moveInput;      // -1 (left) to +1 (right)
    private bool jumpRequested;   // set in Update, consumed in FixedUpdate
    private bool isGrounded;
    private float baseGravityScale; // the Rigidbody2D's Gravity Scale at startup

    /// <summary>True when the player is standing on something in Ground Layers.
    /// Other player scripts (e.g., PlayerFloat) read this.</summary>
    public bool IsGrounded => isGrounded;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();

        // Saved so jump height stays correct even if another script
        // (like PlayerFloat) temporarily changes the gravity scale.
        baseGravityScale = body.gravityScale;
    }

    private void OnEnable()
    {
        moveAction?.action.Enable();
        jumpAction?.action.Enable();
    }

    private void OnDisable()
    {
        moveAction?.action.Disable();
        jumpAction?.action.Disable();
    }

    private void Update()
    {
        if (moveAction != null)
        {
            moveInput = moveAction.action.ReadValue<Vector2>().x;
        }

        if (jumpAction != null && jumpAction.action.WasPressedThisFrame())
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        isGrounded = CheckGrounded();

        // Horizontal movement: set x-velocity directly; keep the y-velocity that gravity produced.
        body.linearVelocity = new Vector2(moveInput * moveSpeed, body.linearVelocity.y);

        if (jumpRequested && isGrounded)
        {
            Jump();
        }

        // Clear the request whether or not we jumped, so a press in mid-air
        // doesn't trigger a surprise jump on landing.
        jumpRequested = false;
    }

    private void Jump()
    {
        // Physics: to rise to height h against gravity g, the launch speed must be
        //   v = sqrt(2 * g * h)          (from v^2 = v0^2 - 2gh, with v = 0 at the top)
        // An impulse changes momentum:  J = m * Δv   (units: N·s = kg·m/s)
        // We zero any existing vertical velocity first so Δv = v and every jump is the same height.
        float gravity = Mathf.Abs(Physics2D.gravity.y * baseGravityScale);
        float launchSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);

        body.linearVelocity = new Vector2(body.linearVelocity.x, 0f);
        body.AddForce(Vector2.up * body.mass * launchSpeed, ForceMode2D.Impulse);
    }

    private bool CheckGrounded()
    {
        // A thin box just under the bottom of the player's collider.
        Bounds bounds = bodyCollider.bounds;
        Vector2 center = new Vector2(bounds.center.x, bounds.min.y - groundCheckDepth * 0.5f);
        Vector2 size = new Vector2(bounds.size.x * 0.9f, groundCheckDepth);
        return Physics2D.OverlapBox(center, size, 0f, groundLayers) != null;
    }

    // Draws the ground-check box in the Scene view when the player is selected.
    // Green = touching ground, red = in the air.
    private void OnDrawGizmosSelected()
    {
        Collider2D col = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
        if (col == null) return;

        Bounds bounds = col.bounds;
        Vector3 center = new Vector3(bounds.center.x, bounds.min.y - groundCheckDepth * 0.5f, 0f);
        Vector3 size = new Vector3(bounds.size.x * 0.9f, groundCheckDepth, 0f);
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(center, size);
    }
}
