using Microsoft.Extensions.Options;
using TransitRealtime;
using Transport.WebApi.Models;
using Transport.WebApi.Options;
using Transport.WebApi.Services.Caching;

namespace Transport.WebApi.Services.Gtfs;

public class GtfsService
{
  private readonly ILogger<GtfsService> _logger;
  private GtfsDataService _gtfsDataService;
  private readonly ICacheService _cacheService;
  private readonly CacheOptions _cacheOptions;

  public GtfsService(
    GtfsDataService gtfsDataService,
    ILogger<GtfsService> logger,
    ICacheService cacheService,
    IOptions<CacheOptions> cacheOptions)
  {
    _gtfsDataService = gtfsDataService;
    _logger = logger;
    _cacheService = cacheService;
    _cacheOptions = cacheOptions.Value;
  }

  #region Realtime Data Retrieval
  public async Task<VehicleCurrentPosition> GetAllVechiclesCurrentPositions()
  {
    return await GetPositions();
  }

  public async Task<VehicleCurrentPosition> GetCurrentVehiclesPositionsByRoute(string routeId)
  {
    var positions = await GetPositions();

    var routePositions = positions
        .Where(kvp => kvp.Key == routeId)
        .SelectMany(kvp => (IEnumerable<string>)kvp.Value)
        .ToList();

    return new VehicleCurrentPosition { { routeId, routePositions } };
  }

  private async Task<JsonSerializedRoutes?> GetRouteInfo(string routeId)
  {
    var cacheKey = $"route-info:{routeId}";

    var cachedRoute = await _cacheService.GetAsync<JsonSerializedRoutes>(cacheKey);
    if (cachedRoute != null)
    {
      return cachedRoute;
    }

    var allRoutes = await GetAllRoutes();
    var route = allRoutes.FirstOrDefault(r => r.RouteId == routeId);

    if (route != null)
    {
      await _cacheService.SetAsync(cacheKey, route, _cacheOptions.StaticCacheDuration);
    }

    return route;
  }

  public async Task<List<EnhancedVehiclePosition>> GetAllVehiclesCurrentPositionsEnhanced()
  {
    var positions = await GetPositions();
    var enhancedPositions = new List<EnhancedVehiclePosition>();

    foreach (var routePositions in positions)
    {
      var routeId = routePositions.Key;
      var vehiclePositions = (List<string>)routePositions.Value;

      var routeInfo = await GetRouteInfo(routeId);

      var vehicles = new List<VehiclePositionData>();
      foreach (var positionStr in vehiclePositions)
      {
        var coords = positionStr.Split(',');
        if (coords.Length >= 2 &&
            double.TryParse(coords[0], out var lat) &&
            double.TryParse(coords[1], out var lng))
        {
          vehicles.Add(new VehiclePositionData
          {
            Latitude = lat,
            Longitude = lng,
            LastUpdate = DateTime.UtcNow
          });
        }
      }

      enhancedPositions.Add(new EnhancedVehiclePosition
      {
        RouteId = routeId,
        RouteShortName = routeInfo?.RouteShortName?.Replace("\"", "") ?? routeId,
        RouteLongName = routeInfo?.RouteLongName?.Replace("\"", "") ?? "",
        RouteType = routeInfo != null && int.TryParse(routeInfo.RouteType, out var type) ? type : 3,
        Vehicles = vehicles
      });
    }

    return enhancedPositions;
  }

  public async Task<EnhancedVehiclePosition?> GetCurrentVehiclesPositionsByRouteEnhanced(string routeId)
  {
    var positions = await GetPositions();

    if (!positions.ContainsKey(routeId))
    {
      return null;
    }

    var vehiclePositions = (List<string>)positions[routeId];
    var routeInfo = await GetRouteInfo(routeId);

    var vehicles = new List<VehiclePositionData>();
    foreach (var positionStr in vehiclePositions)
    {
      var coords = positionStr.Split(',');
      if (coords.Length >= 2 &&
          double.TryParse(coords[0], out var lat) &&
          double.TryParse(coords[1], out var lng))
      {
        vehicles.Add(new VehiclePositionData
        {
          Latitude = lat,
          Longitude = lng,
          LastUpdate = DateTime.UtcNow
        });
      }
    }

    return new EnhancedVehiclePosition
    {
      RouteId = routeId,
      RouteShortName = routeInfo?.RouteShortName?.Replace("\"", "") ?? routeId,
      RouteLongName = routeInfo?.RouteLongName?.Replace("\"", "") ?? "",
      RouteType = routeInfo != null && int.TryParse(routeInfo.RouteType, out var type) ? type : 3,
      Vehicles = vehicles
    };
  }
  #endregion

