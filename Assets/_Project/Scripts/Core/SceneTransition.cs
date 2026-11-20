using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The transition between scenes (GameFlow.Load): fade to black, a title card while the next scene loads, fade in.
///   Into a region:   its name (large), its subtitle (e.g. "Region 1 · Forces &amp; Momentum") and progress from the save
///                    ("Shards 6/24 · Machine parts 1/2 · Rooms solved 3/8", or "First visit").
///   Into the village: "Alderbrook" / "Home".  To the title screen: just the fade.
/// An ink flourish draws itself under the name. The card stays at least Card Time seconds (never cutting loading short)
/// and any key or button skips it after Skip After seconds. Uses unscaled time (menus may have stopped time).
///
/// Setup: the SceneTransition prefab in Assets/_Project/Resources (built by Create UI Prefabs). It survives the scene
/// change on its own and removes itself when done. Region names come from the RegionMapData assets in Regions.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private CanvasGroup card;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text subtitle;
    [SerializeField] private TMP_Text progress;
    [SerializeField] private Image flourish;
    [Tooltip("Region map data, used for each region's name, subtitle and totals.")]
    [SerializeField] private RegionMapData[] regions = new RegionMapData[0];
    [SerializeField, Min(0.01f)] private float fadeTime = 0.4f;
    [SerializeField, Min(0f)] private float cardTime = 1.5f;
    [SerializeField, Min(0f)] private float skipAfter = 0.5f;

    private const string ResourcePath = "SceneTransition";
    private static SceneTransition running;

    /// <summary>True while a transition is playing (GameFlow ignores further loads meanwhile).</summary>
    public static bool Busy => running != null;

    /// <summary>Loads 'scene' through the transition. Returns false if the prefab is missing (the caller loads directly).</summary>
    public static bool Go(string scene, string regionId)
    {
        if (running != null) return true;
        SceneTransition prefab = Resources.Load<SceneTransition>(ResourcePath);
        if (prefab == null) return false;
        running = Instantiate(prefab);
        DontDestroyOnLoad(running.gameObject);
        running.StartCoroutine(running.Play(scene, regionId));
        return true;
    }

    private IEnumerator Play(string scene, string regionId)
    {
        bool showCard = Fill(scene, regionId);
        group.alpha = 0f;
        card.alpha = 0f;
        flourish.fillAmount = 0f;

        // 1. Fade to black.
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime) { group.alpha = t / fadeTime; yield return null; }
        group.alpha = 1f;

        // 2. Load behind the card.
        AsyncOperation load = SceneManager.LoadSceneAsync(scene);
        load.allowSceneActivation = false;
        float shown = 0f;
        while (true)
        {
            shown += Time.unscaledDeltaTime;
            if (showCard)
            {
                card.alpha = Mathf.Clamp01(shown / 0.25f);
                flourish.fillAmount = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((shown - 0.15f) / 0.8f));
            }
            bool loaded = load.progress >= 0.9f;
            bool skipped = shown >= skipAfter && AnyPress();
            if (loaded && (!showCard || shown >= cardTime || skipped)) break;
            yield return null;
        }

        // 3. Switch scenes, then fade the card and the black away.
        load.allowSceneActivation = true;
        while (!load.isDone) yield return null;
        yield return null; // one frame for the new scene to place its players and camera
        float cardStart = card.alpha;
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            float k = t / fadeTime;
            card.alpha = cardStart * (1f - k);
            group.alpha = 1f - k;
            yield return null;
        }
        running = null;
        Destroy(gameObject);
    }

    // Card text for the destination; false = no card (just the fade).
    private bool Fill(string scene, string regionId)
    {
        if (scene == GameFlow.VillageScene)
        {
            title.text = "Alderbrook";
            subtitle.text = "Home";
            int spend = VillageFund.Balance;
            progress.text = spend > 0 ? $"Village Fund: {spend} shard{(spend == 1 ? "" : "s")} to spend" : "";
            return true;
        }
        RegionMapData map = regionId != null ? regions.FirstOrDefault(r => r != null && r.regionId == regionId) : null;
        if (map == null) return false;
        title.text = map.regionName;
        subtitle.text = map.subtitle;
        if (!SaveSystem.HasEvent("visited_" + map.regionId)) progress.text = "First visit";
        else
        {
            (int s, int st) = map.ShardTotals();
            (int p, int pt) = map.PartTotals();
            (int r, int rt) = map.SolvedTotals();
            progress.text = $"Shards {s}/{st}   ·   Machine parts {p}/{pt}   ·   Rooms solved {r}/{rt}";
        }
        return true;
    }

    private static bool AnyPress() =>
        (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        || Gamepad.all.Any(g => g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame)
        || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

#if UNITY_EDITOR
    public void EditorSetup(CanvasGroup all, CanvasGroup cardGroup, TMP_Text titleText, TMP_Text subtitleText, TMP_Text progressText,
        Image flourishImage, RegionMapData[] regionMaps)
    {
        group = all; card = cardGroup; title = titleText; subtitle = subtitleText; progress = progressText; flourish = flourishImage;
        regions = regionMaps;
    }
#endif
}
