using System.Runtime.CompilerServices;
using Transport.WebApi.Options;
using Transport.WebApi.Services.Caching;

namespace Transport.WebApi.Services;

public class CachedGtfsDataService : IGtfsDataService
{
  private readonly GtfsDataService _baseService;
  private readonly ICacheService _cacheService;
  private readonly CacheOptions _cacheOptions;
  private readonly ILogger<CachedGtfsDataService> _logger;

  public CachedGtfsDataService(
    GtfsDataService baseService,
    ICacheService cacheService,
    CacheOptions cacheOptions,
    ILogger<CachedGtfsDataService> logger)
  {
    _baseService = baseService;
    _cacheService = cacheService;
    _cacheOptions = cacheOptions;
    _logger = logger;
  }

  #region Realtime Data Retrieval
  public async Task<byte[]> GetRealtimeDataAsync()
  {
    var cacheKey = CacheKeyGenerator.GetRealtimeDataKey();

    return await _cacheService.GetOrSetAsync(
      cacheKey,
      async () =>
      {
        _logger.LogDebug("Fetching realtime data from source");
        return await _baseService.GetRealtimeDataAsync();
      },
      _cacheOptions.RealtimeCacheDuration
    );
  }
  #endregion

  #region Static Data Retrieval
  public async Task<List<string>> GetStaticFileDataAsync(GtfsStaticDataFile fileName)
  {
    var cacheKey = CacheKeyGenerator.GetStaticDataKey(fileName);

    return await _cacheService.GetOrSetAsync(
      cacheKey,
      async () =>
      {
        _logger.LogInformation("Fetching static data for file: {FileName} from source", fileName);
        return await _baseService.GetStaticFileDataAsync(fileName);
      },
      _cacheOptions.StaticCacheDuration
    );
  }
  #endregion
}
