using TMPro;
using UnityEngine;

/// <summary>
/// Lets this player use levers and other interactables with the Interact button
/// (shared keyboard: P1 = E, P2 = Enter; full keyboard: E; gamepad: North / Y / △).
///
/// Every frame it finds the NEAREST interactable within Interact Range and shows a small
/// prompt above it ("P1: E"); pressing Interact uses it. Input comes from this player's own
/// PlayerInputReader, so each player only triggers things with their own button, and the
/// prompt shows that player's actual key.
///
/// Doesn't touch movement or abilities.
/// </summary>
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerInteractor : MonoBehaviour
{
    [Tooltip("How far (units) from the player's center an interactable's collider can be.")]
    [SerializeField] private float interactRange = 1f;
    [Tooltip("Layers searched for interactables. Leave as Nothing to use the 'Interactable' layer.")]
    [SerializeField] private LayerMask interactableLayers;

    [Header("Prompt")]
    [SerializeField] private bool showPrompt = true;
    [Tooltip("Prompt text size in pixels at a 1080p screen.")]
    [SerializeField, Min(4f)] private float promptFontSizePx = 28f;
    [Tooltip("Leave empty to use the project's default TMP font.")]
    [SerializeField] private TMP_FontAsset promptFont;

    private PlayerInputReader input;
    private readonly Collider2D[] results = new Collider2D[8];

    private IInteractable target;
    private Collider2D targetCollider;
    private ScreenLabel prompt;

    /// <summary>0 = Player 1, 1 = Player 2.</summary>
    public int PlayerIndex => input != null ? input.PlayerIndex : -1;

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        if (interactableLayers.value == 0)
        {
            interactableLayers = LayerMask.GetMask("Interactable");
        }
    }

    private void Start()
    {
        if (showPrompt)
        {
            prompt = WorldText.Create($"{name} (prompt)", promptFont, promptFontSizePx, Color.white, 3f);
            prompt.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        FindTarget();

        if (input.InteractPressedThisFrame && target != null && target.CanInteract(this))
        {
            target.Interact(this);
        }
    }

    private void LateUpdate()
    {
        if (prompt == null) return;

        bool show = target != null && targetCollider != null;
        if (prompt.gameObject.activeSelf != show) prompt.gameObject.SetActive(show);
        if (!show) return;

        // Above the interactable; P1 slightly left, P2 slightly right, so both prompts fit.
        Bounds b = targetCollider.bounds;
        float side = PlayerIndex == 1 ? 0.55f : -0.55f;
        prompt.WorldPosition = new Vector3(b.center.x + side, b.max.y + 0.35f, 0f);
        string verb = target.InteractVerb;
        prompt.SetText(string.IsNullOrEmpty(verb)
            ? $"P{PlayerIndex + 1}: {input.InteractDisplayName}"
            : $"P{PlayerIndex + 1}: {input.InteractDisplayName}  {verb}");
    }

    private void FindTarget()
    {
        Vector2 origin = transform.position;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(interactableLayers);
        filter.useTriggers = true; // interactables usually use trigger colliders

        int count = Physics2D.OverlapCircle(origin, interactRange, filter, results);

        target = null;
        targetCollider = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            IInteractable candidate = results[i].GetComponentInParent<IInteractable>();
            if (candidate == null) continue;

            float distance = Vector2.Distance(origin, results[i].ClosestPoint(origin));
            if (distance < bestDistance)
            {
                target = candidate;
                targetCollider = results[i];
                bestDistance = distance;
            }
        }
    }

    private void OnDestroy()
    {
        if (prompt != null) Destroy(prompt.gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
