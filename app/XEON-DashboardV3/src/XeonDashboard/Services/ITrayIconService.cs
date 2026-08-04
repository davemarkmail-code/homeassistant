namespace XeonDashboard.Services;

/// <summary>
/// Owns the system-tray icon and its menu (Show Dashboard, Reload Dashboard,
/// Settings, Exit). It only raises events; the application controller decides
/// what each does, keeping UI mechanism separate from policy.
/// </summary>
public interface ITrayIconService : IDisposable
{
    event EventHandler ShowDashboardRequested;
    event EventHandler ReloadRequested;
    event EventHandler SettingsRequested;
    event EventHandler ExitRequested;

    /// <summary>Creates and shows the tray icon. Call once, on the UI thread.</summary>
    void Initialize();

    /// <summary>Shows a transient balloon/toast from the tray icon.</summary>
    void ShowBalloon(string title, string message);
}
