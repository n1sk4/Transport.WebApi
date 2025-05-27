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
  private readonly CacheOptions _cacheOptions;

  public GtfsDataController(ILogger<GtfsDataController> logger, IGtfsService gtfsService, IOptions<CacheOptions> cacheOptions)
  {
    _logger = logger;
    _gtfsService = gtfsService;
    _cacheOptions = cacheOptions.Value;
  }

  #region Realtime Data Retrieval

  /// <summary>
  /// Gets all vehicles (cached for 30 seconds)
  /// </summary>
  [HttpGet("GetAllVehicles")]
  [ProducesResponseType(200)]
  [ProducesResponseType(204)]
  [ProducesResponseType(500)]
  public async Task<IActionResult> GetAllVehicles()
  {
    try
    {
      _logger.LogDebug("GetAllVehicles called");
      var feedEntities = await _gtfsService.GetAllVehicles();

      if (feedEntities?.Length > 0)
      {
        _logger.LogInformation("Retrieved {VehicleCount} vehicles", feedEntities.Length);
        return Ok(feedEntities);
      }

      _logger.LogInformation("No vehicles found");
      return NoContent();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving all vehicles");
      return StatusCode(500, "Internal server error while retrieving vehicles");
    }
  }

  /// <summary>
  /// Gets a specific vehicle by ID (cached for 30 seconds)
  /// </summary>
  [HttpGet("GetAVehicleById")]
  [ProducesResponseType(200)]
  [ProducesResponseType(404)]
  [ProducesResponseType(400)]
  [ProducesResponseType(500)]
  public async Task<IActionResult> GetAVehicleById([Required] string vehicleId)
  {
    if (string.IsNullOrWhiteSpace(vehicleId))
    {
      return BadRequest("Vehicle ID cannot be empty");
    }

    try
    {
      _logger.LogDebug("GetAVehicleById called for vehicleId: {VehicleId}", vehicleId);
      var feedEntity = await _gtfsService.GetAVehicleById(vehicleId);

      if (feedEntity != null)
      {
        _logger.LogInformation("Found vehicle: {VehicleId}", vehicleId);
        return Ok(feedEntity);
      }

      _logger.LogInformation("Vehicle not found: {VehicleId}", vehicleId);
      return NotFound($"Vehicle with ID {vehicleId} not found");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving vehicle {VehicleId}", vehicleId);
      return StatusCode(500, "Internal server error while retrieving vehicle");
    }
  }

  /// <summary>
  /// Gets all vehicles for a specific route (cached for 30 seconds)
  /// </summary>
  [HttpGet("GetAllVehiclesByRoute")]
  [ProducesResponseType(200)]
  [ProducesResponseType(204)]
  [ProducesResponseType(400)]
  [ProducesResponseType(500)]
  public async Task<IActionResult> GetCurrentVehiclePositionsByRoute([Required] string routeId)
  {
    if (string.IsNullOrWhiteSpace(routeId))
    {
      return BadRequest("Route ID cannot be empty");
    }

    try
    {
      _logger.LogDebug("GetAllVehiclesByRoute called for routeId: {RouteId}", routeId);
      var feedEntities = await _gtfsService.GetAllVehiclesByRoute(routeId);

      if (feedEntities?.Length > 0)
      {
        _logger.LogInformation("Retrieved {VehicleCount} vehicles for route {RouteId}",
          feedEntities.Length, routeId);
        return Ok(feedEntities);
      }

      _logger.LogInformation("No vehicles found for route {RouteId}", routeId);
      return NoContent();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving vehicles for route {RouteId}", routeId);
      return StatusCode(500, "Internal server error while retrieving vehicles for route");
    }
  }

  /// <summary>
  /// Gets all vehicle positions by route Id (cached for 30 seconds)
  /// </summary>
  [HttpGet("GetAllVehiclePositionsByRouteId")]
  [ProducesResponseType(200)]
  [ProducesResponseType(204)]
  [ProducesResponseType(400)]
  [ProducesResponseType(500)]
  public async Task<IActionResult> GetAllVehiclePositionsByRouteId([Required] string routeId)
  {
    try
    {
      _logger.LogDebug("GetAllVehiclePositionsByRouteId called");
      var feedEntity = await _gtfsService.GetAllVehiclePositionsByRouteId(routeId);
      if (feedEntity != null)
      {
        _logger.LogInformation("Retrieved vehicle positions");
        return Ok(feedEntity);
      }
      _logger.LogInformation("No vehicle positions found");
      return NoContent();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving all vehicle positions");
      return StatusCode(500, "Internal server error while retrieving vehicle positions");
    }
  }

  /// <summary>
  /// Gets all vehicle positions (cached for 30 seconds)
  /// </summary>
  [HttpGet("GetAllVehiclePositions")]
  [ProducesResponseType(200)]
  [ProducesResponseType(204)]
  [ProducesResponseType(400)]
  [ProducesResponseType(500)]
  public async Task<IActionResult> GetAllVehiclePositions()
  {
    try
    {
      _logger.LogDebug("GetAllVehiclePositions called");
      var feedEntity = await _gtfsService.GetAllVehiclePositions();
      if (feedEntity != null)
      {
        _logger.LogInformation("Retrieved vehicle positions");
        return Ok(feedEntity);
      }
      _logger.LogInformation("No vehicle positions found");
      return NoContent();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving all vehicle positions");
      return StatusCode(500, "Internal server error while retrieving vehicle positions");
    }
  }

  #endregion

  #region Static Data Retrieval

  /// <summary>
  /// Gets all data from a GTFS static file (cached for 24 hours)
  /// </summary>
  [HttpGet("GetAllStaticFileData")]
  [ProducesResponseType(200)]
  [ProducesResponseType(404)]
  [ProducesResponseType(500)]
  public async Task<IActionResult> GetAllStaticFileData([Required] GtfsStaticDataFile fileName)
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
  [HttpGet("GetRouteShapeData")]
  [ProducesResponseType(200)]
  [ProducesResponseType(404)]
  [ProducesResponseType(400)]
  [ProducesResponseType(500)]
  public async Task<IActionResult> GetRouteShapeData([Required] string routeId)
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

  #region Cache Management (monitoring)

  /// <summary>
  /// Gets cache statistics (development only)
  /// </summary>
  [HttpGet("cache/stats")]
  [ProducesResponseType(200)]
  public IActionResult GetCacheStats()
  {
    if (!HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() ?? false)
    {
      return NotFound();
    }

    var stats = new
    {
      CacheConfiguration = new
      {
        RealtimeCacheSeconds = _cacheOptions.RealtimeCacheSeconds,
        StaticCacheHours = _cacheOptions.StaticCacheHours,
        CacheSizeLimit = _cacheOptions.CacheSizeLimit,
        CompactionPercentage = _cacheOptions.CompactionPercentage,
        EnableCacheHealthCheck = _cacheOptions.EnableCacheHealthCheck,
        LogCacheOperations = _cacheOptions.LogCacheOperations
      },
      Timestamp = DateTime.UtcNow
    };

    return Ok(stats);
  }

  #endregion
}
