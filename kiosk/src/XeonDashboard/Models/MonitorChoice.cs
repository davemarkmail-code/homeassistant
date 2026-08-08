namespace XeonDashboard.Models;

/// <summary>
/// One entry in the Settings monitor picker. <see cref="Value"/> is what gets
/// persisted to <see cref="AppSettings.MonitorName"/> — the EDID friendly name,
/// or an empty string for "Primary display".
/// </summary>
public sealed record MonitorChoice(string Display, string Value);
