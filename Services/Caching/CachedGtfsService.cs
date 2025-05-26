using TransitRealtime;
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

  public async Task<FeedEntity[]> GetAllVehicles()
  {
    var cacheKey = CacheKeyGenerator.GetAllVehiclesKey();

    return await _cacheService.GetOrSetAsync(
      cacheKey,
      async () =>
      {
        _logger.LogDebug("Fetching all vehicles from source");
        return await _baseService.GetAllVehicles();
      },
      _cacheOptions.RealtimeCacheSeconds
    );
  }

  public async Task<FeedEntity?> GetAVehicleById(string vehicleId)
  {
    var cacheKey = CacheKeyGenerator.GetVehicleByIdKey(vehicleId);

    return await _cacheService.GetOrSetAsync<FeedEntity>(
        cacheKey,
        async () =>
        {
          _logger.LogDebug("Fetching vehicle {VehicleId} from source", vehicleId);
#pragma warning disable CS8603 // Possible null reference return.
          return await _baseService.GetAVehicleById(vehicleId);
#pragma warning restore CS8603 // Possible null reference return.
        },
        _cacheOptions.RealtimeCacheSeconds
    );
  }

  public async Task<FeedEntity[]> GetAllVehiclesByRoute(string routeId)
  {
    var cacheKey = CacheKeyGenerator.GetVehiclesByRouteKey(routeId);

    return await _cacheService.GetOrSetAsync(
      cacheKey,
      async () =>
      {
        _logger.LogDebug("Fetching vehicles for route {RouteId} from source", routeId);
        return await _baseService.GetAllVehiclesByRoute(routeId);
      },
      _cacheOptions.RealtimeCacheSeconds
    );
  }

  public async Task<List<Position>> GetAllVehiclePositionsByRouteId(string routeId)
  {
    var cacheKey = CacheKeyGenerator.GetAllVehiclePositionsByRouteIdKey();
    return await _cacheService.GetOrSetAsync(
      cacheKey,
      async () =>
      {
        _logger.LogDebug("Fetching all vehicle positions from source");
        return await _baseService.GetAllVehiclePositionsByRouteId(routeId);
      },
      _cacheOptions.RealtimeCacheSeconds
    );
  }

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

  // Delegate other methods to base service
  public async Task<FeedMessage> GetAllRealtimeData()
  {
    return await _baseService.GetAllRealtimeData();
  }

  public async Task<FeedEntity> GetAllDataRealtime()
  {
    return await _baseService.GetAllDataRealtime();
  }
}
