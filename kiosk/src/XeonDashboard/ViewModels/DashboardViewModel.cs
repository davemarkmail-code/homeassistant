using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using XeonDashboard.Services;

namespace XeonDashboard.ViewModels;

public enum DashboardState
{
    Idle,
    Waiting,      // honouring the configured launch delay
    Connecting,   // probing network + Home Assistant
    Ready,        // dashboard shown
    Reconnecting  // lost HA, retrying
}

/// <summary>
/// Owns the appliance's runtime state machine. It decides *when* to show the
/// dashboard; the view owns the WebView2 control and reacts to
/// <see cref="NavigateRequested"/>.
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IStartupCheckService _checks;
    private readonly ILogger<DashboardViewModel> _logger;

    public DashboardViewModel(
        ISettingsService settings,
        IStartupCheckService checks,
        ILogger<DashboardViewModel> logger)
    {
        _settings = settings;
        _checks = checks;
        _logger = logger;
    }

    /// <summary>Raised when the view should point its WebView at this URL.</summary>
    public event Action<string>? NavigateRequested;

    [ObservableProperty]
    private DashboardState _state = DashboardState.Idle;

    [ObservableProperty]
    private string _statusMessage = "Starting…";

    [ObservableProperty]
    private bool _isOverlayVisible = true;

    public string DashboardUrl => _settings.Current.DashboardUrl;

    /// <summary>
    /// Runs the boot sequence from the spec: wait, confirm network, confirm
    /// Home Assistant reachable, then request navigation. Retries connectivity
    /// forever at the configured interval rather than failing.
    /// </summary>
    public async Task RunStartupSequenceAsync(CancellationToken ct)
    {
        var s = _settings.Current;

        try
        {
            State = DashboardState.Waiting;
            StatusMessage = $"Starting in {s.LaunchDelaySeconds}s…";
            _logger.LogInformation("Startup delay: {Delay}s", s.LaunchDelaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(0, s.LaunchDelaySeconds)), ct);

            await WaitUntilReachableAsync(ct);

            _logger.LogInformation("Home Assistant reachable. Navigating to dashboard.");
            NavigateRequested?.Invoke(s.DashboardUrl);
            // The view flips State to Ready / hides the overlay on
            // NavigationCompleted (see NotifyNavigationCompleted).
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Startup sequence cancelled (shutdown).");
        }
    }

    /// <summary>Called by the view when a reconnect is needed (HA became unreachable).</summary>
    public async Task ReconnectAsync(CancellationToken ct)
    {
        State = DashboardState.Reconnecting;
        IsOverlayVisible = true;
        StatusMessage = "Reconnecting to Home Assistant…";
        _logger.LogWarning("Dashboard connection lost; entering reconnect loop.");

        await WaitUntilReachableAsync(ct);
        NavigateRequested?.Invoke(_settings.Current.DashboardUrl);
    }

    /// <summary>Called by the view when navigation finishes successfully.</summary>
    public void NotifyNavigationCompleted(bool success)
    {
        if (success)
        {
            State = DashboardState.Ready;
            IsOverlayVisible = false;
            StatusMessage = string.Empty;
        }
    }

    private async Task WaitUntilReachableAsync(CancellationToken ct)
    {
        var s = _settings.Current;
        var interval = TimeSpan.FromSeconds(Math.Max(1, s.ReconnectIntervalSeconds));

        while (!ct.IsCancellationRequested)
        {
            State = DashboardState.Connecting;

            if (!_checks.IsNetworkAvailable())
            {
                StatusMessage = "Waiting for network…";
            }
            else if (await _checks.IsHomeAssistantReachableAsync(s.DashboardUrl, ct))
            {
                return;
            }
            else
            {
                StatusMessage = "Waiting for Home Assistant…";
            }

            await Task.Delay(interval, ct);
        }
    }
}
