using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// A gateway with two rune stones on the ground: the village trailhead (into a region) and the trail home at the
/// west edge of a region's first room (To Village). Each player standing on the stones lights one of them (cyan);
/// with both lit for Hold Time seconds the players leave (GameFlow: transition card while loading):
///   into the region at the furthest room reached there (room 1 on a first visit), or home to the village trailhead.
/// Armed only once nobody is on the stones: players who arrive standing on them (spawn points) must step off first.
/// No text by default. If one player waits there alone for Hint Delay seconds, a small hint appears above the gateway:
/// a dot per player (lit = on the stones) and "Together".
/// Setup: a trigger Collider2D covering the stones (VillageBuilder / Room 1 > Add Trail Home make it, with the stones).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RegionGate : MonoBehaviour
{
    [Tooltip("On: this gate leads home to the village (trail home). Off: into Region.")]
    [SerializeField] private bool toVillage;
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

    [Header("Hint")]
    [Tooltip("Seconds one player waits alone on the stones before the hint shows.")]
    [SerializeField, Min(0f)] private float hintDelay = 1.5f;
    [SerializeField] private Vector2 hintOffset = new Vector2(0f, 1.8f);

    private static readonly Color[] PlayerColors = { new Color32(232, 132, 48, 255), new Color32(72, 186, 206, 255) };

    private readonly HashSet<PlayerMovement> inside = new HashSet<PlayerMovement>();
    private float timer;
    private float aloneTimer;
    private bool leaving;
    private bool armed;
    private float disarmedUntil;
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
        if (!armed && inside.Count == 0 && Time.time >= disarmedUntil) armed = true; // everyone has stepped off (or was never on)
        int players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).Length;
        int on = leaving ? 2 : armed ? inside.Count : 0;
        UpdateStones(on);
        if (leaving || !armed) { if (!leaving) ShowHint(false); return; }

        bool all = players > 0 && inside.Count >= players;
        timer = all ? timer + Time.deltaTime : 0f;
        aloneTimer = inside.Count > 0 && !all ? aloneTimer + Time.deltaTime : 0f;
        ShowHint(aloneTimer >= hintDelay);

        if (all && timer >= holdTime)
        {
            leaving = true;
            ShowHint(false);
            if (toVillage) GameFlow.ToVillage();
            else GameFlow.EnterRegion(region, SaveSystem.Data.region == region ? SaveSystem.Data.furthestRoom : 1);
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

    // Players put straight onto the stones (spawn, travel, reset) must step off before the gate works.
    // A short wait lets the trigger notice them first.
    private void OnEnable()
    {
        RoomManager.PlayersPlaced += Disarm;
        disarmedUntil = Time.time + 0.25f;
    }

    private void Disarm()
    {
        if (leaving) return;
        armed = false;
        disarmedUntil = Time.time + 0.25f;
    }

    private void OnDisable()
    {
        RoomManager.PlayersPlaced -= Disarm;
        ShowHint(false);
    }

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
    public void EditorSetup(string regionId, string displayName) { region = regionId; regionName = displayName; toVillage = false; }

    public void EditorSetupHome() { toVillage = true; regionName = "Alderbrook"; }

    public void EditorSetupStones(SpriteRenderer[] renderers, Light2D[] lights, Sprite dark, Sprite lit)
    {
        stones = renderers; stoneLights = lights; stoneDark = dark; stoneLit = lit;
    }
#endif
}
