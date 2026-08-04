using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace XeonDashboard.Services;

public sealed class TrayIconService : ITrayIconService
{
    private readonly ILogger<TrayIconService> _logger;
    private NotifyIcon? _notifyIcon;

    public TrayIconService(ILogger<TrayIconService> logger) => _logger = logger;

    public event EventHandler? ShowDashboardRequested;
    public event EventHandler? ReloadRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler? ExitRequested;

    public void Initialize()
    {
        if (_notifyIcon is not null) return;

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show Dashboard", null, (_, _) => ShowDashboardRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("Reload Dashboard", null, (_, _) => ReloadRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Settings…", null, (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "XEON Dashboard",
            Visible = true,
            ContextMenuStrip = menu
        };

        // Double-clicking the tray icon shows the dashboard.
        _notifyIcon.DoubleClick += (_, _) => ShowDashboardRequested?.Invoke(this, EventArgs.Empty);

        _logger.LogInformation("Tray icon initialised.");
    }

    public void ShowBalloon(string title, string message)
    {
        if (_notifyIcon is null) return;
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(4000);
    }

    private Icon LoadIcon()
    {
        // Prefer a shipped icon; fall back to a stock one so the tray always works.
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        try
        {
            if (File.Exists(path)) return new Icon(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load {Path}; using stock icon.", path);
        }
        return SystemIcons.Application;
    }

    public void Dispose()
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }
}
