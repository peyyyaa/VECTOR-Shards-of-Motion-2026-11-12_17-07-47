using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One character's sprite animations (Idle, Run, Jump, Fall, Land, Float, Dash, Push, Cast).
/// Created and filled by Tools > Shards of Motion > Characters > 1. Build Character Sprites
/// (Assets/_Project/Art/Characters/P1/P1_Animations.asset, same for P2). PlayerAnimator plays them.
/// </summary>
[CreateAssetMenu(menuName = "Shards of Motion/Character Animation Set")]
public class CharacterAnimationSet : ScriptableObject
{
    [System.Serializable]
    public class Clip
    {
        public string name;
        public Sprite[] frames = new Sprite[0];
        [Min(1f)] public float framesPerSecond = 10f;
        [Tooltip("Loop, or hold the last frame (Jump, Land, Cast).")]
        public bool loop = true;
    }

    public List<Clip> clips = new List<Clip>();

    public Clip Get(string clipName)
    {
        foreach (Clip c in clips) if (c.name == clipName) return c;
        return null;
    }
}
