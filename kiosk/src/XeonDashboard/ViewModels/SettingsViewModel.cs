using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using XeonDashboard.Models;
using XeonDashboard.Services;

namespace XeonDashboard.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IMonitorService _monitors;
    private readonly IStartupRegistrationService _startup;
    private readonly ILogger<SettingsViewModel> _logger;

    public SettingsViewModel(
        ISettingsService settings,
        IMonitorService monitors,
        IStartupRegistrationService startup,
        ILogger<SettingsViewModel> logger)
    {
        _settings = settings;
        _monitors = monitors;
        _startup = startup;
        _logger = logger;

        LoadFromSettings();
        RefreshMonitors();
    }

    /// <summary>Friendly, non-technical note shown prominently in the window.</summary>
    public string ScalingNotice =>
        "A quick heads-up: this app simply displays your Home Assistant dashboard at " +
        "the screen's native size — it can't resize or rearrange the dashboard itself. " +
        "If things look too big or too small, adjust the layout inside Home Assistant " +
        "(card sizes, columns, or a kiosk/panel view) so it fits your display nicely.";

    [ObservableProperty] private string _selectedMonitorValue = string.Empty;

    // --- URL builder fields (assembled into AppSettings.DashboardUrl) --------
    [ObservableProperty][NotifyPropertyChangedFor(nameof(ComposedUrl))] private string _server = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(ComposedUrl))] private string _port = "8123";
    [ObservableProperty][NotifyPropertyChangedFor(nameof(ComposedUrl))] private string _dashboardPath = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(ComposedUrl))] private bool _useHttps;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(ComposedUrl))] private bool _kioskMode = true;

    /// <summary>The full URL the app will open, assembled from the fields above.</summary>
    public string ComposedUrl => BuildUrl();

    [ObservableProperty] private int _launchDelaySeconds;
    [ObservableProperty] private int _reconnectIntervalSeconds;
    [ObservableProperty] private bool _alwaysOnTop;
    [ObservableProperty] private bool _hideMouse;
    [ObservableProperty] private int _hideMouseAfterSeconds;
    [ObservableProperty] private bool _exitSilentlyIfMonitorMissing;
    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private string _webViewProfilePath = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<MonitorChoice> Monitors { get; } = new();

    public bool Saved { get; private set; }

    /// <summary>Raised with <c>true</c> on save, <c>false</c> on cancel.</summary>
    public event Action<bool>? RequestClose;

    private void LoadFromSettings()
    {
        var s = _settings.Current;

        // Decompose any existing URL back into the friendly fields.
        if (!string.IsNullOrWhiteSpace(s.DashboardUrl) &&
            Uri.TryCreate(s.DashboardUrl, UriKind.Absolute, out var uri))
        {
            UseHttps = string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase);
            Server = uri.Host;
            Port = uri.IsDefaultPort ? string.Empty : uri.Port.ToString();
            DashboardPath = uri.AbsolutePath.Trim('/');
            KioskMode = uri.Query.IndexOf("kiosk", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        else
        {
            // First-run defaults: the boilerplate is pre-filled so the user only
            // needs to type their server address and dashboard path.
            Port = "8123";
            KioskMode = true;
        }

        SelectedMonitorValue = s.MonitorName;
        LaunchDelaySeconds = s.LaunchDelaySeconds;
        ReconnectIntervalSeconds = s.ReconnectIntervalSeconds;
        AlwaysOnTop = s.AlwaysOnTop;
        HideMouse = s.HideMouse;
        HideMouseAfterSeconds = s.HideMouseAfterSeconds;
        ExitSilentlyIfMonitorMissing = s.ExitSilentlyIfMonitorMissing;
        StartWithWindows = _startup.IsEnabled();
        WebViewProfilePath = s.WebViewProfilePath;
    }

    private string BuildUrl()
    {
        var scheme = UseHttps ? "https" : "http";

        // Accept a pasted scheme in the server box and strip it.
        var host = (Server ?? string.Empty).Trim();
        if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) host = host[7..];
        else if (host.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) host = host[8..];
        host = host.TrimEnd('/');

        var port = (Port ?? string.Empty).Trim();
        var path = (DashboardPath ?? string.Empty).Trim().TrimStart('/');

        var url = $"{scheme}://{host}";
        if (!string.IsNullOrEmpty(port)) url += $":{port}";
        url += $"/{path}";

        if (KioskMode && url.IndexOf("kiosk", StringComparison.OrdinalIgnoreCase) < 0)
            url += url.Contains('?') ? "&kiosk" : "?kiosk";

        return url;
    }

    [RelayCommand]
    private void RefreshMonitors()
    {
        Monitors.Clear();
        Monitors.Add(new MonitorChoice("Primary display", string.Empty));

        foreach (var m in _monitors.GetMonitors())
            Monitors.Add(new MonitorChoice($"{m.FriendlyName} ({m.Width}×{m.Height})", m.FriendlyName));

        // Keep the currently-configured monitor selectable even if it isn't
        // plugged in right now.
        if (!string.IsNullOrWhiteSpace(SelectedMonitorValue) &&
            !Monitors.Any(c => c.Value == SelectedMonitorValue))
        {
            Monitors.Add(new MonitorChoice($"{SelectedMonitorValue} (not currently connected)", SelectedMonitorValue));
        }
    }

    [RelayCommand]
    private void Save()
    {
        var s = _settings.Current;
        s.DashboardUrl = ComposedUrl;
        s.MonitorName = SelectedMonitorValue ?? string.Empty;
        s.LaunchDelaySeconds = Math.Max(0, LaunchDelaySeconds);
        s.ReconnectIntervalSeconds = Math.Max(1, ReconnectIntervalSeconds);
        s.AlwaysOnTop = AlwaysOnTop;
        s.HideMouse = HideMouse;
        s.HideMouseAfterSeconds = Math.Max(1, HideMouseAfterSeconds);
        s.ExitSilentlyIfMonitorMissing = ExitSilentlyIfMonitorMissing;
        s.StartWithWindows = StartWithWindows;
        s.WebViewProfilePath = (WebViewProfilePath ?? string.Empty).Trim();

        _settings.Save();
        _startup.SetEnabled(StartWithWindows);

        Saved = true;
        _logger.LogInformation("Settings saved from the settings window.");
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        Saved = false;
        RequestClose?.Invoke(false);
    }
}
