using Microsoft.AspNetCore.Mvc;

namespace Transport.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GtfsDataController : ControllerBase
{
  private readonly ILogger<GtfsDataController> _logger;

  public GtfsDataController(ILogger<GtfsDataController> logger)
  {
    _logger = logger;
  }

  [HttpGet("GetCurrentVehiclePositions")]
  public IActionResult GetCurrentVehiclePositions()
  {
    _logger.LogInformation("GetCurrentVehiclePositions called");
    return Ok(new { Message = "Current vehicle positions fetched successfully." });
  }

  [HttpGet("GetCurrentVehiclePosition")]
  public IActionResult GetCurrentVehiclePosition(string vehicleId)
  {
    _logger.LogInformation("GetCurrentVehiclePosition called for vehicleId: {VehicleId}", vehicleId);
    return Ok(new { VehicleId = vehicleId, Message = "Current position fetched successfully." });
  }
}
