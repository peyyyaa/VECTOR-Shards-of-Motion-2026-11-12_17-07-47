using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// The way out of the village into a region: the trailhead gateway, with two rune stones on the ground under it.
/// Each player standing on the stones lights one of them (cyan); with both lit for Hold Time seconds, the screen fades
/// to black and the region loads at the furthest room reached there (room 1 on a first visit).
/// No text by default. If one player waits there alone for Hint Delay seconds, a small hint appears above the gateway:
/// a dot per player (lit = on the stones) and "Together".
/// Setup: a trigger Collider2D covering the stones (VillageBuilder makes it, with the stones and their lights).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RegionGate : MonoBehaviour
{
    [SerializeField] private string region = "Region1";
    [SerializeField] private string regionName = "the Shattered Grounds";
    [SerializeField, Min(0f)] private float holdTime = 0.6f;

    [Header("Rune stones")]
    [Tooltip("Lit in order: the first when one player is on the stones, the second when both are.")]
    [SerializeField] private SpriteRenderer[] stones = new SpriteRenderer[0];
    [SerializeField] private Light2D[] stoneLights = new Light2D[0];
    [SerializeField] private Sprite stoneDark;
    [SerializeField] private Sprite stoneLit;
    [SerializeField, Min(0f)] private float stoneLightIntensity = 0.9f;

    [Header("Hint and fade")]
    [Tooltip("Seconds one player waits alone on the stones before the hint shows.")]
    [SerializeField, Min(0f)] private float hintDelay = 1.5f;
    [SerializeField] private Vector2 hintOffset = new Vector2(0f, 1.8f);
    [SerializeField, Min(0.01f)] private float fadeTime = 0.4f;

    private static readonly Color[] PlayerColors = { new Color32(232, 132, 48, 255), new Color32(72, 186, 206, 255) };

    private readonly HashSet<PlayerMovement> inside = new HashSet<PlayerMovement>();
    private float timer;
    private float aloneTimer;
    private bool leaving;
    private Hint hint;

    public string RegionName => regionName;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement p = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerMovement>() : null;
        if (p != null) inside.Add(p);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerMovement p = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerMovement>() : null;
        if (p != null) inside.Remove(p);
    }

    private void Update()
    {
        inside.RemoveWhere(p => p == null);
        int players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).Length;
        int on = leaving ? Mathf.Max(inside.Count, 2) : inside.Count;
        UpdateStones(on);
        if (leaving) return;

        bool all = players > 0 && inside.Count >= players;
        timer = all ? timer + Time.deltaTime : 0f;
        aloneTimer = inside.Count > 0 && !all ? aloneTimer + Time.deltaTime : 0f;
        ShowHint(aloneTimer >= hintDelay);

        if (all && timer >= holdTime)
        {
            leaving = true;
            ShowHint(false);
            StartCoroutine(FadeAndLeave());
        }
    }

    private void UpdateStones(int on)
    {
        for (int i = 0; i < stones.Length; i++)
        {
            bool lit = i < on;
            if (stones[i] != null) stones[i].sprite = lit ? stoneLit : stoneDark;
            if (i < stoneLights.Length && stoneLights[i] != null)
            {
                float target = lit ? stoneLightIntensity * (0.85f + 0.15f * Mathf.Sin(Time.time * 4f + i)) : 0f;
                stoneLights[i].intensity = Mathf.MoveTowards(stoneLights[i].intensity, target, Time.deltaTime * 4f);
            }
        }
    }

    private IEnumerator FadeAndLeave()
    {
        Image black = null;
        UIRoot ui = UIRoot.Instance;
        if (ui != null)
        {
            GameObject go = new GameObject("Gate Fade", typeof(RectTransform));
            go.transform.SetParent(ui.transform, false);
            go.transform.SetAsLastSibling();
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            black = go.AddComponent<Image>();
            black.color = new Color(0f, 0f, 0f, 0f);
            black.raycastTarget = false;
        }
        for (float t = 0f; t < fadeTime && black != null; t += Time.unscaledDeltaTime)
        {
            black.color = new Color(0f, 0f, 0f, t / fadeTime);
            yield return null;
        }
        if (black != null) black.color = Color.black;
        int room = SaveSystem.Data.region == region ? SaveSystem.Data.furthestRoom : 1;
        GameFlow.EnterRegion(region, room);
    }

    // ------------------------------------------------------------------ the "Together" hint

    private void ShowHint(bool show)
    {
        if (!show) { if (hint != null) hint.Remove(); hint = null; return; }
        if (hint == null) hint = Hint.Create();
        if (hint == null) return;
        for (int i = 0; i < PlayerColors.Length; i++) hint.SetPlayer(i, IsInside(i));
        hint.WorldPosition = transform.position + (Vector3)hintOffset;
    }

    private bool IsInside(int playerIndex)
    {
        foreach (PlayerMovement p in inside)
        {
            PlayerInputReader r = p != null ? p.GetComponent<PlayerInputReader>() : null;
            if (r != null && r.PlayerIndex == playerIndex) return true;
        }
        return false;
    }

    private void OnDisable() => ShowHint(false);

    /// <summary>Two player dots and "Together", in the interact-prompt style, pinned above the gateway.</summary>
    private class Hint
    {
        private RectTransform root;
        private readonly Image[] dots = new Image[2];

        public Vector3 WorldPosition
        {
            set
            {
                Camera cam = Camera.main;
                if (cam == null || root == null) return;
                Vector3 s = cam.WorldToScreenPoint(value);
                root.position = new Vector3(Mathf.Round(s.x), Mathf.Round(s.y), 0f);
            }
        }

        public static Hint Create()
        {
            UIRoot ui = UIRoot.Instance;
            if (ui == null || ui.LabelContainer == null) return null;
            Hint h = new Hint();
            GameObject go = new GameObject("Gate hint", typeof(RectTransform));
            go.transform.SetParent(ui.LabelContainer, false);
            h.root = (RectTransform)go.transform;
            h.root.sizeDelta = Vector2.zero;
            for (int i = 0; i < 2; i++)
            {
                GameObject d = new GameObject("Dot_P" + (i + 1), typeof(RectTransform));
                d.transform.SetParent(h.root, false);
                RectTransform rt = (RectTransform)d.transform;
                rt.sizeDelta = new Vector2(15f, 15f);
                rt.anchoredPosition = new Vector2(-62f + i * 20f, 0f);
                h.dots[i] = d.AddComponent<Image>();
                h.dots[i].sprite = ui.PromptDotSprite;
                h.dots[i].raycastTarget = false;
            }
            GameObject t = new GameObject("Text", typeof(RectTransform));
            t.transform.SetParent(h.root, false);
            TextMeshProUGUI text = t.AddComponent<TextMeshProUGUI>();
            if (ui.PaperFont != null) text.font = ui.PaperFont;
            text.text = "Together";
            text.fontSize = 26f;
            text.color = new Color32(255, 251, 240, 255);
            text.outlineWidth = 0.22f;
            text.outlineColor = new Color32(43, 28, 18, 230);
            text.alignment = TextAlignmentOptions.Left;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            RectTransform tr = text.rectTransform;
            tr.pivot = new Vector2(0f, 0.5f);
            tr.sizeDelta = new Vector2(140f, 34f);
            tr.anchoredPosition = new Vector2(-34f, 1f);
            return h;
        }

        public void SetPlayer(int i, bool here)
        {
            Color c = PlayerColors[i];
            c.a = here ? 1f : 0.3f;
            dots[i].color = c;
        }

        public void Remove() { if (root != null) Object.Destroy(root.gameObject); }
    }

#if UNITY_EDITOR
    public void EditorSetup(string regionId, string displayName) { region = regionId; regionName = displayName; }

    public void EditorSetupStones(SpriteRenderer[] renderers, Light2D[] lights, Sprite dark, Sprite lit)
    {
        stones = renderers; stoneLights = lights; stoneDark = dark; stoneLit = lit;
    }
#endif
}
