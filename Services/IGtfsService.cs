using TransitRealtime;
using Transport.WebApi.Options;

namespace Transport.WebApi.Services;

public interface IGtfsService
{
  Task<FeedMessage> GetAllRealtimeData();
  Task<FeedEntity> GetAllDataRealtime();
  Task<FeedEntity[]> GetAllVehicles();
  Task<FeedEntity?> GetAVehicleById(string vehicleId);
  Task<FeedEntity[]> GetAllVehiclesByRoute(string routeId);
  Task<List<string>> GetAllStaticFileData(GtfsStaticDataFile fileName);
  Task<List<string>> GetRouteShape(string routeId);
}
