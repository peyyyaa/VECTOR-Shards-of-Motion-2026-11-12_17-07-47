using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One project in the Village Fund ledger: its name and description, and a button that reads "Fund (3)",
/// "Need 2 more" or (greyed out) "Funded". Built by Create UI Prefabs; FundPanel fills it in.
/// </summary>
public class FundRow : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text buttonText;

    private static readonly Color Ink = new Color32(74, 47, 28, 255), Green = new Color32(62, 98, 48, 255);

    public Button Button => button;

    public void Init(FundPanel panel, int index) => button.onClick.AddListener(() => panel.Fund(index));

    public void Show(VillageFund.Project p, int balance)
    {
        bool funded = VillageFund.IsFunded(p);
        nameText.text = p.name;
        nameText.color = funded ? Green : Ink;
        descriptionText.text = funded ? $"Done! See {p.where}." : p.description;
        button.interactable = !funded;
        buttonText.text = funded ? "Funded" : balance >= p.price ? $"Fund ({p.price})" : $"Need {p.price - balance} more";
    }

    // Mouse: hovering selects the row's button, like moving there with the keys.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable) EventSystem.current?.SetSelectedGameObject(button.gameObject);
    }

#if UNITY_EDITOR
    public void EditorSetup(TMP_Text title, TMP_Text description, Button fundButton, TMP_Text fundLabel)
    {
        nameText = title; descriptionText = description; button = fundButton; buttonText = fundLabel;
    }
#endif
}
