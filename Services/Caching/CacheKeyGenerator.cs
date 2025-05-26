using Transport.WebApi.Options;

namespace Transport.WebApi.Services.Caching;

public static class CacheKeyGenerator
{
  // Static data cache keys (daily cache)
  public static string GetStaticDataKey(GtfsStaticDataFile fileName)
    => $"gtfs:static:{fileName}:{DateTime.UtcNow:yyyy-MM-dd}";

  // Realtime data cache keys (short-lived cache)
  public static string GetRealtimeDataKey()
    => "gtfs:realtime:feed";

  public static string GetVehicleByIdKey(string vehicleId)
    => $"gtfs:realtime:vehicle:{vehicleId}";

  public static string GetVehiclesByRouteKey(string routeId)
    => $"gtfs:realtime:route:{routeId}";

  public static string GetAllVehiclePositionsByRouteIdKey()
    => "gtfs:realtime:vehicles:positions";

  public static string GetAllVehiclesKey()
    => "gtfs:realtime:vehicles:all";

  // Route shape cache key (daily cache)
  public static string GetRouteShapeKey(string routeId)
    => $"gtfs:shape:route:{routeId}:{DateTime.UtcNow:yyyy-MM-dd}";
}
