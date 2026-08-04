using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using XeonDashboard.Helpers;
using static XeonDashboard.Helpers.NativeMethods;
using WinFormsScreen = System.Windows.Forms.Screen;

namespace XeonDashboard.Services;

/// <summary>
/// Resolves displays by their EDID friendly name.
///
/// Windows exposes friendly monitor names ("XENEON EDGE") only through the CCD
/// API (<see cref="QueryDisplayConfig"/>). That API gives us the GDI device
/// name (\\.\DISPLAYn) for each active target, which we then match against
/// <see cref="WinFormsScreen"/> to obtain physical pixel bounds.
///
/// If the CCD query fails for any reason we degrade gracefully: name matching
/// is unavailable, so <see cref="FindByName"/> returns null (the app then
/// exits silently, per spec), unless the caller asked for the primary display.
/// </summary>
public sealed class MonitorService : IMonitorService
{
    private readonly ILogger<MonitorService> _logger;

    public MonitorService(ILogger<MonitorService> logger) => _logger = logger;

    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var friendlyByGdiName = QueryFriendlyNames();
        var result = new List<MonitorInfo>();

        foreach (var screen in WinFormsScreen.AllScreens)
        {
            var bounds = screen.Bounds; // physical pixels under Per-Monitor V2
            friendlyByGdiName.TryGetValue(screen.DeviceName, out var friendly);

            result.Add(new MonitorInfo(
                FriendlyName: string.IsNullOrWhiteSpace(friendly) ? screen.DeviceName : friendly!,
                GdiDeviceName: screen.DeviceName,
                X: bounds.X, Y: bounds.Y, Width: bounds.Width, Height: bounds.Height));
        }

        _logger.LogInformation("Detected {Count} display(s): {Displays}",
            result.Count, string.Join(" | ", result));

        return result;
    }

    public MonitorInfo? FindByName(string? friendlyName)
    {
        var monitors = GetMonitors();

        if (string.IsNullOrWhiteSpace(friendlyName))
        {
            var primaryScreen = WinFormsScreen.PrimaryScreen;
            var primary = monitors.FirstOrDefault(m =>
                primaryScreen is not null && m.GdiDeviceName == primaryScreen.DeviceName)
                ?? monitors.FirstOrDefault();

            _logger.LogInformation("No monitor name configured; using primary: {Monitor}", primary);
            return primary;
        }

        var wanted = friendlyName.Trim();
        var match = monitors.FirstOrDefault(m =>
            string.Equals(m.FriendlyName.Trim(), wanted, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            _logger.LogWarning("Monitor '{Wanted}' not found among detected displays.", wanted);

        return match;
    }

    /// <summary>Maps GDI device name (\\.\DISPLAYn) -> EDID friendly name via the CCD API.</summary>
    private Dictionary<string, string> QueryFriendlyNames()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            int rc = GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS,
                out uint pathCount, out uint modeCount);
            if (rc != ERROR_SUCCESS)
            {
                _logger.LogWarning("GetDisplayConfigBufferSizes failed ({Code}).", rc);
                return map;
            }

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

            rc = QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS,
                ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
            if (rc != ERROR_SUCCESS)
            {
                _logger.LogWarning("QueryDisplayConfig failed ({Code}).", rc);
                return map;
            }

            for (int i = 0; i < pathCount; i++)
            {
                var path = paths[i];

                var targetName = new DISPLAYCONFIG_TARGET_DEVICE_NAME
                {
                    header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                    {
                        type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
                        size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
                        adapterId = path.targetInfo.adapterId,
                        id = path.targetInfo.id
                    }
                };
                if (DisplayConfigGetDeviceInfo(ref targetName) != ERROR_SUCCESS)
                    continue;

                var sourceName = new DISPLAYCONFIG_SOURCE_DEVICE_NAME
                {
                    header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                    {
                        type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
                        size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>(),
                        adapterId = path.sourceInfo.adapterId,
                        id = path.sourceInfo.id
                    }
                };
                if (DisplayConfigGetDeviceInfo(ref sourceName) != ERROR_SUCCESS)
                    continue;

                var gdi = sourceName.viewGdiDeviceName;
                var friendly = targetName.monitorFriendlyDeviceName;
                if (!string.IsNullOrWhiteSpace(gdi) && !string.IsNullOrWhiteSpace(friendly))
                    map[gdi] = friendly;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CCD friendly-name query threw; friendly names unavailable this run.");
        }

        return map;
    }
}
