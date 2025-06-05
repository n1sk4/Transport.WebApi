using System.Reflection.Metadata;
using Transport.WebApi.Options;

namespace Transport.WebApi.Services.Caching;

public static class CacheKeyGenerator
{
  #region Realtime Data Cache Keys
  public static string GetRealtimeDataKey()
    => $"gtfs:realtime:{DateTime.UtcNow:yyyy-MM-dd}";
  public static string GetAllVehiclesCurrentPositionsKey()
    => $"gtfs:realtime:all-vehicles:{DateTime.UtcNow:yyyy-MM-dd}";
  public static string GetCurrentVehiclesPositionsByRouteKey(string routeId)
    => $"gtfs:realtime:current-vehicles:route:{routeId}:{DateTime.UtcNow:yyyy-MM-dd}";
  #endregion

  #region Static Data Cache Keys
  // Static data cache keys (daily cache)
  public static string GetStaticDataKey(GtfsStaticDataFile fileName)
    => $"gtfs:static:{fileName}:{DateTime.UtcNow:yyyy-MM-dd}";

  // Route shape cache key (daily cache)
  public static string GetRouteShapeKey(string routeId)
    => $"gtfs:shape:route:{routeId}:{DateTime.UtcNow:yyyy-MM-dd}";
  #endregion
}
