using UnityEngine;

/// <summary>
/// Shows a block's mass ("2 kg") above it, read from its Rigidbody2D, so same-sized
/// crates can be told apart. Change the Rigidbody2D's Mass and the label follows.
/// Shown when a player is near, and while the block moves (plus Linger seconds after it stops).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MassLabel : FloatingLabel
{
    [Tooltip("Speed (m/s) above which the block counts as moving, so its label shows.")]
    [SerializeField] private float movingSpeed = 0.3f;
    [Tooltip("Seconds the label stays after the block stops moving.")]
    [SerializeField] private float linger = 1f;

    private Rigidbody2D body;
    private float busyUntil = float.NegativeInfinity;

    protected override bool IsBusy => Time.time < busyUntil;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    protected override void Refresh()
    {
        if (body.linearVelocity.sqrMagnitude > movingSpeed * movingSpeed) busyUntil = Time.time + linger;
        SetText($"{body.mass:0.##} kg");
    }
}
