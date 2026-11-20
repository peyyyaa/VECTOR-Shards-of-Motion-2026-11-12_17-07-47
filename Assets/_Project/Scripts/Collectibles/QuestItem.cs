using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A hidden quest item (e.g. a part of the region's machine). Either player touches it to pick it up: it pops,
/// a "Found: ..." label appears for a moment, and its Id is remembered so it stays gone (this play session for
/// now; the save system will store Found later, and village quests / the region finale will read it).
/// Setup: a trigger Collider2D on a layer that collides with players, a SpriteRenderer for the art.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class QuestItem : MonoBehaviour
{
    [Tooltip("Unique id, e.g. R1_Part_Flywheel. Used by quests and the save file.")]
    [SerializeField] private string itemId = "R1_Part";
    [Tooltip("Name shown when found.")]
    [SerializeField] private string displayName = "Machine Part";
    [SerializeField] private float bobHeight = 0.1f;
    [SerializeField] private float bobSpeed = 1.8f;
    [SerializeField] private FxLibrary fx;
    [SerializeField] private Color glowColor = new Color32(255, 214, 120, 255);

    /// <summary>Ids of every quest item found so far.</summary>
    public static readonly HashSet<string> Found = new HashSet<string>();
    public static event System.Action<QuestItem> Collected;

    public string Id => itemId;
    public string DisplayName => displayName;

    private Vector3 basePosition;

    private void Start()
    {
        basePosition = transform.position;
        if (Found.Contains(itemId)) gameObject.SetActive(false);
    }

    private void Update()
    {
        transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null || rb.GetComponent<PlayerMovement>() == null || Found.Contains(itemId)) return;

        Found.Add(itemId);
        Collected?.Invoke(this);

        Fx.Flash(transform.position, glowColor, 2.5f, 2f, 0.4f);
        if (fx != null && fx.sparkle != null)
            for (int i = 0; i < 10; i++)
                Fx.Sprite(fx.sparkle, transform.position, glowColor, Random.Range(0.5f, 0.9f), 6).Move(Random.insideUnitCircle * 2.5f, -1f, 1.5f);
        if (fx != null && fx.ring.Length > 0) Fx.Frames(fx.ring, transform.position, glowColor, 0.35f, 6).Grow(1f, 2f);

        ScreenLabel label = WorldText.Create("Found label", null, 30f, glowColor);
        if (label != null)
        {
            label.SetText($"Found: {displayName}");
            label.WorldPosition = transform.position + Vector3.up * 0.9f;
            Destroy(label.gameObject, 2.5f);
        }
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    public void EditorSetup(string id, string itemName, FxLibrary library)
    {
        itemId = id; displayName = itemName; fx = library;
    }
#endif
}
