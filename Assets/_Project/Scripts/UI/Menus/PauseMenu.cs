using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Pause menu (part of UI_Canvas). Either player opens it with Esc or a gamepad's Start button; Esc / Start / B
/// closes it again. While paused, time stops and both players' controls are switched off.
///   Resume | Restart Room | Journal (re-read this room's entry) | Settings | Return to Village | Quit to Title
/// "Return to Village" only shows once a Village scene exists. Restart Room and Journal need a RoomManager.
///
/// Note: the pause buttons are read straight from the keyboard/gamepads (a menu key, not gameplay input).
/// Gameplay itself still reads input only through each player's PlayerInputReader.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button resume;
    [SerializeField] private Button restartRoom;
    [SerializeField] private Button journal;
    [SerializeField] private Button settings;
    [SerializeField] private Button village;
    [SerializeField] private Button title;
    [SerializeField] private SettingsPanel settingsPanel;

    private RoomManager manager;
    private LocalCoopSpawner spawner;

    public static bool IsPaused { get; private set; }

    private void Awake()
    {
        resume.onClick.AddListener(Resume);
        restartRoom.onClick.AddListener(() => { Resume(); if (manager != null) manager.ResetCurrentRoom(); });
        journal.onClick.AddListener(OpenJournal);
        settings.onClick.AddListener(() => { panel.SetActive(false); settingsPanel.Open(ShowPanel); });
        village.onClick.AddListener(GameFlow.ToVillage);
        title.onClick.AddListener(GameFlow.ToTitle);
        panel.SetActive(false);
        settingsPanel.gameObject.SetActive(false);
        IsPaused = false;
    }

    private void Start()
    {
        manager = FindFirstObjectByType<RoomManager>();
        spawner = FindFirstObjectByType<LocalCoopSpawner>();
        restartRoom.gameObject.SetActive(manager != null);
        journal.gameObject.SetActive(manager != null);
        village.gameObject.SetActive(GameFlow.VillageExists);
    }

    private void Update()
    {
        bool pressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                       || Gamepad.all.Any(g => g.startButton.wasPressedThisFrame);
        bool back = Gamepad.all.Any(g => g.buttonEast.wasPressedThisFrame);
        if (settingsPanel.IsOpen || Time.frameCount == SettingsPanel.ClosedFrame) return; // Settings handles its own Back

        JournalPanel j = UIRoot.Instance != null ? UIRoot.Instance.Journal : null;
        if (!IsPaused && pressed && (j == null || !j.IsOpen)) Pause();
        else if (IsPaused && (pressed || back)) Resume();
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        SetPlayerControls(false);
        ShowPanel();
    }

    public void Resume()
    {
        IsPaused = false;
        panel.SetActive(false);
        settingsPanel.gameObject.SetActive(false);
        Time.timeScale = 1f;
        SetPlayerControls(true);
    }

    private void ShowPanel()
    {
        panel.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(resume.gameObject);
    }

    private void OpenJournal()
    {
        Resume();
        if (manager == null || manager.CurrentRoom == null) return;
        JournalStone stone = manager.CurrentRoom.GetComponentsInChildren<JournalStone>(true).FirstOrDefault(s => s.HasEntry);
        if (stone != null) stone.OpenFromMenu();
    }

    private void SetPlayerControls(bool on)
    {
        if (spawner == null) return;
        foreach (PlayerInput p in spawner.Players)
        {
            if (p == null) continue;
            if (on) p.ActivateInput(); else p.DeactivateInput();
        }
    }

    private void OnDestroy()
    {
        if (IsPaused) Time.timeScale = 1f; // never leave the game frozen when the scene changes
        IsPaused = false;
    }

#if UNITY_EDITOR
    public void EditorSetup(GameObject p, Button res, Button restart, Button jour, Button sett, Button vil, Button tit, SettingsPanel sp)
    {
        panel = p; resume = res; restartRoom = restart; journal = jour; settings = sett; village = vil; title = tit; settingsPanel = sp;
    }
#endif
}
