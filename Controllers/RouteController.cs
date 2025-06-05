using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Transport.WebApi.Models;
using Transport.WebApi.Services;

namespace Transport.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RouteController : ControllerBase
{
  private readonly ILogger<RouteController> _logger;
  private readonly IGtfsService _gtfsService;

  public RouteController(ILogger<RouteController> logger, IGtfsService gtfsService)
  {
    _logger = logger;
    _gtfsService = gtfsService;
  }

  /// <summary>
  /// Gets all available routes (cached for 24 hours)
  /// </summary>
  [HttpGet("AllRoutes")]
  [ProducesResponseType(200, Type = typeof(JsonSerializedRoutes))]
  [ProducesResponseType(404)]
  [ProducesResponseType(500)]
  public async Task<ActionResult<JsonSerializedRoutes>> GetAllRoutes()
  {
    try
    {
      _logger.LogDebug("GetAllRoutes called");
      var routes = await _gtfsService.GetAllRoutes();
      if (routes?.Count > 0)
      {
        _logger.LogInformation("Retrieved {RouteCount} routes", routes.Count);
        return Ok(routes);
      }
      _logger.LogWarning("No routes found");
      return NotFound("No routes found");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving all routes");
      return StatusCode(500, "Internal server error while retrieving routes");
    }
  }

  /// <summary>
  /// Gets route shape data for a specific route (cached for 24 hours)
  /// </summary>
  [HttpGet("RouteShape")]
  [ProducesResponseType(200, Type = typeof(JsonSerializedRouteShapes))]
  [ProducesResponseType(404)]
  [ProducesResponseType(400)]
  [ProducesResponseType(500)]
  public async Task<ActionResult<JsonSerializedRouteShapes>> GetRouteShape([Required] string routeId)
  {
    if (string.IsNullOrWhiteSpace(routeId))
    {
      return BadRequest("Route ID cannot be empty");
    }

    try
    {
      _logger.LogDebug("GetRouteShapeData called for routeId: {RouteId}", routeId);
      var shapeData = await _gtfsService.GetRouteShape(routeId);

      if (shapeData?.Count > 0)
      {
        _logger.LogInformation("Retrieved {ShapePointCount} shape points for route {RouteId}",
          shapeData.Count, routeId);
        return Ok(shapeData);
      }

      _logger.LogInformation("No shape data found for route {RouteId}", routeId);
      return NotFound($"No shape data found for route ID {routeId}");
    }
    catch (InvalidDataException ex)
    {
      _logger.LogWarning(ex, "Invalid shape data for route: {RouteId}", routeId);
      return NotFound(ex.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving route shape data for {RouteId}", routeId);
      return StatusCode(500, "Internal server error while retrieving route shape data");
    }
  }
}
