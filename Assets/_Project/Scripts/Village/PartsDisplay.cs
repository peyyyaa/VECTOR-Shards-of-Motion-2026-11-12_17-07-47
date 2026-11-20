using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wren's research table: each machine part the players have found (QuestItem ids in the save) appears on it.
/// Parts not found yet stay hidden. Checked when the scene starts (parts are only found out in the regions).
/// </summary>
public class PartsDisplay : MonoBehaviour
{
    [Serializable]
    public class Part
    {
        public string itemId;
        public SpriteRenderer art;
    }

    [SerializeField] private List<Part> parts = new List<Part>();

    private void Start()
    {
        foreach (Part p in parts)
            if (p.art != null) p.art.enabled = QuestItem.Found.Contains(p.itemId);
    }

#if UNITY_EDITOR
    public void EditorAdd(string id, SpriteRenderer sr) => parts.Add(new Part { itemId = id, art = sr });
#endif
}
