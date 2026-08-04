using XeonDashboard.Helpers;

namespace XeonDashboard.Services;

public interface IMonitorService
{
    /// <summary>All active displays, resolved with EDID friendly names where available.</summary>
    IReadOnlyList<MonitorInfo> GetMonitors();

    /// <summary>
    /// Finds a display by EDID friendly name (case-insensitive, trimmed).
    /// When <paramref name="friendlyName"/> is null/empty, the primary display
    /// is returned. Returns <c>null</c> when no match is found.
    /// </summary>
    MonitorInfo? FindByName(string? friendlyName);
}
