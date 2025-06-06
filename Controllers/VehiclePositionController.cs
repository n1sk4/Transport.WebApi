using Microsoft.AspNetCore.Mvc;
using Transport.WebApi.Models;
using Transport.WebApi.Services;

namespace Transport.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VehiclePositionController : ControllerBase
{
  private readonly ILogger<VehiclePositionController> _logger;
  private readonly IGtfsService _gtfsService;

  public VehiclePositionController(ILogger<VehiclePositionController> logger, IGtfsService gtfsService)
  {
    _logger = logger;
    _gtfsService = gtfsService;
  }

  /// <summary>
  /// Gets the current positions of all vehicles
  /// </summary>
  [HttpGet("CurrentPositions")]
  [ProducesResponseType(200, Type = typeof(List<VehicleCurrentPosition>))]
  [ProducesResponseType(404)]
  [ProducesResponseType(500)]
  public async Task<ActionResult<List<VehicleCurrentPosition>>> GetCurrentVehiclePositions()
  {
    try
    {
      _logger.LogDebug("GetCurrentVehiclePositions called");
      var vehiclePositions = await _gtfsService.GetAllVechiclesCurrentPositions();
      if (vehiclePositions != null && vehiclePositions.Count > 0)
      {
        _logger.LogInformation("Retrieved {Count} current vehicle positions", vehiclePositions.Count);
        return Ok(vehiclePositions);
      }
      _logger.LogWarning("No current vehicle positions found");
      return NotFound("No current vehicle positions found");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving current vehicle positions");
      return StatusCode(500, "Internal server error while retrieving vehicle positions");
    }
  }

  /// <summary>
  /// Gets the current position of a vehicle by its route
  /// </summary>
  [HttpGet("CurrentPositionByRoute")]
  [ProducesResponseType(200, Type = typeof(List<VehicleCurrentPosition>))]
  [ProducesResponseType(404)]
  [ProducesResponseType(500)]
  public async Task<ActionResult<List<VehicleCurrentPosition>>> GetCurrentVehiclePositionByRoute([FromQuery] string routeId)
  {
    try
    {
      _logger.LogDebug("GetCurrentVehiclePosition called for route: {RouteId}", routeId);
      var vehiclePosition = await _gtfsService.GetCurrentVehiclesPositionsByRoute(routeId);
      if (vehiclePosition != null)
      {
        _logger.LogInformation("Retrieved current position for route: {RouteId}", routeId);
        return Ok(vehiclePosition);
      }
      _logger.LogWarning("No current position found for route: {RouteId}", routeId);
      return NotFound($"No current position found for route {routeId}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving current vehicle position for route: {RouteId}", routeId);
      return StatusCode(500, "Internal server error while retrieving vehicle position");
    }
  }
}
