using UnityEngine;

/// <summary>
/// The wall map in Wren's Lab: Interact opens the map screen (MapPanel on UI_Canvas). Either player can open it;
/// the map stops time and both players' controls until it's closed (or they travel).
/// Setup: a trigger Collider2D on the Interactable layer, plus the map board's sprite (VillageBuilder adds both).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WallMap : MonoBehaviour, IInteractable
{
    private static MapPanel Panel => UIRoot.Instance != null ? UIRoot.Instance.Map : null;

    public string InteractVerb => "Map";

    // Not on the frame the map closed: the press that closed it must not open it again.
    public bool CanInteract(PlayerInteractor player) =>
        isActiveAndEnabled && Panel != null && !Panel.IsOpen && !PauseMenu.IsPaused && Time.frameCount > MapPanel.ClosedFrame + 1;

    public void Interact(PlayerInteractor player) => Panel?.Open();

    private void Reset() => GetComponent<Collider2D>().isTrigger = true;
}
