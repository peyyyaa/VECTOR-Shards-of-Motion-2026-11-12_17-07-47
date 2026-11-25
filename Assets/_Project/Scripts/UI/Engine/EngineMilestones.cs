using UnityEngine;

/// <summary>
/// The Heart Engine's milestone board (finale, part 2): one panel that says what the momentum is for.
///   Momentum 42 / 100 kg·m/s   (friction -3 kg·m/s every second)
///   30 · Hall lamps: lit
///   60 · Shard platform: up            (it holds only while the engine stays at 60 or more)
///   100 · Awaken the Heart
/// A milestone shows bright while its signal is on, a softer "reached" colour once reached but now drained (or, for a
/// stage whose effect stays, e.g. the lamps, "lit"), and grey until then. Purely visual: reads the engine and the
/// stage signals (EngineStage / the engine's own gate). Replaces the engine's small gauge in this room (MomentumEngine
/// Show Gauge off). Setup: the engine; per milestone a signal, its threshold, its name, its words when on / drained.
/// </summary>
public class EngineMilestones : MonoBehaviour
{
    [System.Serializable]
    public class Milestone
    {
        public SignalSource signal;
        public float threshold;
        public string name;
        [Tooltip("Shown after the name while the signal is on, e.g. \"lit\".")]
        public string onWord;
        [Tooltip("Shown once reached but no longer on (e.g. \"sank: refill\"). Empty = keeps the on word (the effect stays).")]
        public string drainedWord;
    }

    [SerializeField] private MomentumEngine engine;
    [SerializeField] private Milestone[] milestones = new Milestone[0];
    [Tooltip("Room-local position of the panel's centre.")]
    [SerializeField] private Vector2 at;
    [SerializeField, Min(0f)] private float drain = 3f;

    private ScreenLabel panel;
    private bool[] reached = new bool[0];
    private Transform origin;

    private void Awake()
    {
        origin = transform.parent != null ? transform.parent : transform;
    }

    private void Update()
    {
        if (engine == null) return;
        if (!engine.Installed) { Hide(); return; }
        if (reached.Length != milestones.Length) reached = new bool[milestones.Length];
        if (panel == null)
        {
            panel = WorldText.Create("Engine milestones", null, 21f, Color.white, 6.2f);
            if (panel == null) return;
        }
        string text = $"<b>Momentum {engine.Momentum:0} / {engine.MaxMomentum:0} kg·m/s</b>\n" +
                      $"<size=72%><color=#b8c4d4>friction takes {drain:0} kg·m/s every second</color></size>";
        for (int i = 0; i < milestones.Length; i++)
        {
            Milestone m = milestones[i];
            bool on = m.signal != null && m.signal.IsActive;
            if (on) reached[i] = true;
            string line = $"{m.threshold:0} · {m.name}";
            if (on) line = $"<color=#8dffb0>{line}: {m.onWord}</color>";
            else if (reached[i]) line = string.IsNullOrEmpty(m.drainedWord) ? $"<color=#8dffb0>{line}: {m.onWord}</color>" : $"<color=#ffd27a>{line}: {m.drainedWord}</color>";
            else line = $"<color=#9aa3ad>{line}</color>";
            text += "\n" + line;
        }
        panel.SetText(text);
        panel.WorldPosition = origin.TransformPoint(at);
    }

    private void Hide()
    {
        if (panel != null) Destroy(panel.gameObject);
        panel = null;
    }

    private void OnDisable() => Hide();

#if UNITY_EDITOR
    public void EditorSetup(MomentumEngine target, Milestone[] list, Vector2 position, float drainPerSecond)
    {
        engine = target; milestones = list; at = position; drain = drainPerSecond;
    }
#endif
}
