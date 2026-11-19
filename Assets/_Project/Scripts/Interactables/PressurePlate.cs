using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A pressure plate that activates when the total weight resting on it reaches a threshold.
///
/// Physics: weight is the force of gravity on a mass,  W = m * g  (newtons).
///   g is the world gravity from Project Settings > Physics 2D (currently 29.43 m/s², 3× Earth).
///   Example: a 2 kg block weighs 2 * 29.43 ≈ 58.9 N; a 1 kg player weighs ≈ 29.4 N.
///
/// Stacks: weight passes down through whatever is underneath (a normal-force chain).
///   If a player stands on a block that's on the plate, the plate feels BOTH:
///   W_total = Σ m * g over the whole stack. So the plate counts bodies in its zone, plus
///   anything resting on top of them, plus anything on top of those, and so on.
///
/// Resting check: a body in the plate's zone only counts once it's moving slower than
/// Max Resting Speed (default 0.5 m/s). A block sliding across, or a player walking over,
/// doesn't press the plate until it stops on it. Bodies stacked on a resting body count as before.
///
/// Simplifications:
/// - A real scale measures the normal force pushing on it, which equals m * g only when
///   the stack is at rest. This plate adds up m * g for every counted body — the resting check
///   makes that much closer to true, but a body bouncing on top of the stack still counts.
/// - A body resting on the stack AND on something else (e.g., straddling a ledge) is
///   counted with its full weight, though really only part of it would press down here.
/// - We use the world's g and ignore each body's Gravity Scale. Every body normally uses
///   Gravity Scale 1; abilities change it only briefly (float, dash), and weight on the plate
///   shouldn't flicker because of that.
///
/// Setup: needs a Collider2D with "Is Trigger" ticked. Wire On Pressed / On Released in the
/// Inspector to whatever should react (e.g., ExitDoor.Open / ExitDoor.Close).
/// If several plates/levers control one thing, put them in a PuzzleRelay's Inputs instead.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PressurePlate : SignalSource
{
    [Header("Activation")]
    [Tooltip("Total weight needed to press the plate, in newtons (N). With g = 29.43: 1 kg ≈ 29.4 N, 2 kg ≈ 58.9 N.")]
    [SerializeField] private float requiredWeight = 45f;
    [Tooltip("A body in the plate's zone only counts once it's moving slower than this (m/s), so a block sliding or passing over doesn't trigger the plate.")]
    [SerializeField] private float maxRestingSpeed = 0.5f;

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

    /// <summary>SignalSource: on while pressed, so a PuzzleRelay can use this plate as an input.</summary>
    public override bool IsActive => isPressed;

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
            // Only bodies that have settled count; something sliding across doesn't press the plate.
            bool resting = rb.linearVelocity.magnitude <= maxRestingSpeed;
            if (rb.bodyType == RigidbodyType2D.Dynamic && resting && stack.Add(rb))
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
