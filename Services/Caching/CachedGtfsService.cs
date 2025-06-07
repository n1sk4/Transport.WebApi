using Transport.WebApi.Models;
using Transport.WebApi.Options;
using Transport.WebApi.Services.Caching;
using Transport.WebApi.Services.Gtfs;

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
      _cacheOptions.RealtimeCacheDuration
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
      _cacheOptions.RealtimeCacheDuration
    );
  }
  public async Task<List<EnhancedVehiclePosition>> GetAllVehiclesCurrentPositionsEnhanced()
  {
    return await _cacheService.GetOrSetAsync(
      $"{CacheKeyGenerator.GetAllVehiclesCurrentPositionsKey()}-enhanced",
      async () =>
      {
        _logger.LogDebug("Fetching all vehicles' enhanced current positions from source");
        return await _baseService.GetAllVehiclesCurrentPositionsEnhanced();
      },
      _cacheOptions.RealtimeCacheDuration
    );
  }

  public async Task<EnhancedVehiclePosition?> GetCurrentVehiclesPositionsByRouteEnhanced(string routeId)
  {
    var cacheKey = $"{CacheKeyGenerator.GetCurrentVehiclesPositionsByRouteKey(routeId)}-enhanced";
    var wrapper = await _cacheService.GetOrSetAsync(
      cacheKey,
      async () =>
      {
        _logger.LogDebug("Fetching enhanced current vehicle positions for route {RouteId} from source", routeId);
        var result = await _baseService.GetCurrentVehiclesPositionsByRouteEnhanced(routeId);
        return new EnhancedVehiclePositionWrapper { Value = result };
      },
      _cacheOptions.RealtimeCacheDuration
    );

    return wrapper?.Value;
  }
  #endregion

  #region Static Data Retrieval
  public async Task<List<string>> GetAllStaticFileData(GtfsStaticDataFile fileName)
  {
    return await _baseService.GetAllStaticFileData(fileName);
  }

  public async Task<List<JsonSerializedRoutes>> GetAllRoutes()
  {
    var cacheKey = CacheKeyGenerator.GetAllRoutesKey();
    return await _cacheService.GetOrSetAsync(
      cacheKey,
      async () =>
      {
        _logger.LogDebug("Fetching all routes from source");
        return await _baseService.GetAllRoutes();
      },
      _cacheOptions.StaticCacheDuration
    );
  }

  public async Task<List<JsonSerializedRouteShapes>> GetRouteShape(string routeId)
  {
    var cacheKey = CacheKeyGenerator.GetRouteShapeKey(routeId);

    return await _cacheService.GetOrSetAsync(
      cacheKey,
      async () =>
      {
        _logger.LogDebug("Fetching route shape for {RouteId} from source", routeId);
        return await _baseService.GetRouteShape(routeId);
      },
      _cacheOptions.StaticCacheDuration
    );
  }
  #endregion
}

internal class EnhancedVehiclePositionWrapper
{
  public EnhancedVehiclePosition? Value { get; set; }
}
