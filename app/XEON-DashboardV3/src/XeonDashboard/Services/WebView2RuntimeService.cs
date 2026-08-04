using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

namespace XeonDashboard.Services;

public sealed class WebView2RuntimeService : IWebView2RuntimeService
{
    private readonly ILogger<WebView2RuntimeService> _logger;

    public WebView2RuntimeService(ILogger<WebView2RuntimeService> logger) => _logger = logger;

    public string DownloadPageUrl => "https://developer.microsoft.com/microsoft-edge/webview2/";

    // If you place the Evergreen bootstrapper next to the app, first-run install
    // becomes fully automatic. Filename must match Microsoft's exactly.
    private static string BootstrapperPath =>
        Path.Combine(AppContext.BaseDirectory, "MicrosoftEdgeWebView2Setup.exe");

    public bool IsInstalled()
    {
        try
        {
            var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            return !string.IsNullOrEmpty(version);
        }
        catch (Exception ex)
        {
            _logger.LogInformation("WebView2 runtime not detected: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<bool> TryEnsureInstalledAsync()
    {
        if (IsInstalled()) return true;

        if (!File.Exists(BootstrapperPath))
        {
            _logger.LogWarning("WebView2 missing and no bundled bootstrapper at {Path}.", BootstrapperPath);
            return false;
        }

        try
        {
            _logger.LogInformation("Installing WebView2 runtime via bundled bootstrapper…");
            var psi = new ProcessStartInfo(BootstrapperPath, "/silent /install")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null) return false;

            await process.WaitForExitAsync();
            _logger.LogInformation("Bootstrapper exited with code {Code}.", process.ExitCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebView2 bootstrapper failed to run.");
            return false;
        }

        return IsInstalled();
    }
}
