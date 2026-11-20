using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A "◀ value ▶" setting that works the same with mouse, keyboard and gamepad: click (or Submit) for the next value,
/// Left / Right while it's selected to step back and forth. Used for display mode, resolution and Wren's tip timer.
/// </summary>
[RequireComponent(typeof(Button))]
public class CycleOption : MonoBehaviour, IMoveHandler
{
    [SerializeField] private TMP_Text valueText;

    private string[] options = new string[0];
    private int index;
    private Action<int> onChanged;

    public int Index => index;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => Step(1));
    }

    public void Setup(string[] values, int selected, Action<int> changed)
    {
        options = values;
        index = Mathf.Clamp(selected, 0, Mathf.Max(0, values.Length - 1));
        onChanged = changed;
        Refresh();
    }

    public void OnMove(AxisEventData e)
    {
        if (e.moveDir == MoveDirection.Left) { Step(-1); e.Use(); }
        else if (e.moveDir == MoveDirection.Right) { Step(1); e.Use(); }
    }

    private void Step(int delta)
    {
        if (options.Length == 0) return;
        index = (index + delta + options.Length) % options.Length;
        Refresh();
        onChanged?.Invoke(index);
    }

    private void Refresh()
    {
        if (valueText != null) valueText.text = options.Length > 0 ? $"<  {options[index]}  >" : "";
    }

#if UNITY_EDITOR
    public void EditorSetup(TMP_Text value) => valueText = value;
#endif
}
