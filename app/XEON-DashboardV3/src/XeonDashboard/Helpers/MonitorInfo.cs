namespace XeonDashboard.Helpers;

/// <summary>
/// A physical display resolved by its EDID friendly name (e.g. "XENEON EDGE").
/// Bounds are in <b>physical pixels</b> in the virtual desktop coordinate space,
/// which is what the Win32 window-positioning calls expect under Per-Monitor V2
/// DPI awareness.
/// </summary>
/// <param name="FriendlyName">EDID friendly name reported by the monitor.</param>
/// <param name="GdiDeviceName">GDI device name, e.g. <c>\\.\DISPLAY1</c>.</param>
/// <param name="X">Left edge in physical pixels (virtual desktop space).</param>
/// <param name="Y">Top edge in physical pixels.</param>
/// <param name="Width">Width in physical pixels.</param>
/// <param name="Height">Height in physical pixels.</param>
public sealed record MonitorInfo(
    string FriendlyName,
    string GdiDeviceName,
    int X,
    int Y,
    int Width,
    int Height)
{
    public override string ToString() =>
        $"{FriendlyName} [{GdiDeviceName}] {Width}x{Height} @ ({X},{Y})";
}
