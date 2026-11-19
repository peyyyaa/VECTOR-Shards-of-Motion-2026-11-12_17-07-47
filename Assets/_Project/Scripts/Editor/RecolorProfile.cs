using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Settings for one recolored copy of an imported texture (Tools > Shards of Motion > Recolor).
/// The original texture is never changed: the tool writes a project-owned copy to
/// Assets/_Project/Art/Recolors/&lt;Output Name&gt;.png with the same sprite slicing, and (optionally)
/// a copy of its Tile Palette that paints with the recolored tiles.
///
/// Each rule recolors pixels whose hue is in a range (optionally only inside named sprites):
/// the pixel's hue is replaced, and its saturation/brightness scaled, so the original pixel-art
/// shading is kept. Rules are checked in order; the first rule that matches a pixel wins.
/// Edit the rules here and run Build All again to update the art.
/// </summary>
[CreateAssetMenu(menuName = "Shards of Motion/Recolor Profile")]
public class RecolorProfile : ScriptableObject
{
    [Tooltip("The imported texture to copy and recolor (left unchanged).")]
    public Texture2D source;
    [Tooltip("File name of the recolored copy (no extension).")]
    public string outputName = "Recolor";

    [Header("Tile Palette (tilesets only)")]
    public bool copyTilePalette = false;
    [Tooltip("The original Tile Palette prefab whose tiles use the source texture.")]
    public GameObject sourcePalette;

    public List<RecolorRule> rules = new List<RecolorRule>();
}

[Serializable]
public class RecolorRule
{
    public string label = "Rule";
    [Tooltip("Only recolor inside these sprites (names as in the texture's Sprite Editor). Empty = whole texture.")]
    public string[] onlySprites = new string[0];

    [Header("Which pixels (hue in degrees; From > To wraps around red)")]
    [Range(0f, 360f)] public float hueFrom = 0f;
    [Range(0f, 360f)] public float hueTo = 360f;
    [Range(0f, 1f)] public float minSaturation = 0f;

    [Header("New color")]
    [Range(0f, 360f)] public float targetHue = 0f;
    [Min(0f)] public float saturationMultiply = 1f;
    [Min(0f)] public float valueMultiply = 1f;
}
