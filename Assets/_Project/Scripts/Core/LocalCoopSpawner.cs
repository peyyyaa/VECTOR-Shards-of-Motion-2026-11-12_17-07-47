using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Spawns both co-op players when the room starts, and gives each one their own controls
/// based on what's plugged in:
///   - 2+ gamepads:  P1 = gamepad 1,       P2 = gamepad 2
///   - 1 gamepad:    P1 = full keyboard,    P2 = gamepad
///   - no gamepads:  P1 = keyboard (left),  P2 = keyboard (right)   [shared keyboard]
///
/// Uses PlayerInput.Instantiate, Unity's documented way to create a player with a specific
/// control scheme and device — including two players sharing one keyboard.
///
/// Limitation: devices are checked once, when the room starts. Plugging a controller in
/// mid-game won't reassign controls (a later feature if needed).
///
/// Control scheme names must match InputSystem_Actions:
/// "KeyboardLeft", "KeyboardRight", "Keyboard&Mouse", "Gamepad".
/// </summary>
public class LocalCoopSpawner : MonoBehaviour
{
    [Header("Player Prefabs (each needs a Player Input component)")]
    [SerializeField] private GameObject player1Prefab;
    [SerializeField] private GameObject player2Prefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform player1Spawn;
    [SerializeField] private Transform player2Spawn;

    private const string SchemeKeyboardLeft = "KeyboardLeft";
    private const string SchemeKeyboardRight = "KeyboardRight";
    private const string SchemeFullKeyboard = "Keyboard&Mouse";
    private const string SchemeGamepad = "Gamepad";

    private readonly List<PlayerInput> players = new List<PlayerInput>();

    /// <summary>The spawned players (index 0 = Player 1).</summary>
    public IReadOnlyList<PlayerInput> Players => players;

    private void Start()
    {
        if (player1Prefab == null || player2Prefab == null)
        {
            Debug.LogError("LocalCoopSpawner: assign both player prefabs.", this);
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        IReadOnlyList<Gamepad> gamepads = Gamepad.all;

        if (gamepads.Count >= 2)
        {
            Spawn(player1Prefab, 0, player1Spawn, SchemeGamepad, gamepads[0]);
            Spawn(player2Prefab, 1, player2Spawn, SchemeGamepad, gamepads[1]);
        }
        else if (gamepads.Count == 1 && keyboard != null)
        {
            // The full-keyboard scheme expects a mouse too; fall back to the left half if there isn't one.
            if (mouse != null)
                Spawn(player1Prefab, 0, player1Spawn, SchemeFullKeyboard, keyboard, mouse);
            else
                Spawn(player1Prefab, 0, player1Spawn, SchemeKeyboardLeft, keyboard);

            Spawn(player2Prefab, 1, player2Spawn, SchemeGamepad, gamepads[0]);
        }
        else if (keyboard != null)
        {
            Spawn(player1Prefab, 0, player1Spawn, SchemeKeyboardLeft, keyboard);
            Spawn(player2Prefab, 1, player2Spawn, SchemeKeyboardRight, keyboard);
        }
        else
        {
            Debug.LogError("LocalCoopSpawner: no keyboard or gamepads found; can't spawn players.", this);
            return;
        }

        foreach (PlayerInput p in players)
        {
            Debug.Log($"Player {p.playerIndex + 1}: scheme '{p.currentControlScheme}', " +
                      $"devices [{string.Join(", ", p.devices)}]");
        }
    }

    private void Spawn(GameObject prefab, int playerIndex, Transform spawnPoint, string scheme, params InputDevice[] devices)
    {
        PlayerInput player = PlayerInput.Instantiate(prefab, playerIndex, scheme, -1, devices);

        if (spawnPoint != null)
        {
            player.transform.SetPositionAndRotation(spawnPoint.position, Quaternion.identity);

            // Keep the physics body in sync with the new position before the first physics step.
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.position = spawnPoint.position;
        }

        player.name = $"Player{playerIndex + 1}";
        players.Add(player);
    }
}
