using System.Reflection.Metadata;
using Transport.WebApi.Options;

namespace Transport.WebApi.Services.Caching;

public static class CacheKeyGenerator
{
  #region Realtime Data Cache Keys
  public static string GetRealtimeDataKey()
    => $"gtfs:realtime:feed";
  public static string GetAllVehiclesCurrentPositionsKey()
    => $"gtfs:realtime:all-vehicles";
  public static string GetCurrentVehiclesPositionsByRouteKey(string routeId)
    => $"gtfs:realtime:route{routeId}";
  #endregion

  #region Static Data Cache Keys
  public static string GetStaticDataKey(GtfsStaticDataFile fileName)
    => $"gtfs:static:{fileName}:{DateTime.UtcNow:yyyy-MM-dd}";
  public static string GetAllRoutesKey()
    => $"gtfs:static:routes:{DateTime.UtcNow:yyyy-MM-dd}";
  public static string GetRouteShapeKey(string routeId)
    => $"gtfs:shape:route:{routeId}:{DateTime.UtcNow:yyyy-MM-dd}";
  #endregion
}
