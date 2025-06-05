using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using Transport.WebApi.Options;
using Transport.WebApi.Services;
using Transport.WebApi.Services.Caching;

namespace Transport.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GtfsDataController : ControllerBase
{
  private readonly ILogger<GtfsDataController> _logger;
  private readonly IGtfsService _gtfsService;

  public GtfsDataController(ILogger<GtfsDataController> logger, IGtfsService gtfsService, IOptions<CacheOptions> cacheOptions)
  {
    _logger = logger;
    _gtfsService = gtfsService;
  }

  #region Realtime Data Retrieval

  #endregion

  #region Static Data Retrieval

  /// <summary>
  /// Gets all data from a GTFS static file (cached for 24 hours)
  /// </summary>
  [HttpGet("StaticData")]
  [ProducesResponseType(200)]
  [ProducesResponseType(404)]
  [ProducesResponseType(500)]
  public async Task<IActionResult> GettAllDataFromStaticFile([Required] GtfsStaticDataFile fileName)
  {
    try
    {
      _logger.LogDebug("GetAllStaticFileData called for file: {FileName}", fileName);
      var staticData = await _gtfsService.GetAllStaticFileData(fileName);

      if (staticData?.Count > 0)
      {
        _logger.LogInformation("Retrieved {RecordCount} records from {FileName}",
          staticData.Count, fileName);
        return Ok(staticData);
      }

      _logger.LogWarning("No data found in static file: {FileName}", fileName);
      return NotFound($"No data found in file {fileName}");
    }
    catch (InvalidDataException ex)
    {
      _logger.LogWarning(ex, "Invalid data in static file: {FileName}", fileName);
      return NotFound(ex.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving static file data: {FileName}", fileName);
      return StatusCode(500, "Internal server error while retrieving static data");
    }
  }

  /// <summary>
  /// Gets route shape data for a specific route (cached for 24 hours)
  /// </summary>
  [HttpGet("RouteShape")]
  [ProducesResponseType(200)]
  [ProducesResponseType(404)]
  [ProducesResponseType(400)]
  [ProducesResponseType(500)]
  public async Task<IActionResult> GetRouteShape([Required] string routeId)
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

  #endregion
}
