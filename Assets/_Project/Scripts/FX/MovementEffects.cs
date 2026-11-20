using UnityEngine;

/// <summary>
/// Dash and float visuals (both players). Reads PlayerDash / PlayerFloat and only draws.
/// - Dash: a dust kick at the start, then fading after-images of the character in the player's colour.
/// - Float: faint sparkles drifting off the cape (P1) or hair (P2) while floating.
/// Colours are set per player (Player1 / Player2 prefab) by Tools > Shards of Motion > Effects > 2. Set Up Player Effects.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class MovementEffects : MonoBehaviour
{
    [SerializeField] private FxLibrary fx;
    [Tooltip("The character sprite (the 'Visual' child), copied for dash after-images.")]
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Color color = new Color32(232, 137, 58, 255);
    [SerializeField] private Color sparkleColor = new Color32(255, 230, 150, 255);
    [SerializeField] private float afterImageInterval = 0.035f;
    [SerializeField] private float afterImageLife = 0.2f;
    [SerializeField] private float sparkleInterval = 0.1f;

    private PlayerMovement movement;
    private PlayerDash dash;
    private PlayerFloat floatAbility;
    private Collider2D body;
    private bool wasDashing;
    private float afterImageTimer, sparkleTimer;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        dash = GetComponent<PlayerDash>();
        floatAbility = GetComponent<PlayerFloat>();
        body = GetComponent<Collider2D>();
    }

    private void LateUpdate()
    {
        if (fx == null) return;
        int dir = movement.FacingDirection;

        bool dashing = dash != null && dash.IsDashing;
        if (dashing && !wasDashing)
        {
            if (movement.IsGrounded && fx.dust.Length > 0)
            {
                Vector3 feet = new Vector3(body.bounds.center.x, body.bounds.min.y + 0.1f, 0f);
                for (int i = 0; i < 4; i++)
                    Fx.Frames(fx.dust, feet, Color.white, Random.Range(0.3f, 0.45f), 4).Move(new Vector2(-dir * Random.Range(0.8f, 2f), Random.Range(0.1f, 0.6f)), 0f, 3f);
            }
            afterImageTimer = 0f;
        }
        wasDashing = dashing;

        if (dashing && visual != null && visual.sprite != null)
        {
            afterImageTimer -= Time.deltaTime;
            if (afterImageTimer <= 0f)
            {
                afterImageTimer = afterImageInterval;
                Color c = color; c.a = 0.55f;
                Fx.Sprite(visual.sprite, visual.transform.position, c, afterImageLife).Flip(visual.flipX).Order("Player", -1);
            }
        }

        bool floating = floatAbility != null && floatAbility.IsFloating;
        if (floating && fx.sparkle != null)
        {
            sparkleTimer -= Time.deltaTime;
            if (sparkleTimer <= 0f)
            {
                sparkleTimer = sparkleInterval;
                Vector3 p = transform.position + new Vector3(-dir * Random.Range(0.2f, 0.6f), Random.Range(-0.1f, 0.6f), 0f);
                Fx.Sprite(fx.sparkle, p, sparkleColor, Random.Range(0.5f, 0.8f), 3).Move(new Vector2(-dir * Random.Range(0.1f, 0.4f), Random.Range(0.2f, 0.6f)), 0f, 1f);
            }
        }
    }

#if UNITY_EDITOR
    public void EditorSetup(FxLibrary library, SpriteRenderer characterVisual, Color tint, Color sparkle)
    {
        fx = library; visual = characterVisual; color = tint; sparkleColor = sparkle;
    }
#endif
}
