using Microsoft.AspNetCore.Mvc;
using Transport.WebApi.Models;
using Transport.WebApi.Services.Gtfs;

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
  /// Gets the current positions of all vehicles with route type information
  /// </summary>
  [HttpGet("CurrentPositionsEnhanced")]
  [ProducesResponseType(200, Type = typeof(List<EnhancedVehiclePosition>))]
  [ProducesResponseType(404)]
  [ProducesResponseType(500)]
  public async Task<ActionResult<List<EnhancedVehiclePosition>>> GetCurrentVehiclePositionsEnhanced()
  {
    try
    {
      var clientETag = Request.Headers.IfNoneMatch.FirstOrDefault()?.Trim('"');
      var vehiclePositions = await _gtfsService.GetAllVehiclesCurrentPositionsEnhanced();

      if (vehiclePositions != null && vehiclePositions.Count > 0)
      {
        var hash = ComputeSimpleHash(vehiclePositions);

        // Return 304 if client has current version
        if (clientETag == hash)
        {
          return StatusCode(304);
        }

        SetCacheHeaders(vehiclePositions);
        return Ok(vehiclePositions);
      }

      return NotFound("No current vehicle positions found");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving enhanced vehicle positions");
      return StatusCode(500, "Internal server error");
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

  /// <summary>
  /// Gets the current position of vehicles by route with route type information
  /// </summary>
  [HttpGet("CurrentPositionByRouteEnhanced")]
  [ProducesResponseType(200, Type = typeof(EnhancedVehiclePosition))]
  [ProducesResponseType(404)]
  [ProducesResponseType(500)]
  public async Task<ActionResult<EnhancedVehiclePosition>> GetCurrentVehiclePositionByRouteEnhanced([FromQuery] string routeId)
  {
    try
    {
      _logger.LogDebug("GetCurrentVehiclePositionEnhanced called for route: {RouteId}", routeId);
      var vehiclePosition = await _gtfsService.GetCurrentVehiclesPositionsByRouteEnhanced(routeId);
      if (vehiclePosition != null)
      {
        _logger.LogInformation("Retrieved enhanced position data for route: {RouteId}", routeId);
        return Ok(vehiclePosition);
      }
      _logger.LogWarning("No current position found for route: {RouteId}", routeId);
      return NotFound($"No current position found for route {routeId}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving enhanced vehicle position for route: {RouteId}", routeId);
      return StatusCode(500, "Internal server error while retrieving vehicle position");
    }
  }

  #region Helper Methods
  private void SetCacheHeaders<T>(T data)
  {
    var hash = ComputeSimpleHash(data);
    Response.Headers.ETag = $"\"{hash}\"";
    Response.Headers.CacheControl = "public, max-age=5, must-revalidate";
  }

  private string ComputeSimpleHash<T>(T data)
  {
    var json = System.Text.Json.JsonSerializer.Serialize(data);
    return json.GetHashCode().ToString("x");
  }
  #endregion
}
