using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
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

  [HttpGet("GetAllRealtimeData")]
  public async Task<IActionResult> GetAllRealtimeData()
  {
    _logger.LogInformation("GetAllRealtimeData called");
    var feedMessage = await _gtfsService.GetAllRealtimeData();

    var formatter = new Google.Protobuf.JsonFormatter(new Google.Protobuf.JsonFormatter.Settings(true));
    string json = formatter.Format((Google.Protobuf.IMessage)feedMessage, 1);
    return Content(json, "application/json");
  }

  [HttpGet("GetAllCurrentVehiclePositions")]
  public async Task<IActionResult> GetAllCurrentVehiclePositions()
  {
    _logger.LogInformation("GetCurrentVehiclePositions called");
    TransitRealtime.FeedEntity[] feedEntities = await _gtfsService.GetCurrentVehiclePositions();
    return feedEntities.Length > 0 ? Ok(feedEntities) : NotFound("No current vehicle positions found.");
  }

  [HttpGet("GetCurrentVehiclePosition")]
  public async Task<IActionResult> GetCurrentVehiclePosition([Required]string vehicleId)
  {
    _logger.LogInformation("GetCurrentVehiclePosition called for vehicleId: {VehicleId}", vehicleId);
    TransitRealtime.FeedEntity? feedEntity = await _gtfsService.GetCurrentVehiclePosition(vehicleId);
    return feedEntity != null ? Ok(feedEntity) : NotFound($"Vehicle with ID {vehicleId} not found.");
  }
}
