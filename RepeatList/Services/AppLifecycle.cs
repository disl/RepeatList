namespace RepeatList.Services;

/// <summary>
/// Coordinates background/foreground transitions so that timers and background
/// work can be paused when the app moves to the background (Android: Activity.OnPause)
/// and resumed when it returns to the foreground.
/// </summary>
public static class AppLifecycle
{
    private static readonly object SyncLock = new();
    private static bool _isInBackground;

    /// <summary>True while the app is in the background (Android: OnPause fired).</summary>
    public static bool IsInBackground
    {
        get { lock (SyncLock) return _isInBackground; }
    }

    /// <summary>Raised when the app moves to the background.</summary>
    public static event EventHandler? Backgrounded;

    /// <summary>Raised when the app returns to the foreground.</summary>
    public static event EventHandler? Foregrounded;

    public static void NotifyBackgrounded()
    {
        lock (SyncLock)
        {
            if (_isInBackground) return;
            _isInBackground = true;
        }
        Backgrounded?.Invoke(null, EventArgs.Empty);
    }

    public static void NotifyForegrounded()
    {
        lock (SyncLock)
        {
            if (!_isInBackground) return;
            _isInBackground = false;
        }
        Foregrounded?.Invoke(null, EventArgs.Empty);
    }
}
