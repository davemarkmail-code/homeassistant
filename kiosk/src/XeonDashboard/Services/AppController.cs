using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XeonDashboard.Helpers;
using XeonDashboard.Views;
using WpfApplication = System.Windows.Application;

namespace XeonDashboard.Services;

public sealed class AppController : IAppController
{
    private readonly IServiceProvider _services;
    private readonly ISettingsService _settings;
    private readonly IMonitorService _monitors;
    private readonly ITrayIconService _tray;
    private readonly ILogger<AppController> _logger;

    private DashboardWindow? _dashboard;

    public AppController(
        IServiceProvider services,
        ISettingsService settings,
        IMonitorService monitors,
        ITrayIconService tray,
        ILogger<AppController> logger)
    {
        _services = services;
        _settings = settings;
        _monitors = monitors;
        _tray = tray;
        _logger = logger;
    }

    public void Run()
    {
        _tray.Initialize();
        _tray.ShowDashboardRequested += (_, _) => ShowOrStartDashboard();
        _tray.ReloadRequested += (_, _) => _dashboard?.ReloadDashboard();
        _tray.SettingsRequested += (_, _) => ShowSettings();
        _tray.ExitRequested += (_, _) => ExitApp();

        // First run (or a build with no URL configured yet): let the user set up
        // before we throw a chromeless window on screen.
        var needsSetup = _settings.IsFirstRun || string.IsNullOrWhiteSpace(_settings.Current.DashboardUrl);
        if (needsSetup)
        {
            _logger.LogInformation("First-run setup: opening Settings before launch.");
            ShowSettings();
        }

        StartKiosk();
    }

    private void StartKiosk()
    {
        if (string.IsNullOrWhiteSpace(_settings.Current.DashboardUrl))
        {
            _logger.LogWarning("No dashboard URL configured; staying in the tray.");
            _tray.ShowBalloon("XEON Dashboard", "Set your dashboard URL in Settings to get started.");
            return;
        }

        var target = _monitors.FindByName(_settings.Current.MonitorName);
        if (target is null)
        {
            if (_settings.Current.ExitSilentlyIfMonitorMissing)
            {
                _logger.LogWarning("Configured display not found; exiting silently per settings.");
                ExitApp();
                return;
            }

            _logger.LogWarning("Configured display not found; prompting via Settings.");
            _tray.ShowBalloon("Display not found",
                "The chosen display wasn't detected. Pick a connected one in Settings.");

            if (!ShowSettings())
                return; // user cancelled; leave the app sitting in the tray

            target = _monitors.FindByName(_settings.Current.MonitorName);
            if (target is null)
                return;
        }

        ShowDashboard(target);
    }

    private void ShowDashboard(MonitorInfo target)
    {
        _dashboard ??= _services.GetRequiredService<DashboardWindow>();
        _dashboard.Topmost = _settings.Current.AlwaysOnTop;
        _dashboard.ApplyMonitor(target);

        if (!_dashboard.IsVisible)
            _dashboard.Show();

        _dashboard.Activate();
        _logger.LogInformation("Dashboard shown on {Monitor}.", target);
    }

    private void ShowOrStartDashboard()
    {
        if (_dashboard is { IsVisible: true })
        {
            _dashboard.Activate();
            return;
        }
        StartKiosk();
    }

    /// <summary>Shows the settings dialog modally; returns true if the user saved.</summary>
    private bool ShowSettings()
    {
        var window = _services.GetRequiredService<SettingsWindow>();
        if (_dashboard is { IsVisible: true })
            window.Owner = _dashboard;

        window.ShowDialog();
        return window.ViewModel.Saved;
    }

    private void ExitApp()
    {
        _logger.LogInformation("Exit requested; shutting down.");
        _tray.Dispose();
        WpfApplication.Current.Shutdown();
    }
}
