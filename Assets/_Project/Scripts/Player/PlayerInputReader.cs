using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads THIS player's controls and exposes them as simple values for the other
/// player scripts (movement, float, dash, abilities).
///
/// Why: Unity's Player Input component gives each player a private copy of the input
/// actions, paired to that player's own device(s) (half a keyboard, a full keyboard, or
/// a gamepad). Gameplay scripts read from this reader instead of the shared input asset,
/// so Player 1's keys never move Player 2.
///
/// Action names must match the "Player" map in InputSystem_Actions:
/// Move, Jump, Dash, Ability, Interact.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputReader : MonoBehaviour
{
    private PlayerInput playerInput;
    private InputAction move;
    private InputAction jump;
    private InputAction dash;
    private InputAction ability;
    private InputAction interact;
    private bool initialized;
    private bool setupFailed;

    /// <summary>0 = Player 1, 1 = Player 2 (assigned by Unity's Player Input).</summary>
    public int PlayerIndex => playerInput != null ? playerInput.playerIndex : -1;

    /// <summary>Horizontal input from -1 (left) to +1 (right).</summary>
    public float MoveX => Ready() ? move.ReadValue<Vector2>().x : 0f;

    public bool JumpPressedThisFrame => Ready() && jump.WasPressedThisFrame();
    public bool JumpHeld => Ready() && jump.IsPressed();
    public bool DashPressedThisFrame => Ready() && dash.WasPressedThisFrame();
    public bool AbilityPressedThisFrame => Ready() && ability.WasPressedThisFrame();
    public bool AbilityHeld => Ready() && ability.IsPressed();
    public bool InteractPressedThisFrame => Ready() && interact.WasPressedThisFrame();

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    // Looks up the actions the first time they're needed. (Unity doesn't guarantee which
    // component's Awake runs first, so we don't rely on Player Input being set up in our Awake.)
    private bool Ready()
    {
        if (initialized) return true;
        if (setupFailed || playerInput == null) return false;

        // playerInput.actions is this player's own copy of the asset.
        InputActionAsset actions = playerInput.actions;
        if (actions == null)
        {
            Debug.LogError($"{name}: Player Input has no Actions asset assigned.", this);
            setupFailed = true; // log once, not every frame
            return false;
        }

        move = actions.FindAction("Player/Move", throwIfNotFound: true);
        jump = actions.FindAction("Player/Jump", throwIfNotFound: true);
        dash = actions.FindAction("Player/Dash", throwIfNotFound: true);
        ability = actions.FindAction("Player/Ability", throwIfNotFound: true);
        interact = actions.FindAction("Player/Interact", throwIfNotFound: true);
        initialized = true;
        return true;
    }
}
