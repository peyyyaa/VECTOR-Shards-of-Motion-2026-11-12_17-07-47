using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The way out of the village into a region. When BOTH players stand inside this trigger (for Hold Time seconds),
/// the region loads, at the furthest room reached there (room 1 on a first visit). While only one is inside,
/// a hint asks for the other. Setup: a trigger Collider2D that players can enter.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RegionGate : MonoBehaviour
{
    [SerializeField] private string region = "Region1";
    [SerializeField] private string regionName = "the Shattered Grounds";
    [SerializeField, Min(0f)] private float holdTime = 0.6f;
    [SerializeField] private Vector2 hintOffset = new Vector2(0f, 2.2f);

    private readonly HashSet<PlayerMovement> inside = new HashSet<PlayerMovement>();
    private float timer;
    private bool leaving;
    private ScreenLabel hint;

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
        if (leaving) return;
        int players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).Length;
        bool all = players > 0 && inside.Count >= players;
        timer = all ? timer + Time.deltaTime : 0f;
        ShowHint(inside.Count > 0 && !all ? "Both players: step in together" : inside.Count > 0 ? $"Into {regionName}..." : null);
        if (all && timer >= holdTime)
        {
            leaving = true;
            int room = SaveSystem.Data.region == region ? SaveSystem.Data.furthestRoom : 1;
            GameFlow.EnterRegion(region, room);
        }
    }

    private void ShowHint(string text)
    {
        if (text == null) { if (hint != null) Destroy(hint.gameObject); hint = null; return; }
        if (hint == null) hint = WorldText.Create("Gate hint", null, 26f, new Color(0.75f, 0.95f, 1f), 6f);
        if (hint == null) return;
        hint.SetText(text);
        hint.WorldPosition = transform.position + (Vector3)hintOffset;
    }

#if UNITY_EDITOR
    public void EditorSetup(string regionId, string displayName) { region = regionId; regionName = displayName; }
#endif
}
