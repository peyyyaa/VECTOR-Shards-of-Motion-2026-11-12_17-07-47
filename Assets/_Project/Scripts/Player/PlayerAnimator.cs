using UnityEngine;

/// <summary>
/// Picks and plays this player's sprite animation from what the player is doing. It only READS the
/// other player scripts (movement, float, dash, abilities); it never changes physics or input.
///
/// Priority, highest first:
///   Cast   - Kinetic Burst fired in the last Cast Hold seconds, or Wind Tether holding an object
///   Dash   - PlayerDash.IsDashing
///   Float  - PlayerFloat.IsFloating
///   Jump / Fall - in the air, rising / falling (also while launched)
///   Land   - for Land Time seconds after touching down
///   Push   - PlayerMovement.IsPushing (walking into a block on the ground)
///   Run    - move input held on the ground
///   Idle
/// Facing: the sprite flips to face PlayerMovement.FacingDirection (the art faces right).
///
/// Setup (done by Tools > Shards of Motion > Characters > 3. Set Up Player Visuals):
/// on the player root, with Visual = a child SpriteRenderer whose scale cancels the player's
/// 0.8 x 1.5 body scale, so the 32-PPU art isn't stretched. Animation Set = P1/P2_Animations.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private CharacterAnimationSet animationSet;
    [Tooltip("Child SpriteRenderer that shows the character (not the player root, which is scaled 0.8 x 1.5).")]
    [SerializeField] private SpriteRenderer visual;
    [Tooltip("Seconds the cast pose shows after a Kinetic Burst.")]
    [SerializeField] private float castHold = 0.35f;
    [Tooltip("Seconds the landing squash shows after touching down from a fall.")]
    [SerializeField] private float landTime = 0.1f;
    [Tooltip("Vertical speed (m/s) above which an airborne player counts as rising (Jump) rather than falling.")]
    [SerializeField] private float risingSpeed = 0.5f;

    private PlayerMovement movement;
    private PlayerInputReader input;
    private Rigidbody2D body;
    private PlayerFloat floatAbility;
    private PlayerDash dash;
    private KineticBurst burst;
    private WindTether tether;

    private CharacterAnimationSet.Clip clip;
    private string clipName;
    private float clipTime;
    private bool wasGrounded = true;
    private float landTimer;

    public string CurrentClip => clipName;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        input = GetComponent<PlayerInputReader>();
        body = GetComponent<Rigidbody2D>();
        floatAbility = GetComponent<PlayerFloat>();
        dash = GetComponent<PlayerDash>();
        burst = GetComponent<KineticBurst>();
        tether = GetComponent<WindTether>();
    }

    private void Update()
    {
        if (animationSet == null || visual == null) return;

        bool grounded = movement.IsGrounded;
        if (grounded && !wasGrounded) landTimer = landTime;
        wasGrounded = grounded;
        if (landTimer > 0f) landTimer -= Time.deltaTime;

        Play(ChooseClip(grounded));
        visual.flipX = movement.FacingDirection < 0;

        if (clip == null || clip.frames.Length == 0) return;
        clipTime += Time.deltaTime;
        int frame = (int)(clipTime * clip.framesPerSecond);
        frame = clip.loop ? frame % clip.frames.Length : Mathf.Min(frame, clip.frames.Length - 1);
        visual.sprite = clip.frames[frame];
    }

    private string ChooseClip(bool grounded)
    {
        if ((burst != null && Time.time - burst.LastFireTime < castHold) || (tether != null && tether.Target != null)) return "Cast";
        if (dash != null && dash.IsDashing) return "Dash";
        if (floatAbility != null && floatAbility.IsFloating) return "Float";
        if (!grounded) return body.linearVelocity.y > risingSpeed ? "Jump" : "Fall";
        if (landTimer > 0f) return "Land";
        if (movement.IsPushing) return "Push";
        if (input != null && Mathf.Abs(input.MoveX) > 0.01f) return "Run";
        return "Idle";
    }

    private void Play(string name)
    {
        if (name == clipName) return;
        CharacterAnimationSet.Clip next = animationSet.Get(name) ?? animationSet.Get("Idle");
        if (next == null) return;
        clipName = name;
        clip = next;
        clipTime = 0f;
    }

#if UNITY_EDITOR
    public void EditorSetup(CharacterAnimationSet set, SpriteRenderer renderer)
    {
        animationSet = set;
        visual = renderer;
    }

    public void EditorSetAnimationSet(CharacterAnimationSet set) => animationSet = set;
#endif
}
