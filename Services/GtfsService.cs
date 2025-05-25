using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Google.Protobuf;
using TransitRealtime;

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
  public async Task<FeedMessage> GetAllRealtimeData()
  {
    byte[] data = await _gtfsDataService.GetRealtimeDataAsync();
    FeedMessage feedMessage = FeedMessage.Parser.ParseFrom(data);
    var formatter = new Google.Protobuf.JsonFormatter(new Google.Protobuf.JsonFormatter.Settings(true));
    var json = formatter.Format(feedMessage);

    return feedMessage;
  }

  public async Task<FeedEntity> GetAllDataRealtime()
  {
    FeedMessage feedMessage = await GetAllRealtimeData();
    if (feedMessage.Entity.Count > 0)
    {
      return feedMessage.Entity[0];
    }
    else
    {
      throw new InvalidDataException("No data available in the feed.");
    }
  }

  public async Task<FeedEntity[]> GetCurrentVehiclePositions()
  {
    FeedMessage feedMessage = await GetAllRealtimeData();
    return feedMessage.Entity.Where(entity => entity.Vehicle != null).ToArray();
  }

  public async Task<FeedEntity?> GetCurrentVehiclePosition(string vehicleId)
  {
    FeedMessage feedMessage = await GetAllRealtimeData();
    foreach (var entity in feedMessage.Entity)
    {
      if (entity.Vehicle != null && entity.Vehicle.Vehicle.Id == vehicleId)
      {
        return entity;
      }
    }
    return null;
  }

  public async Task<FeedEntity[]> GetCurrentVehiclePositionsByRoute(string routeId)
  {
    FeedMessage feedMessage = await GetAllRealtimeData();
    return feedMessage.Entity
      .Where(entity => entity.Vehicle != null && entity.Vehicle.Trip != null && entity.Vehicle.Trip.RouteId == routeId)
      .ToArray();
  }

  #endregion

  #region Static Data Retrieval

  public async Task<List<string>> GetAllFileDataStatic(string fileName)
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

  #endregion
}
