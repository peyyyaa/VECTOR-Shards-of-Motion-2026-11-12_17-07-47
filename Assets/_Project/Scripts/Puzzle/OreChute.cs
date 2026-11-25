using UnityEngine;

/// <summary>
/// An old ore chute with a pull handle (the finale). Interact: if the Heart Cart stands under the chute's mouth, one
/// chunk of shard ore drops into it (the cart gets heavier, HeartCart.AddOre). Says so when the cart isn't there or is full.
/// Setup: a trigger Collider2D on the Interactable layer (the handle); Mouth = a point above where the cart must stand.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class OreChute : MonoBehaviour, IInteractable
{
    [SerializeField] private HeartCart cart;
    [SerializeField] private Transform mouth;
    [Tooltip("How far (m, sideways) the cart's centre may be from under the mouth.")]
    [SerializeField, Min(0.1f)] private float catchWidth = 0.9f;
    [SerializeField] private Sprite oreSprite;

    private ScreenLabel message;
    private float messageTimer;

    public string InteractVerb => "Drop ore";

    public bool CanInteract(PlayerInteractor player) => isActiveAndEnabled && cart != null;

    public void Interact(PlayerInteractor player)
    {
        Vector3 m = mouth != null ? mouth.position : transform.position;
        Vector3 c = cart.transform.position;
        if (Mathf.Abs(c.x - m.x) > catchWidth || c.y > m.y) { Show("Nothing under the chute.\nBring the cart beneath it."); return; }
        if (!cart.AddOre()) { Show($"The cart is full ({cart.Mass:0} kg)."); return; }
        if (oreSprite != null)
            Fx.Sprite(oreSprite, m, Color.white, 0.25f, 8).Move(new Vector2(0f, -2f), 40f).Hold();
        Fx.Flash(c, new Color(0.5f, 0.95f, 1f), 1.6f, 1.2f, 0.25f);
        Show($"Ore in: the cart is now {cart.Mass:0} kg.");
    }

    private void Update()
    {
        if (message != null && (messageTimer -= Time.deltaTime) <= 0f) { Destroy(message.gameObject); message = null; }
    }

    private void Show(string text)
    {
        if (message == null) message = WorldText.CreatePaper("Ore chute message", 26f, 6f);
        if (message == null) return;
        message.SetText(text);
        message.WorldPosition = transform.position + Vector3.up * 1.6f;
        messageTimer = 2.5f;
    }

    private void OnDisable()
    {
        if (message != null) Destroy(message.gameObject);
        message = null;
    }

#if UNITY_EDITOR
    public void EditorSetup(HeartCart heartCart, Transform mouthPoint, Sprite ore) { cart = heartCart; mouth = mouthPoint; oreSprite = ore; }
#endif
}
