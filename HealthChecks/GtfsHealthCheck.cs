using Microsoft.Extensions.Diagnostics.HealthChecks;
using Transport.WebApi.Services.Gtfs;

namespace Transport.WebApi.HealthChecks;

public class GtfsHealthCheck : IHealthCheck
{
  private readonly IGtfsDataService _gtfsDataService;
  private readonly ILogger<GtfsHealthCheck> _logger;

  public GtfsHealthCheck(IGtfsDataService gtfsDataService, ILogger<GtfsHealthCheck> logger)
  {
    _gtfsDataService = gtfsDataService;
    _logger = logger;
  }

  public async Task<HealthCheckResult> CheckHealthAsync(
    HealthCheckContext context,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var startTime = DateTime.UtcNow;

      // Try to fetch a small amount of data to verify the service is working
      var data = await _gtfsDataService.GetRealtimeDataAsync();

      var duration = DateTime.UtcNow - startTime;

      if (data.Length > 0)
      {
        return HealthCheckResult.Healthy($"GTFS service is responsive. Response time: {duration.TotalMilliseconds:F0}ms, Data size: {data.Length} bytes");
      }
      else
      {
        return HealthCheckResult.Degraded("GTFS service returned empty data");
      }
    }
    catch (HttpRequestException ex)
    {
      _logger.LogWarning(ex, "GTFS service HTTP request failed during health check");
      return HealthCheckResult.Unhealthy("GTFS service HTTP request failed", ex);
    }
    catch (TaskCanceledException ex)
    {
      _logger.LogWarning(ex, "GTFS service request timed out during health check");
      return HealthCheckResult.Unhealthy("GTFS service request timed out", ex);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "GTFS service health check failed");
      return HealthCheckResult.Unhealthy("GTFS service is not available", ex);
    }
  }
}
