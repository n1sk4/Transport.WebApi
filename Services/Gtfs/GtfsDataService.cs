using Microsoft.Extensions.Options;
using System.IO.Compression;
using Transport.WebApi.Options;

namespace Transport.WebApi.Services.Gtfs;

public class GtfsDataService
{
  private readonly HttpClient _httpClient;
  private readonly ILogger<GtfsDataService> _logger;
  private readonly GtfsOptions _gtfsOptions;
  private readonly Dictionary<GtfsStaticDataFile, List<string>> _staticDataCache = new();

  public GtfsDataService(HttpClient httpClient, ILogger<GtfsDataService> logger, IOptions<GtfsOptions> gtfsOptions)
  {
    _gtfsOptions = gtfsOptions.Value;
    _httpClient = httpClient;
    _httpClient.BaseAddress = new Uri(gtfsOptions.Value.BaseUrl);
    _logger = logger;
  }

  #region Realtime Data Retrieval
  public async Task<byte[]> GetRealtimeDataAsync()
  {
    try
    {
      _logger.LogDebug("Fetching realtime GTFS data.");
      var response = await _httpClient.GetAsync(_gtfsOptions.RealtimeDataEndpoint);
      response.EnsureSuccessStatusCode();
      var content = await response.Content.ReadAsByteArrayAsync();
      _logger.LogDebug("Successfully fetched realtime GTFS data.");
      return content;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error fetching realtime GTFS data.");
      throw;
    }
  }
  #endregion

  #region Static Data Retrieval

  public async Task<List<string>> GetStaticFileDataAsync(GtfsStaticDataFile fileName)
  {
    if (_staticDataCache.TryGetValue(fileName, out var cachedData))
    {
      return cachedData;
    }

    try
    {
      var zipData = await _httpClient.GetByteArrayAsync(_gtfsOptions.StaticDataEndpoint);

      using var zipStream = new MemoryStream(zipData);
      using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
      var entry = archive.GetEntry(fileName.GetFileName());
      if (entry == null)
      {
        _logger.LogError($"File {fileName} not found in GTFS static data.");
        return new List<string>();
      }

      using var stream = entry.Open();
      using var reader = new StreamReader(stream);

      var lines = new List<string>();
      string? line;
      bool isFirst = true;
      while ((line = await reader.ReadLineAsync()) != null)
      {
        if (isFirst)
        {
          isFirst = false; // skip header
          continue;
        }
        lines.Add(line);
      }

      _staticDataCache[fileName] = lines;
      return lines;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error fetching data from GTFS static data: {ex.Message}");
      return new List<string>();
    }
  }
  #endregion
}
