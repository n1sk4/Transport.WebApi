using Transport.WebApi.Models;
using Transport.WebApi.Options;

namespace Transport.WebApi.Services;

public interface IGtfsService
{
  #region Realtime Data Retrieval
  Task<VehicleCurrentPosition> GetAllVechiclesCurrentPositions();
  Task<VehicleCurrentPosition> GetCurrentVehiclesPositionsByRoute(string routeId);
  #endregion
  #region Static Data Retrieval
  Task<List<string>> GetAllStaticFileData(GtfsStaticDataFile fileName);
  Task<List<string>> GetRouteShape(string routeId);
  #endregion
}
