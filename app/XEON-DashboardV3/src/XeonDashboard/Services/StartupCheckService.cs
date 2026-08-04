using System.Net.Http;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;

namespace XeonDashboard.Services;

public sealed class StartupCheckService : IStartupCheckService
{
    private readonly ILogger<StartupCheckService> _logger;
    private readonly HttpClient _http;

    public StartupCheckService(ILogger<StartupCheckService> logger)
    {
        _logger = logger;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    public bool IsNetworkAvailable()
    {
        var available = NetworkInterface.GetIsNetworkAvailable();
        _logger.LogDebug("Network available: {Available}", available);
        return available;
    }

    public async Task<bool> IsHomeAssistantReachableAsync(string url, CancellationToken ct = default)
    {
        try
        {
            // HEAD would be ideal, but HA doesn't answer HEAD on all paths, so
            // use GET and treat any HTTP status as "the server is up".
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _http.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, ct);

            _logger.LogDebug("HA probe {Url} -> {Status}", url, (int)response.StatusCode);
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "HA probe {Url} failed.", url);
            return false;
        }
    }
}
