using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using XeonDashboard.Helpers;
using XeonDashboard.Models;
using XeonDashboard.Services;
using XeonDashboard.ViewModels;
using static XeonDashboard.Helpers.NativeMethods;

namespace XeonDashboard.Views;

/// <summary>
/// The single appliance window. Responsibilities that must live in the view:
/// forcing a truly borderless frame, pinning the window to an exact monitor in
/// physical pixels, and driving the WebView2 control's lifecycle. All *policy*
/// (when to navigate, reconnect, etc.) lives in <see cref="DashboardViewModel"/>.
/// </summary>
public partial class DashboardWindow : Window
{
    private readonly DashboardViewModel _viewModel;
    private readonly AppSettings _settings;
    private readonly IWebView2RuntimeService _runtime;
    private readonly ILogger<DashboardWindow> _logger;
    private readonly CancellationTokenSource _cts = new();

    private MonitorInfo? _targetMonitor;

    public DashboardWindow(
        DashboardViewModel viewModel,
        ISettingsService settingsService,
        IWebView2RuntimeService runtime,
        ILogger<DashboardWindow> logger)
    {
        _viewModel = viewModel;
        _settings = settingsService.Current;
        _runtime = runtime;
        _logger = logger;

        DataContext = _viewModel;
        Topmost = _settings.AlwaysOnTop;

        _viewModel.NavigateRequested += OnNavigateRequested;

        InitializeComponent();
    }

    /// <summary>Supplies the resolved target monitor before the window is shown.</summary>
    public void ApplyMonitor(MonitorInfo monitor) => _targetMonitor = monitor;

    /// <summary>Reloads the current dashboard (used by the tray menu and shortcuts).</summary>
    public void ReloadDashboard()
    {
        Dispatcher.Invoke(() =>
        {
            _logger.LogInformation("Reload requested.");
            WebView.CoreWebView2?.Reload();
        });
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;

        StripWindowChrome(hwnd);
        PinToTargetMonitor(hwnd);
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        await InitializeWebViewAsync();
        _ = _viewModel.RunStartupSequenceAsync(_cts.Token);
    }

    protected override void OnClosed(EventArgs e)
    {
        _cts.Cancel();
        _viewModel.NavigateRequested -= OnNavigateRequested;
        _cts.Dispose();
        base.OnClosed(e);
    }

    // ---- Window chrome / placement --------------------------------------

    private static void StripWindowChrome(IntPtr hwnd)
    {
        // WindowStyle="None" already removes most of the frame; we also clear
        // the caption/border/system-menu bits defensively so nothing (a driver,
        // a theme) can reintroduce chrome, and mark the window as a tool window
        // to keep it out of Alt-Tab — it should feel like an appliance.
        var style = GetWindowLongPtr(hwnd, GWL_STYLE).ToInt64();
        style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);
        SetWindowLongPtr(hwnd, GWL_STYLE, new IntPtr(style));

        var exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
        exStyle |= WS_EX_TOOLWINDOW;
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(exStyle));
    }

    private void PinToTargetMonitor(IntPtr hwnd)
    {
        if (_targetMonitor is null)
        {
            _logger.LogWarning("No target monitor set; leaving window at default placement.");
            return;
        }

        var m = _targetMonitor;
        bool ok = SetWindowPos(hwnd, IntPtr.Zero, m.X, m.Y, m.Width, m.Height,
            SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);

        _logger.LogInformation("Pinned window to {Monitor} (SetWindowPos ok={Ok}).", m, ok);
    }

    // ---- WebView2 --------------------------------------------------------

    private async Task InitializeWebViewAsync()
    {
        try
        {
            // Make sure the WebView2 runtime exists before we touch the control.
            // On Win10/11 it's normally already present, so this is instant.
            if (!_runtime.IsInstalled())
            {
                _viewModel.IsOverlayVisible = true;
                _viewModel.StatusMessage = "Preparing the WebView2 runtime…";
                var provisioned = await _runtime.TryEnsureInstalledAsync();
                if (!provisioned)
                {
                    _viewModel.StatusMessage =
                        "Microsoft WebView2 Runtime is required. Opening the download page…";
                    _logger.LogWarning("WebView2 runtime unavailable; directing user to download page.");
                    try
                    {
                        Process.Start(new ProcessStartInfo(_runtime.DownloadPageUrl) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Could not open the WebView2 download page.");
                    }
                    return;
                }
            }

            // A persisted user-data folder is what keeps the Home Assistant
            // login alive across reboots — do NOT point this at a temp path.
            var profile = _settings.EffectiveWebViewProfilePath;
            System.IO.Directory.CreateDirectory(profile);

            var env = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: profile);

            await WebView.EnsureCoreWebView2Async(env);

            var core = WebView.CoreWebView2;
            var s = core.Settings;
            // Appliance feel: no context menu, no dev tools, no browser keys.
            s.AreDefaultContextMenusEnabled = false;
            s.AreDevToolsEnabled = false;
            s.IsStatusBarEnabled = false;
            s.AreBrowserAcceleratorKeysEnabled = false;
            s.IsZoomControlEnabled = false;
            s.IsGeneralAutofillEnabled = false;
            s.IsPasswordAutosaveEnabled = false;

            core.NavigationCompleted += OnNavigationCompleted;

            _logger.LogInformation("WebView2 initialised. Profile: {Profile}", profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebView2 initialisation failed.");
        }
    }

    private void OnNavigateRequested(string url)
    {
        Dispatcher.Invoke(() =>
        {
            if (WebView.CoreWebView2 is null)
            {
                _logger.LogWarning("Navigate requested before WebView2 was ready.");
                return;
            }
            _logger.LogInformation("Navigating to {Url}", url);
            WebView.CoreWebView2.Navigate(url);
        });
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            _viewModel.NotifyNavigationCompleted(true);
            return;
        }

        _logger.LogWarning("Navigation failed: {Status}. Entering reconnect loop.", e.WebErrorStatus);
        _ = _viewModel.ReconnectAsync(_cts.Token);
    }
}
