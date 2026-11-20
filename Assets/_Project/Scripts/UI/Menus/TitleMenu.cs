using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Title screen (Title scene): Continue (only with a save) | New Game | Settings | Quit.
/// New Game over an existing save asks once more ("Start over? Press again") so progress isn't wiped by accident.
/// Works with mouse, keyboard and gamepad (EventSystem + Input System UI module).
/// </summary>
public class TitleMenu : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGame;
    [SerializeField] private Button settings;
    [SerializeField] private Button quit;
    [SerializeField] private TMP_Text newGameLabel;
    [SerializeField] private SettingsPanel settingsPanel;
    [SerializeField] private GameObject menu;

    private bool confirming;

    private void Start()
    {
        continueButton.gameObject.SetActive(SaveSystem.HasSave);
        continueButton.onClick.AddListener(GameFlow.Continue);
        newGame.onClick.AddListener(NewGame);
        settings.onClick.AddListener(() => { menu.SetActive(false); settingsPanel.Open(ShowMenu); });
        quit.onClick.AddListener(GameFlow.Quit);
        settingsPanel.gameObject.SetActive(false);
        ShowMenu();
    }

    private void ShowMenu()
    {
        menu.SetActive(true);
        EventSystem.current?.SetSelectedGameObject((SaveSystem.HasSave ? continueButton : newGame).gameObject);
    }

    private void NewGame()
    {
        if (SaveSystem.HasSave && !confirming)
        {
            confirming = true;
            newGameLabel.text = "Start over? Press again";
            return;
        }
        GameFlow.NewGame();
    }

    private void Update()
    {
        // Moving off the New Game button cancels the "start over?" question.
        if (confirming && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != newGame.gameObject)
        {
            confirming = false;
            newGameLabel.text = "New Game";
        }
    }

#if UNITY_EDITOR
    public void EditorSetup(Button cont, Button ng, Button sett, Button q, TMP_Text ngLabel, SettingsPanel sp, GameObject menuRoot)
    {
        continueButton = cont; newGame = ng; settings = sett; quit = q; newGameLabel = ngLabel; settingsPanel = sp; menu = menuRoot;
    }
#endif
}
