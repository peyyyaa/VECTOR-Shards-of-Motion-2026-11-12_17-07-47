using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Stops the game behind a full-screen menu opened from the world (the map from the lab wall, the Village Fund ledger):
/// time stops and both players' controls are switched off, as in the pause menu. If the pause menu already did that,
/// Freeze does nothing and returns false, so the menu knows not to Unfreeze.
/// The pause menu ignores Esc / Start while frozen (and on the frame it ends), so one press doesn't do two things.
/// Freeze(keepControlsForMenu: true) is for screens both players use (the engine assembly): time stops and gameplay
/// ignores input (PlayerInputReader.GameplaySuspended), but the players' controls stay on so the menu can read them.
/// </summary>
public static class MenuFreeze
{
    public static bool Active { get; private set; }

    /// <summary>Frame the last freeze ended on.</summary>
    public static int EndedFrame { get; private set; } = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { Active = false; EndedFrame = -1; }

    /// <summary>Freezes the game unless it's already paused. Returns true if this call froze it.</summary>
    public static bool Freeze(bool keepControlsForMenu = false)
    {
        if (PauseMenu.IsPaused || Active) return false;
        Active = true;
        Time.timeScale = 0f;
        if (keepControlsForMenu) PlayerInputReader.GameplaySuspended = true;
        else SetPlayerControls(false);
        return true;
    }

    public static void Unfreeze()
    {
        if (!Active) return;
        Active = false;
        EndedFrame = Time.frameCount;
        Time.timeScale = 1f;
        PlayerInputReader.GameplaySuspended = false;
        SetPlayerControls(true);
    }

    private static void SetPlayerControls(bool on)
    {
        LocalCoopSpawner spawner = Object.FindFirstObjectByType<LocalCoopSpawner>();
        if (spawner == null) return;
        foreach (PlayerInput p in spawner.Players)
        {
            if (p == null) continue;
            if (on) p.ActivateInput(); else p.DeactivateInput();
        }
    }
}
