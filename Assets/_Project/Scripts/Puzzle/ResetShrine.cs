using UnityEngine;

/// <summary>
/// A reset point: a player presses Interact here to reset the current room. Every block,
/// lever, relay and door of the room goes back to how it started, and both players return
/// to where they entered the room. Use it when a puzzle gets stuck (block wedged, lost, etc.).
///
/// Setup: a Collider2D with Is Trigger ON, on the Interactable layer.
/// (Village Props' "Stone of Recall" is a good visual.)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ResetShrine : MonoBehaviour, IInteractable
{
    [Tooltip("Seconds before the shrine can be used again.")]
    [SerializeField, Min(0f)] private float cooldown = 1f;

    private RoomManager manager;
    private float cooldownRemaining;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        manager = FindFirstObjectByType<RoomManager>();
        if (manager == null) Debug.LogError($"{name}: ResetShrine needs a RoomManager in the scene.", this);
    }

    private void Update()
    {
        if (cooldownRemaining > 0f) cooldownRemaining -= Time.deltaTime;
    }

    public bool CanInteract(PlayerInteractor player) => isActiveAndEnabled && manager != null && cooldownRemaining <= 0f;

    public string InteractVerb => "Reset room";

    public void Interact(PlayerInteractor player)
    {
        cooldownRemaining = cooldown;
        manager.ResetCurrentRoom();
    }
}
