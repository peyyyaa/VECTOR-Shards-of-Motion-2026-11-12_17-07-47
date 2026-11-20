using UnityEngine;

/// <summary>
/// Lets this player use levers and other interactables with the Interact button
/// (shared keyboard: P1 = E, P2 = Enter; full keyboard: E; gamepad: North / Y / △).
///
/// Every frame it finds the NEAREST interactable within Interact Range and reports it to InteractPrompts,
/// which shows a small key-cap prompt above it ([E] Read; shared when both players aim at the same thing);
/// pressing Interact uses it. Input comes from this player's own
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
    [Tooltip("Show the key-cap prompt above the interactable (look: InteractPrompt, sprites/font on UI_Canvas).")]
    [SerializeField] private bool showPrompt = true;

    private PlayerInputReader input;
    private readonly Collider2D[] results = new Collider2D[8];

    private IInteractable target;
    private Collider2D targetCollider;

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

    private void Update()
    {
        FindTarget();

        // While the journal is open, Interact belongs to it (tap = next page, hold = close), so a lever
        // next to a journal stone isn't pulled by accident.
        bool journalOpen = UIRoot.Instance != null && UIRoot.Instance.Journal != null && UIRoot.Instance.Journal.IsOpen;
        if (!journalOpen && input.InteractPressedThisFrame && target != null && target.CanInteract(this))
        {
            target.Interact(this);
        }
    }

    private void LateUpdate()
    {
        if (!showPrompt) return;
        bool show = target != null && targetCollider != null;
        InteractPrompts.Report(PlayerIndex, show ? targetCollider : null, input.InteractDisplayName, show ? target.InteractVerb : null);
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

    private void OnDisable() => InteractPrompts.Report(PlayerIndex, null, null, null);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
