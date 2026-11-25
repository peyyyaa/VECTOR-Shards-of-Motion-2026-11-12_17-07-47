using UnityEngine;

/// <summary>
/// The Winch Hammer's catch (P1's job): Interact to let the wound hammer go. Shows how high it is wound, so players can
/// see what P2's winding has stored ("Hammer 1.2 m"). Setup: a trigger Collider2D on the Interactable layer; the hammer.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HammerCatch : MonoBehaviour, IInteractable
{
    [SerializeField] private WinchHammer hammer;
    [SerializeField] private Vector2 labelOffset = new Vector2(0f, 1.4f);

    private ScreenLabel label;

    public string InteractVerb => hammer != null && hammer.Height >= 0.05f ? "Release hammer" : "";

    public bool CanInteract(PlayerInteractor player) => isActiveAndEnabled && hammer != null && !hammer.Swinging && hammer.Height >= 0.05f;

    public void Interact(PlayerInteractor player)
    {
        if (hammer != null) hammer.Release();
    }

    private void Update()
    {
        if (hammer == null) return;
        if (label == null) label = WorldText.Create("Hammer height", null, 20f, new Color(1f, 0.9f, 0.7f), 5f);
        if (label == null) return;
        label.SetText(hammer.Swinging ? "Hammer: swinging!"
                    : hammer.Height < 0.05f ? "Hammer not wound\n<size=75%>P2: tether the brass ring by the exit</size>"
                    : $"Hammer wound: {hammer.Height:0.0} / {hammer.MaxHeight:0.0} m\n<size=75%>Interact here to let it go</size>");
        label.WorldPosition = transform.position + (Vector3)labelOffset;
    }

    private void OnDisable()
    {
        if (label != null) Destroy(label.gameObject);
        label = null;
    }

#if UNITY_EDITOR
    public void EditorSetup(WinchHammer target) => hammer = target;
#endif
}