  #region Static Data Retrieval
  public async Task<List<string>> GetAllStaticFileData(GtfsStaticDataFile fileName)
  {
    var fileData = await _gtfsDataService.GetStaticFileDataAsync(fileName);
    if(fileData.Count > 0)
    {
      return fileData;
    }
    else
    {
      throw new InvalidDataException($"No data available in the file {fileName}.");
    }
  }

  public async Task<List<JsonSerializedRoutes>> GetAllRoutes()
  {
    var fileData = await _gtfsDataService.GetStaticFileDataAsync(GtfsStaticDataFile.RoutesFile);
    if (fileData.Count > 0)
    {
      return fileData
        .Select(line =>
        {
          var parts = line.Split(',');
          return new JsonSerializedRoutes()
          {
            RouteId = parts.Length > 0 ? parts[0] : string.Empty,
            RouteShortName = parts.Length > 2 ? parts[2].Trim('\"') : string.Empty,
            RouteLongName = parts.Length > 3 ? parts[3].Trim('\"') : string.Empty,
            RouteType = parts.Length > 5 ? parts[5] : string.Empty
          };
        })
        .ToList();
    }
    else
    {
      throw new InvalidDataException("No route data available.");
    }
  }

  public async Task<List<JsonSerializedRouteShapes>> GetRouteShape(string routeId)
  {
    var fileData = await _gtfsDataService.GetStaticFileDataAsync(GtfsStaticDataFile.ShapesFile);
    if (fileData.Count > 0)
    {
      return fileData
        .Where(line =>
        {
          var parts = line.Split(',');
          if (parts.Length == 0) return false;
          var shapeId = parts[0].Replace("\"", string.Empty);
          return shapeId.StartsWith($"{routeId}_");
        })
        .Select(line =>
        {
          var parts = line.Split(",");
          string directionValue = string.Empty;
          if (parts.Length > 0)
          {
            var shapeId = parts[0].Replace("\"", string.Empty);
            var underscoreIndex = shapeId.IndexOf('_');
            if (underscoreIndex >= 0 && shapeId.Length > underscoreIndex + 1)
            {
              var dirChar = shapeId[underscoreIndex + 1];
              directionValue = dirChar == '1' ? "outbound" :
                                    dirChar == '2' ? "inbound" : string.Empty;
            }
          }
          return new JsonSerializedRouteShapes()
          {
            Direction = directionValue,
            Latitude = parts.Length > 1 ? parts[1] : "0.0",
            Longitude = parts.Length > 2 ? parts[2] : "0.0",
          };
        })
        .ToList();
    }
    else
    {
      throw new InvalidDataException($"No shape data available for route {routeId}.");
    }
  }
  #endregion

  #region Helper Methods
  private async Task<FeedMessage> GetAllRealtimeData()
  {
    byte[] data = await _gtfsDataService.GetRealtimeDataAsync();
    FeedMessage feedMessage = FeedMessage.Parser.ParseFrom(data);
    var formatter = new Google.Protobuf.JsonFormatter(new Google.Protobuf.JsonFormatter.Settings(true));
    var json = formatter.Format(feedMessage);

    return feedMessage;
  }

  private async Task<VehicleCurrentPosition> GetPositions()
  {
    var feedMessage = await GetAllRealtimeData();
    var vehiclePositions = feedMessage.Entity
        .Where(e => e.Vehicle != null)
        .Select(e => e.Vehicle)
        .ToList();
    if (vehiclePositions.Count == 0)
    {
      throw new InvalidDataException("No vehicle positions available in the realtime data.");
    }

    var vehiclePositionDict = new VehicleCurrentPosition();
    foreach (var vehicle in vehiclePositions)
    {
      if (!vehiclePositionDict.ContainsKey(vehicle.Trip.RouteId))
      {
        vehiclePositionDict[vehicle.Trip.RouteId] = new List<string>();
      }
      var position = $"{vehicle.Position.Latitude},{vehicle.Position.Longitude}";
      ((List<string>)vehiclePositionDict[vehicle.Trip.RouteId]).Add(position);
    }

    return vehiclePositionDict;
  }
  #endregion
}
