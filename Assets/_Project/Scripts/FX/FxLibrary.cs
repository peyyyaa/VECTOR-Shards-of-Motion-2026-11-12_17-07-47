using UnityEngine;

/// <summary>
/// All the effect sprites in one asset (Assets/_Project/Art/FX/FX_Library.asset), so effect scripts need a
/// single reference. Created and filled by Tools > Shards of Motion > Effects > 1. Build Effect Sprites.
/// The sprites are white or pale where they should be tinted, so one sprite serves both players' colours.
/// </summary>
[CreateAssetMenu(menuName = "Shards of Motion/FX Library")]
public class FxLibrary : ScriptableObject
{
    [Tooltip("Kinetic Burst shockwave, drawn at the burst's real range and cone; pivot at the player's centre.")]
    public Sprite[] shockwave = new Sprite[0];
    public Sprite[] ring = new Sprite[0];
    public Sprite[] dust = new Sprite[0];
    public Sprite spark;
    public Sprite wisp;
    public Sprite dot;
    public Sprite arrow;
    public Sprite sparkle;
}
