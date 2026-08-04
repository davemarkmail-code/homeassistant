using System.IO;

namespace XeonDashboard.Helpers;

/// <summary>
/// Canonical on-disk locations for the application. Everything lives under
/// <c>%LocalAppData%\XEON Dashboard\</c> so the app is fully self-contained and
/// requires no administrator privileges.
/// </summary>
public static class AppPaths
{
    public const string ProductFolderName = "XEON Dashboard";

    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ProductFolderName);

    public static string LogsDirectory { get; } = Path.Combine(Root, "Logs");

    public static string SettingsFile { get; } = Path.Combine(Root, "settings.json");

    public static string DefaultWebViewProfile { get; } = Path.Combine(Root, "WebView2");

    /// <summary>Ensures the base directories exist. Safe to call repeatedly.</summary>
    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(LogsDirectory);
    }
}
