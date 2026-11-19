using UnityEngine;

/// <summary>
/// Teaches the float: the first times a player falls for a moment without floating, a small hint
/// appears above them ("P2: Hold Up Arrow to float"), using that player's real Jump key.
/// Once the player has floated, the hint never shows again (for this play session).
///
/// Lives on Player_Base. Doesn't change movement: it only reads PlayerFloat / PlayerMovement.
/// </summary>
[RequireComponent(typeof(PlayerFloat))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInputReader))]
public class FloatHint : MonoBehaviour
{
    [Tooltip("Seconds of falling (without floating) before the hint appears.")]
    [SerializeField, Min(0f)] private float fallTimeBeforeHint = 0.3f;
    [Tooltip("Only falls faster than this (m/s) count, so small hops don't trigger the hint.")]
    [SerializeField, Min(0f)] private float minFallSpeed = 1.5f;
    [Tooltip("Text size in pixels at a 1080p screen.")]
    [SerializeField, Min(4f)] private float fontSizePx = 24f;

    private static readonly bool[] learned = new bool[4]; // per player index, per play session

    private PlayerFloat playerFloat;
    private PlayerMovement movement;
    private PlayerInputReader input;
    private Rigidbody2D body;
    private ScreenLabel label;
    private float fallTimer;

    // Clear "learned" when entering Play mode (static fields can survive between plays in the editor).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetLearned()
    {
        for (int i = 0; i < learned.Length; i++) learned[i] = false;
    }

    private void Awake()
    {
        playerFloat = GetComponent<PlayerFloat>();
        movement = GetComponent<PlayerMovement>();
        input = GetComponent<PlayerInputReader>();
        body = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        label = WorldText.Create($"{name} (float hint)", null, fontSizePx, new Color(0.7f, 0.95f, 1f), 5f);
        label.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (label == null) return;

        int index = Mathf.Clamp(input.PlayerIndex, 0, learned.Length - 1);
        if (playerFloat.IsFloating) learned[index] = true;

        bool falling = !movement.IsGrounded && !movement.ControlLocked && body.linearVelocity.y < -minFallSpeed;
        fallTimer = falling && !playerFloat.IsFloating ? fallTimer + Time.deltaTime : 0f;

        bool show = !learned[index] && fallTimer >= fallTimeBeforeHint;
        if (label.gameObject.activeSelf != show) label.gameObject.SetActive(show);
        if (!show) return;

        label.SetText($"P{index + 1}: Hold {input.JumpDisplayName} to float");
        label.WorldPosition = transform.position + new Vector3(0f, 1.4f, 0f);
    }

    private void OnDestroy()
    {
        if (label != null) Destroy(label.gameObject);
    }
}
