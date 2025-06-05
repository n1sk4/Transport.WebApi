using TransitRealtime;
using Transport.WebApi.Models;
using Transport.WebApi.Options;

namespace Transport.WebApi.Services;

public class GtfsService
{
  private readonly ILogger<GtfsService> _logger;
  private GtfsDataService _gtfsDataService;

  public GtfsService(GtfsDataService gtfsDataService, ILogger<GtfsService> logger)
  {
    _gtfsDataService = gtfsDataService;
    _logger = logger;
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
        .SelectMany<KeyValuePair<string, object>, string>(kvp => (IEnumerable<string>)kvp.Value)
        .ToList();

    return new VehicleCurrentPosition { { routeId, routePositions } };
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

  public async Task<List<string>> GetRouteShape(string routeId)
  {
    var fileData = await _gtfsDataService.GetStaticFileDataAsync(GtfsStaticDataFile.ShapesFile);
    if (fileData.Count > 0)
    {
      var routeShape = fileData
          .Where(line => line.Contains(routeId))
          .Select(line =>
          {
            var parts = line.Split(',');
            return parts.Length > 1 ? string.Join(",", parts.Skip(1)) : string.Empty;
          })
          .ToList();
      return routeShape;
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
