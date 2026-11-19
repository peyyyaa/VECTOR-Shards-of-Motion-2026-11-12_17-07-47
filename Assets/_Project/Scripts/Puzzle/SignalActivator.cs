using UnityEngine;

/// <summary>
/// Turns components and objects on while a signal (plate, lever, relay) is on, and off while
/// it's off. Use it for things that have no signal input of their own, e.g. a water current
/// (Unity's AreaEffector2D), lights, particle effects or extra colliders.
///
/// Example (water gate): one lever → the gate's Mover (its own Signal field) AND this
/// activator, which switches the current's AreaEffector2D on when the gate opens.
///
/// It follows the signal every physics step, so a room reset (which resets the lever) also
/// switches the targets back. Don't list this activator's own GameObject in Game Objects:
/// switching it off would stop the activator itself.
/// </summary>
public class SignalActivator : MonoBehaviour
{
    [SerializeField] private SignalSource signal;
    [SerializeField] private bool invert = false;

    [Tooltip("Components to enable/disable (e.g., an AreaEffector2D).")]
    [SerializeField] private Behaviour[] behaviours = new Behaviour[0];
    [Tooltip("Objects to show/hide.")]
    [SerializeField] private GameObject[] gameObjects = new GameObject[0];

    private bool applied;
    private bool lastState;

    private void FixedUpdate()
    {
        if (signal == null) return;

        bool on = signal.IsActive != invert;
        if (applied && on == lastState) return;
        applied = true;
        lastState = on;

        foreach (Behaviour b in behaviours)
        {
            if (b != null) b.enabled = on;
        }
        foreach (GameObject g in gameObjects)
        {
            if (g != null && g != gameObject) g.SetActive(on);
        }
    }
}
