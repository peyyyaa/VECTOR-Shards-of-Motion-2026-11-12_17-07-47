/// <summary>
/// Anything a player can use with the Interact button (levers, reset shrines, journal stones).
/// Found by PlayerInteractor through a physics query, so the object needs a Collider2D
/// (usually a trigger) on the Interactable layer.
///
/// It's an interface (not a base class) so a component can be BOTH interactable and a
/// SignalSource, like a Lever.
/// </summary>
public interface IInteractable
{
    /// <summary>False to ignore this player's press (e.g., still on cooldown, or disabled).</summary>
    bool CanInteract(PlayerInteractor player);

    /// <summary>Called once when a player in range presses Interact.</summary>
    void Interact(PlayerInteractor player);

    /// <summary>Word shown in the prompt after the key, e.g. "P1: E  Use". Empty = key only.</summary>
    string InteractVerb => "";
}
