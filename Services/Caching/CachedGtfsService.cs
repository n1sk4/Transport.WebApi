using TransitRealtime;
using Transport.WebApi.Models;
using Transport.WebApi.Options;
using Transport.WebApi.Services.Caching;

namespace Transport.WebApi.Services;

public class CachedGtfsService : IGtfsService
{
  private readonly GtfsService _baseService;
  private readonly ICacheService _cacheService;
  private readonly CacheOptions _cacheOptions;
  private readonly ILogger<CachedGtfsService> _logger;

  public CachedGtfsService(
    GtfsService baseService,
    ICacheService cacheService,
    CacheOptions cacheOptions,
    ILogger<CachedGtfsService> logger)
  {
    _baseService = baseService;
    _cacheService = cacheService;
    _cacheOptions = cacheOptions;
    _logger = logger;
  }

  #region Realtime Data Retrieval
  public async Task<VehicleCurrentPosition> GetAllVechiclesCurrentPositions()
  {
    return await _cacheService.GetOrSetAsync(
      CacheKeyGenerator.GetAllVehiclesCurrentPositionsKey(),
      async () =>
      {
        _logger.LogDebug("Fetching all vehicles' current positions from source");
        return await _baseService.GetAllVechiclesCurrentPositions();
      },
      _cacheOptions.RealtimeCacheSeconds
    );
  }
  public async Task<VehicleCurrentPosition> GetCurrentVehiclesPositionsByRoute(string routeId)
  {
    var cacheKey = CacheKeyGenerator.GetCurrentVehiclesPositionsByRouteKey(routeId);
    return await _cacheService.GetOrSetAsync(
      cacheKey,
      async () =>
      {
        _logger.LogDebug("Fetching current vehicle positions for route {RouteId} from source", routeId);
        return await _baseService.GetCurrentVehiclesPositionsByRoute(routeId);
      },
      _cacheOptions.RealtimeCacheSeconds
    );
  }
  #endregion

  #region Static Data Retrieval
  public async Task<List<string>> GetAllStaticFileData(GtfsStaticDataFile fileName)
  {
    // Static data caching is handled at the data service level
    return await _baseService.GetAllStaticFileData(fileName);
  }

  public async Task<List<string>> GetRouteShape(string routeId)
  {
    var cacheKey = CacheKeyGenerator.GetRouteShapeKey(routeId);

    return await _cacheService.GetOrSetAsync(
      cacheKey,
      async () =>
      {
        _logger.LogDebug("Fetching route shape for {RouteId} from source", routeId);
        return await _baseService.GetRouteShape(routeId);
      },
      _cacheOptions.RealtimeCacheSeconds
    );
  }
  #endregion
}
