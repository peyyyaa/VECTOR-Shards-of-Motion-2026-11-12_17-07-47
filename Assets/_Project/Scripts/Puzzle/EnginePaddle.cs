using UnityEngine;

/// <summary>
/// The Momentum Engine's paddle: a solid block crates slam into. It hands the crate's momentum along the hit (p = m·v) to the
/// engine. Players don't count. Setup: a solid BoxCollider2D; Engine = the MomentumEngine.
/// </summary>
public class EnginePaddle : MonoBehaviour
{
    [SerializeField] private MomentumEngine engine;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D rb = collision.rigidbody;
        if (engine == null || rb == null || rb.bodyType != RigidbodyType2D.Dynamic || rb.GetComponent<PlayerMovement>() != null) return;
        Vector2 normal = collision.contactCount > 0 ? collision.GetContact(0).normal : Vector2.right;
        float speed = Mathf.Abs(Vector2.Dot(collision.relativeVelocity, normal));
        engine.AddMomentum(rb.mass * speed);
    }

#if UNITY_EDITOR
    public void EditorSetup(MomentumEngine e) => engine = e;
#endif
}
