using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace XeonDashboard.Services;

public sealed class StartupRegistrationService : IStartupRegistrationService
{
    private const string ShortcutName = "XEON Dashboard.lnk";

    private readonly ILogger<StartupRegistrationService> _logger;

    public StartupRegistrationService(ILogger<StartupRegistrationService> logger) => _logger = logger;

    private static string ShortcutPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Startup), ShortcutName);

    private static string ExecutablePath =>
        Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "XeonDashboard.exe");

    public bool IsEnabled() => File.Exists(ShortcutPath);

    public void SetEnabled(bool enabled)
    {
        try
        {
            if (enabled)
                CreateShortcut();
            else if (File.Exists(ShortcutPath))
                File.Delete(ShortcutPath);

            _logger.LogInformation("Start-with-Windows set to {Enabled}.", enabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set start-with-Windows to {Enabled}.", enabled);
        }
    }

    // Uses the Windows Script Host COM object to author a .lnk. This avoids any
    // extra NuGet dependency and needs no elevation.
    private void CreateShortcut()
    {
        var exe = ExecutablePath;
        var progId = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell is unavailable.");

        dynamic? shell = null;
        dynamic? shortcut = null;
        try
        {
            shell = Activator.CreateInstance(progId)!;
            shortcut = shell.CreateShortcut(ShortcutPath);
            shortcut.TargetPath = exe;
            shortcut.WorkingDirectory = Path.GetDirectoryName(exe);
            shortcut.Description = "XEON Dashboard";
            shortcut.Save();
        }
        finally
        {
            if (shortcut is not null) Marshal.FinalReleaseComObject(shortcut);
            if (shell is not null) Marshal.FinalReleaseComObject(shell);
        }
    }
}
