using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Transport.WebApi.Options;
using Transport.WebApi.Services;

namespace Transport.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GtfsDataController : ControllerBase
{
  private readonly ILogger<GtfsDataController> _logger;
  private readonly GtfsService _gtfsService;

  public GtfsDataController(ILogger<GtfsDataController> logger, GtfsService gtfsService)
  {
    _logger = logger;
    _gtfsService = gtfsService;
  }

  #region Realtime Data Retrieval
  [HttpGet("GetAllRealtimeData")]
  public async Task<IActionResult> GetAllRealtimeData()
  {
    _logger.LogDebug("GetAllRealtimeData called");
    var realtimeData = await _gtfsService.GetAllRealtimeData();
    return realtimeData != null ? Ok(realtimeData) : NotFound("No realtime data.");
  }

  [HttpGet("GetAllCurrentVehiclePositions")]
  public async Task<IActionResult> GetAllCurrentVehiclePositions()
  {
    _logger.LogDebug("GetCurrentVehiclePositions called");
    var feedEntities = await _gtfsService.GetCurrentVehiclePositions();
    return feedEntities.Length > 0 ? Ok(feedEntities) : NotFound("No current vehicle positions found.");
  }

  [HttpGet("GetCurrentVehiclePosition")]
  public async Task<IActionResult> GetCurrentVehiclePosition([Required]string vehicleId)
  {
    _logger.LogDebug("GetCurrentVehiclePosition called for vehicleId: {VehicleId}", vehicleId);
    var feedEntity = await _gtfsService.GetCurrentVehiclePosition(vehicleId);
    return feedEntity != null ? Ok(feedEntity) : NotFound($"Vehicle with ID {vehicleId} not found.");
  }

  [HttpGet("GetCurrentVehiclePositionsByRoute")]
  public async Task<IActionResult> GetCurrentVehiclePositionsByRoute([Required] string routeId)
  {
    _logger.LogDebug("GetCurrentVehiclePositionsByRoute called for routeId: {RouteId}", routeId);
    var feedEntities = await _gtfsService.GetCurrentVehiclePositionsByRoute(routeId);
    return feedEntities.Length > 0 ? Ok(feedEntities) : NotFound($"No current vehicle positions found for route ID {routeId}.");
  }
  #endregion

  #region Static Data Retrieval
  
  [HttpGet("GetAllStaticFileData")]
  public async Task<IActionResult> GetAllStaticFileData([Required] GtfsStaticDataFile fileName)
  {
    _logger.LogDebug("GetAllStaticData called");
    var staticData = await _gtfsService.GetAllStaticFileData(fileName);
    return staticData != null ? Ok(staticData) : NotFound("No static data");
  }

  #endregion
}
