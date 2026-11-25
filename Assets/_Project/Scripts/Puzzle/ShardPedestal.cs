using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One of the two pedestals beside the Great Shard (finale, part 3). It is "held" while a player stands on it and keeps
/// their own Interact pressed (read through that player's PlayerInputReader). The Great Shard frees itself only while
/// BOTH pedestals are held, by two different players. Its rune light brightens with the shard's progress; a beam of
/// light runs from it to the shard while held.
/// Setup: a trigger Collider2D over the pedestal top (where a player stands); Beam = a SpriteRenderer with a 1x1 px
/// sprite (stretched to the shard); a Light2D for the rune.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShardPedestal : MonoBehaviour
{
    [SerializeField] private Transform shard;
    [SerializeField] private SpriteRenderer beam;
    [SerializeField] private Light2D rune;
    [SerializeField] private Color color = new Color(0.55f, 0.95f, 1f);

    private PlayerInputReader standing;
    private ScreenLabel prompt;

    /// <summary>The player holding this pedestal (null if none).</summary>
    public PlayerInputReader Holder => standing != null && standing.InteractHeld ? standing : null;

    private void OnTriggerStay2D(Collider2D other)
    {
        PlayerInputReader r = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerInputReader>() : null;
        if (r != null && (standing == null || r == standing)) standing = r;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerInputReader r = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerInputReader>() : null;
        if (r != null && r == standing) standing = null;
    }

    /// <summary>Called by the Great Shard every frame: progress 0..1 (glow), freed (prompt off).</summary>
    public void Show(float progress, bool freed)
    {
        bool held = Holder != null && !freed;
        if (rune != null) rune.intensity = 0.4f + 2f * progress + (held ? 0.6f : 0f);
        if (beam != null && shard != null)
        {
            beam.enabled = held;
            if (held)
            {
                Vector3 a = transform.position + Vector3.up * 0.6f, b = shard.position;
                Vector3 d = b - a;
                beam.transform.position = (a + b) * 0.5f;
                beam.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                beam.transform.localScale = new Vector3(d.magnitude * 32f, (2f + 4f * progress), 1f);
                beam.color = new Color(color.r, color.g, color.b, 0.35f + 0.5f * progress + 0.1f * Mathf.Sin(Time.time * 20f));
            }
        }
        bool showPrompt = !freed && standing != null && !held;
        if (showPrompt && prompt == null) prompt = WorldText.Create("Pedestal prompt", null, 22f, Color.white, 6f);
        if (prompt != null)
        {
            if (!showPrompt) { Destroy(prompt.gameObject); prompt = null; }
            else
            {
                prompt.SetText($"Hold {standing.InteractDisplayName} together");
                prompt.WorldPosition = transform.position + Vector3.up * 2.4f;
            }
        }
    }

    private void OnDisable()
    {
        if (prompt != null) Destroy(prompt.gameObject);
        prompt = null;
    }

#if UNITY_EDITOR
    public void EditorSetup(Transform greatShard, SpriteRenderer beamRenderer, Light2D runeLight, Color tint)
    {
        shard = greatShard; beam = beamRenderer; rune = runeLight; color = tint;
    }
#endif
}
