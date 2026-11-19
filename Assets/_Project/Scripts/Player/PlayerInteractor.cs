using UnityEngine;

/// <summary>
/// Lets this player use levers and other interactables with the Interact button
/// (shared keyboard: P1 = E, P2 = Enter; full keyboard: E; gamepad: North / Y / △).
///
/// On a press, it finds the NEAREST interactable within Interact Range and uses it.
/// Input comes from this player's own PlayerInputReader, so each player only
/// triggers things with their own button.
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

    private PlayerInputReader input;
    private readonly Collider2D[] results = new Collider2D[8];

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
        if (input.InteractPressedThisFrame)
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Vector2 origin = transform.position;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(interactableLayers);
        filter.useTriggers = true; // interactables usually use trigger colliders

        int count = Physics2D.OverlapCircle(origin, interactRange, filter, results);

        IInteractable best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            IInteractable candidate = results[i].GetComponentInParent<IInteractable>();
            if (candidate == null || !candidate.CanInteract(this)) continue;

            float distance = Vector2.Distance(origin, results[i].ClosestPoint(origin));
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        best?.Interact(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
