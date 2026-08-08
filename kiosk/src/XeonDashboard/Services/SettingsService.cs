using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using XeonDashboard.Helpers;
using XeonDashboard.Models;

namespace XeonDashboard.Services;

public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<SettingsService> _logger;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        Current = Load();
    }

    public AppSettings Current { get; private set; }

    public bool IsFirstRun { get; private set; }

    public AppSettings Load()
    {
        AppPaths.EnsureCreated();

        var existed = File.Exists(AppPaths.SettingsFile);
        IsFirstRun = !existed;

        if (!existed)
        {
            _logger.LogInformation("No settings file found. Writing defaults to {Path}.", AppPaths.SettingsFile);
            Current = new AppSettings();
            Save();
            return Current;
        }

        try
        {
            var json = File.ReadAllText(AppPaths.SettingsFile);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            Current = loaded ?? new AppSettings();
            _logger.LogInformation("Loaded settings from {Path}.", AppPaths.SettingsFile);
        }
        catch (Exception ex)
        {
            // Never let a corrupt settings file take down the appliance.
            _logger.LogError(ex, "Failed to read settings; falling back to defaults.");
            Current = new AppSettings();
        }

        return Current;
    }

    public void Save()
    {
        try
        {
            AppPaths.EnsureCreated();
            var json = JsonSerializer.Serialize(Current, JsonOptions);
            File.WriteAllText(AppPaths.SettingsFile, json);
            _logger.LogInformation("Saved settings to {Path}.", AppPaths.SettingsFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings.");
        }
    }
}
