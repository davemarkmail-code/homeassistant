using XeonDashboard.Helpers;

namespace XeonDashboard.Models;

/// <summary>
/// User-editable configuration, persisted to
/// <c>%LocalAppData%\XEON Dashboard\settings.json</c>.
/// Defaults match the appliance's intended production configuration.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Home Assistant dashboard URL to display.</summary>
    /// <remarks>
    /// Blank by default so a freshly-distributed build prompts the new user to
    /// enter their own address rather than pointing at someone else's instance.
    /// </remarks>
    public string DashboardUrl { get; set; } = string.Empty;

    /// <summary>
    /// EDID friendly name of the target monitor. When empty, the primary
    /// display is used instead (convenient for development on other hardware).
    /// </summary>
    public string MonitorName { get; set; } = "XENEON EDGE";

    /// <summary>Delay before the startup checks begin, in seconds.</summary>
    public int LaunchDelaySeconds { get; set; } = 10;

    /// <summary>How often to retry when Home Assistant is unreachable, in seconds.</summary>
    public int ReconnectIntervalSeconds { get; set; } = 5;

    /// <summary>Hide the mouse cursor after a period of inactivity.</summary>
    public bool HideMouse { get; set; } = false;

    /// <summary>Seconds of inactivity before the cursor is hidden.</summary>
    public int HideMouseAfterSeconds { get; set; } = 5;

    /// <summary>Keep the dashboard window above all others.</summary>
    public bool AlwaysOnTop { get; set; } = false;

    /// <summary>Launch automatically when the current user signs in.</summary>
    public bool StartWithWindows { get; set; } = false;

    /// <summary>
    /// When the configured monitor isn't found: if <c>true</c>, exit silently
    /// (ideal for a dedicated appliance whose display may be asleep at boot);
    /// if <c>false</c>, open Settings so the user can pick a connected display
    /// (better default for distributed builds).
    /// </summary>
    public bool ExitSilentlyIfMonitorMissing { get; set; } = false;

    /// <summary>
    /// WebView2 user-data folder. Persisting this is what keeps the Home
    /// Assistant login alive across reboots. Empty means use the default
    /// under the product folder.
    /// </summary>
    public string WebViewProfilePath { get; set; } = string.Empty;

    /// <summary>Resolved profile path, falling back to the product default.</summary>
    public string EffectiveWebViewProfilePath =>
        string.IsNullOrWhiteSpace(WebViewProfilePath)
            ? AppPaths.DefaultWebViewProfile
            : WebViewProfilePath;
}
