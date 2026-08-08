namespace XeonDashboard.Services;

/// <summary>
/// Detects and, where possible, silently provisions the Microsoft WebView2
/// Runtime so end users never have to install a prerequisite by hand.
/// </summary>
public interface IWebView2RuntimeService
{
    /// <summary>True when a usable WebView2 runtime is present on the machine.</summary>
    bool IsInstalled();

    /// <summary>
    /// If the runtime is missing and a bundled bootstrapper is available, runs it
    /// silently. Returns true once a runtime is present.
    /// </summary>
    Task<bool> TryEnsureInstalledAsync();

    /// <summary>Official download page, used as a last-resort fallback.</summary>
    string DownloadPageUrl { get; }
}
