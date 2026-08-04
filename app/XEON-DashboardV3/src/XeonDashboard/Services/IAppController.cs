namespace XeonDashboard.Services;

/// <summary>
/// Coordinates the whole application lifecycle: tray icon wiring, first-run
/// flow, the monitor gate, and showing the dashboard / settings windows.
/// </summary>
public interface IAppController
{
    /// <summary>Entry point called once from <c>App.OnStartup</c>.</summary>
    void Run();
}
