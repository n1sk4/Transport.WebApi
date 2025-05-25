namespace Transport.WebApi.Services;

public class GtfsDataService
{
  private readonly HttpClient _httpClient;
  private readonly ILogger<GtfsDataService> _logger;

  public GtfsDataService(HttpClient httpClient, ILogger<GtfsDataService> logger)
  {
    _httpClient = httpClient;
    _logger = logger;
  }

  public async Task<byte[]> GetStaticDataAsync()
  {
    try
    {
      _logger.LogInformation("Fetching static GTFS data.");
      var response = await _httpClient.GetAsync("gtfs-scheduled/latest");
      response.EnsureSuccessStatusCode();
      var content = await response.Content.ReadAsByteArrayAsync();
      _logger.LogInformation("Successfully fetched static GTFS data.");
      return content;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error fetching static GTFS data.");
      throw;
    }
  }

  public async Task<byte[]> GetRealtimeDataAsync()
  {
    try
    {
      _logger.LogInformation("Fetching realtime GTFS data.");
      var response = await _httpClient.GetAsync("gtfs-rt-protobuf");
      response.EnsureSuccessStatusCode();
      var content = await response.Content.ReadAsByteArrayAsync();
      _logger.LogInformation("Successfully fetched realtime GTFS data.");
      return content;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error fetching realtime GTFS data.");
      throw;
    }
  }
}
