using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A pressure plate that activates when the total weight resting on it reaches a threshold.
///
/// Physics: weight is the force of gravity on a mass,  W = m * g  (newtons).
///   Example: a 2 kg block weighs 2 * 9.81 ≈ 19.6 N; a 1 kg player weighs ≈ 9.8 N.
///
/// Stacks: weight passes down through whatever is underneath (a normal-force chain).
///   If a player stands on a block that's on the plate, the plate feels BOTH:
///   W_total = Σ m * g over the whole stack. So the plate counts bodies in its zone, plus
///   anything resting on top of them, plus anything on top of those, and so on.
///
/// Simplifications:
/// - A real scale measures the normal force pushing on it, which equals m * g only when
///   the stack is at rest. This plate adds up m * g for every body in the stack — even one
///   that's bouncing or still falling through.
/// - A body resting on the stack AND on something else (e.g., straddling a ledge) is
///   counted with its full weight, though really only part of it would press down here.
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

    // Reused each step while walking up the stack.
    private readonly HashSet<Rigidbody2D> stack = new HashSet<Rigidbody2D>();
    private readonly Queue<Rigidbody2D> toVisit = new Queue<Rigidbody2D>();
    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];

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
        // 1. Start from the bodies directly in the plate's zone.
        stack.Clear();
        toVisit.Clear();
        removeList.Clear();
        foreach (KeyValuePair<Rigidbody2D, int> entry in bodiesOnPlate)
        {
            Rigidbody2D rb = entry.Key;
            if (rb == null || !rb.gameObject.activeInHierarchy)
            {
                removeList.Add(rb); // destroyed or disabled while on the plate
                continue;
            }
            if (rb.bodyType == RigidbodyType2D.Dynamic && stack.Add(rb))
            {
                toVisit.Enqueue(rb);
            }
        }
        foreach (Rigidbody2D rb in removeList)
        {
            bodiesOnPlate.Remove(rb);
        }

        // 2. Walk up the stack: add anything resting on top of a body we've already counted.
        while (toVisit.Count > 0)
        {
            Rigidbody2D below = toVisit.Dequeue();
            int count = below.GetContacts(contacts);
            for (int i = 0; i < count; i++)
            {
                Rigidbody2D above = OtherBody(contacts[i], below);
                if (above != null && IsRestingOn(above, below, contacts[i]) && stack.Add(above))
                {
                    toVisit.Enqueue(above);
                }
            }
        }

        // 3. Total weight of the whole stack:  W = Σ m * g
        float g = Mathf.Abs(Physics2D.gravity.y);
        float total = 0f;
        foreach (Rigidbody2D rb in stack)
        {
            total += rb.mass * g;
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

    // The body on the other side of a contact from 'self' (or null for static colliders).
    private static Rigidbody2D OtherBody(ContactPoint2D contact, Rigidbody2D self)
    {
        Rigidbody2D a = contact.collider != null ? contact.collider.attachedRigidbody : null;
        Rigidbody2D b = contact.otherCollider != null ? contact.otherCollider.attachedRigidbody : null;
        Rigidbody2D other = a == self ? b : a;
        if (other == null || other == self || other.bodyType != RigidbodyType2D.Dynamic) return null;
        return other;
    }

    // 'above' is resting on 'below' if the contact surface is mostly horizontal (a top/bottom
    // contact, not side-by-side) and 'above' sits higher. We check the normal's size, not its
    // sign, so it works whichever side of the contact Unity reports it from.
    private static bool IsRestingOn(Rigidbody2D above, Rigidbody2D below, ContactPoint2D contact)
    {
        bool verticalContact = Mathf.Abs(contact.normal.y) > 0.7f;
        bool isHigher = above.worldCenterOfMass.y > below.worldCenterOfMass.y;
        return verticalContact && isHigher;
    }

    private void UpdateColor()
    {
        if (plateRenderer != null)
        {
            plateRenderer.color = isPressed ? pressedColor : releasedColor;
        }
    }
}
