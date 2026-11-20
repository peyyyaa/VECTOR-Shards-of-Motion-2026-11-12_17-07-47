using System;
using UnityEngine;

/// <summary>
/// One visible piece of a Village Fund restoration project (Docs/Village_Art_Plan.md). Shows its Broken group until the
/// project is funded, then its Restored group. Several objects can share a project id (e.g. every lamp is "lamps").
/// The state is the save event "fund_&lt;id&gt;"; RestorationProject.Restore(id) funds a project (the shop will call it)
/// and every piece updates at once, with a short sparkle.
/// Setup: set the Project Id, and drag the two child groups into Broken / Restored (either may be empty).
/// </summary>
public class RestorationProject : MonoBehaviour
{
    /// <summary>The projects, in shop order.</summary>
    public static readonly string[] All = { "well", "lamps", "garden", "shrine" };

    public static event Action Changed;

    [SerializeField] private string projectId = "well";
    [SerializeField] private GameObject broken;
    [SerializeField] private GameObject restored;
    [Tooltip("For the sparkle when the project is funded (optional).")]
    [SerializeField] private FxLibrary fx;

    public string ProjectId => projectId;

    public static string EventId(string id) => "fund_" + id;
    public static bool IsRestored(string id) => SaveSystem.HasEvent(EventId(id));

    public static void Restore(string id)
    {
        SaveSystem.MarkEvent(EventId(id));
        Changed?.Invoke();
    }

    /// <summary>Dev tools only: un-fund a project (the save normally never loses an event).</summary>
    public static void DevUnrestore(string id)
    {
        if (SaveSystem.Data.events != null && SaveSystem.Data.events.Remove(EventId(id))) SaveSystem.Save();
        Changed?.Invoke();
    }

    private void OnEnable()
    {
        Changed += Refresh;
        Apply(false);
    }

    private void OnDisable() => Changed -= Refresh;

    private void Refresh() => Apply(true);

    private void Apply(bool announce)
    {
        bool done = IsRestored(projectId);
        bool wasRestored = restored != null && restored.activeSelf;
        if (broken != null) broken.SetActive(!done);
        if (restored != null) restored.SetActive(done);
        if (announce && done && !wasRestored) Sparkle();
    }

    private void Sparkle()
    {
        if (fx == null || fx.sparkle == null || restored == null) return;
        Bounds b = new Bounds(restored.transform.position, Vector3.one);
        foreach (SpriteRenderer sr in restored.GetComponentsInChildren<SpriteRenderer>()) b.Encapsulate(sr.bounds);
        for (int i = 0; i < 10; i++)
        {
            Vector3 at = new Vector3(UnityEngine.Random.Range(b.min.x, b.max.x), UnityEngine.Random.Range(b.min.y, b.max.y), 0f);
            Fx.Sprite(fx.sparkle, at, new Color(1f, 0.95f, 0.7f), UnityEngine.Random.Range(0.6f, 1.1f), 3).Move(Vector2.up * 0.4f);
        }
    }

#if UNITY_EDITOR
    public void EditorSetup(string id, GameObject brokenGroup, GameObject restoredGroup, FxLibrary library)
    {
        projectId = id; broken = brokenGroup; restored = restoredGroup; fx = library;
    }
#endif
}
