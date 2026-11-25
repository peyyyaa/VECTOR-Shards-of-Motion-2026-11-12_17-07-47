using UnityEngine;

/// <summary>
/// The cart chute at the end of the finale's first part: when the Heart Cart rolls into this trigger it drops down the
/// chute to the engine hall below (it disappears with a flash), the event "Delivered Event" is saved, and this signal
/// stays ON from then on (e.g. it opens the way on). Already delivered (saved): on from the start, cart hidden.
/// Setup: a trigger Collider2D at the chute's mouth.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CartChute : SignalSource
{
    [SerializeField] private HeartCart cart;
    [SerializeField] private string deliveredEvent = "r1_cart_delivered";

    private bool delivered;

    public override bool IsActive => delivered;

    private void Start()
    {
        delivered = SaveSystem.HasEvent(deliveredEvent);
        if (delivered && cart != null) cart.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (delivered || cart == null || other.attachedRigidbody == null || other.attachedRigidbody.gameObject != cart.gameObject) return;
        delivered = true;
        SaveSystem.MarkEvent(deliveredEvent);
        Fx.Flash(cart.transform.position, new Color(0.5f, 0.95f, 1f), 3f, 1.8f, 0.5f);
        cart.gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    public void EditorSetup(HeartCart heartCart, string eventId) { cart = heartCart; deliveredEvent = eventId; }
#endif
}
