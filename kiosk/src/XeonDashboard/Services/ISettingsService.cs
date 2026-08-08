using XeonDashboard.Models;

namespace XeonDashboard.Services;

/// <summary>
/// Loads and persists <see cref="AppSettings"/>. A single instance is shared
/// across the app; <see cref="Current"/> always reflects the latest load.
/// </summary>
public interface ISettingsService
{
    AppSettings Current { get; }

    /// <summary>True when no settings file existed at startup (a fresh install).</summary>
    bool IsFirstRun { get; }

    /// <summary>Loads settings from disk, creating defaults if none exist.</summary>
    AppSettings Load();

    /// <summary>Persists the current settings to disk.</summary>
    void Save();
}
