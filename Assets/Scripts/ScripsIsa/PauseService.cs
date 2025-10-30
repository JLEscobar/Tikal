using System;
using UnityEngine;

public static class PauseService
{
    public static event Action<bool> OnPauseChanged;
    private static bool _isPaused;

    public static bool IsPaused => _isPaused;

    public static void SetPaused(bool paused)
    {
        if (_isPaused == paused) return;
        _isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        OnPauseChanged?.Invoke(paused);
    }

    public static void TogglePause()
    {
        SetPaused(!_isPaused);
    }
}
