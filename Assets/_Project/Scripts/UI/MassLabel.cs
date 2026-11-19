using UnityEngine;

/// <summary>
/// Shows a block's mass ("2 kg") above it, read from its Rigidbody2D, so same-sized
/// crates can be told apart. Change the Rigidbody2D's Mass and the label follows.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MassLabel : FloatingLabel
{
    private Rigidbody2D body;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    protected override void Refresh()
    {
        SetText($"{body.mass:0.##} kg");
    }
}
