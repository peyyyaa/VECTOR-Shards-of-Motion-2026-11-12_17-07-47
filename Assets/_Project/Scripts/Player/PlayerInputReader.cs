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
///
/// Menus that both players use at once (the engine assembly screen) set GameplaySuspended: the gameplay values above
/// then read as "nothing pressed", while the Menu* values below still give each player's own stick/keys and Interact.
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

    /// <summary>While true, gameplay reads no input (a two-player menu is using the controls). Set by MenuFreeze.</summary>
    public static bool GameplaySuspended { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => GameplaySuspended = false;

    /// <summary>0 = Player 1, 1 = Player 2 (assigned by Unity's Player Input).</summary>
    public int PlayerIndex => playerInput != null ? playerInput.playerIndex : -1;

    /// <summary>Horizontal input from -1 (left) to +1 (right).</summary>
    public float MoveX => Gameplay() ? move.ReadValue<Vector2>().x : 0f;

    public bool JumpPressedThisFrame => Gameplay() && jump.WasPressedThisFrame();
    public bool JumpHeld => Gameplay() && jump.IsPressed();
    public bool DashPressedThisFrame => Gameplay() && dash.WasPressedThisFrame();
    public bool AbilityPressedThisFrame => Gameplay() && ability.WasPressedThisFrame();
    public bool AbilityHeld => Gameplay() && ability.IsPressed();
    public bool InteractPressedThisFrame => Gameplay() && interact.WasPressedThisFrame();
    public bool InteractHeld => Gameplay() && interact.IsPressed();

    /// <summary>Menu cursor direction (this player's stick / movement keys, both axes), even while gameplay is suspended.</summary>
    public Vector2 MenuMove => Ready() ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;

    /// <summary>This player's Interact key/button, for menus ("grab"), even while gameplay is suspended.</summary>
    public bool MenuGrabPressedThisFrame => Ready() && interact.WasPressedThisFrame();

    private bool Gameplay() => !GameplaySuspended && Ready();

    /// <summary>The Interact key/button for THIS player's controls (e.g., "E", "Enter", "Y"), for prompts.</summary>
    public string InteractDisplayName => DisplayName(interact, "Interact");

    /// <summary>The Jump key/button for THIS player's controls (e.g., "W", "Up Arrow", "A"), for hints.</summary>
    public string JumpDisplayName => DisplayName(jump, "Jump");

    private string DisplayName(InputAction action, string fallback)
    {
        if (!Ready()) return fallback;
        string shown = action.GetBindingDisplayString(group: playerInput.currentControlScheme);
        return string.IsNullOrEmpty(shown) ? fallback : shown;
    }

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
