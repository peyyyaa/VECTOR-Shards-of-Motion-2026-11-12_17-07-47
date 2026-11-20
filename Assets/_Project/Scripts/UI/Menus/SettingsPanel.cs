using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Settings (shared by the title screen and the pause menu): master / music / sound volume, display mode, resolution,
/// Wren's tip timer, and a controls card for both players read from the real input bindings. Every change is saved
/// at once (SaveSystem) and applied (GameFlow). Back (button, Esc, gamepad B/East) closes it.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider master;
    [SerializeField] private Slider music;
    [SerializeField] private Slider sound;
    [SerializeField] private CycleOption display;
    [SerializeField] private CycleOption resolution;
    [SerializeField] private CycleOption tips;
    [SerializeField] private TMP_Text controlsText;
    [SerializeField] private Button back;
    [Tooltip("The project's input actions (InputSystem_Actions), for the controls card.")]
    [SerializeField] private InputActionAsset inputActions;

    private static readonly float[] TipDelays = { 0f, 60f, 90f, 120f };
    private static readonly string[] TipNames = { "Off", "60 s", "90 s", "120 s" };
    private Resolution[] resolutions = new Resolution[0];
    private Action onClose;
    private bool loading;

    public bool IsOpen => gameObject.activeSelf;

    /// <summary>Frame the panel last closed on, so the same Esc/B press does not also close the menu behind it.</summary>
    public static int ClosedFrame { get; private set; } = -1;

    private void Awake()
    {
        master.onValueChanged.AddListener(v => Changed(s => s.masterVolume = v));
        music.onValueChanged.AddListener(v => Changed(s => s.musicVolume = v));
        sound.onValueChanged.AddListener(v => Changed(s => s.soundVolume = v));
        back.onClick.AddListener(Close);
    }

    public void Open(Action closed)
    {
        onClose = closed;
        gameObject.SetActive(true);
        loading = true;
        SettingsData s = SaveSystem.Data.settings;
        master.value = s.masterVolume;
        music.value = s.musicVolume;
        sound.value = s.soundVolume;
        display.Setup(new[] { "Fullscreen", "Windowed" }, s.fullscreen ? 0 : 1, i => Changed(d => d.fullscreen = i == 0));

        resolutions = Screen.resolutions.GroupBy(r => (r.width, r.height)).Select(g => g.First()).ToArray();
        int current = Array.FindIndex(resolutions, r => r.width == (s.resolutionWidth > 0 ? s.resolutionWidth : Screen.width)
                                                       && r.height == (s.resolutionHeight > 0 ? s.resolutionHeight : Screen.height));
        resolution.Setup(resolutions.Select(r => $"{r.width} x {r.height}").ToArray(), Mathf.Max(0, current),
            i => Changed(d => { d.resolutionWidth = resolutions[i].width; d.resolutionHeight = resolutions[i].height; }));

        int tip = Array.IndexOf(TipDelays, s.tipDelay);
        tips.Setup(TipNames, tip < 0 ? 2 : tip, i => Changed(d => d.tipDelay = TipDelays[i]));
        loading = false;

        controlsText.text = ControlsCard();
        EventSystem.current?.SetSelectedGameObject(master.gameObject);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        ClosedFrame = Time.frameCount;
        Action closed = onClose;
        onClose = null;
        closed?.Invoke();
    }

    private void Update()
    {
        bool backPressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                           || Gamepad.all.Any(g => g.buttonEast.wasPressedThisFrame);
        if (backPressed) Close();
    }

    private void Changed(Action<SettingsData> change)
    {
        if (loading) return;
        change(SaveSystem.Data.settings);
        SaveSystem.SettingsUpdated();
    }

    // "Move: A / D   Jump: W ..." for each control scheme, straight from the bindings.
    private string ControlsCard()
    {
        if (inputActions == null) return "";
        InputActionMap map = inputActions.FindActionMap("Player");
        if (map == null) return "";
        (string group, string title)[] schemes =
        {
            ("KeyboardLeft", "Shared keyboard: Player 1"), ("KeyboardRight", "Shared keyboard: Player 2"), ("Gamepad", "Gamepad"),
        };
        string[] actions = { "Move", "Jump", "Dash", "Ability", "Interact" };
        StringBuilder sb = new StringBuilder();
        foreach ((string group, string title) in schemes)
        {
            List<string> parts = new List<string>();
            foreach (string a in actions)
            {
                InputAction action = map.FindAction(a);
                string keys = action != null ? action.GetBindingDisplayString(group: group) : "";
                if (!string.IsNullOrEmpty(keys)) parts.Add($"{a}: {keys}");
            }
            if (parts.Count > 0) sb.AppendLine($"<b>{title}</b>\n{string.Join("    ", parts)}\n");
        }
        sb.Append("Pause: Esc / Start");
        return sb.ToString();
    }

#if UNITY_EDITOR
    public void EditorSetup(Slider m, Slider mu, Slider so, CycleOption d, CycleOption r, CycleOption t, TMP_Text controls, Button b, InputActionAsset actions)
    {
        master = m; music = mu; sound = so; display = d; resolution = r; tips = t; controlsText = controls; back = b; inputActions = actions;
    }
#endif
}
