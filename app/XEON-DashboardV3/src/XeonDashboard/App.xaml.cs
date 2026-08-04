using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using XeonDashboard.Helpers;
using XeonDashboard.Services;
using XeonDashboard.ViewModels;
using XeonDashboard.Views;

// WinForms is enabled (for Screen bounds), so 'Application' is ambiguous
// between WinForms and WPF. This app is WPF — bind the bare name to WPF.
using Application = System.Windows.Application;

namespace XeonDashboard;

public partial class App : Application
{
    private IHost? _host;
    private Mutex? _singleInstanceMutex;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // --- Single instance -------------------------------------------------
        _singleInstanceMutex = new Mutex(initiallyOwned: true,
            name: @"Global\XeonDashboard_SingleInstance", createdNew: out var isNew);
        if (!isNew)
        {
            // Another copy is already running; quietly bow out.
            Shutdown(0);
            return;
        }

        AppPaths.EnsureCreated();
        ConfigureSerilog();

        // Last line of defence: never let an unhandled exception kill the
        // appliance without a log entry.
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        try
        {
            _host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices(ConfigureServices)
                .Build();

            await _host.StartAsync();

            var logger = _host.Services.GetRequiredService<ILogger<App>>();
            logger.LogInformation("XEON Dashboard starting.");

            // All lifecycle policy lives in the controller.
            _host.Services.GetRequiredService<IAppController>().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error during startup.");
            await ShutdownAsync(1);
        }
    }

    private static void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
    {
        // Core services (UI-agnostic).
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IMonitorService, MonitorService>();
        services.AddSingleton<IStartupCheckService, StartupCheckService>();
        services.AddSingleton<IStartupRegistrationService, StartupRegistrationService>();
        services.AddSingleton<IWebView2RuntimeService, WebView2RuntimeService>();

        // Tray + orchestration.
        services.AddSingleton<ITrayIconService, TrayIconService>();
        services.AddSingleton<IAppController, AppController>();

        // Views / view-models.
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<DashboardWindow>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsWindow>();
    }

    private static void ConfigureSerilog()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: Path.Combine(AppPaths.LogsDirectory, "xeon-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled dispatcher exception (handled to keep the appliance alive).");
        e.Handled = true; // an appliance should not crash to desktop
    }

    private async Task ShutdownAsync(int exitCode)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        Log.CloseAndFlush();
        Shutdown(exitCode);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_host is not null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
        }
        finally
        {
            _singleInstanceMutex?.Dispose();
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
