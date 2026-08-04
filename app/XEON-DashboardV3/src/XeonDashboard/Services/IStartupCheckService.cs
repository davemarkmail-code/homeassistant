namespace XeonDashboard.Services;

public interface IStartupCheckService
{
    /// <summary>
    /// Performs a single reachability probe against the given dashboard URL.
    /// Any HTTP response (including 401/403) counts as reachable — we only care
    /// that Home Assistant answered.
    /// </summary>
    Task<bool> IsHomeAssistantReachableAsync(string url, CancellationToken ct = default);

    /// <summary>True when the machine reports an active network connection.</summary>
    bool IsNetworkAvailable();
}
