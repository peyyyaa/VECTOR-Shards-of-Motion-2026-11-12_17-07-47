using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A pressure plate that activates when the total weight resting on it reaches a threshold.
///
/// Physics: weight is the force of gravity on a mass,  W = m * g  (newtons).
///   Example: a 2 kg block weighs 2 * 9.81 ≈ 19.6 N; a 1 kg player weighs ≈ 9.8 N.
///
/// Simplifications:
/// - A real scale measures the normal force pushing on it, which equals m * g only when
///   the object is at rest. This plate simply adds up m * g for every Rigidbody2D inside
///   its trigger zone — even one that's bouncing or still falling through.
/// - We use the world's g (9.81 m/s²) and ignore each body's Gravity Scale, because Gravity
///   Scale is a game-feel tweak (the player uses 3 for snappy jumps), not a change of mass.
///
/// Setup: needs a Collider2D with "Is Trigger" ticked. Wire On Pressed / On Released in the
/// Inspector to whatever should react (e.g., ExitDoor.Open / ExitDoor.Close).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PressurePlate : MonoBehaviour
{
    [Header("Activation")]
    [Tooltip("Total weight needed to press the plate, in newtons (N). 2 kg ≈ 19.6 N, 1 kg ≈ 9.8 N.")]
    [SerializeField] private float requiredWeight = 15f;

    [Header("Feedback (optional)")]
    [SerializeField] private SpriteRenderer plateRenderer;
    [SerializeField] private Color releasedColor = new Color(0.6f, 0.6f, 0.2f);
    [SerializeField] private Color pressedColor = new Color(0.2f, 0.9f, 0.3f);

    [Header("Events")]
    [SerializeField] private UnityEvent onPressed;
    [SerializeField] private UnityEvent onReleased;

    [Header("Debug (read-only)")]
    [Tooltip("Current total weight on the plate in newtons. Updated during Play; editing it does nothing.")]
    [SerializeField] private float currentWeight;

    // How many of each body's colliders are inside the trigger (one body can have several).
    private readonly Dictionary<Rigidbody2D, int> bodiesOnPlate = new Dictionary<Rigidbody2D, int>();
    private readonly List<Rigidbody2D> removeList = new List<Rigidbody2D>();
    private bool isPressed;

    public bool IsPressed => isPressed;

    private void Reset()
    {
        // Called when the component is first added: make the collider a trigger automatically.
        GetComponent<Collider2D>().isTrigger = true;
        plateRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        UpdateColor();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        bodiesOnPlate.TryGetValue(rb, out int count);
        bodiesOnPlate[rb] = count + 1;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null || !bodiesOnPlate.TryGetValue(rb, out int count)) return;

        if (count <= 1) bodiesOnPlate.Remove(rb);
        else bodiesOnPlate[rb] = count - 1;
    }

    private void FixedUpdate()
    {
        float g = Mathf.Abs(Physics2D.gravity.y);
        float total = 0f;

        removeList.Clear();
        foreach (KeyValuePair<Rigidbody2D, int> entry in bodiesOnPlate)
        {
            Rigidbody2D rb = entry.Key;
            if (rb == null || !rb.gameObject.activeInHierarchy)
            {
                removeList.Add(rb); // destroyed or disabled while on the plate
                continue;
            }
            total += rb.mass * g; // W = m * g
        }
        foreach (Rigidbody2D rb in removeList)
        {
            bodiesOnPlate.Remove(rb);
        }

        currentWeight = total;

        bool shouldBePressed = total >= requiredWeight;
        if (shouldBePressed != isPressed)
        {
            isPressed = shouldBePressed;
            UpdateColor();

            if (isPressed) onPressed.Invoke();
            else onReleased.Invoke();
        }
    }

    private void UpdateColor()
    {
        if (plateRenderer != null)
        {
            plateRenderer.color = isPressed ? pressedColor : releasedColor;
        }
    }
}
