using Transport.WebApi.Models;
using Transport.WebApi.Options;

namespace Transport.WebApi.Services.Gtfs;

public interface IGtfsService
{
  #region Realtime Data Retrieval
  Task<VehicleCurrentPosition> GetAllVechiclesCurrentPositions();
  Task<VehicleCurrentPosition> GetCurrentVehiclesPositionsByRoute(string routeId);
  Task<List<EnhancedVehiclePosition>> GetAllVehiclesCurrentPositionsEnhanced();
  Task<EnhancedVehiclePosition?> GetCurrentVehiclesPositionsByRouteEnhanced(string routeId);
  #endregion

  #region Static Data Retrieval
  Task<List<string>> GetAllStaticFileData(GtfsStaticDataFile fileName);
  Task<List<JsonSerializedRoutes>> GetAllRoutes();
  Task<List<JsonSerializedRouteShapes>> GetRouteShape(string routeId);
  #endregion
}
